using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;
using System.Text;

namespace CinemaApi.Services
{
    public class FilmService
    {
        private readonly CinemaContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public FilmService(CinemaContext db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }

        // idUtenteAutenticato è nullable: null se chi chiama non è loggato,
        // in quel caso GiaRecensito resta sempre false.
        public async Task<FilmDettaglioDto?> GetFilmDettaglio(int id, int? idUtenteAutenticato)
        {
            var film = await _db.Film
                .Where(f => f.IdFilm == id)
                .Where (f=>f.Stato == StatiFilm.Approvato)
                .Select(f => new FilmDettaglioDto
                {
                    IdFilm = f.IdFilm,
                    Titolo = f.Titolo,
                    Regista = f.Regista,
                    AnnoUscita = f.AnnoUscita,
                    Genere = f.Genere,
                    Attori = f.Attori,
                    ImmagineUrl = f.ImmagineUrl,
                    Descrizione = f.Descrizione,
                    VotoMedio = f.Recensioni.Any() ? f.Recensioni.Average(r => r.Voto) : null,
                    NumeroRecensioni = f.Recensioni.Count(),
                    TmdbId = f.TmdbId,
                    DurataMinuti = f.DurataMinuti,
                    GiaRecensito = idUtenteAutenticato != null &&
                                    f.Recensioni.Any(r => r.IdUtente == idUtenteAutenticato),
                    NumeroSpettatori = _db.Visioni.Where(v => v.IdFilm == f.IdFilm).Select(v => v.IdUtente).Distinct().Count(),
                    InDaVedere = idUtenteAutenticato != null &&
                                 _db.DaVedere.Any(d => d.IdFilm == f.IdFilm && d.IdUtente == idUtenteAutenticato),
                    NumeroVisioni = _db.Visioni.Count(v => v.IdFilm == f.IdFilm && v.IdUtente == idUtenteAutenticato),
                    UltimaVisione = _db.Visioni
                        .Where(v => v.IdFilm == f.IdFilm && v.IdUtente == idUtenteAutenticato)
                        .Max(v => (DateOnly?)v.DataVisione)
                })
                .FirstOrDefaultAsync();

            return film;
        }

        public async Task<Film?> GetFilmById(int id)
        {
            return await _db.Film.FindAsync(id);
        }



        // PAGINAZIONE — esempio concreto per ricordare la formula Skip/Take:
        //
        // Supponiamo 50 film totali, pageSize = 20 (20 film per pagina).
        //
        // pageNumber = 1  →  Skip = (1-1) * 20 = 0   → salto 0 film, prendo i film  1-20
        // pageNumber = 2  →  Skip = (2-1) * 20 = 20  → salto i primi 20, prendo i film 21-40
        // pageNumber = 3  →  Skip = (3-1) * 20 = 40  → salto i primi 40, prendo i film 41-50
        //
        // Regola generale: "quante pagine intere ho già mostrato prima di questa"
        // (pageNumber - 1) moltiplicato per "quanti elementi ci sono in ogni pagina"
        // (pageSize) = quanti elementi devo SALTARE prima di iniziare a prendere
        // quelli della pagina richiesta.

        public async Task<PagedResultDto<Film>> GetFilm(string? titolo, string? regista, int? annoUscita, string? genere, string? attori, int pageNumber, int pageSize)
        {
            //Da Ricordare : la query costruita non è ancora eseguita, la uso due volte, una volta per contare il totale una volta per prendere la pagina specifica, se eseguissi ToListAsync non si potrebbe contare niente.
            var query = _db.Film
                .Where(f => titolo == null || f.Titolo.Contains(titolo))
                .Where(f => regista == null || f.Regista.Contains(regista))
                .Where(f => genere == null || f.Genere.Contains(genere))
                .Where(f => annoUscita == null || f.AnnoUscita == annoUscita)
                .Where(f => attori == null || f.Attori.Contains(attori))
                .Where(f => f.Stato == StatiFilm.Approvato);


            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResultDto<Film>
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = items
            };
        }

        // Se il film c'è già (approvato o in attesa) non si crea un doppione:
        // si restituisce quello esistente in "Doppione" e "Film" resta null
        public async Task<(Film? Film, Film? Doppione)> CreateFilm(CreateFilmDto dto, int idUtente, string ruolo)
        {
            var esistente = await TrovaFilmSimile(new[] { dto.Titolo }, dto.AnnoUscita);
            if (esistente != null) return (null, esistente);

            var film = new Film
            {
                Titolo = dto.Titolo,
                Regista = dto.Regista,
                AnnoUscita = dto.AnnoUscita,
                Genere = dto.Genere,
                Attori = dto.Attori,
                ImmagineUrl = dto.ImmagineUrl,
                Descrizione = dto.Descrizione,
                IdUtente = idUtente,
                // Admin: film approvato subito. Utente normale: in attesa di revisione.
                Stato = ruolo == Ruoli.Admin ? StatiFilm.Approvato : StatiFilm.InAttesa
            };
            _db.Film.Add(film);
            await _db.SaveChangesAsync();
            return (film, null);
        }

