namespace CinemaApi.DTOs.ResponseDto
{
    // Un commento dell'utente con il posto in cui l'ha scritto (GET /api/commenti/miei)
    public class MioCommentoDto
    {
        public int IdCommento { get; set; }
        public string? Contenuto { get; set; }
        public bool Spoiler { get; set; }
        public DateTime? DataCommento { get; set; }

        public int? IdFilm { get; set; }
        public string? TitoloFilm { get; set; }
        public string? ImmagineFilm { get; set; }
        public string? TipoFilm { get; set; }

        // Sotto la recensione di qualcuno...
        public int? IdRecensione { get; set; }
        public string? AutoreRecensione { get; set; }
        // ...oppure in una discussione (eventualmente di un episodio)
        public int? IdDiscussione { get; set; }
        public string? TitoloDiscussione { get; set; }
        public int? Stagione { get; set; }
        public int? Episodio { get; set; }

        // Se è una risposta: l'autore del commento principale del filo
        public int? IdCommentoPadre { get; set; }
        public string? InRispostaA { get; set; }

        public int NumeroMiPiace { get; set; }
        // Risposte degli altri a questo commento (solo per i commenti principali)
        public int NumeroRisposte { get; set; }
    }
}
