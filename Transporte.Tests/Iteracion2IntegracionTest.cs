using NUnit.Framework;

namespace Transporte.Tests
{
    public class Iteracion2IntegracionTest
    {
        [Test]
        public void Integracion_MedioBoleto_ConsumeHastaNegativoRecargaYVuelveAPagar()
        {
            // En la iteración 2 el medio boleto no poseía restricciones de intervalo ni tope diario
            var tipoIteracion2 = new TarjetaTipo(2, "Medio boleto estudiantil", 0.5m, 0, null);
            var tarjeta = new Tarjeta
            {
                TarjetaTipoId = 2,
                TarjetaTipo = tipoIteracion2
            };
            tarjeta.Cargar(2000);
            var colectivo = new Colectivo("Linea K");
            decimal mitadTarifa = Colectivo.TarifaBasica / 2m; // 790

            // Viaje 1: 2000 - 790 = 1210
            Assert.IsTrue(colectivo.PagarCon(tarjeta, out var b1));
            Assert.AreEqual(mitadTarifa, b1!.Monto);
            Assert.AreEqual(1210m, tarjeta.Saldo);
            Assert.AreEqual(1210m, b1.SaldoRestante);

            // Viaje 2: 1210 - 790 = 420
            Assert.IsTrue(colectivo.PagarCon(tarjeta, out var b2));
            Assert.AreEqual(mitadTarifa, b2!.Monto);
            Assert.AreEqual(420m, tarjeta.Saldo);
            Assert.AreEqual(420m, b2.SaldoRestante);

            // Viaje 3 (entra en negativo): 420 - 790 = -370
            Assert.IsTrue(colectivo.PagarCon(tarjeta, out var b3));
            Assert.AreEqual(mitadTarifa, b3!.Monto);
            Assert.AreEqual(-370m, tarjeta.Saldo);
            Assert.AreEqual(-370m, b3.SaldoRestante);

            // Viaje 4 (acumula negativo): -370 - 790 = -1160
            Assert.IsTrue(colectivo.PagarCon(tarjeta, out var b4));
            Assert.AreEqual(mitadTarifa, b4!.Monto);
            Assert.AreEqual(-1160m, tarjeta.Saldo);
            Assert.AreEqual(-1160m, b4.SaldoRestante);

            // Viaje 5 (acumula negativo dentro del límite de -2000): -1160 - 790 = -1950
            Assert.IsTrue(colectivo.PagarCon(tarjeta, out var b5));
            Assert.AreEqual(mitadTarifa, b5!.Monto);
            Assert.AreEqual(-1950m, tarjeta.Saldo);
            Assert.AreEqual(-1950m, b5.SaldoRestante);

            // Viaje 6: superaría el límite de -2000 (-1950 - 790 = -2740 < -2000) -> rechazado
            Assert.IsFalse(colectivo.PagarCon(tarjeta, out var b6));
            Assert.IsNull(b6);
            Assert.AreEqual(-1950m, tarjeta.Saldo);

            // Recarga de $3000: descuenta los viajes plus acumulados (-1950 + 3000 = 1050)
            tarjeta.Cargar(3000);
            Assert.AreEqual(1050m, tarjeta.Saldo);

            // Vuelve a pagar con medio boleto: 1050 - 790 = 260
            Assert.IsTrue(colectivo.PagarCon(tarjeta, out var b7));
            Assert.AreEqual(mitadTarifa, b7!.Monto);
            Assert.AreEqual(260m, tarjeta.Saldo);
            Assert.AreEqual(260m, b7.SaldoRestante);
        }
    }
}
