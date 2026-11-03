namespace CinemaApi.Services
{
    // Ogni qualche ora controlla se sono usciti episodi nuovi delle serie che gli utenti
    // stanno guardando e crea le notifiche (CalendarioSerieService.NotificaNuoviEpisodi).
    // Un errore (TMDB non raggiungibile, DB occupato…) si registra nel log e si riprova al giro dopo:
    // non deve mai fermare l'API.
    public class NuoviEpisodiWorker : BackgroundService
    {
        private static readonly TimeSpan Intervallo = TimeSpan.FromHours(6);
        // All'avvio si aspetta un po': prima l'API deve essere pronta a rispondere
        private static readonly TimeSpan AttesaIniziale = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NuoviEpisodiWorker> _logger;

        public NuoviEpisodiWorker(IServiceScopeFactory scopeFactory, ILogger<NuoviEpisodiWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(AttesaIniziale, stoppingToken);
                using var timer = new PeriodicTimer(Intervallo);
                do
                {
                    await Controlla();
                } while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException)
            {
                // L'API si sta spegnendo
            }
        }

        private async Task Controlla()
        {
            try
            {
                // CinemaContext e i service sono "scoped": servono uno scope a ogni giro
                using var scope = _scopeFactory.CreateScope();
                var calendario = scope.ServiceProvider.GetRequiredService<CalendarioSerieService>();
                var create = await calendario.NotificaNuoviEpisodi();
                if (create > 0) _logger.LogInformation("Notifiche di nuovi episodi create: {Numero}", create);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Controllo dei nuovi episodi non riuscito: si riprova al prossimo giro");
            }
        }
    }
}
