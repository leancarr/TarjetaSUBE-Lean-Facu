using System;
using NUnit.Framework;

namespace Transporte.Tests
{
    public class ColectivoTest
    {
        private Colectivo colectivo = null!;

        [SetUp]
        public void Setup()
        {
            colectivo = new Colectivo("Linea 122");
        }

        [Test]
        public void PagarCon_SaldoSuficiente_GeneraBoleto()
        {
            var tarjeta = new Tarjeta();
            tarjeta.Cargar(2000);

            var boleto = colectivo.PagarCon(tarjeta);

            Assert.IsNotNull(boleto);
            Assert.AreEqual(Colectivo.TarifaBasica, boleto.Monto);
            Assert.AreEqual(420, boleto.SaldoRestante);
            Assert.AreEqual(420, tarjeta.Saldo);
            Assert.AreEqual(tarjeta, boleto.Tarjeta);
            Assert.AreEqual(colectivo, boleto.Colectivo);
            Assert.IsTrue((DateTime.Now - boleto.FechaHora).TotalSeconds < 5);
        }

        [Test]
        public void PagarCon_SaldoInsuficiente_LanzaExcepcionYNoModificaSaldo()
        {
            var tarjeta = new Tarjeta();
            tarjeta.Cargar(2000);
            colectivo.PagarCon(tarjeta); // Saldo queda en 420

            Assert.Throws<SaldoInsuficienteException>(() => colectivo.PagarCon(tarjeta));
            Assert.AreEqual(420, tarjeta.Saldo);
        }

        [Test]
        public void PagarCon_TarjetaNula_LanzaArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => colectivo.PagarCon(null!));
        }

        [Test]
        public void PagarCon_MetodoEnMinuscula_FuncionaIgual()
        {
            var tarjeta = new Tarjeta();
            tarjeta.Cargar(3000);

            var boleto = colectivo.pagarCon(tarjeta);

            Assert.IsNotNull(boleto);
            Assert.AreEqual(1580, boleto.Monto);
            Assert.AreEqual(1420, tarjeta.Saldo);
        }
    }
}
