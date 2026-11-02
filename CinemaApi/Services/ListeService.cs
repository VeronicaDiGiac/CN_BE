using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    // Liste personali di film. I controlli su chi può vedere/modificare stanno nel controller;
    // qui le query rispettano sempre la regola "le liste private le vede solo il proprietario".
    public class ListeService
    {
        private const int NumeroCopertine = 4;

        private readonly CinemaContext _db;

        public ListeService(CinemaContext db) { _db = db; }

        public Task<bool> FilmEsiste(int idFilm) => _db.FilmApprovatoEsiste(idFilm);

        public Task<Lista?> GetLista(int idLista) => _db.Liste.FindAsync(idLista).AsTask();

        // Liste di un utente: le private solo se le chiede il proprietario
        public Task<List<ListaRiassuntoDto>> GetListeUtente(int idUtente, int? idRichiedente) =>
            Riassunti(_db.Liste.Where(l => l.IdUtente == idUtente && (l.Pubblica || l.IdUtente == idRichiedente)), null);

        // Liste dell'utente autenticato, con ContieneFilm per la scelta "Aggiungi a una lista"
        public Task<List<ListaRiassuntoDto>> GetMie(int idUtente, int? idFilm) =>
            Riassunti(_db.Liste.Where(l => l.IdUtente == idUtente), idFilm);

        private Task<List<ListaRiassuntoDto>> Riassunti(IQueryable<Lista> query, int? idFilm) =>
            query
                .OrderByDescending(l => l.DataModifica)
                .Select(l => new ListaRiassuntoDto
                {
                    IdLista = l.IdLista,
                    IdUtente = l.IdUtente,
                    UserName = l.Utente!.UserName,
                    Titolo = l.Titolo,
                    Descrizione = l.Descrizione,
                    Pubblica = l.Pubblica,
                    DataModifica = l.DataModifica,
                    NumeroFilm = l.Film!.Count(),
                    Copertine = l.Film!
                        .Where(lf => lf.Film!.ImmagineUrl != null)
                        .OrderBy(lf => lf.Posizione)
                        .Select(lf => lf.Film!.ImmagineUrl!)
                        .Take(NumeroCopertine)
                        .ToList(),
                    ContieneFilm = idFilm != null && l.Film!.Any(lf => lf.IdFilm == idFilm)
                })
                .ToListAsync();

        // null se non esiste o se è privata e chi la chiede non è il proprietario
        // (stessa risposta nei due casi: non si rivela l'esistenza delle liste private)
        public Task<ListaDettaglioDto?> GetDettaglio(int idLista, int? idRichiedente) =>
            _db.Liste
                .Where(l => l.IdLista == idLista && (l.Pubblica || l.IdUtente == idRichiedente))
                .Select(l => new ListaDettaglioDto
                {
                    IdLista = l.IdLista,
                    IdUtente = l.IdUtente,
                    UserName = l.Utente!.UserName,
                    Titolo = l.Titolo,
                    Descrizione = l.Descrizione,
                    Pubblica = l.Pubblica,
                    DataCreazione = l.DataCreazione,
                    DataModifica = l.DataModifica,
                    Film = l.Film!
                        .OrderBy(lf => lf.Posizione)
                        .Select(lf => new ListaFilmDto
                        {
                            IdFilm = lf.IdFilm,
                            Posizione = lf.Posizione,
                            Nota = lf.Nota,
                            Titolo = lf.Film!.Titolo,
                            AnnoUscita = lf.Film.AnnoUscita,
                            Regista = lf.Film.Regista,
                            ImmagineUrl = lf.Film.ImmagineUrl
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

        public async Task<Lista> Crea(SalvaListaDto dto, int idUtente)
        {
            var adesso = DateTime.Now;
            var lista = new Lista
            {
                IdUtente = idUtente,
                Titolo = dto.Titolo.Trim(),
                Descrizione = string.IsNullOrWhiteSpace(dto.Descrizione) ? null : dto.Descrizione.Trim(),
                Pubblica = dto.Pubblica,
                DataCreazione = adesso,
                DataModifica = adesso
            };
            _db.Liste.Add(lista);
            await _db.SaveChangesAsync();
            return lista;
        }

        public async Task Aggiorna(Lista lista, SalvaListaDto dto)
        {
            lista.Titolo = dto.Titolo.Trim();
            lista.Descrizione = string.IsNullOrWhiteSpace(dto.Descrizione) ? null : dto.Descrizione.Trim();
            lista.Pubblica = dto.Pubblica;
            lista.DataModifica = DateTime.Now;
            await _db.SaveChangesAsync();
        }

        public async Task Elimina(Lista lista)
        {
            using var transazione = await _db.Database.BeginTransactionAsync();
            await _db.ListeFilm.Where(lf => lf.IdLista == lista.IdLista).ExecuteDeleteAsync();
            _db.Liste.Remove(lista);
            await _db.SaveChangesAsync();
            await transazione.CommitAsync();
        }

        // Idempotente: se il film c'è già si aggiorna solo la nota. I nuovi film vanno in fondo.
        public async Task AggiungiFilm(Lista lista, int idFilm, string? nota)
        {
            nota = string.IsNullOrWhiteSpace(nota) ? null : nota.Trim();
            var esistente = await _db.ListeFilm.FindAsync(lista.IdLista, idFilm);
            if (esistente != null)
            {
                esistente.Nota = nota;
            }
            else
            {
                var ultima = await _db.ListeFilm
                    .Where(lf => lf.IdLista == lista.IdLista)
                    .MaxAsync(lf => (int?)lf.Posizione) ?? 0;
                _db.ListeFilm.Add(new ListaFilm
                {
                    IdLista = lista.IdLista,
                    IdFilm = idFilm,
                    Posizione = ultima + 1,
                    Nota = nota,
                    DataAggiunta = DateTime.Now
                });
            }
            lista.DataModifica = DateTime.Now;
            await _db.SaveChangesAsync();
        }

        // false se il film non era nella lista
        public async Task<bool> RimuoviFilm(Lista lista, int idFilm)
        {
            var tolti = await _db.ListeFilm
                .Where(lf => lf.IdLista == lista.IdLista && lf.IdFilm == idFilm)
                .ExecuteDeleteAsync();
            if (tolti == 0) return false;

            lista.DataModifica = DateTime.Now;
            await _db.SaveChangesAsync();
            return true;
        }

        // false se gli id non sono esattamente i film della lista (né uno di più né uno di meno)
        public async Task<bool> Riordina(Lista lista, List<int> idFilm)
        {
            var righe = await _db.ListeFilm.Where(lf => lf.IdLista == lista.IdLista).ToListAsync();
            if (idFilm.Count != righe.Count || !righe.Select(r => r.IdFilm).ToHashSet().SetEquals(idFilm))
                return false;

            var posizione = idFilm.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i + 1);
            foreach (var riga in righe) riga.Posizione = posizione[riga.IdFilm];

            lista.DataModifica = DateTime.Now;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
