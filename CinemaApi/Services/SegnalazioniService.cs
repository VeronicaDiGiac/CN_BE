using CinemaApi.Helpers;
using CinemaApi.DTOs.RequestDto;
using CinemaApi.DTOs.ResponseDto;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApi.Services
{
    public class SegnalazioniService
    {
        private readonly CinemaContext _db;

        public SegnalazioniService(CinemaContext db)
        {
            _db = db;
        }

        public async Task<List<SegnalazioneDto>> GetSegnalazioni()
        {
            return await _db.Segnalazioni
                .Select(s => new SegnalazioneDto
                {
                    IdSegnalazione = s.IdSegnalazione,
                    IdUtente = s.IdUtente,
                    UserName = s.Utente != null ? s.Utente.UserName : null,
                    TipoContenuto = s.TipoContenuto,
                    IdContenuto = s.IdContenuto,
                    Motivo = s.Motivo,
                    DataSegnalazione = s.DataSegnalazione,
                    Stato = s.Stato,
                    DataGestione = s.DataGestione,
                    IdAdminGestore = s.IdAdminGestore,

                    IdDiscussione = s.TipoContenuto == TipiContenuto.Discussione
                        ? s.IdContenuto
                        : s.TipoContenuto == TipiContenuto.Commento
                        ? _db.Commenti.Where(c => c.IdCommento == s.IdContenuto).Select(c => c.IdDiscussione).FirstOrDefault()
                        : null,

                    // idFilm ricavato a seconda del tipo di contenuto segnalato
                    IdFilm = s.TipoContenuto == TipiContenuto.Recensione
                        ? _db.Recensioni
                            .Where(r => r.IdRecensione == s.IdContenuto)
                            .Select(r => (int?)r.IdFilm)
                            .FirstOrDefault()
                        : s.TipoContenuto == TipiContenuto.Film
                        ? (int?)s.IdContenuto
                        : s.TipoContenuto == TipiContenuto.Discussione
                        ? _db.Discussioni
                            .Where(d => d.IdDiscussione == s.IdContenuto)
                            .Select(d => (int?)d.IdFilm)
                            .FirstOrDefault()
                        : _db.Commenti
                            .Where(c => c.IdCommento == s.IdContenuto)
                            .Select(c => c.Recensione != null ? (int?)c.Recensione.IdFilm : (int?)c.Discussione!.IdFilm)
                            .FirstOrDefault()
                })
                .ToListAsync();
        }

        // TipoContenuto è già validato dal DTO: qui controllo che il contenuto esista davvero.
        public async Task<bool> ContenutoEsiste(string tipoContenuto, int idContenuto)
        {
            if (tipoContenuto == TipiContenuto.Recensione)
                return await _db.Recensioni.AnyAsync(r => r.IdRecensione == idContenuto);
            if (tipoContenuto == TipiContenuto.Discussione)
                return await _db.Discussioni.AnyAsync(d => d.IdDiscussione == idContenuto);
            if (tipoContenuto == TipiContenuto.Film)
                return await _db.FilmApprovatoEsiste(idContenuto);
            return await _db.Commenti.AnyAsync(c => c.IdCommento == idContenuto);
        }

        public async Task<SegnalazioneDto> CreateSegnalazione(CreateSegnalazioneDto dto, int idUtente)
        {
            var segnalazione = new Segnalazione
            {
                IdUtente = idUtente,               // dal token, non dal dto
                TipoContenuto = dto.TipoContenuto,
                IdContenuto = dto.IdContenuto,
                Motivo = dto.Motivo,
                DataSegnalazione = DateTime.Now
            };
            _db.Segnalazioni.Add(segnalazione);
            await _db.SaveChangesAsync();
            return new SegnalazioneDto
            {
                IdSegnalazione = segnalazione.IdSegnalazione,
                IdUtente = segnalazione.IdUtente,
                TipoContenuto = segnalazione.TipoContenuto,
                IdContenuto = segnalazione.IdContenuto,
                Motivo = segnalazione.Motivo,
                DataSegnalazione = segnalazione.DataSegnalazione,
                Stato = segnalazione.Stato
            };
        }

    

    public async Task<bool> UpdateStato(int idSegnalazione, UpdateStatoSegnalazioneDto dto, int idAdmin)
        {
            var segnalazione = await _db.Segnalazioni.FindAsync(idSegnalazione);
            if ( segnalazione == null) return false;
            segnalazione.Stato = dto.Stato;
            segnalazione.DataGestione = DateTime.Now;
            segnalazione.IdAdminGestore = idAdmin;

            await _db.SaveChangesAsync();
            return true;
            // se null → false
            // aggiorna Stato, DataGestione, IdAdminGestore
            // salva
            // return true
        }


    }
}