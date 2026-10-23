using System;
using Microsoft.EntityFrameworkCore;

namespace Transporte
{
    public class TransporteDbContext : DbContext
    {
        public DbSet<Tarjeta> Tarjetas { get; set; } = null!;
        public DbSet<TarjetaTipo> TarjetaTipos { get; set; } = null!;
        public DbSet<Colectivo> Colectivos { get; set; } = null!;
        public DbSet<Boleto> Boletos { get; set; } = null!;

        public TransporteDbContext(DbContextOptions<TransporteDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Tarjeta>()
                .HasOne(t => t.TarjetaTipo)
                .WithMany()
                .HasForeignKey(t => t.TarjetaTipoId);

            modelBuilder.Entity<TarjetaTipo>().HasData(
                TiposDeTarjeta.Normal,
                TiposDeTarjeta.MedioBoleto,
                TiposDeTarjeta.BoletoGratuito,
                TiposDeTarjeta.FranquiciaCompleta
            );

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