        public async Task<bool> UpdateFilm(int id, CreateFilmDto dto)
        {
            var film = await _db.Film.FindAsync(id);
            if (film == null) return false;
            film.Titolo = dto.Titolo;
            film.Regista = dto.Regista;
            film.AnnoUscita = dto.AnnoUscita;
            film.Genere = dto.Genere;
            film.Attori = dto.Attori;
            // Aggiorna l'immagine solo se inviata: altrimenti si perderebbe
            // quella caricata con POST /api/film/{id}/immagine.
            if (dto.ImmagineUrl != null)
            {
                film.ImmagineUrl = dto.ImmagineUrl;
            }
            film.Descrizione = dto.Descrizione;
            await _db.SaveChangesAsync();
            return true;
        }

        // Eliminazione a cascata: sparisce il film con recensioni, commenti e preferiti.
        public async Task<bool> DeleteFilm(int id)
        {
            var film = await _db.Film.FindAsync(id);
            if (film == null) return false;

            using var transazione = await _db.Database.BeginTransactionAsync();
            var idRecensioni = await _db.Recensioni
                .Where(r => r.IdFilm == id)
                .Select(r => r.IdRecensione)
                .ToListAsync();
            await EliminazioneHelper.EliminaRecensioni(_db, idRecensioni);
            await _db.Preferiti.Where(p => p.IdFilm == id).ExecuteDeleteAsync();
            await _db.Visioni.Where(v => v.IdFilm == id).ExecuteDeleteAsync();
            await _db.DaVedere.Where(d => d.IdFilm == id).ExecuteDeleteAsync();
            await _db.Notifiche.Where(n => n.IdFilm == id).ExecuteDeleteAsync();
            await _db.ListeFilm.Where(lf => lf.IdFilm == id).ExecuteDeleteAsync();
            _db.Film.Remove(film);
            await _db.SaveChangesAsync();
            await transazione.CommitAsync();
            return true;
        }


        public async Task<String?> SaveImage(int id, IFormFile file)
        {
            var film = await _db.Film.FindAsync(id); 
            if (film == null) return null;
            var estensione = Path.GetExtension(file.FileName);
            //Questo procede una cosa tipo : a3f29c1e-8b4d-4f2a-9c3e-1d5f7a8b9c0d.jpg
            var nomeFileUnivoco = $"{Guid.NewGuid()}{estensione}";
            //Unisce un percorso valido tipo C:\CinemaApi\CinemaApi\wwwroot\uploads\film.
            var cartellaUpload = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "film");
            //Ci aggiunge il nome del file
            var percorsoCompleto = Path.Combine ( cartellaUpload, nomeFileUnivoco );
            //QUesto salva il file sul disco fisico 
            using (var stream = new FileStream(percorsoCompleto, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            var urlRelativo = $"/uploads/film/{nomeFileUnivoco}";
            film.ImmagineUrl = urlRelativo;
            await _db.SaveChangesAsync();
            return urlRelativo;
        }
        public async Task<List<Film>> GetFilmInAttesa()
        {
            return await _db.Film
                .Where(f => f.Stato == StatiFilm.InAttesa)
                .ToListAsync();
        }

        // Unisce due schede dello stesso film: tutto quello che riguarda "daEliminare" passa a
        // "daTenere", poi "daEliminare" viene cancellato. Dove lo stesso utente ha qualcosa su
        // entrambe (recensione, preferito, "da vedere", stessa lista) vince quella da tenere.
        // false se uno dei due film non esiste.
        public async Task<bool> UnisciFilm(int idDaTenere, int idDaEliminare)
        {
            var tenere = await _db.Film.FindAsync(idDaTenere);
            var eliminare = await _db.Film.FindAsync(idDaEliminare);
            if (tenere == null || eliminare == null) return false;

            using var transazione = await _db.Database.BeginTransactionAsync();

            // Recensioni: un utente ne ha al massimo una per film
            var autoriSulTenuto = _db.Recensioni.Where(r => r.IdFilm == idDaTenere).Select(r => r.IdUtente);
            var recensioniDoppie = await _db.Recensioni
                .Where(r => r.IdFilm == idDaEliminare && autoriSulTenuto.Contains(r.IdUtente))
                .Select(r => r.IdRecensione)
                .ToListAsync();
            await EliminazioneHelper.EliminaRecensioni(_db, recensioniDoppie);
            await _db.Recensioni.Where(r => r.IdFilm == idDaEliminare)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.IdFilm, idDaTenere));

