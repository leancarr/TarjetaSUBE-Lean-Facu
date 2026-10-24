using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Transporte.Tests
{
    // Clases falsas para probar el motor de reglas desacoplado de Colectivo
    public class ReglaQueRechazaFalsa : IReglaDeViaje
    {
        public int Orden => 15;
        public bool AplicaA(ContextoDeViaje c) => true;
        public bool Evaluar(ContextoDeViaje c) => false;
    }

    public class ReglaQueDuplicaTarifaFalsa : IReglaDeViaje
    {
        public int Orden => 25;
        public bool AplicaA(ContextoDeViaje c) => true;
        public bool Evaluar(ContextoDeViaje c)
        {
            c.Tarifa *= 2m;
            return true;
        }
    }

    public class ReglasDeViajeTest
    {
        [Test]
        public void BeneficioPorTipoDeTarjetaRegla_Normal_NoModificaTarifa()
        {
            var regla = new BeneficioPorTipoDeTarjetaRegla();
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            var colectivo = new Colectivo("Linea 122");
            var contexto = new ContextoDeViaje(tarjeta, colectivo, DateTime.Now, 1580m);

            Assert.AreEqual(40, regla.Orden);
            Assert.IsTrue(regla.AplicaA(contexto));

            bool resultado = regla.Evaluar(contexto);

            Assert.IsTrue(resultado);
            Assert.AreEqual(1580m, contexto.Tarifa);
        }

        [Test]
        public void BeneficioPorTipoDeTarjetaRegla_MedioBoleto_AplicaCincuentaPorCiento()
        {
            var regla = new BeneficioPorTipoDeTarjetaRegla();
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.MedioBoleto };
            var colectivo = new Colectivo("Linea 122");
            var contexto = new ContextoDeViaje(tarjeta, colectivo, DateTime.Now, 1580m);

            bool resultado = regla.Evaluar(contexto);

            Assert.IsTrue(resultado);
            Assert.AreEqual(790m, contexto.Tarifa);
        }

        [Test]
        public void BeneficioPorTipoDeTarjetaRegla_BoletoGratuito_AplicaTarifaCero()
        {
            var regla = new BeneficioPorTipoDeTarjetaRegla();
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.BoletoGratuito };
            var colectivo = new Colectivo("Linea 122");
            var contexto = new ContextoDeViaje(tarjeta, colectivo, DateTime.Now, 1580m);

            bool resultado = regla.Evaluar(contexto);

            Assert.IsTrue(resultado);
            Assert.AreEqual(0m, contexto.Tarifa);
        }

        [Test]
        public void BeneficioPorTipoDeTarjetaRegla_EsTrasbordo_NoAplica()
        {
            var regla = new BeneficioPorTipoDeTarjetaRegla();
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.MedioBoleto };
            var colectivo = new Colectivo("Linea 122");
            var contexto = new ContextoDeViaje(tarjeta, colectivo, DateTime.Now, 1580m)
            {
                EsTrasbordo = true
            };

            Assert.IsFalse(regla.AplicaA(contexto));
        }

        [Test]
        public void BeneficioPorTipoDeTarjetaRegla_Evaluar_TopeDiario()
        {
            var regla = new BeneficioPorTipoDeTarjetaRegla();
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.MedioBoleto };
            var colectivo = new Colectivo("Linea K");
            var fechaHoy = new DateTime(2026, 10, 12, 8, 0, 0); // Lunes 8:00 AM

            // Viaje 1 del día: sin boletos previos -> aplica 50%
            var ctx1 = new ContextoDeViaje(tarjeta, colectivo, fechaHoy, 1580m);
            Assert.IsTrue(regla.Evaluar(ctx1));
            Assert.AreEqual(790m, ctx1.Tarifa);
            tarjeta.Boletos.Add(new Boleto(fechaHoy, "Medio boleto estudiantil", "Linea K", 790m, 5000m, tarjeta.Id, tarjeta, colectivo));

            // Viaje 2 del día: 1 boleto previo hoy -> aplica 50%
            var ctx2 = new ContextoDeViaje(tarjeta, colectivo, fechaHoy.AddHours(2), 1580m);
            Assert.IsTrue(regla.Evaluar(ctx2));
            Assert.AreEqual(790m, ctx2.Tarifa);
            tarjeta.Boletos.Add(new Boleto(fechaHoy.AddHours(2), "Medio boleto estudiantil", "Linea K", 790m, 4210m, tarjeta.Id, tarjeta, colectivo));

            // Viaje 3 del día: ya tiene 2 boletos hoy -> tarifa completa (sin descuento)
            var ctx3 = new ContextoDeViaje(tarjeta, colectivo, fechaHoy.AddHours(4), 1580m);
            Assert.IsTrue(regla.Evaluar(ctx3));
            Assert.AreEqual(1580m, ctx3.Tarifa);
            tarjeta.Boletos.Add(new Boleto(fechaHoy.AddHours(4), "Medio boleto estudiantil", "Linea K", 1580m, 2630m, tarjeta.Id, tarjeta, colectivo));

            // Al día siguiente: vuelve a contar desde 0 para la fecha nueva
            var fechaManana = new DateTime(2026, 10, 13, 8, 0, 0);
            var ctx4 = new ContextoDeViaje(tarjeta, colectivo, fechaManana, 1580m);
            Assert.IsTrue(regla.Evaluar(ctx4));
            Assert.AreEqual(790m, ctx4.Tarifa);
        }

        [Test]
        public void IntervaloMinimoEntreViajesRegla_PropiedadesYAplicaA()
        {
            var regla = new IntervaloMinimoEntreViajesRegla();
            Assert.AreEqual(20, regla.Orden);

            var tarjetaNormal = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            var tarjetaMedio = new Tarjeta { TarjetaTipo = TiposDeTarjeta.MedioBoleto };
            var colectivo = new Colectivo("Linea K");

            var ctxNormal = new ContextoDeViaje(tarjetaNormal, colectivo, DateTime.Now, 1580m);
            var ctxMedio = new ContextoDeViaje(tarjetaMedio, colectivo, DateTime.Now, 1580m);

            Assert.IsFalse(regla.AplicaA(ctxNormal));
            Assert.IsTrue(regla.AplicaA(ctxMedio));
        }

        [Test]
        public void IntervaloMinimoEntreViajesRegla_Evaluar_ControlaIntervaloExacto()
        {
            var regla = new IntervaloMinimoEntreViajesRegla();
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.MedioBoleto };
            var colectivo = new Colectivo("Linea K");
            var fechaInicial = new DateTime(2026, 10, 12, 10, 0, 0);

            // Sin viajes previos -> permite
            var ctx1 = new ContextoDeViaje(tarjeta, colectivo, fechaInicial, 790m);
            Assert.IsTrue(regla.Evaluar(ctx1));

            // Simulamos que se emitió el primer boleto a las 10:00:00
            var boleto1 = new Boleto(fechaInicial, "Medio boleto estudiantil", "Linea K", 790m, 5000m, tarjeta.Id, tarjeta, colectivo);
            tarjeta.Boletos.Add(boleto1);

            // 4 minutos y 59 segundos después (10:04:59) -> rechazado
            var ctx2 = new ContextoDeViaje(tarjeta, colectivo, fechaInicial.AddMinutes(4).AddSeconds(59), 790m);
            Assert.IsFalse(regla.Evaluar(ctx2));

            // 5 minutos exactos después (10:05:00) -> aceptado (con 5:00 justos se puede)
            var ctx3 = new ContextoDeViaje(tarjeta, colectivo, fechaInicial.AddMinutes(5), 790m);
            Assert.IsTrue(regla.Evaluar(ctx3));
        }

        [Test]
        public void Colectivo_ReglaQueRechaza_CancelaViajeYNoModificaSaldo()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var reglas = new List<IReglaDeViaje> { new ReglaQueRechazaFalsa() };
            var colectivo = new Colectivo("Linea K", reloj, reglas);
            var tarjeta = new Tarjeta();
            tarjeta.Cargar(5000);

            bool resultado = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsFalse(resultado);
            Assert.IsNull(boleto);
            Assert.AreEqual(5000m, tarjeta.Saldo);
            Assert.IsEmpty(tarjeta.Boletos);
        }

        [Test]
        public void Colectivo_ReglaQueDuplicaTarifa_AplicaTarifaModificada()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var reglas = new List<IReglaDeViaje> { new ReglaQueDuplicaTarifaFalsa() };
            var colectivo = new Colectivo("Linea K", reloj, reglas);
            var tarjeta = new Tarjeta();
            tarjeta.Cargar(5000);

            bool resultado = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsTrue(resultado);
            Assert.IsNotNull(boleto);
            Assert.AreEqual(3160m, boleto!.TotalAbonado); // 1580 * 2
            Assert.AreEqual(5000m - 3160m, tarjeta.Saldo);
            Assert.AreEqual(1, tarjeta.Boletos.Count);
            Assert.AreSame(boleto, tarjeta.Boletos[0]);
        }

        [Test]
        public void Colectivo_MedioBoleto_SegundoViajeMenosDeCincoMinutosRechazado_CincoMinutosAceptado()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0)); // Lunes 10:00 AM (entre 6 y 22)
            var colectivo = new Colectivo("K", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.MedioBoleto };
            tarjeta.Cargar(10000);

            // 1er viaje: exitoso
            Assert.IsTrue(colectivo.PagarCon(tarjeta, out var b1));
            Assert.IsNotNull(b1);
            Assert.AreEqual(10000m - 790m, tarjeta.Saldo);

            // 4 minutos después: rechazado, sin boleto, saldo intacto
            reloj.Avanzar(TimeSpan.FromMinutes(4));
            Assert.IsFalse(colectivo.PagarCon(tarjeta, out var b2));
            Assert.IsNull(b2);
            Assert.AreEqual(10000m - 790m, tarjeta.Saldo);
            Assert.AreEqual(1, tarjeta.Boletos.Count);

            // 1 minuto más (5:00 exactos desde el primero): aceptado
            reloj.Avanzar(TimeSpan.FromMinutes(1));
            Assert.IsTrue(colectivo.PagarCon(tarjeta, out var b3));
            Assert.IsNotNull(b3);
            Assert.AreEqual(10000m - 790m - 790m, tarjeta.Saldo);
            Assert.AreEqual(2, tarjeta.Boletos.Count);
        }

        [Test]
        public void Colectivo_MedioBoleto_TopeDosViajesConDescuentoPorDia_TerceroTarifaCompletaYSiguienteDiaVuelveDescuento()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 8, 0, 0)); // Lunes 8:00 AM (entre 6 y 22)
            var colectivo = new Colectivo("Linea 122", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.MedioBoleto };
            tarjeta.Cargar(10000);

            // Viaje 1 (08:00): 1er viaje con descuento (paga $790)
            Assert.IsTrue(colectivo.PagarCon(tarjeta, out var b1));
            Assert.AreEqual(790m, b1!.TotalAbonado);
            Assert.AreEqual(9210m, tarjeta.Saldo);

            // Viaje 2 (12:00, respetando el intervalo de 5 min): 2do viaje con descuento (paga $790)
            reloj.Avanzar(TimeSpan.FromHours(4));
            Assert.IsTrue(colectivo.PagarCon(tarjeta, out var b2));
            Assert.AreEqual(790m, b2!.TotalAbonado);
            Assert.AreEqual(8420m, tarjeta.Saldo);

            // Viaje 3 (16:00, mismo día): 3er viaje, ya agotó los 2 con descuento -> paga tarifa completa ($1580)
            reloj.Avanzar(TimeSpan.FromHours(4));
            Assert.IsTrue(colectivo.PagarCon(tarjeta, out var b3));
            Assert.AreEqual(1580m, b3!.TotalAbonado);
            Assert.AreEqual(8420m - 1580m, tarjeta.Saldo);

            // Viaje 4 (martes 13/10 a las 08:00 AM, siguiente día entre 6 y 22): vuelve a aplicar el descuento ($790)
            reloj.Avanzar(TimeSpan.FromHours(16)); // Pasa a las 08:00 del día siguiente
            Assert.AreEqual(DayOfWeek.Tuesday, reloj.Ahora.DayOfWeek);
            Assert.AreEqual(8, reloj.Ahora.Hour);

            Assert.IsTrue(colectivo.PagarCon(tarjeta, out var b4));
            Assert.AreEqual(790m, b4!.TotalAbonado);
            Assert.AreEqual(8420m - 1580m - 790m, tarjeta.Saldo);
        }
    }
}
