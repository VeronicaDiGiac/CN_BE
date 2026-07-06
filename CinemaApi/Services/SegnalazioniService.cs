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

        public async Task<List<SegnalazioneDto>> GetSegalazione()
        {
            return await _db.Segnalazioni
                //.Where(s => s.SegnalazioniId == idUtente) il filtro where non serve, lato admin vedo tutto
                .Select(s => new SegnalazioneDto {
                    IdSegnalazione = s.IdSegnalazione,
                    IdUtente = s.IdUtente,
                    UserName = s.Utente != null ? s.Utente.UserName : null,
                    TipoContenuto = s.TipoContenuto,
                    IdContenuto = s.IdContenuto,
                    Motivo = s.Motivo,
                    DataSegnalazione = s.DataSegnalazione
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

    }
    }
