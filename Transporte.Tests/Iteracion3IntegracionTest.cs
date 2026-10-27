using System;
using NUnit.Framework;

namespace Transporte.Tests
{
    public class Iteracion3IntegracionTest
    {
        [Test]
        public void Escenario_MedioBoleto_PuntaAPunta_IntervaloTopeDiarioYReinicioSiguienteDia()
        {
            // Lunes 12 de Octubre de 2026, 10:00 (dentro de franja laboral/escolar)
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var colectivo = new Colectivo("Linea 122", reloj);
            var tarjeta = new Tarjeta
            {
                Id = 15,
                TarjetaTipoId = 2,
                TarjetaTipo = TiposDeTarjeta.MedioBoleto
            };

            // Cargamos suficiente saldo
            tarjeta.Cargar(20000);
            decimal mitadTarifa = Colectivo.TarifaBasica / 2m;
            decimal tarifaCompleta = Colectivo.TarifaBasica;

            // 1. 10:00 -> 1er viaje: paga la mitad.
            decimal saldoEsperado1 = 20000m - mitadTarifa;
            bool v1 = colectivo.PagarCon(tarjeta, out var b1);
            Assert.IsTrue(v1);
            Assert.IsNotNull(b1);
            Assert.AreEqual(reloj.Ahora, b1!.Fecha);
            Assert.AreEqual("Medio boleto estudiantil", b1.TipoTarjeta);
            Assert.AreEqual("Linea 122", b1.Linea);
            Assert.AreEqual(mitadTarifa, b1.TotalAbonado);
            Assert.AreEqual(saldoEsperado1, b1.SaldoRestante);
            Assert.AreEqual(saldoEsperado1, tarjeta.Saldo);
            Assert.AreEqual(15, b1.TarjetaId);
            Assert.IsFalse(b1.EsViajePlus);

            // 2. 10:03 (+3 minutos) -> Rechazado por regla de intervalo mínimo de 5 minutos
            reloj.Avanzar(TimeSpan.FromMinutes(3));
            bool v2 = colectivo.PagarCon(tarjeta, out var b2);
            Assert.IsFalse(v2);
            Assert.IsNull(b2);
            Assert.AreEqual(saldoEsperado1, tarjeta.Saldo);

            // 3. 10:06 (+3 minutos más = 6 min desde el primero) -> 2do viaje: paga la mitad.
            decimal saldoEsperado2 = saldoEsperado1 - mitadTarifa;
            reloj.Avanzar(TimeSpan.FromMinutes(3));
            bool v3 = colectivo.PagarCon(tarjeta, out var b3);
            Assert.IsTrue(v3);
            Assert.IsNotNull(b3);
            Assert.AreEqual(reloj.Ahora, b3!.Fecha);
            Assert.AreEqual(mitadTarifa, b3.TotalAbonado);
            Assert.AreEqual(saldoEsperado2, b3.SaldoRestante);
            Assert.AreEqual(saldoEsperado2, tarjeta.Saldo);
            Assert.IsFalse(b3.EsViajePlus);

            // 4. 10:12 (+6 minutos) -> 3er viaje del día: se agotaron los 2 viajes con beneficio, paga tarifa completa.
            decimal saldoEsperado3 = saldoEsperado2 - tarifaCompleta;
            reloj.Avanzar(TimeSpan.FromMinutes(6));
            bool v4 = colectivo.PagarCon(tarjeta, out var b4);
            Assert.IsTrue(v4);
            Assert.IsNotNull(b4);
            Assert.AreEqual(reloj.Ahora, b4!.Fecha);
            Assert.AreEqual(tarifaCompleta, b4.TotalAbonado);
            Assert.AreEqual(saldoEsperado3, b4.SaldoRestante);
            Assert.AreEqual(saldoEsperado3, tarjeta.Saldo);
            Assert.IsFalse(b4.EsViajePlus);

            // 5. Día siguiente: Martes 13 de Octubre a las 10:00 -> Se reinicia el beneficio diario, paga la mitad
            decimal saldoEsperado4 = saldoEsperado3 - mitadTarifa;
            reloj.Avanzar(TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(48)));
            Assert.AreEqual(new DateTime(2026, 10, 13, 10, 0, 0), reloj.Ahora);

            bool v5 = colectivo.PagarCon(tarjeta, out var b5);
            Assert.IsTrue(v5);
            Assert.IsNotNull(b5);
            Assert.AreEqual(reloj.Ahora, b5!.Fecha);
            Assert.AreEqual(mitadTarifa, b5.TotalAbonado);
            Assert.AreEqual(saldoEsperado4, b5.SaldoRestante);
            Assert.AreEqual(saldoEsperado4, tarjeta.Saldo);
            Assert.IsFalse(b5.EsViajePlus);

            // Verificamos que se hayan registrado exactamente 4 boletos emitidos (los 4 exitosos)
            Assert.AreEqual(4, tarjeta.Boletos.Count);
        }
    }
}
