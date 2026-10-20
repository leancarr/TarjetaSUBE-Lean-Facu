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
            tarjeta.Descontar(1580); // Saldo queda en 420
            // Con límite de -2000, el saldo mínimo tras descontar no puede ser menor a -2000.
            // 420 - 2500 = -2080 < -2000, por lo que devuelve false.
            Assert.IsFalse(tarjeta.PuedeDescontar(2500));
        }

        [Test]
        public void Descontar_RestaMontoDelSaldo()
        {
            tarjeta.Cargar(2000);
            tarjeta.Descontar(1580);
            Assert.AreEqual(2000 - 1580, tarjeta.Saldo);
        }

        [Test]
        public void PagarCon_SaldoAlLimiteNegativo_DevuelveFalse()
        {
            var t = new Tarjeta();
            t.Cargar(2000);
            var colectivo = new Colectivo();

            // Consume hasta dejar la tarjeta en el límite o cerca del mismo
            while (t.PuedeDescontar(Colectivo.TarifaBasica))
                colectivo.PagarCon(t, out _);

            decimal saldoAntes = t.Saldo;
            bool resultado = colectivo.PagarCon(t, out var boleto);

            Assert.IsFalse(resultado);
            Assert.IsNull(boleto);
            Assert.AreEqual(saldoAntes, t.Saldo);
            Assert.GreaterOrEqual(t.Saldo, Tarjeta.LimiteNegativo);
        }

        [Test]
        public void PagarCon_ViajesPlus_DescuentaCorrectamenteSaldoNegativoAcumulado()
        {
            var t = new Tarjeta();
            t.Cargar(3000);
            var colectivo = new Colectivo();

            // Viaje 1 (con saldo positivo): 3000 - 1580 = 1420
            bool viaje1 = colectivo.PagarCon(t, out var boleto1);
            Assert.IsTrue(viaje1);
            Assert.AreEqual(1420, t.Saldo);
            Assert.AreEqual(1420, boleto1!.SaldoRestante);

            // Viaje 2 (viaje plus 1, pasa a negativo): 1420 - 1580 = -160
            bool viaje2 = colectivo.PagarCon(t, out var boleto2);
            Assert.IsTrue(viaje2);
            Assert.AreEqual(-160, t.Saldo);
            Assert.AreEqual(-160, boleto2!.SaldoRestante);

            // Viaje 3 (viaje plus 2, acumula saldo negativo): -160 - 1580 = -1740 (>= -2000)
            bool viaje3 = colectivo.PagarCon(t, out var boleto3);
            Assert.IsTrue(viaje3);
            Assert.AreEqual(-1740, t.Saldo);
            Assert.AreEqual(-1740, boleto3!.SaldoRestante);

            // Viaje 4: superaría el límite de -2000 (-1740 - 1580 = -3320) -> no se permite
            bool viaje4 = colectivo.PagarCon(t, out var boleto4);
            Assert.IsFalse(viaje4);
            Assert.IsNull(boleto4);
            Assert.AreEqual(-1740, t.Saldo);
        }

        [Test]
        public void Cargar_ConSaldoNegativo_DescuentaLoConsumidoCorrectamente()
        {
            var t = new Tarjeta();
            t.Cargar(3000);
            var colectivo = new Colectivo();

            colectivo.PagarCon(t, out _); // Saldo: 1420
            colectivo.PagarCon(t, out _); // Saldo: -160
            colectivo.PagarCon(t, out _); // Saldo: -1740

            // Al recargar 2000, se descuenta la deuda consumida: -1740 + 2000 = 260
            t.Cargar(2000);
            Assert.AreEqual(260, t.Saldo);
        }

        [Test]
        public void Cargar_DesdeSaldoNegativo_RespetaLimiteSuperiorDe40000()
        {
            var t = new Tarjeta();
            t.Descontar(1000); // Saldo: -1000
            Assert.AreEqual(-1000, t.Saldo);

            t.Cargar(30000); // Saldo: -1000 + 30000 = 29000
            Assert.AreEqual(29000, t.Saldo);

            // Cargar 15000 superaría el límite máximo (29000 + 15000 = 44000 > 40000)
            Assert.Throws<LimiteSaldoExcedidoException>(() => t.Cargar(15000));
            Assert.AreEqual(29000, t.Saldo);
        }
    }
}
