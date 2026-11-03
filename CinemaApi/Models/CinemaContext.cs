using CinemaApi.Helpers;
using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace CinemaApi.Models
{
    public class CinemaContext : DbContext
    {
        public CinemaContext(DbContextOptions<CinemaContext> options) : base(options) { }

        // Ogni film salvato (nuovo o modificato) ha sempre la chiave del titolo aggiornata,
        // qualunque sia il punto del codice che lo salva
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            AggiornaChiaviTitolo();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            AggiornaChiaviTitolo();
            return base.SaveChanges();
        }

        private void AggiornaChiaviTitolo()
        {
            foreach (var voce in ChangeTracker.Entries<Film>())
            {
                if (voce.State is EntityState.Added or EntityState.Modified)
                    voce.Entity.TitoloChiave = voce.Entity.Titolo == null ? null : TitoliHelper.Chiave(voce.Entity.Titolo);
            }
        }

        public DbSet<Utente> Utenti { get; set; }
        public DbSet<Film> Film { get; set; }
        public DbSet<Recensione> Recensioni { get; set; }
        public DbSet<Commento> Commenti { get; set; }
        public DbSet<Preferito> Preferiti { get; set; }

        public DbSet<Follow> Follow { get; set; }

        public DbSet<Segnalazione> Segnalazioni { get; set; }

        public DbSet<PasswordReset> PasswordReset { get; set; }

        public DbSet<Visione> Visioni { get; set; }

        public DbSet<EpisodioVisto> EpisodiVisti { get; set; }

        public DbSet<DaVedere> DaVedere { get; set; }

        public DbSet<MiPiaceRecensione> MiPiaceRecensioni { get; set; }

        public DbSet<Notifica> Notifiche { get; set; }

        public DbSet<Lista> Liste { get; set; }

        public DbSet<ListaFilm> ListeFilm { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MiPiaceRecensione>()
                .HasKey(m => new { m.IdUtente, m.IdRecensione });

            modelBuilder.Entity<ListaFilm>()
                .HasKey(lf => new { lf.IdLista, lf.IdFilm });

            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Follower)
                .WithMany()
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Following)
                .WithMany()
                .HasForeignKey(f => f.FollowingId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}