            // Preferiti e "Da vedere": uno per utente
            var preferitiSulTenuto = _db.Preferiti.Where(p => p.IdFilm == idDaTenere).Select(p => p.IdUtente);
            await _db.Preferiti.Where(p => p.IdFilm == idDaEliminare && preferitiSulTenuto.Contains(p.IdUtente)).ExecuteDeleteAsync();
            await _db.Preferiti.Where(p => p.IdFilm == idDaEliminare)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IdFilm, idDaTenere));

            var daVedereSulTenuto = _db.DaVedere.Where(d => d.IdFilm == idDaTenere).Select(d => d.IdUtente);
            await _db.DaVedere.Where(d => d.IdFilm == idDaEliminare && daVedereSulTenuto.Contains(d.IdUtente)).ExecuteDeleteAsync();
            await _db.DaVedere.Where(d => d.IdFilm == idDaEliminare)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.IdFilm, idDaTenere));

            // Visioni: il diario conserva tutte le visioni
            await _db.Visioni.Where(v => v.IdFilm == idDaEliminare)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.IdFilm, idDaTenere));

            // Liste: un film una volta per lista
            var listeConTenuto = _db.ListeFilm.Where(lf => lf.IdFilm == idDaTenere).Select(lf => lf.IdLista);
            await _db.ListeFilm.Where(lf => lf.IdFilm == idDaEliminare && listeConTenuto.Contains(lf.IdLista)).ExecuteDeleteAsync();
            await _db.ListeFilm.Where(lf => lf.IdFilm == idDaEliminare)
                .ExecuteUpdateAsync(s => s.SetProperty(lf => lf.IdFilm, idDaTenere));

            await _db.Notifiche.Where(n => n.IdFilm == idDaEliminare)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IdFilm, idDaTenere));

            // Collegamento TMDB e campi vuoti della scheda tenuta presi dall'altra.
            // Il TmdbId va prima tolto dal film eliminato: l'indice è unico.
            var tmdbId = eliminare.TmdbId;
            if (tenere.TmdbId == null && tmdbId != null)
            {
                eliminare.TmdbId = null;
                await _db.SaveChangesAsync();
                tenere.TmdbId = tmdbId;
            }
            if (string.IsNullOrWhiteSpace(tenere.Regista)) tenere.Regista = eliminare.Regista;
            tenere.AnnoUscita ??= eliminare.AnnoUscita;
            if (string.IsNullOrWhiteSpace(tenere.Genere)) tenere.Genere = eliminare.Genere;
            if (string.IsNullOrWhiteSpace(tenere.Attori)) tenere.Attori = eliminare.Attori;
            if (string.IsNullOrWhiteSpace(tenere.ImmagineUrl)) tenere.ImmagineUrl = eliminare.ImmagineUrl;
            if (string.IsNullOrWhiteSpace(tenere.Descrizione)) tenere.Descrizione = eliminare.Descrizione;

            _db.Film.Remove(eliminare);
            await _db.SaveChangesAsync();
            await transazione.CommitAsync();
            return true;
        }

        public async Task ImpostaDurata(Film film, int minuti)
        {
            film.DurataMinuti = minuti;
            await _db.SaveChangesAsync();
        }

        // ---- TMDB ----

        // Mette nel catalogo un film TMDB (o restituisce quello che c'è già).
        // I dati arrivano da TMDB, quindi il film è approvato subito e non ha un autore.
        public async Task<TmdbImportazioneDto> ImportaDaTmdb(FilmTmdb dati)
        {
            var esistente = await _db.Film.FirstOrDefaultAsync(f => f.TmdbId == dati.TmdbId);

            // Film inserito a mano prima dell'integrazione: lo si collega invece di duplicarlo
            esistente ??= await TrovaGemelloManuale(dati);

            if (esistente != null)
            {
                esistente.TmdbId = dati.TmdbId;
                esistente.Stato = StatiFilm.Approvato;
                CompletaDaTmdb(esistente, dati);
                await _db.SaveChangesAsync();
                return new TmdbImportazioneDto { IdFilm = esistente.IdFilm, Creato = false };
            }

            var film = new Film { TmdbId = dati.TmdbId, Stato = StatiFilm.Approvato };
            CompletaDaTmdb(film, dati);
            _db.Film.Add(film);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Due importazioni contemporanee dello stesso film: vince la prima (indice unico su TmdbId)
                _db.Entry(film).State = EntityState.Detached;
                var giaImportato = await _db.Film.FirstAsync(f => f.TmdbId == dati.TmdbId);
                return new TmdbImportazioneDto { IdFilm = giaImportato.IdFilm, Creato = false };
            }
            return new TmdbImportazioneDto { IdFilm = film.IdFilm, Creato = true };
        }

        // Cerca un film inserito a mano che sia lo stesso film TMDB anche se il titolo è scritto
        // in modo diverso ("Dune: Parte Due" / "Dune - Parte due") o è quello originale
        // ("Poor Things" / "Povere creature!"). L'anno deve coincidere, se il film manuale ce l'ha.
        private Task<Film?> TrovaGemelloManuale(FilmTmdb dati) =>
            TrovaFilmSimile(new[] { dati.Titolo, dati.TitoloOriginale }, dati.AnnoUscita, soloSenzaTmdb: true);

        // Film già presente con lo stesso titolo (a meno di maiuscole, accenti e punteggiatura,
        // vedi TitoliHelper.Chiave) e un anno compatibile: uguale, oppure mancante in uno dei due.
        // I film rifiutati non contano: si possono riproporre. Ricerca indicizzata su TitoloChiave.
        private Task<Film?> TrovaFilmSimile(IEnumerable<string?> titoli, int? anno, bool soloSenzaTmdb = false)
        {
            var chiavi = titoli
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => TitoliHelper.Chiave(t!))
                .Distinct()
                .ToList();

            return _db.Film
                .Where(f => f.TitoloChiave != null && chiavi.Contains(f.TitoloChiave))
                .Where(f => f.Stato != StatiFilm.Rifiutato)
                .Where(f => !soloSenzaTmdb || f.TmdbId == null)
                .Where(f => anno == null || f.AnnoUscita == null || f.AnnoUscita == anno)
                // A parità, meglio indicare la scheda approvata che una in attesa
                .OrderBy(f => f.Stato == StatiFilm.Approvato ? 0 : 1)
                .FirstOrDefaultAsync();
        }

        // Chiave del titolo per i film salvati prima che esistesse la colonna (idempotente)
        public async Task CompletaChiaviTitolo()
        {
            var senzaChiave = await _db.Film.Where(f => f.TitoloChiave == null && f.Titolo != null).ToListAsync();
            if (senzaChiave.Count == 0) return;
            foreach (var film in senzaChiave) _db.Entry(film).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

        // TmdbId -> IdFilm per i film TMDB già presenti (e visibili) nel catalogo
        public async Task<Dictionary<int, int>> IdFilmPerTmdb(IEnumerable<int> tmdbIds)
        {
            var ids = tmdbIds.ToList();
            return await _db.Film
                .Where(f => f.TmdbId != null && ids.Contains(f.TmdbId.Value) && f.Stato == StatiFilm.Approvato)
                .ToDictionaryAsync(f => f.TmdbId!.Value, f => f.IdFilm);
        }

        public async Task<List<Film>> GetFilmSenzaTmdb()
        {
            return await _db.Film
                .Where(f => f.TmdbId == null)
                .OrderBy(f => f.IdFilm)
                .ToListAsync();
        }

        // null se il film non esiste, false se quel film TMDB è già collegato a un altro film
        public async Task<bool?> CollegaTmdb(int idFilm, FilmTmdb dati)
        {
            var film = await _db.Film.FindAsync(idFilm);
            if (film == null) return null;

            var giaUsato = await _db.Film.AnyAsync(f => f.TmdbId == dati.TmdbId && f.IdFilm != idFilm);
            if (giaUsato) return false;

            film.TmdbId = dati.TmdbId;
            CompletaDaTmdb(film, dati);
            await _db.SaveChangesAsync();
            return true;
        }

        // Riempie solo i campi vuoti: quello che hanno scritto gli utenti non si sovrascrive
        private static void CompletaDaTmdb(Film film, FilmTmdb dati)
        {
            if (string.IsNullOrWhiteSpace(film.Titolo)) film.Titolo = Taglia(dati.Titolo, 200);
            film.AnnoUscita ??= dati.AnnoUscita;
            if (string.IsNullOrWhiteSpace(film.Regista)) film.Regista = Taglia(dati.Regista, 200);
            if (string.IsNullOrWhiteSpace(film.Genere)) film.Genere = Taglia(dati.Genere, 200);
            if (string.IsNullOrWhiteSpace(film.Attori)) film.Attori = Taglia(dati.Attori, 200);
            if (string.IsNullOrWhiteSpace(film.ImmagineUrl)) film.ImmagineUrl = dati.LocandinaUrl;
            if (string.IsNullOrWhiteSpace(film.Descrizione)) film.Descrizione = Taglia(dati.Descrizione, 4000);
            film.DurataMinuti ??= dati.DurataMinuti;
        }

        // Le colonne del DB hanno una lunghezza massima: meglio tagliare che far fallire l'importazione
        private static string? Taglia(string? testo, int max) =>
            testo == null || testo.Length <= max ? testo : testo[..(max - 1)] + "…";

        public async Task<bool> UpdateStatoFilm(int id, string nuovoStato)
        {
            var film = await _db.Film.FindAsync(id);
            if (film == null) return false;
            film.Stato = nuovoStato;
            await _db.SaveChangesAsync();
            return true;
        }
    }




}