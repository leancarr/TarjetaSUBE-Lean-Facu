using System;
using NUnit.Framework;

namespace Transporte.Tests
{
    public class UsoFrecuenteReglaTest
    {
        [Test]
        public void UsoFrecuenteRegla_PropiedadesYAplicaA()
        {
            var regla = new UsoFrecuenteRegla();
            Assert.AreEqual(50, regla.Orden);

            var tarjetaNormal = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            var tarjetaMedio = new Tarjeta { TarjetaTipo = TiposDeTarjeta.MedioBoleto };
            var tarjetaJubilado = new Tarjeta { TarjetaTipo = TiposDeTarjeta.FranquiciaCompleta };
            var colectivo = new Colectivo("Linea K");

            var ctxNormal = new ContextoDeViaje(tarjetaNormal, colectivo, DateTime.Now, Colectivo.TarifaBasica);
            var ctxMedio = new ContextoDeViaje(tarjetaMedio, colectivo, DateTime.Now, Colectivo.TarifaBasica);
            var ctxJubilado = new ContextoDeViaje(tarjetaJubilado, colectivo, DateTime.Now, Colectivo.TarifaBasica);
            var ctxTrasbordo = new ContextoDeViaje(tarjetaNormal, colectivo, DateTime.Now, Colectivo.TarifaBasica) { EsTrasbordo = true };

            Assert.IsTrue(regla.AplicaA(ctxNormal));
            Assert.IsFalse(regla.AplicaA(ctxMedio));
            Assert.IsFalse(regla.AplicaA(ctxJubilado));
            Assert.IsFalse(regla.AplicaA(ctxTrasbordo));
        }

        [Test]
        public void PagarCon_Viaje29_CobraTarifaNormal()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 20, 9, 0, 0));
            var colectivo = new Colectivo("Linea 122", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(30000);

            // 28 viajes previos en el mes -> este es el viaje 29
            HistorialDePrueba.AgregarViajes(tarjeta, 28, new DateTime(2026, 10, 1, 8, 0, 0));

            bool ok = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsTrue(ok);
            Assert.IsNotNull(boleto);
            Assert.AreEqual(Colectivo.TarifaBasica, boleto!.TotalAbonado);
        }

        [Test]
        public void PagarCon_Viaje30_AplicaVeintePorCientoDeDescuento()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 20, 9, 0, 0));
            var colectivo = new Colectivo("Linea 122", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(30000);

            // 29 viajes previos en el mes -> este es el viaje 30 (20% descuento)
            HistorialDePrueba.AgregarViajes(tarjeta, 29, new DateTime(2026, 10, 1, 8, 0, 0));

            bool ok = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsTrue(ok);
            Assert.IsNotNull(boleto);
            decimal tarifaEsperada = Colectivo.TarifaBasica * 0.80m;
            Assert.AreEqual(tarifaEsperada, boleto!.TotalAbonado);
        }

        [Test]
        public void PagarCon_Viaje79_AplicaVeintePorCientoDeDescuento()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 20, 9, 0, 0));
            var colectivo = new Colectivo("Linea 122", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(30000);

            // 78 viajes previos en el mes -> este es el viaje 79 (20% descuento)
            HistorialDePrueba.AgregarViajes(tarjeta, 78, new DateTime(2026, 10, 1, 8, 0, 0));

            bool ok = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsTrue(ok);
            Assert.IsNotNull(boleto);
            decimal tarifaEsperada = Colectivo.TarifaBasica * 0.80m;
            Assert.AreEqual(tarifaEsperada, boleto!.TotalAbonado);
        }

        [Test]
        public void PagarCon_Viaje80_AplicaVeinticincoPorCientoDeDescuento()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 20, 9, 0, 0));
            var colectivo = new Colectivo("Linea 122", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(30000);

            // 79 viajes previos en el mes -> este es el viaje 80 (25% descuento)
            HistorialDePrueba.AgregarViajes(tarjeta, 79, new DateTime(2026, 10, 1, 8, 0, 0));

            bool ok = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsTrue(ok);
            Assert.IsNotNull(boleto);
            decimal tarifaEsperada = Colectivo.TarifaBasica * 0.75m;
            Assert.AreEqual(tarifaEsperada, boleto!.TotalAbonado);
        }

        [Test]
        public void PagarCon_ViajesDeMesesAnteriores_NoSeCuentanParaUsoFrecuente()
        {
            // Fecha actual: Octubre 2026
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 5, 9, 0, 0));
            var colectivo = new Colectivo("Linea 122", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(30000);

            // 50 viajes realizados en Septiembre 2026
            HistorialDePrueba.AgregarViajes(tarjeta, 50, new DateTime(2026, 9, 15, 8, 0, 0));

            // Viaje en Octubre -> es el 1er viaje de Octubre -> tarifa normal
            bool ok = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsTrue(ok);
            Assert.IsNotNull(boleto);
            Assert.AreEqual(Colectivo.TarifaBasica, boleto!.TotalAbonado);
        }

        [Test]
        public void PagarCon_MedioBoleto_NoAplicaDescuentoUsoFrecuente()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 20, 9, 0, 0));
            var colectivo = new Colectivo("Linea 122", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.MedioBoleto };
            tarjeta.Cargar(30000);

            // 50 viajes previos en el mes
            HistorialDePrueba.AgregarViajes(tarjeta, 50, new DateTime(2026, 10, 1, 8, 0, 0));

            bool ok = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsTrue(ok);
            Assert.IsNotNull(boleto);
            // El medio boleto paga la mitad de la tarifa básica, sin acumular 20% adicional de uso frecuente
            Assert.AreEqual(Colectivo.TarifaBasica / 2m, boleto!.TotalAbonado);
        }
    }
}
