using NUnit.Framework;

namespace Transporte.Tests
{
    public class TarjetaTest
    {
        private Tarjeta tarjeta = null!;

        [SetUp]
        public void Setup()
        {
            tarjeta = new Tarjeta();
        }

        [Test]
        public void SaldoInicial_EsCero()
        {
            Assert.AreEqual(0, tarjeta.Saldo);
        }

        [Test]
        public void Propiedad_Id_AsignaYRecuperaCorrectamente()
        {
            tarjeta.Id = 42;
            Assert.AreEqual(42, tarjeta.Id);
        }

        [TestCase(2000)]
        [TestCase(3000)]
        [TestCase(4000)]
        [TestCase(5000)]
        [TestCase(8000)]
        [TestCase(10000)]
        [TestCase(15000)]
        [TestCase(20000)]
        [TestCase(25000)]
        [TestCase(30000)]
        public void Cargar_MontoAceptado_ActualizaSaldo(decimal monto)
        {
            tarjeta.Cargar(monto);
            Assert.AreEqual(monto, tarjeta.Saldo);
        }

        [Test]
        public void Cargar_MontoNoAceptado_LanzaExcepcion()
        {
            Assert.Throws<MontoDeCargaInvalidoException>(() => tarjeta.Cargar(1500));
        }

        [Test]
        public void Cargar_ExcedeLimiteSaldo_LanzaExcepcion()
        {
            tarjeta.Cargar(30000);
            Assert.Throws<LimiteSaldoExcedidoException>(() => tarjeta.Cargar(15000));
        }

        [Test]
        public void Cargar_LlegaJustoAlLimite_ActualizaSaldo()
        {
            tarjeta.Cargar(20000);
            tarjeta.Cargar(20000);
            Assert.AreEqual(40000, tarjeta.Saldo);
        }

        [Test]
        public void PuedeDescontar_SaldoSuficiente_DevuelveTrue()
        {
            tarjeta.Cargar(2000);
            Assert.IsTrue(tarjeta.PuedeDescontar(1580));
        }

        [Test]
        public void PuedeDescontar_SaldoInsuficiente_DevuelveFalse()
        {
            tarjeta.Cargar(2000);
            tarjeta.Descontar(1580);
            Assert.IsFalse(tarjeta.PuedeDescontar(1580));
        }

        [Test]
        public void Descontar_RestaMontoDelSaldo()
        {
            tarjeta.Cargar(2000);
            tarjeta.Descontar(1580);
            Assert.AreEqual(2000 - 1580, tarjeta.Saldo);
        }
    }
}
