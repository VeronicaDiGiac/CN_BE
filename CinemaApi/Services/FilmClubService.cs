using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    // "Film club della settimana": un titolo per settimana (da lunedì a domenica) con una discussione
    // dedicata. Lo sceglie l'admin; se non l'ha fatto, al primo accesso della settimana lo sceglie il BE:
    // il film più presente nelle liste "Da vedere" tra quelli non usati negli ultimi sei mesi.
    public class FilmClubService
    {
        private const int SettimaneSenzaRipetere = 26;

        private readonly CinemaContext _db;
        private readonly ILogger<FilmClubService> _log;

        public FilmClubService(CinemaContext db, ILogger<FilmClubService> log)
        {
            _db = db;
            _log = log;
        }

        public static DateOnly InizioSettimana(DateOnly giorno) =>
            giorno.AddDays(-(((int)giorno.DayOfWeek + 6) % 7));

        private static DateOnly Oggi() => DateOnly.FromDateTime(DateTime.Now);

        public async Task<FilmClubHomeDto> Home(int? idUtente)
        {
            var inizio = InizioSettimana(Oggi());
            var corrente = await Assicura(inizio);
            var precedente = await _db.FilmClub.FirstOrDefaultAsync(c => c.DataInizio == inizio.AddDays(-7));
            return new FilmClubHomeDto
            {
                Corrente = corrente == null ? null : await Dto(corrente, idUtente),
                Precedente = precedente == null ? null : await Dto(precedente, idUtente)
            };
        }

        // Il club della settimana, creato se manca; null se il catalogo non ha titoli adatti
        private async Task<FilmClub?> Assicura(DateOnly inizio)
        {
            var club = await _db.FilmClub.FirstOrDefaultAsync(c => c.DataInizio == inizio);
            if (club == null)
            {
                var idFilm = await Scegli(inizio);
                if (idFilm == null) return null;
                club = new FilmClub { IdFilm = idFilm.Value, DataInizio = inizio, DataCreazione = DateTime.Now };
                _db.FilmClub.Add(club);
                try { await _db.SaveChangesAsync(); }
                catch (DbUpdateException)
                {
                    // Un'altra richiesta l'ha appena creato (DataInizio è UNIQUE): vale quello
                    _db.ChangeTracker.Clear();
                    club = await _db.FilmClub.FirstAsync(c => c.DataInizio == inizio);
                }
            }
            await AssicuraDiscussione(club);
            return club;
        }

        private async Task<int?> Scegli(DateOnly inizio)
        {
            var dal = inizio.AddDays(-7 * SettimaneSenzaRipetere);
            var usati = _db.FilmClub.Where(c => c.DataInizio >= dal).Select(c => c.IdFilm);
            var candidati = _db.Film.Where(f => f.Stato == StatiFilm.Approvato && f.Tipo == TipiTitolo.Film
                                                && f.TmdbId != null && f.ImmagineUrl != null && !usati.Contains(f.IdFilm));

            var piuAttesi = await candidati
                .Select(f => new { f.IdFilm, DaVedere = _db.DaVedere.Count(d => d.IdFilm == f.IdFilm), Media = f.Voti!.Average(v => (double?)v.Voto) })
                .Where(x => x.DaVedere > 0)
                .OrderByDescending(x => x.DaVedere).ThenByDescending(x => x.Media).ThenBy(x => x.IdFilm)
                .Select(x => (int?)x.IdFilm)
                .FirstOrDefaultAsync();
            if (piuAttesi != null) return piuAttesi;

            // Nessuno l'ha in lista: uno qualsiasi, diverso ogni settimana
            var ids = await candidati.OrderBy(f => f.IdFilm).Select(f => f.IdFilm).ToListAsync();
            return ids.Count == 0 ? null : ids[inizio.DayNumber / 7 % ids.Count];
        }

        // La discussione del club, a nome del primo admin; se è stata eliminata se ne crea un'altra
        private async Task AssicuraDiscussione(FilmClub club)
        {
            if (club.IdDiscussione != null && await _db.Discussioni.AnyAsync(d => d.IdDiscussione == club.IdDiscussione)) return;

            var idAdmin = await _db.Utenti.Where(u => u.Ruolo == Ruoli.Admin).OrderBy(u => u.IdUtente)
                .Select(u => (int?)u.IdUtente).FirstOrDefaultAsync();
            if (idAdmin == null)
            {
                _log.LogWarning("Film club: nessun admin a cui intestare la discussione");
                return;
            }
            var titoloFilm = await _db.Film.Where(f => f.IdFilm == club.IdFilm).Select(f => f.Titolo).FirstOrDefaultAsync();
            var adesso = DateTime.Now;
            var discussione = new Discussione
            {
                IdFilm = club.IdFilm,
                IdUtente = idAdmin.Value,
                Titolo = $"Film club: settimana dal {club.DataInizio.ToString("d MMMM", new System.Globalization.CultureInfo("it-IT"))}",
                Contenuto = $"{titoloFilm} è il film club di questa settimana. L'avete visto? Raccontate qui cosa ne pensate (segnate gli spoiler!).",
                DataCreazione = adesso,
                DataUltimaAttivita = adesso
            };
            _db.Discussioni.Add(discussione);
            await _db.SaveChangesAsync();
            await _db.FilmClub.Where(c => c.IdFilmClub == club.IdFilmClub)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.IdDiscussione, discussione.IdDiscussione));
            club.IdDiscussione = discussione.IdDiscussione;
        }

        private async Task<FilmClubDto> Dto(FilmClub club, int? idUtente)
        {
            var fine = club.DataInizio.AddDays(6);
            var dalle = club.DataInizio.ToDateTime(TimeOnly.MinValue);
            var alle = fine.AddDays(1).ToDateTime(TimeOnly.MinValue);

            var dto = await _db.Film.Where(f => f.IdFilm == club.IdFilm).Select(f => new FilmClubDto
            {
                IdFilm = f.IdFilm,
                Titolo = f.Titolo,
                ImmagineUrl = f.ImmagineUrl,
                AnnoUscita = f.AnnoUscita,
                Regista = f.Regista,
                Descrizione = f.Descrizione,
                Tipo = f.Tipo,
                VotoMedio = f.Voti!.Average(v => (double?)v.Voto),
                NumeroVoti = f.Voti!.Count(),
                VistoDa = _db.Visioni.Where(v => v.IdFilm == f.IdFilm &&
                        ((v.DataVisione >= club.DataInizio && v.DataVisione <= fine) || (v.DataCreazione >= dalle && v.DataCreazione < alle)))
                    .Select(v => v.IdUtente).Distinct().Count(),
                Visto = idUtente != null && _db.Visioni.Any(v => v.IdFilm == f.IdFilm && v.IdUtente == idUtente)
            }).FirstAsync();

            dto.DataInizio = club.DataInizio;
            dto.DataFine = fine;
            dto.SceltoDaAdmin = club.SceltoDaAdmin;
            if (club.IdDiscussione != null && await _db.Discussioni.AnyAsync(d => d.IdDiscussione == club.IdDiscussione))
            {
                dto.IdDiscussione = club.IdDiscussione;
                dto.Messaggi = await _db.Commenti.CountAsync(c => c.IdDiscussione == club.IdDiscussione);
            }
            return dto;
        }

        // ---- Pannello admin ----

        // Settimana in corso e prossima (la prossima può non essere ancora decisa)
        public async Task<List<FilmClubDto>> Programma()
        {
            var inizio = InizioSettimana(Oggi());
            var elenco = new List<FilmClubDto>();
            var corrente = await Assicura(inizio);
            if (corrente != null) elenco.Add(await Dto(corrente, null));
            var prossimo = await _db.FilmClub.FirstOrDefaultAsync(c => c.DataInizio == inizio.AddDays(7));
            if (prossimo != null) elenco.Add(await Dto(prossimo, null));
            return elenco;
        }

        // false se il titolo non è nel catalogo. Se cambia il film, la discussione vecchia
        // si elimina solo se nessuno ci ha ancora scritto
        public async Task<bool> Imposta(int idFilm, bool prossimaSettimana)
        {
            if (!await _db.Film.AnyAsync(f => f.IdFilm == idFilm && f.Stato == StatiFilm.Approvato)) return false;
            var inizio = InizioSettimana(Oggi()).AddDays(prossimaSettimana ? 7 : 0);

            var club = await _db.FilmClub.FirstOrDefaultAsync(c => c.DataInizio == inizio);
            if (club == null)
            {
                club = new FilmClub { IdFilm = idFilm, DataInizio = inizio, SceltoDaAdmin = true, DataCreazione = DateTime.Now };
                _db.FilmClub.Add(club);
                await _db.SaveChangesAsync();
            }
            else if (club.IdFilm != idFilm)
            {
                if (club.IdDiscussione != null && !await _db.Commenti.AnyAsync(c => c.IdDiscussione == club.IdDiscussione))
                    await EliminazioneHelper.EliminaDiscussioni(_db, new List<int> { club.IdDiscussione.Value });
                club.IdFilm = idFilm;
                club.IdDiscussione = null;
                club.SceltoDaAdmin = true;
                await _db.SaveChangesAsync();
            }
            else if (!club.SceltoDaAdmin)
            {
                club.SceltoDaAdmin = true;
                await _db.SaveChangesAsync();
            }
            // La discussione della prossima settimana nasce quando la settimana comincia
            if (!prossimaSettimana) await AssicuraDiscussione(club);
            return true;
        }
    }
}
