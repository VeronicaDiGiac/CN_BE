namespace CinemaApi.DTOs.ResponseDto
{
    // Situazione dei voti dopo una modifica: il FE aggiorna stelle e media senza ricaricare la pagina.
    // Per un film: media e numero dei voti al film; per una recensione: delle stelle date alla recensione.
    public class VotiDto
    {
        public double? VotoMedio { get; set; }
        public int NumeroVoti { get; set; }
        // Il voto dell'utente autenticato (null se l'ha tolto)
        public int? VotoUtente { get; set; }
    }
}
