namespace CinemaApi.Middleware
{
    public static class SecurityHeadersMiddlewareExtensions
    {
        // Header di sicurezza su ogni risposta (Middleware/SecurityHeadersMiddleware.cs).
        // Stesso stile delle altre voci della pipeline in Program.cs: una chiamata Use… con un nome parlante.
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
            app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
