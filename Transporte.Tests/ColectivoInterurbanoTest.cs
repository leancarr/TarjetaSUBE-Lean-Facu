using System;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Transporte.Tests
{
    public class ColectivoInterurbanoTest
    {
        [Test]
        public void ColectivoInterurbano_ConstructoresYTarifaBase()
        {
            Assert.AreEqual(3000m, ColectivoInterurbano.TarifaInterurbana);

            var c1 = new ColectivoInterurbano();
            Assert.AreEqual(string.Empty, c1.Linea);

            var c2 = new ColectivoInterurbano("Rosario–Baigorria");
            Assert.AreEqual("Rosario–Baigorria", c2.Linea);

            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var c3 = new ColectivoInterurbano("Rosario–Gálvez", reloj);
            Assert.AreEqual("Rosario–Gálvez", c3.Linea);

            var c4 = new ColectivoInterurbano("Rosario–Pérez", reloj, ReglasDeViaje.PorDefecto());
            Assert.AreEqual("Rosario–Pérez", c4.Linea);

            // Test alias pagarCon en minúsculas
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(5000);
            Assert.IsTrue(c3.pagarCon(tarjeta, out var boleto));
            Assert.IsNotNull(boleto);
        }

        [Test]
        public void PagarCon_TarjetaNormal_PagaTarifaInterurbanaCompleta()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var colectivo = new ColectivoInterurbano("Rosario–Gálvez", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(5000);

            bool ok = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsTrue(ok);
            Assert.IsNotNull(boleto);
            Assert.AreEqual(ColectivoInterurbano.TarifaInterurbana, boleto!.TotalAbonado);
            Assert.AreEqual(5000m - ColectivoInterurbano.TarifaInterurbana, tarjeta.Saldo);
            Assert.AreEqual("Rosario–Gálvez", boleto.Linea);
        }

        [Test]
        public void PagarCon_MedioBoleto_PagaLaMitadDeTarifaInterurbana()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var colectivo = new ColectivoInterurbano("Rosario–Gálvez", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.MedioBoleto };
            tarjeta.Cargar(5000);

            bool ok = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsTrue(ok);
            Assert.IsNotNull(boleto);
            decimal tarifaEsperada = ColectivoInterurbano.TarifaInterurbana / 2m;
            Assert.AreEqual(tarifaEsperada, boleto!.TotalAbonado);
            Assert.AreEqual(5000m - tarifaEsperada, tarjeta.Saldo);
        }

        [Test]
        public void PagarCon_BoletoGratuitoYFranquiciaCompleta_PaganCero()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var colectivo = new ColectivoInterurbano("Rosario–Baigorria", reloj);

            // Gratuito estudiantil
            var tarjetaGratuita = new Tarjeta { TarjetaTipo = TiposDeTarjeta.BoletoGratuito };
            tarjetaGratuita.Cargar(5000);
            bool okGratuito = colectivo.PagarCon(tarjetaGratuita, out var boletoGratuito);

            Assert.IsTrue(okGratuito);
            Assert.IsNotNull(boletoGratuito);
            Assert.AreEqual(0m, boletoGratuito!.TotalAbonado);
            Assert.AreEqual(5000m, tarjetaGratuita.Saldo);

            // Franquicia completa (jubilados)
            var tarjetaJubilado = new Tarjeta { TarjetaTipo = TiposDeTarjeta.FranquiciaCompleta };
            tarjetaJubilado.Cargar(5000);
            bool okJubilado = colectivo.PagarCon(tarjetaJubilado, out var boletoJubilado);

            Assert.IsTrue(okJubilado);
            Assert.IsNotNull(boletoJubilado);
            Assert.AreEqual(0m, boletoJubilado!.TotalAbonado);
            Assert.AreEqual(5000m, tarjetaJubilado.Saldo);
        }

        [Test]
        public void PagarCon_SaldoCero_NoPuedePagarPorExcederLimiteNegativo()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var colectivo = new ColectivoInterurbano("Rosario–Gálvez", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            // Saldo inicial es 0. 0 - 3000 = -3000 < -2000 (LimiteNegativo)

            bool ok = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsFalse(ok);
            Assert.IsNull(boleto);
            Assert.AreEqual(0m, tarjeta.Saldo);
        }

        [Test]
        public void PagarCon_UsoFrecuenteSobre3000_AplicaDescuentosEnViajes30y80()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 20, 10, 0, 0));
            var colectivo = new ColectivoInterurbano("Rosario–Gálvez", reloj);

            // Tarjeta para viaje 30
            var tarjeta30 = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta30.Cargar(30000);
            HistorialDePrueba.AgregarViajes(tarjeta30, 29, new DateTime(2026, 10, 1, 8, 0, 0), colectivo);

            bool ok30 = colectivo.PagarCon(tarjeta30, out var boleto30);
            Assert.IsTrue(ok30);
            Assert.IsNotNull(boleto30);
            // 3000 * (1 - 0.20) = 2400
            Assert.AreEqual(2400m, boleto30!.TotalAbonado);

            // Tarjeta para viaje 80
            var tarjeta80 = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta80.Cargar(30000);
            HistorialDePrueba.AgregarViajes(tarjeta80, 79, new DateTime(2026, 10, 1, 8, 0, 0), colectivo);

            bool ok80 = colectivo.PagarCon(tarjeta80, out var boleto80);
            Assert.IsTrue(ok80);
            Assert.IsNotNull(boleto80);
            // 3000 * (1 - 0.25) = 2250
            Assert.AreEqual(2250m, boleto80!.TotalAbonado);
        }

        [Test]
        public void PagarCon_FranquiciaEnSabado_RechazaElPago()
        {
            // Sábado 17 de Octubre de 2026 a las 10:00 (fuera de lunes a viernes)
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 17, 10, 0, 0));
            var colectivo = new ColectivoInterurbano("Rosario–Gálvez", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.MedioBoleto };
            tarjeta.Cargar(5000);

            bool ok = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsFalse(ok);
            Assert.IsNull(boleto);
            Assert.AreEqual(5000m, tarjeta.Saldo);
        }

        [Test]
        public void PagarCon_BoletoInformaLaLinea()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var colectivo = new ColectivoInterurbano("Rosario–Gálvez", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(5000);

            bool ok = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsTrue(ok);
            Assert.IsNotNull(boleto);
            Assert.AreEqual("Rosario–Gálvez", boleto!.Linea);
        }

        [Test]
        public void DbContext_MapeaColectivoInterurbanoComoHerenciaDeColectivo()
        {
            var options = new DbContextOptionsBuilder<TransporteDbContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;

            using var context = new TransporteDbContext(options);
            var entityType = context.Model.FindEntityType(typeof(ColectivoInterurbano));

            Assert.IsNotNull(entityType);
            Assert.AreEqual(typeof(Colectivo), entityType!.BaseType?.ClrType);
        }
    }
}
