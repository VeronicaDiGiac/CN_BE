using CinemaApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace CinemaApi.Models
{
    public class CinemaContext : DbContext
    {
        public CinemaContext(DbContextOptions<CinemaContext> options) : base(options) { }

        public DbSet<Utente> Utenti { get; set; }
        public DbSet<Film> Film { get; set; }
        public DbSet<Recensione> Recensioni { get; set; }
        public DbSet<Commento> Commenti { get; set; }
        public DbSet<Preferito> Preferiti { get; set; }

        public DbSet<Follow> Follow { get; set; }
    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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