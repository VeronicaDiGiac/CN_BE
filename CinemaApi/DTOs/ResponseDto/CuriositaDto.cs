namespace CinemaApi.DTOs.ResponseDto
{
    // "Curiosità sulla produzione" da Wikipedia (testo CC BY-SA: il FE cita la fonte con il link)
    public class CuriositaDto
    {
        // "it" o "en"
        public string Lingua { get; set; } = "";
        public string TitoloPagina { get; set; } = "";
        public string Url { get; set; } = "";
        public List<SezioneCuriositaDto> Sezioni { get; set; } = new();
    }

    public class SezioneCuriositaDto
    {
        // Come nella pagina Wikipedia (es. "Riprese", "Filming")
        public string Titolo { get; set; } = "";
        // Testo semplice, accorciato a fine frase
        public string Testo { get; set; } = "";
    }
}
