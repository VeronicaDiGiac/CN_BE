namespace CinemaApi.Middleware
{
    // Header di sicurezza su tutte le risposte: niente "indovinare" il tipo di file,
    // niente inserimento dell'API in un iframe di un altro sito.
    // Middleware "a convenzione": il costruttore riceve il prossimo anello della pipeline
    // (RequestDelegate) e InvokeAsync viene chiamato a ogni richiesta.
    // Si registra in Program.cs con app.UseSecurityHeaders() (SecurityHeadersMiddlewareExtensions.cs).
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly bool _sviluppo;

        // Il middleware viene creato una volta sola all'avvio: qui si possono ricevere solo
        // servizi Singleton (come IHostEnvironment). Uno Scoped andrebbe chiesto in InvokeAsync.
        public SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment ambiente)
        {
            _next = next;
            _sviluppo = ambiente.IsDevelopment();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var h = context.Response.Headers;
            h["X-Content-Type-Options"] = "nosniff";
            h["X-Frame-Options"] = "DENY";
            h["Referrer-Policy"] = "strict-origin-when-cross-origin";
            // In sviluppo niente CSP: bloccherebbe la pagina di Swagger
            if (!_sviluppo)
                h["Content-Security-Policy"] = "default-src 'none'; img-src 'self'; frame-ancestors 'none'";

            await _next(context);
        }
    }
}
