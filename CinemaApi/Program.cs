using CinemaApi.Extensions;
using CinemaApi.Middleware;

// Program.cs è solo l'elenco di cosa c'è e in che ordine:
// i dettagli di ogni voce stanno in Extensions/ e Middleware/.

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ---- Servizi (cosa esiste nell'applicazione) ----
// Qui si REGISTRANO i servizi nel contenitore della Dependency Injection: nessuna richiesta passa da qui.
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

// Sistemazione dei dati all'avvio (una query se è già tutto a posto).
// Non è un middleware: gira una volta sola, prima che arrivino richieste.
await app.CompletaChiaviTitoloAsync();

// ---- Pipeline delle richieste ----
// Ogni richiesta HTTP attraversa questi middleware dall'alto in basso; la risposta li ripercorre
// dal basso in alto. Ognuno può modificare richiesta/risposta, passare al successivo o rispondere
// subito fermando la catena. L'ORDINE IN CUI SONO SCRITTI È L'ORDINE IN CUI GIRANO.

// 1. Proxy e HSTS (solo fuori da sviluppo; Extensions/ProxyExtensions.cs).
//    Online l'API sta dietro un proxy: legge l'IP vero del visitatore da X-Forwarded-For
//    (serve ai limiti per IP) e manda l'header HSTS, che obbliga il browser a usare sempre HTTPS.
//    È il primo perché tutti gli altri devono già vedere l'IP e lo schema giusti.
app.UseProxyEHsts();

// 2. Header di sicurezza (Middleware/SecurityHeadersMiddleware.cs, scritto da noi).
//    Aggiunge a ogni risposta: nosniff (il browser non "indovina" il tipo dei file),
//    X-Frame-Options DENY (niente API dentro iframe di altri siti), Referrer-Policy e,
//    fuori da sviluppo, una Content-Security-Policy. Stando in alto valgono anche per 401, 404 e 429.
//    Attenzione: non per i 500, perché UseExceptionHandler svuota la risposta (header compresi)
//    prima di scrivere l'errore.
app.UseSecurityHeaders();

// 3. CORS (Extensions/CorsExtensions.cs).
//    Dice al browser quali siti possono chiamare l'API (solo il nostro FE, da configurazione).
//    Risponde da solo alle richieste "preflight" OPTIONS. Va prima di tutto ciò che può
//    produrre una risposta d'errore, altrimenti il browser la nasconde al FE.
app.UseCinemaCors();

// 4. Gestione degli errori (Helpers/GestoreErrori.cs, registrato in AddCinemaServices).
//    Avvolge tutto quello che viene dopo: se un controller o un middleware successivo lancia
//    un'eccezione, la intercetta, la scrive nel log e risponde 500 con un messaggio generico.
//    Sta dopo CORS perché anche le risposte 500 devono avere gli header CORS.
app.UseExceptionHandler();

// 5. Swagger (solo in sviluppo; Extensions/SwaggerExtensions.cs).
//    Serve la pagina /swagger con la documentazione degli endpoint. Online risponde 404.
app.UseCinemaSwagger();

// 6. Compressione (Extensions/CompressionExtensions.cs).
//    Comprime le risposte con brotli o gzip se il browser li accetta: il JSON pesa 4-5 volte meno.
//    Esclude /api/auth (risposte con il token) per prudenza contro gli attacchi tipo BREACH.
app.UseCinemaCompression();

// 7. Redirect a HTTPS (middleware di ASP.NET Core).
//    Se la richiesta arriva in http://, risponde 307 con l'indirizzo https:// e la catena si ferma.
app.UseHttpsRedirection();

// 8. Autenticazione: CHI SEI (Extensions/AuthExtensions.cs).
//    Se c'è l'header "Authorization: Bearer …", verifica firma, scadenza, issuer e audience del JWT,
//    controlla nel database che la password non sia cambiata nel frattempo e riempie User
//    (id, ruolo). Non blocca nessuno: senza token User resta semplicemente anonimo.
app.UseAuthentication();

// 9. File statici (middleware di ASP.NET Core).
//    Se la richiesta è per un file in wwwroot (es. un'immagine caricata), lo restituisce
//    e la catena si ferma qui: i file non arrivano ai controller.
app.UseStaticFiles();

// 10. Upload esterni (solo se configurati; Extensions/UploadExtensions.cs).
//     Come il precedente, ma per la cartella degli upload fuori da wwwroot (online: dati/),
//     servita all'indirizzo /uploads.
app.UseUploadEsterni();

// 11. Autorizzazione: COSA PUOI FARE (middleware di ASP.NET Core).
//     Guarda gli attributi dell'endpoint: se ha [Authorize] e User è anonimo risponde 401,
//     se User non ha il ruolo richiesto risponde 403, e la catena si ferma.
//     Deve stare dopo UseAuthentication, altrimenti User sarebbe sempre anonimo.
app.UseAuthorization();

// 12. Limiti di richieste per IP (Extensions/RateLimitingExtensions.cs).
//     Conta le richieste degli endpoint con [EnableRateLimiting] (login, password dimenticata, TMDB)
//     e oltre il limite risponde 429 senza arrivare al controller.
//     - Richiede la registrazione AddRateLimiter, fatta in AddCinemaRateLimiting:
//       senza, l'applicazione va in errore.
//     - Essendo dopo UseAuthorization, le richieste già respinte con 401/403 non vengono contate.
//       Per i nostri limiti va bene: login, password dimenticata e TMDB sono endpoint anonimi
//       e UseAuthorization non li blocca. Per limitare TUTTE le richieste, anche quelle respinte,
//       andrebbe spostato prima di UseAuthentication (restando dopo UseCinemaCors).
app.UseRateLimiter();

// 13. Controller: fine della catena.
//     Trova l'azione del controller che corrisponde a metodo e URL, la esegue e
//     produce la risposta, che poi ripercorre i middleware all'indietro.
app.MapControllers();

// Avvia il server e resta in ascolto delle richieste (non è un middleware).
app.Run();
