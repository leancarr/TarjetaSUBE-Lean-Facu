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
            var retiro = new RetiroDeBici(new DateTime(2026, 10, 12, 10, 0, 0), 2418.75m, tarjeta, estacion);

            retiro.Id = 99;
            retiro.TarjetaId = 77;
            retiro.EstacionId = 88;

            Assert.AreEqual(99, retiro.Id);
            Assert.AreEqual(77, retiro.TarjetaId);
            Assert.AreEqual(88, retiro.EstacionId);
            Assert.AreEqual(tarjeta, retiro.Tarjeta);
            Assert.AreEqual(estacion, retiro.Estacion);

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
        public void DbContext_MapeaEstacionBicicletasYRetiroDeBici()
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
        }
    }
}
