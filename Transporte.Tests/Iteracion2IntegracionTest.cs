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
            decimal mitadTarifa = Colectivo.TarifaBasica / 2m; // 1075

            // Viaje 1: 2000 - 1075 = 925
            Assert.IsTrue(colectivo.PagarCon(tarjeta, out var b1));
            Assert.AreEqual(mitadTarifa, b1!.Monto);
            Assert.AreEqual(2000m - mitadTarifa, tarjeta.Saldo);
            Assert.AreEqual(2000m - mitadTarifa, b1.SaldoRestante);

            // Viaje 2 (entra en negativo): 925 - 1075 = -150
            Assert.IsTrue(colectivo.PagarCon(tarjeta, out var b2));
            Assert.AreEqual(mitadTarifa, b2!.Monto);
            Assert.AreEqual(925m - mitadTarifa, tarjeta.Saldo);
            Assert.AreEqual(925m - mitadTarifa, b2.SaldoRestante);

            // Viaje 3 (acumula negativo): -150 - 1075 = -1225 (>= -2000)
            Assert.IsTrue(colectivo.PagarCon(tarjeta, out var b3));
            Assert.AreEqual(mitadTarifa, b3!.Monto);
            Assert.AreEqual(-150m - mitadTarifa, tarjeta.Saldo);
            Assert.AreEqual(-150m - mitadTarifa, b3.SaldoRestante);

            // Viaje 4: superaría el límite de -2000 (-1225 - 1075 = -2300 < -2000) -> rechazado
            Assert.IsFalse(colectivo.PagarCon(tarjeta, out var b4));
            Assert.IsNull(b4);
            Assert.AreEqual(-1225m, tarjeta.Saldo);

            // Recarga de $3000: descuenta los viajes plus acumulados (-1225 + 3000 = 1775)
            tarjeta.Cargar(3000);
            Assert.AreEqual(1775m, tarjeta.Saldo);

            // Vuelve a pagar con medio boleto: 1775 - 1075 = 700
            Assert.IsTrue(colectivo.PagarCon(tarjeta, out var b5));
            Assert.AreEqual(mitadTarifa, b5!.Monto);
            Assert.AreEqual(1775m - mitadTarifa, tarjeta.Saldo);
            Assert.AreEqual(1775m - mitadTarifa, b5.SaldoRestante);
        }
    }
}
