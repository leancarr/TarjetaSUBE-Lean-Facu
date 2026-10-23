using System;
using NUnit.Framework;

namespace Transporte.Tests
{
    public class BoletoTest
    {
        private DateTime fechaFija;
        private ProveedorDeFechaFalso reloj = null!;

        [SetUp]
        public void Setup()
        {
            fechaFija = new DateTime(2026, 10, 12, 10, 0, 0); // Lunes 10:00
            reloj = new ProveedorDeFechaFalso(fechaFija);
        }

        [Test]
        public void Boleto_TarjetaNormal_InformaTodosLosDatosCorrectos()
        {
            var colectivo = new Colectivo("Linea 122", reloj);
            var tarjeta = new Tarjeta { Id = 10, TarjetaTipoId = 1, TarjetaTipo = TiposDePrueba.Normal };
            tarjeta.Cargar(5000);

            bool resultado = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsTrue(resultado);
            Assert.IsNotNull(boleto);
            Assert.AreEqual(fechaFija, boleto!.Fecha);
            Assert.AreEqual("Normal", boleto.TipoTarjeta);
            Assert.AreEqual("Linea 122", boleto.Linea);
            Assert.AreEqual(Colectivo.TarifaBasica, boleto.TotalAbonado);
            Assert.AreEqual(5000 - Colectivo.TarifaBasica, boleto.SaldoRestante);
            Assert.AreEqual(10, boleto.TarjetaId);
            Assert.IsFalse(boleto.EsViajePlus);
        }

        [Test]
        public void Boleto_MedioBoleto_InformaTodosLosDatosCorrectos()
        {
            var colectivo = new Colectivo("Linea K", reloj);
            var tarjeta = new Tarjeta { Id = 20, TarjetaTipoId = 2, TarjetaTipo = TiposDePrueba.MedioBoleto };
            tarjeta.Cargar(3000);
            decimal tarifaMedio = Colectivo.TarifaBasica / 2m;

            bool resultado = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsTrue(resultado);
            Assert.IsNotNull(boleto);
            Assert.AreEqual(fechaFija, boleto!.Fecha);
            Assert.AreEqual("Medio boleto estudiantil", boleto.TipoTarjeta);
            Assert.AreEqual("Linea K", boleto.Linea);
            Assert.AreEqual(tarifaMedio, boleto.TotalAbonado);
            Assert.AreEqual(3000 - tarifaMedio, boleto.SaldoRestante);
            Assert.AreEqual(20, boleto.TarjetaId);
            Assert.IsFalse(boleto.EsViajePlus);
        }

        [Test]
        public void Boleto_BoletoGratuitoEstudiantil_InformaCostoCeroYDatosCorrectos()
        {
            var colectivo = new Colectivo("Linea 115", reloj);
            var tarjeta = new Tarjeta { Id = 30, TarjetaTipoId = 3, TarjetaTipo = TiposDePrueba.BoletoGratuito };

            bool resultado = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsTrue(resultado);
            Assert.IsNotNull(boleto);
            Assert.AreEqual(fechaFija, boleto!.Fecha);
            Assert.AreEqual("Boleto gratuito estudiantil", boleto.TipoTarjeta);
            Assert.AreEqual("Linea 115", boleto.Linea);
            Assert.AreEqual(0m, boleto.TotalAbonado);
            Assert.AreEqual(0m, boleto.SaldoRestante);
            Assert.AreEqual(30, boleto.TarjetaId);
            Assert.IsFalse(boleto.EsViajePlus);
        }

        [Test]
        public void Boleto_FranquiciaCompleta_InformaCostoCeroYDatosCorrectos()
        {
            var colectivo = new Colectivo("Linea 142", reloj);
            var tarjeta = new Tarjeta { Id = 40, TarjetaTipoId = 4, TarjetaTipo = TiposDePrueba.FranquiciaCompleta };

            bool resultado = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsTrue(resultado);
            Assert.IsNotNull(boleto);
            Assert.AreEqual(fechaFija, boleto!.Fecha);
            Assert.AreEqual("Franquicia completa", boleto.TipoTarjeta);
            Assert.AreEqual("Linea 142", boleto.Linea);
            Assert.AreEqual(0m, boleto.TotalAbonado);
            Assert.AreEqual(0m, boleto.SaldoRestante);
            Assert.AreEqual(40, boleto.TarjetaId);
            Assert.IsFalse(boleto.EsViajePlus);
        }

        [Test]
        public void Boleto_ViajePlus_InformaTotalAbonadoYEsViajePlusTrue()
        {
            var colectivo = new Colectivo("Linea 102", reloj);
            var tarjeta = new Tarjeta { Id = 50, TarjetaTipoId = 1, TarjetaTipo = TiposDePrueba.Normal };
            // Saldo inicial es 0 -> viaje plus permitido (0 - 1580 = -1580 >= -2000)

            bool resultado = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsTrue(resultado);
            Assert.IsNotNull(boleto);
            Assert.AreEqual(fechaFija, boleto!.Fecha);
            Assert.AreEqual("Normal", boleto.TipoTarjeta);
            Assert.AreEqual("Linea 102", boleto.Linea);
            Assert.AreEqual(Colectivo.TarifaBasica, boleto.TotalAbonado);
            Assert.AreEqual(-Colectivo.TarifaBasica, boleto.SaldoRestante);
            Assert.AreEqual(50, boleto.TarjetaId);
            Assert.IsTrue(boleto.EsViajePlus);
        }

        [Test]
        public void Constructor_ConParametrosCompletos_InicializaPropiedadesDirectamente()
        {
            var tarjeta = new Tarjeta { Id = 99 };
            var colectivo = new Colectivo("Linea K");
            var fecha = new DateTime(2026, 10, 15, 14, 30, 0);

            var boleto = new Boleto(fecha, "Normal", "Linea K", 1580m, 3420m, 99, tarjeta, colectivo);

            Assert.AreEqual(fecha, boleto.Fecha);
            Assert.AreEqual(fecha, boleto.FechaHora);
            Assert.AreEqual("Normal", boleto.TipoTarjeta);
            Assert.AreEqual("Linea K", boleto.Linea);
            Assert.AreEqual(1580m, boleto.TotalAbonado);
            Assert.AreEqual(1580m, boleto.Monto);
            Assert.AreEqual(3420m, boleto.SaldoRestante);
            Assert.AreEqual(99, boleto.TarjetaId);
            Assert.AreEqual(tarjeta, boleto.Tarjeta);
            Assert.AreEqual(colectivo, boleto.Colectivo);
            Assert.IsFalse(boleto.EsViajePlus);
        }
    }
}
