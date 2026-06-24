using System.ComponentModel.DataAnnotations;

namespace CinemaApi.Models
{
    public class Follow
    {
        [Key]
        public int IdFollow { get; set; }
        public int FollowerId { get; set; }     // chi segue
        public int FollowingId { get; set; }    // chi viene seguito
        public DateTime? DataFollow { get; set; }

        // Navigation properties verso Utenti.
        // Due relazioni diverse verso la STESSA tabella, quindi vanno
        // distinte esplicitamente ( nel CinemaContext con Fluent API).
        public Utente? Follower { get; set; }
        public Utente? Following { get; set; }
    }
}