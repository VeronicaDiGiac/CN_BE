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

                    // idFilm ricavato a seconda del tipo di contenuto segnalato
                    IdFilm = s.TipoContenuto == "Recensione"
                        ? _db.Recensioni
                            .Where(r => r.IdRecensione == s.IdContenuto)
                            .Select(r => (int?)r.IdFilm)
                            .FirstOrDefault()
                        : _db.Commenti
                            .Where(c => c.IdCommento == s.IdContenuto)
                            .Select(c => (int?)c.Recensione.IdFilm)
                            .FirstOrDefault()
                })
                .ToListAsync();
        }

        public async Task<Segnalazione?> CreateSegnalazione(CreateSegnalazioneDto dto, int idUtente)
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
            return segnalazione;
        }

    

    public async Task<bool> UpdateStato(int idSegnalazione, UpdateStatoSegnalazioneDto dto, int idAdmin)
        {
            var segnalazione = await _db.Segnalazioni.FindAsync(idSegnalazione);
            if ( segnalazione == null) return false;
            segnalazione.Stato = dto.Stato;
            segnalazione.DataSegnalazione = DateTime.Now;
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