using Microsoft.AspNetCore.HttpOverrides;
using CinemaApi.Controllers;
using CinemaApi.Helpers;
using CinemaApi.Models;
using CinemaApi.Services;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<CinemaContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("CinemaDB")));

builder.Services.AddCors();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "CinemaApi", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Inserisci il token JWT. Esempio: Bearer eyJhbGc..."
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 5 * 1024 * 1024; // 5 MB
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 5 * 1024 * 1024; // 5 MB
});
builder.Services.AddScoped<FilmService>();
builder.Services.AddScoped<RecensioniService>();
builder.Services.AddScoped<CommentiService>();
builder.Services.AddScoped<PreferitiService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UtentiService>();
builder.Services.AddScoped<FollowService>();
builder.Services.AddScoped<SegnalazioniService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<VisioniService>();
builder.Services.AddScoped<DaVedereService>();
builder.Services.AddScoped<FeedService>();
builder.Services.AddScoped<NotificheService>();
builder.Services.AddScoped<ListeService>();
builder.Services.AddScoped<StatisticheService>();
builder.Services.AddScoped<EpisodiService>();
builder.Services.AddScoped<CalendarioSerieService>();
builder.Services.AddScoped<DiscussioniService>();
// Accesso con Google: verifica dei token di Auth0 (singleton per tenere in cache le chiavi pubbliche)
builder.Services.AddSingleton<VerificaAuth0>();
// Controllo periodico dei nuovi episodi delle serie seguite (notifiche)
builder.Services.AddHostedService<NuoviEpisodiWorker>();

// Errori imprevisti: gestiti tutti in Helpers/GestoreErrori.cs (niente try/catch nei controller)
builder.Services.AddExceptionHandler<GestoreErrori>();
builder.Services.AddProblemDetails();

// TMDB: HttpClient dedicato + cache in memoria per non ripetere le stesse chiamate
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<TmdbService>(c =>
{
    c.BaseAddress = new Uri("https://api.themoviedb.org/3/");
    c.Timeout = TimeSpan.FromSeconds(10);
});

// "Password dimenticata?": massimo 10 richieste ogni 15 minuti per indirizzo IP,
// così nessuno può usare l'endpoint per riempire di mail la casella di qualcun altro
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(AuthController.LimitePasswordDimenticata, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "sconosciuto",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(15)
            }));

    // Login, registrazione e accesso con Google: 10 tentativi al minuto per IP rendono
    // inutile provare password a raffica, senza disturbare chi sbaglia una volta
    // (configurabile con "Limiti:AccessiAlMinuto": i test automatici in locale ne fanno molti di più)
    var accessiAlMinuto = builder.Configuration.GetValue("Limiti:AccessiAlMinuto", 10);
    options.AddPolicy(AuthController.LimiteAccesso, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "sconosciuto",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = accessiAlMinuto,
                Window = TimeSpan.FromMinutes(1)
            }));

    // TMDB: una visita al catalogo fa già circa 10 richieste (sezioni, ricerca, persone, filtri), quindi
    // 150 al minuto per IP lasciano navigare senza blocchi. Le risposte sono comunque in cache nel BE.
    // Configurabile con "Limiti:TmdbAlMinuto" (i test end-to-end in locale ne fanno di più)
    var tmdbAlMinuto = builder.Configuration.GetValue("Limiti:TmdbAlMinuto", 150);
    options.AddPolicy(TmdbController.LimiteTmdb, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "sconosciuto",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = tmdbAlMinuto,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// La chiave JWT non sta in appsettings.json: va negli User Secrets
// (dotnet user-secrets set "Jwt:Key" "<chiave di almeno 32 caratteri>").
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key mancante. Impostala con: dotnet user-secrets set \"Jwt:Key\" \"<chiave di almeno 32 caratteri>\"");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };

        // Dopo un cambio o un reset della password i token emessi prima non valgono più
        // (si scollegano tutti i dispositivi). Costa una query leggera per ogni richiesta autenticata.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var idUtente = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var versione = context.Principal?.FindFirst(AuthService.ClaimVersionePassword)?.Value;
                var db = context.HttpContext.RequestServices.GetRequiredService<CinemaContext>();

                if (!int.TryParse(idUtente, out var id) || !await AuthService.TokenAncoraValido(db, id, versione))
                    context.Fail("Sessione non più valida: la password è stata cambiata");
            }
        };
    });

// In produzione l'API sta dietro il proxy di Azure: l'IP del visitatore arriva in X-Forwarded-For.
// Senza questo i limiti per IP vedrebbero solo l'indirizzo del proxy. ForwardLimit = 1: si usa
// solo il valore aggiunto dal proxy, non quelli che un client potrebbe inventare.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.ForwardLimit = 1;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

// Origini del FE autorizzate a chiamare l'API (in produzione: l'indirizzo del sito)
var originiFe = builder.Configuration.GetSection("Cors:Origini").Get<string[]>() ?? Array.Empty<string>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders();
    // Il browser userà sempre HTTPS per questo sito
    app.UseHsts();
}

// Header di sicurezza su tutte le risposte: niente "indovinare" il tipo di file,
// niente inserimento dell'API in un iframe di un altro sito
app.Use(async (context, next) =>
{
    var h = context.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "strict-origin-when-cross-origin";
    if (!app.Environment.IsDevelopment())
        h["Content-Security-Policy"] = "default-src 'none'; img-src 'self'; frame-ancestors 'none'";
    await next();
});

// Film salvati prima della colonna TitoloChiave (Database/FilmTitoloChiave.sql): la si calcola
// una volta sola. Se è già tutto a posto è una sola query, quindi resta sicuro a ogni avvio.
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<FilmService>().CompletaChiaviTitolo();
}

// Solo il FE può chiamare l'API dal browser (gli altri siti vengono bloccati dal browser)
app.UseCors(p => p.WithOrigins(originiFe).AllowAnyHeader().AllowAnyMethod());

// Dopo UseCors: anche le risposte di errore devono avere gli header CORS,
// altrimenti il browser nasconde al FE lo status 500
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseStaticFiles();

// Immagini caricate dagli utenti in una cartella fuori da wwwroot (produzione)
if (PercorsiUpload.Esterna(app.Configuration))
{
    var radiceUpload = PercorsiUpload.Radice(app.Configuration, app.Environment);
    Directory.CreateDirectory(radiceUpload);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(radiceUpload),
        RequestPath = "/uploads"
    });
}

app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();
