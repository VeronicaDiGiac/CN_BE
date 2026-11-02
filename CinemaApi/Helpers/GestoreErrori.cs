using CinemaApi.DTOs.ResponseDto;
using Microsoft.AspNetCore.Diagnostics;

namespace CinemaApi.Helpers
{
    // Unico punto in cui si gestiscono gli errori imprevisti di tutti i controller
    // (al posto di un try/catch in ogni azione). Registra l'errore nel log e risponde 500
    // con un ErrorResponseDto: il messaggio tecnico va in Dettaglio solo in sviluppo,
    // così in produzione non si mostrano all'esterno dettagli interni (SQL, percorsi…).
    public class GestoreErrori : IExceptionHandler
    {
        private readonly ILogger<GestoreErrori> _logger;
        private readonly IHostEnvironment _ambiente;

        public GestoreErrori(ILogger<GestoreErrori> logger, IHostEnvironment ambiente)
        {
            _logger = logger;
            _ambiente = ambiente;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception eccezione, CancellationToken cancellationToken)
        {
            _logger.LogError(eccezione, "Errore non gestito su {Metodo} {Percorso}", context.Request.Method, context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new ErrorResponseDto
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Messaggio = MessaggiErrore.ErroreGenerico,
                Dettaglio = _ambiente.IsDevelopment() ? eccezione.Message : null
            }, cancellationToken);
            return true;
        }
    }
}
