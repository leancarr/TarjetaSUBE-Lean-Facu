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
            Assert.AreEqual(0, tarjeta.SaldoPendiente);
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
            Assert.AreEqual(0, tarjeta.SaldoPendiente);
        }

        [Test]
        public void Cargar_MontoNoAceptado_LanzaExcepcion()
        {
            Assert.Throws<MontoDeCargaInvalidoException>(() => tarjeta.Cargar(1500));
        }

        [Test]
        public void Cargar_SuperaSaldoMaximo_DejaSaldoEn56000YExcedenteEnSaldoPendiente()
        {
            tarjeta.Cargar(30000);
            tarjeta.Cargar(20000); // Saldo: 50000, Pendiente: 0
            tarjeta.Cargar(10000); // Carga de 10000: entran 6000 a Saldo, 4000 quedan pendientes

            Assert.AreEqual(56000m, tarjeta.Saldo);
            Assert.AreEqual(4000m, tarjeta.SaldoPendiente);
        }

        [Test]
        public void Cargar_DosCargasSeguidas_AcumulanSaldoPendiente()
        {
            tarjeta.Cargar(30000);
            tarjeta.Cargar(20000);
            tarjeta.Cargar(10000); // Saldo: 56000, Pendiente: 4000
            Assert.AreEqual(56000m, tarjeta.Saldo);
            Assert.AreEqual(4000m, tarjeta.SaldoPendiente);

            tarjeta.Cargar(15000); // 4000 + 15000 = 19000 pendiente
            Assert.AreEqual(56000m, tarjeta.Saldo);
            Assert.AreEqual(19000m, tarjeta.SaldoPendiente);
        }

        [Test]
        public void PagarCon_ConSaldoPendiente_DescuentaYReacreditaHastaMaximo()
        {
            var colectivo = new Colectivo("Linea 122");
            tarjeta.Cargar(30000);
            tarjeta.Cargar(20000);
            tarjeta.Cargar(10000); // Saldo: 56000, Pendiente: 4000

            bool viaje = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsTrue(viaje);
            Assert.IsNotNull(boleto);
            Assert.AreEqual(Colectivo.TarifaBasica, boleto!.TotalAbonado);
            Assert.AreEqual(56000m, tarjeta.Saldo);
            Assert.AreEqual(4000m - Colectivo.TarifaBasica, tarjeta.SaldoPendiente);
        }

        [Test]
        public void Descontar_PendienteMenorAlHueco_AcreditaTodoElPendiente()
        {
            tarjeta.Cargar(30000);
            tarjeta.Cargar(25000); // Saldo: 55000
            tarjeta.Cargar(2000);  // Entra 1000 a Saldo (56000), 1000 queda en Pendiente
            Assert.AreEqual(56000m, tarjeta.Saldo);
            Assert.AreEqual(1000m, tarjeta.SaldoPendiente);

            // Descontamos 3000 directamente: Saldo baja a 53000, hueco disponible = 3000.
            // Como el pendiente es 1000 (< 3000), se acredita todo el pendiente (1000) -> Saldo = 54000, Pendiente = 0
            tarjeta.Descontar(3000);

            Assert.AreEqual(54000m, tarjeta.Saldo);
            Assert.AreEqual(0m, tarjeta.SaldoPendiente);
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
            var tarjeta = new Tarjeta();
            tarjeta.Cargar(2000);
            var colectivo = new Colectivo();

            // Consume hasta dejar la tarjeta en el límite o cerca del mismo
            while (tarjeta.PuedeDescontar(Colectivo.TarifaBasica))
                colectivo.PagarCon(tarjeta, out _);

            decimal saldoAntes = tarjeta.Saldo;
            bool resultado = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsFalse(resultado);
            Assert.IsNull(boleto);
            Assert.AreEqual(saldoAntes, tarjeta.Saldo);
            Assert.GreaterOrEqual(tarjeta.Saldo, Tarjeta.LimiteNegativo);
        }

        [Test]
        public void PagarCon_ViajesPlus_DescuentaCorrectamenteSaldoNegativoAcumulado()
        {
            var tarjeta = new Tarjeta();
            tarjeta.Cargar(3000);
            var colectivo = new Colectivo();

            // Viaje 1 (con saldo positivo): 3000 - 1580 = 1420
            bool viaje1 = colectivo.PagarCon(tarjeta, out var boleto1);
            Assert.IsTrue(viaje1);
            Assert.AreEqual(1420, tarjeta.Saldo);
            Assert.AreEqual(1420, boleto1!.SaldoRestante);

            // Viaje 2 (viaje plus 1, pasa a negativo): 1420 - 1580 = -160
            bool viaje2 = colectivo.PagarCon(tarjeta, out var boleto2);
            Assert.IsTrue(viaje2);
            Assert.AreEqual(-160, tarjeta.Saldo);
            Assert.AreEqual(-160, boleto2!.SaldoRestante);

            // Viaje 3 (viaje plus 2, acumula saldo negativo): -160 - 1580 = -1740 (>= -2000)
            bool viaje3 = colectivo.PagarCon(tarjeta, out var boleto3);
            Assert.IsTrue(viaje3);
            Assert.AreEqual(-1740, tarjeta.Saldo);
            Assert.AreEqual(-1740, boleto3!.SaldoRestante);

            // Viaje 4: superaría el límite de -2000 (-1740 - 1580 = -3320) -> no se permite
            bool viaje4 = colectivo.PagarCon(tarjeta, out var boleto4);
            Assert.IsFalse(viaje4);
            Assert.IsNull(boleto4);
            Assert.AreEqual(-1740, tarjeta.Saldo);
        }

        [Test]
        public void Cargar_ConSaldoNegativo_DescuentaLoConsumidoCorrectamente()
        {
            var tarjeta = new Tarjeta();
            tarjeta.Cargar(3000);
            var colectivo = new Colectivo();

            colectivo.PagarCon(tarjeta, out _); // Saldo: 1420
            colectivo.PagarCon(tarjeta, out _); // Saldo: -160
            colectivo.PagarCon(tarjeta, out _); // Saldo: -1740

            // Al recargar 2000, se descuenta la deuda consumida: -1740 + 2000 = 260
            tarjeta.Cargar(2000);
            Assert.AreEqual(260, tarjeta.Saldo);
            Assert.AreEqual(0, tarjeta.SaldoPendiente);
        }

        [Test]
        public void Cargar_DesdeSaldoNegativo_AcreditaHastaMaximoYExcedenteQuedaPendiente()
        {
            var tarjeta = new Tarjeta();
            tarjeta.Descontar(1000); // Saldo: -1000
            Assert.AreEqual(-1000m, tarjeta.Saldo);

            tarjeta.Cargar(30000); // Saldo: -1000 + 30000 = 29000, Pendiente = 0
            Assert.AreEqual(29000m, tarjeta.Saldo);
            Assert.AreEqual(0m, tarjeta.SaldoPendiente);

            tarjeta.Cargar(30000); // 29000 + 30000 = 59000 -> Saldo: 56000, Pendiente: 3000
            Assert.AreEqual(56000m, tarjeta.Saldo);
            Assert.AreEqual(3000m, tarjeta.SaldoPendiente);
        }
    }
}
