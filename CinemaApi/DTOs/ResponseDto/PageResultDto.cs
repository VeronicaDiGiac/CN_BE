namespace CinemaApi.DTOs.ResponseDto
{

    // T => questo contenitore puo contenere qualsiasi tipo
    public class PagedResultDto<T>
    {
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<T> Items { get; set; } = new List<T>();
    }
}