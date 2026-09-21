using System;
using NUnit.Framework;

namespace Transporte.Tests
{
    public class BoletoTest
    {
        [Test]
        public void Constructor_ConParametros_InicializaPropiedadesCorrectamente()
        {
            var tarjeta = new Tarjeta();
            tarjeta.Cargar(2000);
            var colectivo = new Colectivo("Linea K");
            decimal monto = 1580;
            decimal saldoRestante = 420;

            var boleto = new Boleto(monto, saldoRestante, tarjeta, colectivo);

            Assert.AreEqual(monto, boleto.Monto);
            Assert.AreEqual(saldoRestante, boleto.SaldoRestante);
            Assert.AreEqual(tarjeta, boleto.Tarjeta);
            Assert.AreEqual(colectivo, boleto.Colectivo);
            Assert.IsTrue((DateTime.Now - boleto.FechaHora).TotalSeconds < 5);
        }

        [Test]
        public void Constructor_ConFechaEspecifica_AsignaFechaExacta()
        {
            var tarjeta = new Tarjeta();
            var colectivo = new Colectivo("Linea 115");
            var fecha = new DateTime(2026, 9, 21, 15, 30, 0);

            var boleto = new Boleto(1580, 500, tarjeta, colectivo, fecha);

            Assert.AreEqual(fecha, boleto.FechaHora);
            Assert.AreEqual(1580, boleto.Monto);
            Assert.AreEqual(500, boleto.SaldoRestante);
            Assert.AreEqual(tarjeta, boleto.Tarjeta);
            Assert.AreEqual(colectivo, boleto.Colectivo);
        }

        [Test]
        public void Constructor_VacioYSetters_PermiteModificarPropiedades()
        {
            var tarjeta = new Tarjeta();
            var colectivo = new Colectivo();
            var fecha = DateTime.Now;

            var boleto = new Boleto
            {
                Id = 1,
                Monto = 1580,
                SaldoRestante = 1000,
                FechaHora = fecha,
                TarjetaId = 10,
                Tarjeta = tarjeta,
                ColectivoId = 20,
                Colectivo = colectivo
            };

            Assert.AreEqual(1, boleto.Id);
            Assert.AreEqual(1580, boleto.Monto);
            Assert.AreEqual(1000, boleto.SaldoRestante);
            Assert.AreEqual(fecha, boleto.FechaHora);
            Assert.AreEqual(10, boleto.TarjetaId);
            Assert.AreEqual(tarjeta, boleto.Tarjeta);
            Assert.AreEqual(20, boleto.ColectivoId);
            Assert.AreEqual(colectivo, boleto.Colectivo);
        }
    }
}
