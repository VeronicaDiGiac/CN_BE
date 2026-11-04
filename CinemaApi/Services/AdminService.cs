using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    // Pannello di controllo dell'admin: numeri del sito, attività recente ed elenchi da moderare.
    // Le eliminazioni di recensioni, discussioni e commenti usano gli endpoint già esistenti
    // (l'admin può eliminare qualsiasi contenuto); quella degli utenti è qui.
    public class AdminService
    {
        private const int LunghezzaEstratto = 200;
        private const string Aperta = "Aperta";

        private readonly CinemaContext _db;
        private readonly UtentiService _utenti;

        public AdminService(CinemaContext db, UtentiService utenti)
        {
            _db = db;
            _utenti = utenti;
        }

        public async Task<RiepilogoAdminDto> GetRiepilogo(DateTime dal) => new RiepilogoAdminDto
        {
            Dal = dal,
            Utenti = await _db.Utenti.CountAsync(),
            NuoviUtenti = await _db.Utenti.CountAsync(u => u.DataDiRegistrazione >= dal),
            Recensioni = await _db.Recensioni.CountAsync(),
            NuoveRecensioni = await _db.Recensioni.CountAsync(r => r.DataRecensione >= dal),
            Discussioni = await _db.Discussioni.CountAsync(),
            NuoveDiscussioni = await _db.Discussioni.CountAsync(d => d.DataCreazione >= dal),
            Commenti = await _db.Commenti.CountAsync(),
            NuoviCommenti = await _db.Commenti.CountAsync(c => c.DataCommento >= dal),
            FilmInAttesa = await _db.Film.CountAsync(f => f.Stato == StatiFilm.InAttesa),
            SegnalazioniAperte = await _db.Segnalazioni.CountAsync(s => s.Stato == Aperta)
        };

        // Le ultime voci di ogni tipo, unite e ordinate per data (la più recente prima)
        public async Task<List<AttivitaAdminDto>> GetAttivita(int limite, string? tipo)
        {
            var voci = new List<AttivitaAdminDto>();

            if (tipo == null || tipo == TipiAttivitaAdmin.Iscrizione)
                voci.AddRange(await _db.Utenti
                    .Where(u => u.DataDiRegistrazione != null)
                    .OrderByDescending(u => u.DataDiRegistrazione).Take(limite)
                    .Select(u => new AttivitaAdminDto
                    {
                        Tipo = TipiAttivitaAdmin.Iscrizione,
                        Id = u.IdUtente,
                        Data = u.DataDiRegistrazione!.Value,
                        IdUtente = u.IdUtente,
                        UserName = u.UserName
                    }).ToListAsync());

            if (tipo == null || tipo == TipiAttivitaAdmin.Recensione)
                voci.AddRange(await _db.Recensioni
                    .Where(r => r.DataRecensione != null)
                    .OrderByDescending(r => r.DataRecensione).Take(limite)
                    .Select(r => new AttivitaAdminDto
                    {
                        Tipo = TipiAttivitaAdmin.Recensione,
                        Id = r.IdRecensione,
                        Data = r.DataRecensione!.Value,
                        IdUtente = r.IdUtente,
                        UserName = r.Utente != null ? r.Utente.UserName : null,
                        IdFilm = r.IdFilm,
                        TitoloFilm = r.Film != null ? r.Film.Titolo : null,
                        IdRecensione = r.IdRecensione,
                        Testo = r.Contenuto
                    }).ToListAsync());

            if (tipo == null || tipo == TipiAttivitaAdmin.Discussione)
                voci.AddRange(await _db.Discussioni
                    .OrderByDescending(d => d.DataCreazione).Take(limite)
                    .Select(d => new AttivitaAdminDto
                    {
                        Tipo = TipiAttivitaAdmin.Discussione,
                        Id = d.IdDiscussione,
                        Data = d.DataCreazione,
                        IdUtente = d.IdUtente,
                        UserName = d.Utente != null ? d.Utente.UserName : null,
                        IdFilm = d.IdFilm,
                        TitoloFilm = d.Film != null ? d.Film.Titolo : null,
                        IdDiscussione = d.IdDiscussione,
                        Testo = d.Titolo
                    }).ToListAsync());

            if (tipo == null || tipo == TipiAttivitaAdmin.Commento)
                voci.AddRange(await _db.Commenti
                    .Where(c => c.DataCommento != null)
                    .OrderByDescending(c => c.DataCommento).Take(limite)
                    .Select(c => new AttivitaAdminDto
                    {
                        Tipo = TipiAttivitaAdmin.Commento,
                        Id = c.IdCommento,
                        Data = c.DataCommento!.Value,
                        IdUtente = c.IdUtente,
                        UserName = c.Utente != null ? c.Utente.UserName : null,
                        IdFilm = c.Recensione != null ? c.Recensione.IdFilm : c.Discussione != null ? c.Discussione.IdFilm : null,
                        TitoloFilm = c.Recensione != null ? c.Recensione.Film!.Titolo : c.Discussione != null ? c.Discussione.Film!.Titolo : null,
                        IdRecensione = c.IdRecensione,
                        IdDiscussione = c.IdDiscussione,
                        Testo = c.Contenuto
                    }).ToListAsync());

            var risultato = voci.OrderByDescending(v => v.Data).Take(limite).ToList();
            foreach (var v in risultato) v.Testo = Estratto(v.Testo);
            return risultato;
        }

        public async Task<PagedResultDto<UtenteAdminDto>> GetUtenti(string? q, int pageNumber, int pageSize)
        {
            var query = _db.Utenti.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var t = q.Trim();
                query = query.Where(u => u.UserName.Contains(t));
            }

            var totale = await query.CountAsync();
            var voci = await query
                .OrderByDescending(u => u.DataDiRegistrazione).ThenByDescending(u => u.IdUtente)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                .Select(u => new UtenteAdminDto
                {
                    IdUtente = u.IdUtente,
                    UserName = u.UserName,
                    AvatarUrl = u.AvatarUrl,
                    Ruolo = u.Ruolo,
                    DataDiRegistrazione = u.DataDiRegistrazione,
                    AccessoGoogle = u.PasswordHash == null,
                    NumRecensioni = _db.Recensioni.Count(r => r.IdUtente == u.IdUtente),
                    NumDiscussioni = _db.Discussioni.Count(d => d.IdUtente == u.IdUtente),
                    NumCommenti = _db.Commenti.Count(c => c.IdUtente == u.IdUtente),
                    SegnalazioniAperte = _db.Segnalazioni.Count(s => s.Stato == Aperta && (
                        (s.TipoContenuto == TipiContenuto.Recensione && _db.Recensioni.Any(r => r.IdRecensione == s.IdContenuto && r.IdUtente == u.IdUtente)) ||
                        (s.TipoContenuto == TipiContenuto.Discussione && _db.Discussioni.Any(d => d.IdDiscussione == s.IdContenuto && d.IdUtente == u.IdUtente)) ||
                        (s.TipoContenuto == TipiContenuto.Commento && _db.Commenti.Any(c => c.IdCommento == s.IdContenuto && c.IdUtente == u.IdUtente))))
                })
                .ToListAsync();
            return Pagina(voci, totale, pageNumber, pageSize);
        }

        public async Task<PagedResultDto<RecensioneAdminDto>> GetRecensioni(string? q, bool soloSegnalate, int pageNumber, int pageSize)
        {
            var query = _db.Recensioni.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var t = q.Trim();
                query = query.Where(r => (r.Contenuto != null && r.Contenuto.Contains(t)) || r.Utente!.UserName.Contains(t) || r.Film!.Titolo.Contains(t));
            }
            if (soloSegnalate)
                query = query.Where(r => _db.Segnalazioni.Any(s => s.Stato == Aperta && s.TipoContenuto == TipiContenuto.Recensione && s.IdContenuto == r.IdRecensione));

            var totale = await query.CountAsync();
            var voci = await query
                .OrderByDescending(r => r.DataRecensione).ThenByDescending(r => r.IdRecensione)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                .Select(r => new RecensioneAdminDto
                {
                    IdRecensione = r.IdRecensione,
                    IdFilm = r.IdFilm,
                    TitoloFilm = r.Film != null ? r.Film.Titolo : null,
                    IdUtente = r.IdUtente,
                    UserName = r.Utente != null ? r.Utente.UserName : null,
                    Voto = r.Voto,
                    Contenuto = r.Contenuto,
                    Spoiler = r.Spoiler,
                    Data = r.DataRecensione,
                    NumCommenti = r.Commenti!.Count(),
                    NumMiPiace = r.MiPiace!.Count(),
                    SegnalazioniAperte = _db.Segnalazioni.Count(s => s.Stato == Aperta && s.TipoContenuto == TipiContenuto.Recensione && s.IdContenuto == r.IdRecensione)
                })
                .ToListAsync();
            return Pagina(voci, totale, pageNumber, pageSize);
        }

        public async Task<PagedResultDto<DiscussioneAdminDto>> GetDiscussioni(string? q, bool soloSegnalate, int pageNumber, int pageSize)
        {
            var query = _db.Discussioni.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var t = q.Trim();
                query = query.Where(d => d.Titolo.Contains(t) || (d.Contenuto != null && d.Contenuto.Contains(t)) || d.Utente!.UserName.Contains(t) || d.Film!.Titolo.Contains(t));
            }
            if (soloSegnalate)
                query = query.Where(d => _db.Segnalazioni.Any(s => s.Stato == Aperta && s.TipoContenuto == TipiContenuto.Discussione && s.IdContenuto == d.IdDiscussione));

            var totale = await query.CountAsync();
            var voci = await query
                .OrderByDescending(d => d.DataCreazione).ThenByDescending(d => d.IdDiscussione)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                .Select(d => new DiscussioneAdminDto
                {
                    IdDiscussione = d.IdDiscussione,
                    IdFilm = d.IdFilm,
                    TitoloFilm = d.Film != null ? d.Film.Titolo : null,
                    IdUtente = d.IdUtente,
                    UserName = d.Utente != null ? d.Utente.UserName : null,
                    Titolo = d.Titolo,
                    Contenuto = d.Contenuto,
                    Stagione = d.Stagione,
                    Episodio = d.Episodio,
                    Data = d.DataCreazione,
                    NumMessaggi = d.Commenti!.Count(),
                    SegnalazioniAperte = _db.Segnalazioni.Count(s => s.Stato == Aperta && s.TipoContenuto == TipiContenuto.Discussione && s.IdContenuto == d.IdDiscussione)
                })
                .ToListAsync();
            return Pagina(voci, totale, pageNumber, pageSize);
        }

        public async Task<PagedResultDto<CommentoAdminDto>> GetCommenti(string? q, bool soloSegnalate, int pageNumber, int pageSize)
        {
            var query = _db.Commenti.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var t = q.Trim();
                query = query.Where(c => (c.Contenuto != null && c.Contenuto.Contains(t)) || c.Utente!.UserName.Contains(t));
            }
            if (soloSegnalate)
                query = query.Where(c => _db.Segnalazioni.Any(s => s.Stato == Aperta && s.TipoContenuto == TipiContenuto.Commento && s.IdContenuto == c.IdCommento));

            var totale = await query.CountAsync();
            var voci = await query
                .OrderByDescending(c => c.DataCommento).ThenByDescending(c => c.IdCommento)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                .Select(c => new CommentoAdminDto
                {
                    IdCommento = c.IdCommento,
                    IdUtente = c.IdUtente,
                    UserName = c.Utente != null ? c.Utente.UserName : null,
                    Contenuto = c.Contenuto,
                    Data = c.DataCommento,
                    IdFilm = c.Recensione != null ? c.Recensione.IdFilm : c.Discussione != null ? c.Discussione.IdFilm : null,
                    TitoloFilm = c.Recensione != null ? c.Recensione.Film!.Titolo : c.Discussione != null ? c.Discussione.Film!.Titolo : null,
                    IdRecensione = c.IdRecensione,
                    IdDiscussione = c.IdDiscussione,
                    TitoloDiscussione = c.Discussione != null ? c.Discussione.Titolo : null,
                    SegnalazioniAperte = _db.Segnalazioni.Count(s => s.Stato == Aperta && s.TipoContenuto == TipiContenuto.Commento && s.IdContenuto == c.IdCommento)
                })
                .ToListAsync();
            return Pagina(voci, totale, pageNumber, pageSize);
        }

        public Task<Utente?> GetUtente(int id) => _db.Utenti.FindAsync(id).AsTask();

        // Stessa eliminazione a cascata di quando l'utente cancella il proprio profilo
        public Task<bool> EliminaUtente(int id) => _utenti.DeleteProfilo(id);

        private static string? Estratto(string? testo) =>
            testo == null || testo.Length <= LunghezzaEstratto ? testo : testo[..LunghezzaEstratto] + "…";

        private static PagedResultDto<T> Pagina<T>(List<T> voci, int totale, int pageNumber, int pageSize) => new()
        {
            TotalCount = totale,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = voci
        };
    }
}
