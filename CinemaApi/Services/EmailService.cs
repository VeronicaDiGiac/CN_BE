using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Net;

namespace CinemaApi.Services
{
    // Invio email via SMTP. Il server si sceglie solo da configurazione (sezione "Email"):
    // in sviluppo smtp4dev su localhost:2525 (le mail non escono dal PC),
    // in produzione un provider vero (es. Brevo) con utente e password negli User Secrets
    // o nelle impostazioni dell'App Service.
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config) { _config = config; }

        public Task InviaLinkReimpostaPassword(string destinatario, string userName, string link, int minutiValidita)
        {
            var corpo = $"""
                <p>Ciao {WebUtility.HtmlEncode(userName)},</p>
                <p>abbiamo ricevuto una richiesta per reimpostare la password del tuo account Cineverse.</p>
                <p><a href="{WebUtility.HtmlEncode(link)}">Reimposta la password</a></p>
                <p>Il link vale {minutiValidita} minuti e si può usare una sola volta.</p>
                <p>Se non hai chiesto tu il cambio password, ignora questa email: la tua password resta quella di prima.</p>
                """;
            return Invia(destinatario, "Reimposta la tua password Cineverse", corpo);
        }

        public Task InviaConfermaPasswordCambiata(string destinatario, string userName)
        {
            var corpo = $"""
                <p>Ciao {WebUtility.HtmlEncode(userName)},</p>
                <p>la password del tuo account Cineverse è stata appena modificata.</p>
                <p>Se non hai fatto tu questa modifica, reimposta subito la password con "Password dimenticata?" nella pagina di accesso.</p>
                """;
            return Invia(destinatario, "La tua password Cineverse è stata modificata", corpo);
        }

        private async Task Invia(string destinatario, string oggetto, string corpoHtml)
        {
            var sezione = _config.GetSection("Email");
            var host = sezione["Host"]
                ?? throw new InvalidOperationException("Email:Host mancante nella configurazione");
            var mittente = sezione["Mittente"]
                ?? throw new InvalidOperationException("Email:Mittente mancante nella configurazione");

            var messaggio = new MimeMessage();
            messaggio.From.Add(new MailboxAddress(sezione["MittenteNome"] ?? "Cineverse", mittente));
            messaggio.To.Add(MailboxAddress.Parse(destinatario));
            messaggio.Subject = oggetto;
            messaggio.Body = new BodyBuilder { HtmlBody = corpoHtml }.ToMessageBody();

            using var client = new SmtpClient();
            var porta = sezione.GetValue("Porta", 587);
            // smtp4dev non usa TLS; i provider veri sì (StartTls sulla 587)
            var sicurezza = sezione.GetValue("UsaTls", true) ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(host, porta, sicurezza);

            var utente = sezione["Utente"];
            if (!string.IsNullOrEmpty(utente))
                await client.AuthenticateAsync(utente, sezione["Password"] ?? "");

            await client.SendAsync(messaggio);
            await client.DisconnectAsync(true);
        }
    }
}
