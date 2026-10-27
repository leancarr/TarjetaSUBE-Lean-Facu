using System;
using NUnit.Framework;

namespace Transporte.Tests
{
    public class FranjaHorariaFranquiciaReglaTest
    {
        private Tarjeta ObtenerTarjeta(string tipo)
        {
            return tipo switch
            {
                "MedioBoleto" => new Tarjeta { TarjetaTipo = TiposDeTarjeta.MedioBoleto },
                "BoletoGratuito" => new Tarjeta { TarjetaTipo = TiposDeTarjeta.BoletoGratuito },
                "FranquiciaCompleta" => new Tarjeta { TarjetaTipo = TiposDeTarjeta.FranquiciaCompleta },
                _ => new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal }
            };
        }

        [TestCase("MedioBoleto", 2026, 10, 17, 10, 0, false, Description = "Sábado 10:00")]
        [TestCase("BoletoGratuito", 2026, 10, 17, 10, 0, false, Description = "Sábado 10:00")]
        [TestCase("FranquiciaCompleta", 2026, 10, 17, 10, 0, false, Description = "Sábado 10:00")]
        [TestCase("MedioBoleto", 2026, 10, 18, 10, 0, false, Description = "Domingo 10:00")]
        [TestCase("BoletoGratuito", 2026, 10, 18, 10, 0, false, Description = "Domingo 10:00")]
        [TestCase("FranquiciaCompleta", 2026, 10, 18, 10, 0, false, Description = "Domingo 10:00")]
        [TestCase("MedioBoleto", 2026, 10, 12, 5, 59, false, Description = "Lunes 05:59")]
        [TestCase("BoletoGratuito", 2026, 10, 12, 5, 59, false, Description = "Lunes 05:59")]
        [TestCase("FranquiciaCompleta", 2026, 10, 12, 5, 59, false, Description = "Lunes 05:59")]
        [TestCase("MedioBoleto", 2026, 10, 12, 6, 0, true, Description = "Lunes 06:00")]
        [TestCase("BoletoGratuito", 2026, 10, 12, 6, 0, true, Description = "Lunes 06:00")]
        [TestCase("FranquiciaCompleta", 2026, 10, 12, 6, 0, true, Description = "Lunes 06:00")]
        [TestCase("MedioBoleto", 2026, 10, 16, 21, 59, true, Description = "Viernes 21:59")]
        [TestCase("BoletoGratuito", 2026, 10, 16, 21, 59, true, Description = "Viernes 21:59")]
        [TestCase("FranquiciaCompleta", 2026, 10, 16, 21, 59, true, Description = "Viernes 21:59")]
        [TestCase("MedioBoleto", 2026, 10, 16, 22, 0, false, Description = "Viernes 22:00")]
        [TestCase("BoletoGratuito", 2026, 10, 16, 22, 0, false, Description = "Viernes 22:00")]
        [TestCase("FranquiciaCompleta", 2026, 10, 16, 22, 0, false, Description = "Viernes 22:00")]
        public void Franquicias_EvaluacionFranjaHoraria_CumpleRestricciones(string tipo, int anio, int mes, int dia, int hora, int minuto, bool esperado)
        {
            var fecha = new DateTime(anio, mes, dia, hora, minuto, 0);
            var reloj = new ProveedorDeFechaFalso(fecha);
            var colectivo = new Colectivo("Linea 122", reloj);
            var tarjeta = ObtenerTarjeta(tipo);
            tarjeta.Cargar(5000);

            bool resultado = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.AreEqual(esperado, resultado);

            if (!esperado)
            {
                Assert.IsNull(boleto);
                Assert.AreEqual(5000m, tarjeta.Saldo);
                Assert.IsEmpty(tarjeta.Boletos);
            }
            else
            {
                Assert.IsNotNull(boleto);
            }
        }

        [Test]
        public void TarjetaNormal_PagaDeNocheYFinesDeSemana()
        {
            // Sábado a las 23:30 hs
            var fechaSabadoNoche = new DateTime(2026, 10, 17, 23, 30, 0);
            var reloj = new ProveedorDeFechaFalso(fechaSabadoNoche);
            var colectivo = new Colectivo("Linea 122", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(5000);

            bool resultado = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsTrue(resultado);
            Assert.IsNotNull(boleto);
            Assert.AreEqual(Colectivo.TarifaBasica, boleto!.TotalAbonado);
            Assert.AreEqual(5000m - Colectivo.TarifaBasica, tarjeta.Saldo);
        }

        [Test]
        public void FranjaHorariaFranquiciaRegla_Propiedades()
        {
            var regla = new FranjaHorariaFranquiciaRegla();
            Assert.AreEqual(10, regla.Orden);
            Assert.AreEqual(6, FranjaHorariaFranquiciaRegla.HoraInicio);
            Assert.AreEqual(22, FranjaHorariaFranquiciaRegla.HoraFin);
        }
    }
}
