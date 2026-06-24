using System.ComponentModel.DataAnnotations;

namespace CinemaApi.DTOs.RequestDto
{
    public class CreateFollowDto
    {
        // Solo l'id dell'utente che si vuole seguire.
        // Il FollowerId (chi segue) NON va qui: come per tutte le altre
        [Required]
        public int FollowingId { get; set; }
    }
}