using CinemaApi.Extensions;
using CinemaApi.Middleware;

// Program.cs è solo l'elenco di cosa c'è e in che ordine:
// i dettagli di ogni voce stanno in Extensions/ e Middleware/.

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ---- Servizi (cosa esiste nell'applicazione) ----
builder.Services.AddControllers();
builder.Services.AddCinemaDatabase(config);       // Extensions/DatabaseExtensions.cs
builder.Services.AddCinemaServices();             // Extensions/ServicesExtensions.cs
builder.Services.AddTmdb();                       // Extensions/TmdbExtensions.cs
builder.Services.AddCinemaSwagger();              // Extensions/SwaggerExtensions.cs
builder.Services.AddCinemaCors();                 // Extensions/CorsExtensions.cs
builder.Services.AddLimitiUpload();               // Extensions/UploadExtensions.cs
builder.Services.AddCinemaCompression();          // Extensions/CompressionExtensions.cs
builder.Services.AddCinemaRateLimiting(config);   // Extensions/RateLimitingExtensions.cs
builder.Services.AddCinemaAuth(config);           // Extensions/AuthExtensions.cs
builder.Services.AddProxyHeaders();               // Extensions/ProxyExtensions.cs

var app = builder.Build();

// Sistemazione dei dati all'avvio (una query se è già tutto a posto)
await app.CompletaChiaviTitoloAsync();

// ---- Pipeline: ogni richiesta passa da qui, in quest'ordine (l'ordine conta) ----
app.UseProxyEHsts();                              // IP vero dal proxy + HSTS (non in sviluppo)
app.UseMiddleware<SecurityHeadersMiddleware>();   // header di sicurezza su ogni risposta
app.UseCinemaCors();                              // solo il FE può chiamare l'API dal browser
app.UseExceptionHandler();                        // dopo CORS: anche i 500 hanno gli header CORS
app.UseCinemaSwagger();                           // solo in sviluppo
app.UseCinemaCompression();                       // brotli/gzip, tranne /api/auth
app.UseHttpsRedirection();
app.UseAuthentication();                          // chi sei (JWT)
app.UseStaticFiles();
app.UseUploadEsterni();                           // /uploads fuori da wwwroot (produzione)
app.UseAuthorization();                           // cosa puoi fare ([Authorize])
app.UseRateLimiter();                             // limiti per IP
app.MapControllers();

app.Run();
