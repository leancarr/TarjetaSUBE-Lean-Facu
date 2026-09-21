using System;
using Microsoft.EntityFrameworkCore;

namespace Transporte
{
    public class TransporteDbContext : DbContext
    {
        public DbSet<Tarjeta> Tarjetas { get; set; } = null!;
        public DbSet<Colectivo> Colectivos { get; set; } = null!;
        public DbSet<Boleto> Boletos { get; set; } = null!;

        public TransporteDbContext(DbContextOptions<TransporteDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Boleto>()
                .HasOne(b => b.Tarjeta)
                .WithMany()
                .HasForeignKey(b => b.TarjetaId);

            modelBuilder.Entity<Boleto>()
                .HasOne(b => b.Colectivo)
                .WithMany()
                .HasForeignKey(b => b.ColectivoId);
        }
    }
}
