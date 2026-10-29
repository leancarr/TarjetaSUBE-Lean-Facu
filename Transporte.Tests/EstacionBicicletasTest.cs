using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Transporte.Tests
{
    public class EstacionBicicletasTest
    {
        private class RetiroDeBiciParaEF : RetiroDeBici
        {
            public RetiroDeBiciParaEF() : base() { }
        }

        private static IEnumerable<TarjetaTipo> TiposDeTarjetaParaPrueba()
        {
            yield return TiposDeTarjeta.Normal;
            yield return TiposDeTarjeta.MedioBoleto;
            yield return TiposDeTarjeta.BoletoGratuito;
            yield return TiposDeTarjeta.FranquiciaCompleta;
        }

        [Test]
        public void EstacionBicicletas_ConstructoresYPropiedades()
        {
            Assert.AreEqual(2418.75m, EstacionBicicletas.TarifaDiaria);
            Assert.AreEqual(1250m, EstacionBicicletas.MultaPorDemora);
            Assert.AreEqual(TimeSpan.FromHours(1), EstacionBicicletas.TiempoMaximoDeUso);

            var e1 = new EstacionBicicletas();
            Assert.AreEqual(string.Empty, e1.Nombre);

            var e2 = new EstacionBicicletas("Plaza San Martín");
            Assert.AreEqual("Plaza San Martín", e2.Nombre);

            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var e3 = new EstacionBicicletas("Parque Independencia", reloj);
            Assert.AreEqual("Parque Independencia", e3.Nombre);

            Assert.Throws<ArgumentNullException>(() => new EstacionBicicletas("Error", null!));
            Assert.Throws<ArgumentNullException>(() => e3.RetirarBici(null!, out _));
            Assert.Throws<ArgumentNullException>(() => e3.DevolverBici(null!, out _));
        }

        [Test]
        public void RetiroDeBici_PropiedadesYConstructores()
        {
            var ef = new RetiroDeBiciParaEF();
            Assert.IsNotNull(ef);

            var tarjeta = new Tarjeta { Id = 7 };
            var estacion = new EstacionBicicletas("Estación 1") { Id = 12 };
            var retiro = new RetiroDeBici(new DateTime(2026, 10, 12, 10, 0, 0), 2418.75m, tarjeta, estacion, 1250m);

            retiro.Id = 99;
            retiro.TarjetaId = 77;
            retiro.EstacionId = 88;

            Assert.AreEqual(99, retiro.Id);
            Assert.AreEqual(77, retiro.TarjetaId);
            Assert.AreEqual(88, retiro.EstacionId);
            Assert.AreEqual(tarjeta, retiro.Tarjeta);
            Assert.AreEqual(estacion, retiro.Estacion);
            Assert.AreEqual(1250m, retiro.MultaCobrada);
            Assert.IsFalse(retiro.MultaSaldada);

            retiro.SaldarMulta();
            Assert.IsTrue(retiro.MultaSaldada);

            Assert.Throws<ArgumentNullException>(() => new RetiroDeBici(DateTime.Now, 2418.75m, null!, estacion));
            Assert.Throws<ArgumentNullException>(() => new RetiroDeBici(DateTime.Now, 2418.75m, tarjeta, null!));
        }

        [Test]
        public void RetirarBici_Exitoso_CobraTarifaDiariaYDescuentaSaldoExacto()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var estacion = new EstacionBicicletas("Parque Independencia", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(5000);

            bool ok = estacion.RetirarBici(tarjeta, out var retiro);

            Assert.IsTrue(ok);
            Assert.IsNotNull(retiro);
            Assert.AreEqual(EstacionBicicletas.TarifaDiaria, retiro!.TarifaCobrada);
            Assert.AreEqual(0m, retiro.MultaCobrada);
            Assert.AreEqual(5000m - EstacionBicicletas.TarifaDiaria, tarjeta.Saldo); // 2581.25
            Assert.AreEqual(reloj.Ahora, retiro.FechaRetiro);
            Assert.IsNull(retiro.FechaDevolucion);
            Assert.IsTrue(retiro.EstaEnCurso);
            Assert.IsFalse(retiro.TuvoDemora);
            Assert.AreEqual(1, tarjeta.RetirosDeBici.Count);
        }

        [TestCase(0)]
        [TestCase(2000)]
        public void RetirarBici_SaldoInsuficiente_DevuelveFalseYSaldoIntacto(decimal saldoInicial)
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var estacion = new EstacionBicicletas("Parque Independencia", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };

            if (saldoInicial > 0)
            {
                tarjeta.Cargar(saldoInicial);
            }

            bool ok = estacion.RetirarBici(tarjeta, out var retiro);

            Assert.IsFalse(ok);
            Assert.IsNull(retiro);
            Assert.AreEqual(saldoInicial, tarjeta.Saldo);
            Assert.IsEmpty(tarjeta.RetirosDeBici);
        }

        [TestCaseSource(nameof(TiposDeTarjetaParaPrueba))]
        public void RetirarBici_CualquierTarjetaPagaTarifaCompleta_InclusoDomingoNoche(TarjetaTipo tipo)
        {
            // Domingo a las 23:00 (fuera de horario comercial y de colectivos)
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 18, 23, 0, 0));
            var estacion = new EstacionBicicletas("Pellegrini", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = tipo };
            tarjeta.Cargar(5000);

            bool ok = estacion.RetirarBici(tarjeta, out var retiro);

            Assert.IsTrue(ok);
            Assert.IsNotNull(retiro);
            // No hay descuentos por franquicia para bicicletas públicas
            Assert.AreEqual(EstacionBicicletas.TarifaDiaria, retiro!.TarifaCobrada);
            Assert.AreEqual(5000m - EstacionBicicletas.TarifaDiaria, tarjeta.Saldo);
        }

        [Test]
        public void PaseDelDia_SegundoRetiroMismoDiaNoCobra_DiaSiguienteVuelveACobrar()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 8, 0, 0));
            var estacionA = new EstacionBicicletas("Estación A", reloj);
            var estacionB = new EstacionBicicletas("Estación B", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(10000);

            // 1. Primer retiro del día (08:00) -> cobra TarifaDiaria
            bool ok1 = estacionA.RetirarBici(tarjeta, out var retiro1);
            Assert.IsTrue(ok1);
            Assert.AreEqual(EstacionBicicletas.TarifaDiaria, retiro1!.TarifaCobrada);
            Assert.AreEqual(10000m - EstacionBicicletas.TarifaDiaria, tarjeta.Saldo);

            // Devolución a los 30 min (08:30)
            reloj.Avanzar(TimeSpan.FromMinutes(30));
            bool dev1 = estacionB.DevolverBici(tarjeta, out var devuelto1);
            Assert.IsTrue(dev1);
            Assert.IsFalse(devuelto1!.EstaEnCurso);

            // 2. Segundo retiro del MISMO día (14:00) -> pase diario activo, no cobra ($0)
            reloj.Avanzar(TimeSpan.FromHours(5).Add(TimeSpan.FromMinutes(30)));
            bool ok2 = estacionB.RetirarBici(tarjeta, out var retiro2);
            Assert.IsTrue(ok2);
            Assert.AreEqual(0m, retiro2!.TarifaCobrada);
            Assert.AreEqual(10000m - EstacionBicicletas.TarifaDiaria, tarjeta.Saldo); // Saldo no cambia

            // Devolución a los 20 min
            reloj.Avanzar(TimeSpan.FromMinutes(20));
            estacionA.DevolverBici(tarjeta, out _);

            // 3. Retiro al DÍA SIGUIENTE (Martes 13 de Octubre a las 09:00) -> vuelve a cobrar TarifaDiaria
            reloj.Avanzar(TimeSpan.FromHours(18).Add(TimeSpan.FromMinutes(40)));
            Assert.AreEqual(new DateTime(2026, 10, 13, 9, 0, 0), reloj.Ahora);

            bool ok3 = estacionA.RetirarBici(tarjeta, out var retiro3);
            Assert.IsTrue(ok3);
            Assert.AreEqual(EstacionBicicletas.TarifaDiaria, retiro3!.TarifaCobrada);
            Assert.AreEqual(10000m - (2 * EstacionBicicletas.TarifaDiaria), tarjeta.Saldo);
        }

        [Test]
        public void RetirarBici_UnaBiciPorTarjeta_RechazaSegundoRetiroSiHayUnoEnCurso()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var estacion = new EstacionBicicletas("Parque España", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(10000);

            // 1er retiro exitoso
            bool ok1 = estacion.RetirarBici(tarjeta, out var retiro1);
            Assert.IsTrue(ok1);
            Assert.IsTrue(retiro1!.EstaEnCurso);

            // Intento de 2do retiro sin haber devuelto la primera -> rechazado
            bool ok2 = estacion.RetirarBici(tarjeta, out var retiro2);
            Assert.IsFalse(ok2);
            Assert.IsNull(retiro2);
            Assert.AreEqual(10000m - EstacionBicicletas.TarifaDiaria, tarjeta.Saldo);
            Assert.AreEqual(1, tarjeta.RetirosDeBici.Count);
        }

        [Test]
        public void DevolverBici_SinRetiroEnCurso_DevuelveFalse()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var estacion = new EstacionBicicletas("Parque Urquiza", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(5000);

            bool ok = estacion.DevolverBici(tarjeta, out var retiro);

            Assert.IsFalse(ok);
            Assert.IsNull(retiro);
        }

        [Test]
        public void DevolverBici_ControlDeDemora_59Min59Seg_60Min_60Min01Seg()
        {
            // 1. Devolución a 59:59 -> TuvoDemora == false
            var r1 = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var e1 = new EstacionBicicletas("Estación 1", r1);
            var t1 = new Tarjeta(); t1.Cargar(5000);
            e1.RetirarBici(t1, out var ret1);
            r1.Avanzar(TimeSpan.FromMinutes(59).Add(TimeSpan.FromSeconds(59)));
            e1.DevolverBici(t1, out var dev1);
            Assert.IsFalse(dev1!.TuvoDemora, "A los 59:59 no tuvo demora");

            // 2. Devolución a 60:00 (exacto) -> TuvoDemora == false (duración no es mayor al máximo)
            var r2 = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var e2 = new EstacionBicicletas("Estación 2", r2);
            var t2 = new Tarjeta(); t2.Cargar(5000);
            e2.RetirarBici(t2, out var ret2);
            r2.Avanzar(TimeSpan.FromHours(1)); // 60:00 exactos
            e2.DevolverBici(t2, out var dev2);
            Assert.IsFalse(dev2!.TuvoDemora, "A los 60:00 exactos no tuvo demora");

            // 3. Devolución a 60:01 -> TuvoDemora == true (duración mayor al máximo)
            var r3 = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var e3 = new EstacionBicicletas("Estación 3", r3);
            var t3 = new Tarjeta(); t3.Cargar(5000);
            e3.RetirarBici(t3, out var ret3);
            r3.Avanzar(TimeSpan.FromHours(1).Add(TimeSpan.FromSeconds(1))); // 60:01
            e3.DevolverBici(t3, out var dev3);
            Assert.IsTrue(dev3!.TuvoDemora, "A los 60:01 sí tuvo demora");
        }

        [Test]
        public void TieneSaldoPara_BordeExacto()
        {
            var tarjeta = new Tarjeta();

            // Saldo inicial 0
            Assert.IsFalse(tarjeta.TieneSaldoPara(2418.75m));

            // Cargamos 5000 y descontamos hasta dejar exactamente 2418.75m
            tarjeta.Cargar(5000);
            tarjeta.Descontar(5000m - 2418.75m);
            Assert.AreEqual(2418.75m, tarjeta.Saldo);

            // Borde exacto: Saldo == monto -> true
            Assert.IsTrue(tarjeta.TieneSaldoPara(2418.75m));

            // Saldo < monto -> false
            Assert.IsFalse(tarjeta.TieneSaldoPara(2418.76m));

            // Saldo > monto -> true
            Assert.IsTrue(tarjeta.TieneSaldoPara(2418.74m));
        }

        [Test]
        public void Multas_UnaMultaAcumulada_CobraTarifaMasMultaEnProximoPago()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var estacion = new EstacionBicicletas("Parque Independencia", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(10000);

            // Día 1: Retiro y pago del día ($2418.75)
            estacion.RetirarBici(tarjeta, out _);
            Assert.AreEqual(10000m - EstacionBicicletas.TarifaDiaria, tarjeta.Saldo);

            // Devolución tardía (90 min > 1 h) -> genera multa de $1250
            reloj.Avanzar(TimeSpan.FromMinutes(90));
            estacion.DevolverBici(tarjeta, out var r1);
            Assert.IsTrue(r1!.TuvoDemora);
            Assert.IsFalse(r1.MultaSaldada);
            Assert.AreEqual(1250m, estacion.MultaPendiente(tarjeta));

            // Día 2: Próximo retiro con cobro -> cobra TarifaDiaria + MultaPorDemora = 3668.75
            reloj.Avanzar(TimeSpan.FromDays(1));
            bool ok = estacion.RetirarBici(tarjeta, out var r2);

            Assert.IsTrue(ok);
            Assert.IsNotNull(r2);
            Assert.AreEqual(EstacionBicicletas.TarifaDiaria, r2!.TarifaCobrada);
            Assert.AreEqual(EstacionBicicletas.MultaPorDemora, r2.MultaCobrada);
            Assert.IsTrue(r1.MultaSaldada, "La multa previa debe quedar marcada como saldada");
            Assert.AreEqual(0m, estacion.MultaPendiente(tarjeta));
            // 10000 - 2418.75 - (2418.75 + 1250) = 3912.50
            Assert.AreEqual(10000m - EstacionBicicletas.TarifaDiaria - (EstacionBicicletas.TarifaDiaria + EstacionBicicletas.MultaPorDemora), tarjeta.Saldo);
        }

        [Test]
        public void Multas_VariasMultasAcumuladasMismoDia_PasanDeUnDiaAOtroYSeCobranJuntas()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 8, 0, 0));
            var estacion = new EstacionBicicletas("Parque Independencia", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(15000);

            // Día 1 (08:00): Retiro 1 (paga 2418.75)
            estacion.RetirarBici(tarjeta, out _);
            reloj.Avanzar(TimeSpan.FromMinutes(90)); // 09:30 -> demora
            estacion.DevolverBici(tarjeta, out var r1);
            Assert.IsTrue(r1!.TuvoDemora);
            Assert.AreEqual(1250m, estacion.MultaPendiente(tarjeta));

            // Día 1 (14:00): Retiro 2 del mismo día (gratis por pase diario)
            reloj.Avanzar(TimeSpan.FromHours(4).Add(TimeSpan.FromMinutes(30)));
            estacion.RetirarBici(tarjeta, out var r2);
            Assert.AreEqual(0m, r2!.TarifaCobrada);
            Assert.AreEqual(0m, r2.MultaCobrada, "Un 2do retiro el mismo día no cobra multas");

            // Devolución tardía también (100 min) -> 2da multa acumulada
            reloj.Avanzar(TimeSpan.FromMinutes(100));
            estacion.DevolverBici(tarjeta, out var r2Dev);
            Assert.IsTrue(r2Dev!.TuvoDemora);
            Assert.AreEqual(2500m, estacion.MultaPendiente(tarjeta)); // 2 multas de 1250

            // Día 2 (reloj avanza 1 día): Próximo retiro con cobro -> cobra Tarifa ($2418.75) + 2 Multas ($2500) = $4918.75
            reloj.Avanzar(TimeSpan.FromDays(1));
            bool ok = estacion.RetirarBici(tarjeta, out var r3);

            Assert.IsTrue(ok);
            Assert.IsNotNull(r3);
            Assert.AreEqual(EstacionBicicletas.TarifaDiaria, r3!.TarifaCobrada);
            Assert.AreEqual(2500m, r3.MultaCobrada);
            Assert.AreEqual(0m, estacion.MultaPendiente(tarjeta));
            Assert.IsTrue(r1.MultaSaldada);
            Assert.IsTrue(r2Dev.MultaSaldada);
            // Saldo esperado: 15000 - 2418.75 - (2418.75 + 2500) = 7662.50
            Assert.AreEqual(15000m - EstacionBicicletas.TarifaDiaria - (EstacionBicicletas.TarifaDiaria + 2500m), tarjeta.Saldo);
        }

        [Test]
        public void Multas_SaldoAlcanzaParaTarifaPeroNoParaTarifaMasMultas_RechazaYMultasSiguenPendientes()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var estacion = new EstacionBicicletas("Parque Independencia", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(5000);

            // Día 1: Retiro y demora -> multa pendiente = 1250
            estacion.RetirarBici(tarjeta, out _);
            reloj.Avanzar(TimeSpan.FromMinutes(90));
            estacion.DevolverBici(tarjeta, out var r1);
            Assert.IsTrue(r1!.TuvoDemora);
            Assert.AreEqual(1250m, estacion.MultaPendiente(tarjeta));

            // Saldo actual: 5000 - 2418.75 = 2581.25.
            // Para el día 2 se requiere: Tarifa (2418.75) + Multa (1250) = 3668.75.
            // 2581.25 alcanza para la tarifa sola (2581.25 >= 2418.75), pero NO para tarifa + multa (2581.25 < 3668.75).
            reloj.Avanzar(TimeSpan.FromDays(1));
            decimal saldoAntes = tarjeta.Saldo;
            bool ok = estacion.RetirarBici(tarjeta, out var r2);

            Assert.IsFalse(ok, "Debe rechazar porque no alcanza para tarifa + multa");
            Assert.IsNull(r2);
            Assert.AreEqual(saldoAntes, tarjeta.Saldo, "Saldo debe quedar intacto");
            Assert.IsFalse(r1.MultaSaldada, "Multa debe seguir sin saldar");
            Assert.AreEqual(1250m, estacion.MultaPendiente(tarjeta), "Multa pendiente debe seguir intacta");
        }

        [Test]
        public void Multas_SegundoRetiroMismoDia_NoCobraMultas_MultasSiguenPendientes()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var estacion = new EstacionBicicletas("Parque Independencia", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(10000);

            // Retiro 1 a las 10:00 -> paga el día
            estacion.RetirarBici(tarjeta, out _);
            // Devolución con demora a las 11:30 (90 min)
            reloj.Avanzar(TimeSpan.FromMinutes(90));
            estacion.DevolverBici(tarjeta, out _);
            Assert.AreEqual(1250m, estacion.MultaPendiente(tarjeta));

            // Retiro 2 a las 14:00 (mismo día calendario) -> el día ya está pago
            decimal saldoAntes = tarjeta.Saldo;
            reloj.Avanzar(TimeSpan.FromHours(2).Add(TimeSpan.FromMinutes(30)));
            bool ok2 = estacion.RetirarBici(tarjeta, out var r2);

            Assert.IsTrue(ok2);
            Assert.IsNotNull(r2);
            Assert.AreEqual(0m, r2!.TarifaCobrada);
            Assert.AreEqual(0m, r2.MultaCobrada);
            Assert.AreEqual(saldoAntes, tarjeta.Saldo, "No se descuenta saldo en retiros de día ya pago");
            Assert.AreEqual(1250m, estacion.MultaPendiente(tarjeta), "Las multas pendientes continúan acumuladas para el próximo pago");
        }

        [Test]
        public void DbContext_MapeaEstacionBicicletasYRetiroDeBiciConMultas()
        {
            var options = new DbContextOptionsBuilder<TransporteDbContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;

            using var context = new TransporteDbContext(options);
            var entityEstacion = context.Model.FindEntityType(typeof(EstacionBicicletas));
            var entityRetiro = context.Model.FindEntityType(typeof(RetiroDeBici));

            Assert.IsNotNull(entityEstacion);
            Assert.IsNotNull(entityRetiro);
            Assert.IsNotNull(entityRetiro!.FindProperty("FechaRetiro"));
            Assert.IsNotNull(entityRetiro.FindProperty("FechaDevolucion"));
            Assert.IsNotNull(entityRetiro.FindProperty("TarifaCobrada"));
            Assert.IsNotNull(entityRetiro.FindProperty("TuvoDemora"));
            Assert.IsNotNull(entityRetiro.FindProperty("MultaCobrada"));
            Assert.IsNotNull(entityRetiro.FindProperty("MultaSaldada"));
        }
    }
}
