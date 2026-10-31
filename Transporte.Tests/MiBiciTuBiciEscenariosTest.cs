using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Transporte.Tests
{
    // Escenarios de prueba de aceptación para el sistema Mi Bici Tu Bici (Iteración 5)
    // Cubre los escenarios mínimos del enunciado, variantes con multas y aislamiento con Colectivo.
    public class MiBiciTuBiciEscenariosTest
    {
        private static IEnumerable<TarjetaTipo> TiposDeTarjetaParaPrueba()
        {
            yield return TiposDeTarjeta.Normal;
            yield return TiposDeTarjeta.MedioBoleto;
            yield return TiposDeTarjeta.BoletoGratuito;
            yield return TiposDeTarjeta.FranquiciaCompleta;
        }

        [Test]
        public void Escenario1_RetiroExitoso_TarifaExacta_DevolucionSinDemora()
        {
            // Lunes 12/10/2026 10:00:00
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var estacion = new EstacionBicicletas("Parque Independencia", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(5000);

            decimal saldoInicial = tarjeta.Saldo; // 5000

            // 1. Retiro exitoso con saldo suficiente
            bool retiroOk = estacion.RetirarBici(tarjeta, out var retiro);

            Assert.IsTrue(retiroOk, "El retiro debe ser exitoso con saldo suficiente.");
            Assert.IsNotNull(retiro);
            Assert.AreEqual(EstacionBicicletas.TarifaDiaria, retiro!.TarifaCobrada, "Debe cobrar la tarifa diaria de $2418.75.");
            Assert.AreEqual(0m, retiro.MultaCobrada, "No hay multas previas.");
            Assert.AreEqual(saldoInicial - EstacionBicicletas.TarifaDiaria, tarjeta.Saldo, "El saldo debe descontarse exactamente en el valor de la tarifa diaria.");
            Assert.IsTrue(retiro.EstaEnCurso, "El retiro debe quedar en curso.");
            Assert.AreEqual(reloj.Ahora, retiro.FechaRetiro);
            Assert.IsNull(retiro.FechaDevolucion);

            // 2. Devolución a los 45 minutos (dentro del límite de 1 hora)
            reloj.Avanzar(TimeSpan.FromMinutes(45));
            bool devOk = estacion.DevolverBici(tarjeta, out var devolucion);

            Assert.IsTrue(devOk, "La devolución debe completarse correctamente.");
            Assert.IsNotNull(devolucion);
            Assert.AreEqual(retiro, devolucion);
            Assert.IsFalse(devolucion!.EstaEnCurso, "El retiro ya no está en curso tras la devolución.");
            Assert.AreEqual(reloj.Ahora, devolucion.FechaDevolucion, "La fecha de devolución debe ser registrada.");
            Assert.IsFalse(devolucion.TuvoDemora, "No debe registrar demora pues 45 min <= 60 min.");
            Assert.AreEqual(0m, estacion.MultaPendiente(tarjeta), "No debe haber multas pendientes.");
            Assert.AreEqual(saldoInicial - EstacionBicicletas.TarifaDiaria, tarjeta.Saldo, "La devolución a tiempo no afecta el saldo.");
        }

        [TestCase(0)]
        [TestCase(2000)]
        public void Escenario2a_SaldoInsuficiente_SinMulta_RechazaYSaldoIntacto(decimal saldoInicial)
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var estacion = new EstacionBicicletas("Parque Independencia", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };

            if (saldoInicial > 0)
            {
                tarjeta.Cargar(saldoInicial);
            }

            bool ok = estacion.RetirarBici(tarjeta, out var retiro);

            Assert.IsFalse(ok, "El retiro debe rechazarse si el saldo es menor a la tarifa diaria.");
            Assert.IsNull(retiro, "No debe emitirse comprobante de retiro si fue rechazado.");
            Assert.AreEqual(saldoInicial, tarjeta.Saldo, "El saldo debe quedar totalmente intacto.");
            Assert.IsEmpty(tarjeta.RetirosDeBici, "No deben agregarse registros al historial.");
        }

        [Test]
        public void Escenario2b_SaldoAlcanzaParaTarifaPeroNoParaTarifaMasMulta_RechazaYMultaIntacta()
        {
            // Día 1: paga y devuelve tarde (90 min > 1 h), queda multa de $1250
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var estacion = new EstacionBicicletas("Parque Independencia", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(5000);

            estacion.RetirarBici(tarjeta, out _); // saldo queda en 5000 - 2418.75 = 2581.25
            reloj.Avanzar(TimeSpan.FromMinutes(90));
            estacion.DevolverBici(tarjeta, out var rDev);
            Assert.IsTrue(rDev!.TuvoDemora);
            Assert.AreEqual(1250m, estacion.MultaPendiente(tarjeta));

            // Día 2: el saldo (2581.25) alcanza para la tarifa (2418.75) pero NO para tarifa + multa (3668.75)
            reloj.Avanzar(TimeSpan.FromDays(1));
            decimal saldoAntes = tarjeta.Saldo; // 2581.25

            bool ok = estacion.RetirarBici(tarjeta, out var retiro);

            Assert.IsFalse(ok, "Debe rechazar porque no alcanza para cubrir la tarifa más la multa pendiente.");
            Assert.IsNull(retiro, "No debe emitirse comprobante de retiro.");
            Assert.AreEqual(2581.25m, tarjeta.Saldo, "El saldo debe permanecer intacto.");
            Assert.AreEqual(saldoAntes, tarjeta.Saldo);
            Assert.AreEqual(1250m, estacion.MultaPendiente(tarjeta), "La multa debe permanecer pendiente.");
            Assert.IsFalse(rDev.MultaSaldada, "La multa no debe quedar saldada si el retiro fue rechazado.");
        }

        [Test]
        public void Escenario3a_UnaMultaAcumulada_SeCobraTarifaMasMulta_MultaQuedaEnCero()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var estacion = new EstacionBicicletas("Parque Independencia", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(10000);

            // Día 1: Retiro con pase diario ($2418.75) y devolución demorada (90 min)
            estacion.RetirarBici(tarjeta, out _);
            reloj.Avanzar(TimeSpan.FromMinutes(90));
            estacion.DevolverBici(tarjeta, out var r1);
            Assert.IsTrue(r1!.TuvoDemora);
            Assert.AreEqual(1250m, estacion.MultaPendiente(tarjeta));

            // Día 2: Retiro con saldo suficiente para Tarifa ($2418.75) + 1 Multa ($1250) = $3668.75
            reloj.Avanzar(TimeSpan.FromDays(1));
            decimal saldoAntes = tarjeta.Saldo; // 10000 - 2418.75 = 7581.25

            bool ok = estacion.RetirarBici(tarjeta, out var r2);

            Assert.IsTrue(ok, "El retiro debe ser exitoso al contar con saldo suficiente para tarifa y multas.");
            Assert.IsNotNull(r2);
            Assert.AreEqual(EstacionBicicletas.TarifaDiaria, r2!.TarifaCobrada);
            Assert.AreEqual(1250m, r2.MultaCobrada);
            Assert.AreEqual(0m, estacion.MultaPendiente(tarjeta), "La multa pendiente debe quedar en $0.");
            Assert.IsTrue(r1.MultaSaldada, "La multa del primer retiro debe quedar marcada como saldada.");
            // Saldo esperado: 7581.25 - 3668.75 = 3912.50
            Assert.AreEqual(saldoAntes - (EstacionBicicletas.TarifaDiaria + 1250m), tarjeta.Saldo);
            Assert.AreEqual(3912.50m, tarjeta.Saldo);
        }

        [Test]
        public void Escenario3b_VariasMultasAcumuladas_MismoDia_SeCobranTodasEnElSiguientePago()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var estacion = new EstacionBicicletas("Parque Independencia", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(15000);

            // Día 1 - Retiro 1: Paga el día ($2418.75) y devuelve tarde (90 min) -> Multa 1 ($1250)
            estacion.RetirarBici(tarjeta, out _);
            reloj.Avanzar(TimeSpan.FromMinutes(90));
            estacion.DevolverBici(tarjeta, out var r1);
            Assert.IsTrue(r1!.TuvoDemora);

            // Día 1 - Retiro 2: Mismo día, no cobra tarifa ni multas aún
            reloj.Avanzar(TimeSpan.FromHours(1));
            estacion.RetirarBici(tarjeta, out _);
            reloj.Avanzar(TimeSpan.FromMinutes(90));
            estacion.DevolverBici(tarjeta, out var r2);
            Assert.IsTrue(r2!.TuvoDemora);

            // Se acumularon 2 multas de $1250 = $2500
            Assert.AreEqual(2500m, estacion.MultaPendiente(tarjeta));

            // Día 2: Próximo retiro con cobro -> liquida Tarifa ($2418.75) + 2 Multas ($2500) = $4918.75
            reloj.Avanzar(TimeSpan.FromDays(1));
            decimal saldoAntes = tarjeta.Saldo; // 15000 - 2418.75 = 12581.25

            bool ok = estacion.RetirarBici(tarjeta, out var r3);

            Assert.IsTrue(ok);
            Assert.IsNotNull(r3);
            Assert.AreEqual(EstacionBicicletas.TarifaDiaria, r3!.TarifaCobrada);
            Assert.AreEqual(2500m, r3.MultaCobrada);
            Assert.AreEqual(0m, estacion.MultaPendiente(tarjeta));
            Assert.IsTrue(r1.MultaSaldada);
            Assert.IsTrue(r2.MultaSaldada);
            // Saldo: 12581.25 - 4918.75 = 7662.50
            Assert.AreEqual(saldoAntes - 4918.75m, tarjeta.Saldo);
            Assert.AreEqual(7662.50m, tarjeta.Saldo);
        }

        [TestCaseSource(nameof(TiposDeTarjetaParaPrueba))]
        public void Extras_CualquierTipoDeTarjetaPagaTarifaCompleta(TarjetaTipo tipo)
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var estacion = new EstacionBicicletas("Estación Pellegrini", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = tipo };
            tarjeta.Cargar(5000);

            bool ok = estacion.RetirarBici(tarjeta, out var retiro);

            Assert.IsTrue(ok);
            Assert.IsNotNull(retiro);
            Assert.AreEqual(EstacionBicicletas.TarifaDiaria, retiro!.TarifaCobrada, $"La tarjeta {tipo.Nombre} debe pagar tarifa plana sin descuentos.");
            Assert.AreEqual(5000m - EstacionBicicletas.TarifaDiaria, tarjeta.Saldo);
        }

        [Test]
        public void Extras_PaseDelDia_SegundoRetiroMismoDiaNoCobra_DiaSiguienteSiCobra()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 8, 0, 0));
            var estacion = new EstacionBicicletas("Plaza San Martín", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(10000);

            // Retiro 1: paga tarifa completa
            estacion.RetirarBici(tarjeta, out _);
            reloj.Avanzar(TimeSpan.FromMinutes(30));
            estacion.DevolverBici(tarjeta, out _);

            decimal saldoLuegoDelPrimerRetiro = tarjeta.Saldo; // 10000 - 2418.75 = 7581.25

            // Retiro 2 (mismo día calendario a las 14:00): pase libre diario
            reloj.Avanzar(TimeSpan.FromHours(5).Add(TimeSpan.FromMinutes(30)));
            bool ok2 = estacion.RetirarBici(tarjeta, out var r2);
            Assert.IsTrue(ok2);
            Assert.AreEqual(0m, r2!.TarifaCobrada, "El segundo retiro del día no debe cobrar tarifa.");
            Assert.AreEqual(saldoLuegoDelPrimerRetiro, tarjeta.Saldo, "El saldo no debe verse afectado.");
            estacion.DevolverBici(tarjeta, out _);

            // Retiro 3 (día siguiente a las 08:00): nuevo día, vuelve a cobrar tarifa diaria
            reloj.Avanzar(TimeSpan.FromHours(18));
            bool ok3 = estacion.RetirarBici(tarjeta, out var r3);
            Assert.IsTrue(ok3);
            Assert.AreEqual(EstacionBicicletas.TarifaDiaria, r3!.TarifaCobrada, "Al día siguiente se debe abonar nuevamente la tarifa.");
            Assert.AreEqual(saldoLuegoDelPrimerRetiro - EstacionBicicletas.TarifaDiaria, tarjeta.Saldo);
        }

        [Test]
        public void Extras_UnaBiciPorTarjeta_NoPermiteSegundoRetiroSimultaneo()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var est1 = new EstacionBicicletas("Estación 1", reloj);
            var est2 = new EstacionBicicletas("Estación 2", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(10000);

            bool ok1 = est1.RetirarBici(tarjeta, out var r1);
            Assert.IsTrue(ok1);
            Assert.IsNotNull(r1);

            // Intento de segundo retiro mientras la primera está en curso
            bool ok2 = est2.RetirarBici(tarjeta, out var r2);
            Assert.IsFalse(ok2, "No debe permitir retirar una segunda bicicleta en simultáneo.");
            Assert.IsNull(r2);
            Assert.AreEqual(1, tarjeta.RetirosDeBici.Count);
        }

        [Test]
        public void Aislamiento_RetiroDeBiciNoCreaBoleto()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var estacion = new EstacionBicicletas("Parque Independencia", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(5000);

            estacion.RetirarBici(tarjeta, out _);

            Assert.AreEqual(1, tarjeta.RetirosDeBici.Count);
            Assert.AreEqual(0, tarjeta.Boletos.Count, "El retiro de bici no es un Boleto y no debe agregarse a la colección de boletos.");
        }

        [Test]
        public void Aislamiento_RetiroDeBiciNoConsumeIntervaloDe5MinutosDeMedioBoleto()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var estacion = new EstacionBicicletas("Parque Independencia", reloj);
            var colectivo = new Colectivo("Linea 122", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.MedioBoleto };
            tarjeta.Cargar(5000);

            // 10:00: Retiro de bici
            estacion.RetirarBici(tarjeta, out _);

            // 10:01 (solo 1 minuto después, sin esperar los 5 minutos que exige el colectivo)
            reloj.Avanzar(TimeSpan.FromMinutes(1));
            bool viajeOk = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsTrue(viajeOk, "El viaje en colectivo con medio boleto debe permitirse porque el retiro de bici no cuenta como viaje previo de colectivo.");
            Assert.IsNotNull(boleto);
            Assert.AreEqual(Colectivo.TarifaBasica / 2, boleto!.TotalAbonado, "Debe cobrar medio boleto normalmente.");
        }

        [Test]
        public void Aislamiento_RetiroDeBiciNoConsumeTopeDiarioDeBeneficiosDeFranquicia()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var estacion = new EstacionBicicletas("Parque Independencia", reloj);
            var colectivo = new Colectivo("Linea 122", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.MedioBoleto };
            tarjeta.Cargar(10000);

            // 1. Retiro de bicicleta
            estacion.RetirarBici(tarjeta, out _);

            // 2. Viaje 1 en colectivo (con beneficio)
            reloj.Avanzar(TimeSpan.FromMinutes(10));
            bool v1Ok = colectivo.PagarCon(tarjeta, out var b1);
            Assert.IsTrue(v1Ok);
            Assert.AreEqual(Colectivo.TarifaBasica / 2, b1!.TotalAbonado);

            // 3. Viaje 2 en colectivo (con beneficio, segundo beneficio diario del colectivo)
            reloj.Avanzar(TimeSpan.FromMinutes(10));
            bool v2Ok = colectivo.PagarCon(tarjeta, out var b2);
            Assert.IsTrue(v2Ok);
            Assert.AreEqual(Colectivo.TarifaBasica / 2, b2!.TotalAbonado);

            // 4. Viaje 3 en colectivo (supera tope de 2 beneficios diarios -> tarifa completa)
            reloj.Avanzar(TimeSpan.FromMinutes(10));
            bool v3Ok = colectivo.PagarCon(tarjeta, out var b3);
            Assert.IsTrue(v3Ok);
            Assert.AreEqual(Colectivo.TarifaBasica, b3!.TotalAbonado, "El 3er viaje en colectivo paga tarifa completa porque el tope de 2 beneficios diarios es solo de colectivos.");
        }

        [Test]
        public void Aislamiento_RetiroDeBiciNoAfectaUsoFrecuenteDelColectivo()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var estacion = new EstacionBicicletas("Parque Independencia", reloj);
            var colectivo = new Colectivo("Linea 122", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(30000);

            // Hacemos 29 retiros de bicicleta (en días simulados o pases)
            for (int i = 0; i < 29; i++)
            {
                // simulamos retiro y devolución
                tarjeta.RetirosDeBici.Add(new RetiroDeBici(reloj.Ahora.AddDays(-i), 2418.75m, tarjeta, estacion));
            }

            // Primer viaje en colectivo del mes: debe cobrar tarifa básica completa (viaje 1 en colectivo, no viaje 30)
            bool ok = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsTrue(ok);
            Assert.IsNotNull(boleto);
            Assert.AreEqual(Colectivo.TarifaBasica, boleto!.TotalAbonado, "El primer viaje en colectivo no debe tener descuento de uso frecuente aunque haya retiros de bici en el historial.");
            Assert.AreEqual(1, tarjeta.Boletos.Count);
        }

        [Test]
        public void Aislamiento_ViajesEnColectivoNoOtorganPaseDeBicicleta()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var estacion = new EstacionBicicletas("Parque Independencia", reloj);
            var colectivo = new Colectivo("Linea 122", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(5000);

            // Viaje en colectivo
            colectivo.PagarCon(tarjeta, out _);

            // Luego intenta retirar bici: debe cobrar la tarifa diaria de bici completa, no está bonificada por viajar en colectivo
            bool ok = estacion.RetirarBici(tarjeta, out var retiro);

            Assert.IsTrue(ok);
            Assert.IsNotNull(retiro);
            Assert.AreEqual(EstacionBicicletas.TarifaDiaria, retiro!.TarifaCobrada);
        }
    }
}
