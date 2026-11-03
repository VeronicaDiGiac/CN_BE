namespace CinemaApi.DTOs.ResponseDto
{
    // Statistiche di un utente su un anno (Anno valorizzato) o su tutto il diario (Anno null)
    public class StatisticheDto
    {
        public int? Anno { get; set; }
        // Anni con almeno una visione, dal più recente: per il selettore del FE
        public List<int> AnniDisponibili { get; set; } = new();

        public int TotaleVisioni { get; set; }
        public int FilmDiversi { get; set; }
        // Somma delle durate note; VisioniConDurata dice su quante visioni è calcolata
        public int MinutiTotali { get; set; }
        public int VisioniConDurata { get; set; }

        // Serie TV (episodi visti nel periodo)
        public int EpisodiVisti { get; set; }
        public int SerieDiverse { get; set; }
        public int MinutiSerie { get; set; }
        // La serie con più episodi visti nel periodo (Dettaglio = numero di episodi)
        public FilmStatisticaDto? SeriePiuVista { get; set; }

        public int Recensioni { get; set; }
        public double? VotoMedio { get; set; }

        public List<VoceClassificaDto> GeneriTop { get; set; } = new();
        public List<VoceClassificaDto> RegistiTop { get; set; } = new();
        // 12 valori (gennaio..dicembre) se c'è un anno, altrimenti visioni per anno
        public List<VoceClassificaDto> Andamento { get; set; } = new();

        public FilmStatisticaDto? FilmPiuVisto { get; set; }
        public FilmStatisticaDto? FilmVotoPiuAlto { get; set; }
        public FilmStatisticaDto? PrimaVisione { get; set; }
        public FilmStatisticaDto? UltimaVisione { get; set; }
    }

    public class VoceClassificaDto
    {
        public string Nome { get; set; } = "";
        public int Conteggio { get; set; }
    }

    public class FilmStatisticaDto
    {
        public int IdFilm { get; set; }
        public string? Titolo { get; set; }
        public string? ImmagineUrl { get; set; }
        // Numero di visioni, voto o data, secondo la statistica
        public string? Dettaglio { get; set; }
    }
}
