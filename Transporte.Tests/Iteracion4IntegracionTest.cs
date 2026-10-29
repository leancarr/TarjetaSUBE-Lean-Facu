using System;
using System.Linq;
using NUnit.Framework;

namespace Transporte.Tests
{
    public class Iteracion4IntegracionTest
    {
        [Test]
        public void ReglasDeViaje_OrdenFijo_Franja10_Intervalo20_Trasbordo30_Beneficio40_UsoFrecuente50()
        {
            var ordenes = ReglasDeViaje.PorDefecto().OrderBy(r => r.Orden).Select(r => r.Orden).ToArray();
            CollectionAssert.AreEqual(new[] { 10, 20, 30, 40, 50 }, ordenes);

            var reglas = ReglasDeViaje.PorDefecto().OrderBy(r => r.Orden).ToList();
            Assert.IsInstanceOf<FranjaHorariaFranquiciaRegla>(reglas[0]);
            Assert.IsInstanceOf<IntervaloMinimoEntreViajesRegla>(reglas[1]);
            Assert.IsInstanceOf<TrasbordoRegla>(reglas[2]);
            Assert.IsInstanceOf<BeneficioPorTipoDeTarjetaRegla>(reglas[3]);
            Assert.IsInstanceOf<UsoFrecuenteRegla>(reglas[4]);
        }

        [Test]
        public void EscenarioA_Normal_LineaUrbana_TrasbordoGratis_InterurbanoPasadaLaHora()
        {
            // Lunes 12 de Octubre de 2026, 10:00 (lunes a sábado, 7 a 22)
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var coleUrbano1 = new Colectivo("Linea 102", reloj);
            var coleUrbano2 = new Colectivo("Linea 133", reloj);
            var coleInterurbano = new ColectivoInterurbano("Rosario–Gálvez", reloj);

            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(10000);

            // 1. 10:00: Viaje urbano en Linea 102 -> paga tarifa básica completa ($2150)
            bool ok1 = coleUrbano1.PagarCon(tarjeta, out var boleto1);
            Assert.IsTrue(ok1);
            Assert.IsNotNull(boleto1);
            Assert.AreEqual(Colectivo.TarifaBasica, boleto1!.TotalAbonado);
            Assert.IsFalse(boleto1.EsTrasbordo);
            Assert.AreEqual(10000m - Colectivo.TarifaBasica, tarjeta.Saldo);

            // 2. 10:25 (a los 25 min): Trasbordo gratuito a Linea 133
            reloj.Avanzar(TimeSpan.FromMinutes(25));
            bool ok2 = coleUrbano2.PagarCon(tarjeta, out var boleto2);
            Assert.IsTrue(ok2);
            Assert.IsNotNull(boleto2);
            Assert.AreEqual(0m, boleto2!.TotalAbonado);
            Assert.IsTrue(boleto2.EsTrasbordo);
            Assert.AreEqual(10000m - Colectivo.TarifaBasica, tarjeta.Saldo); // Saldo no cambia

            // 3. 11:15 (a los 75 min del 1er boleto pagado): Viaje interurbano pasada la hora -> se cobra $3000
            reloj.Avanzar(TimeSpan.FromMinutes(50));
            bool ok3 = coleInterurbano.PagarCon(tarjeta, out var boleto3);
            Assert.IsTrue(ok3);
            Assert.IsNotNull(boleto3);
            Assert.AreEqual(ColectivoInterurbano.TarifaInterurbana, boleto3!.TotalAbonado);
            Assert.IsFalse(boleto3.EsTrasbordo);
            Assert.AreEqual("Rosario–Gálvez", boleto3.Linea);
            Assert.AreEqual(10000m - Colectivo.TarifaBasica - ColectivoInterurbano.TarifaInterurbana, tarjeta.Saldo);
            Assert.AreEqual(3, tarjeta.Boletos.Count);
        }

        [Test]
        public void EscenarioB_MedioBoleto_SabadoRechazado_Lunes0600PagaMitad_SegundoViajeCuatroMinutosRechazado()
        {
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.MedioBoleto };
            tarjeta.Cargar(10000);

            // 1. Sábado 10:00 -> rechazado por regla de franja horaria de franquicias (Orden 10)
            var relojSabado = new ProveedorDeFechaFalso(new DateTime(2026, 10, 17, 10, 0, 0));
            var coleSabado = new Colectivo("Linea 102", relojSabado);
            bool okSabado = coleSabado.PagarCon(tarjeta, out var boletoSabado);

            Assert.IsFalse(okSabado);
            Assert.IsNull(boletoSabado);
            Assert.AreEqual(10000m, tarjeta.Saldo);
            Assert.IsEmpty(tarjeta.Boletos);

            // 2. Lunes 06:00:00 (inicio exacto de la franja horaria permitida [6, 22)) -> paga la mitad ($1075)
            var relojLunes = new ProveedorDeFechaFalso(new DateTime(2026, 10, 19, 6, 0, 0));
            var coleLunes = new Colectivo("Linea 102", relojLunes);
            decimal mitad = Colectivo.TarifaBasica / 2m;

            bool okLunes1 = coleLunes.PagarCon(tarjeta, out var boletoLunes1);
            Assert.IsTrue(okLunes1);
            Assert.IsNotNull(boletoLunes1);
            Assert.AreEqual(mitad, boletoLunes1!.TotalAbonado);
            Assert.IsFalse(boletoLunes1.EsTrasbordo);
            Assert.AreEqual(10000m - mitad, tarjeta.Saldo);
            Assert.AreEqual(1, tarjeta.Boletos.Count);

            // 3. 2.º viaje a los 4 minutos (06:04:00) -> rechazado por regla de intervalo mínimo de 5 min (Orden 20)
            relojLunes.Avanzar(TimeSpan.FromMinutes(4));
            bool okLunes2 = coleLunes.PagarCon(tarjeta, out var boletoLunes2);
            Assert.IsFalse(okLunes2);
            Assert.IsNull(boletoLunes2);
            Assert.AreEqual(10000m - mitad, tarjeta.Saldo);
            Assert.AreEqual(1, tarjeta.Boletos.Count);

            // Extra: A los 5:00 exactos (06:05:00) -> aceptado, 2.º viaje con beneficio del día
            relojLunes.Avanzar(TimeSpan.FromMinutes(1));
            bool okLunes3 = coleLunes.PagarCon(tarjeta, out var boletoLunes3);
            Assert.IsTrue(okLunes3);
            Assert.IsNotNull(boletoLunes3);
            Assert.AreEqual(mitad, boletoLunes3!.TotalAbonado);
            Assert.AreEqual(10000m - (2 * mitad), tarjeta.Saldo);
            Assert.AreEqual(2, tarjeta.Boletos.Count);

            // Extra: 3.er viaje regular en el mismo día (10:00:00) -> agotado el tope de 2 beneficios, paga completo
            relojLunes.Avanzar(TimeSpan.FromHours(3).Add(TimeSpan.FromMinutes(55)));
            bool okLunes4 = coleLunes.PagarCon(tarjeta, out var boletoLunes4);
            Assert.IsTrue(okLunes4);
            Assert.IsNotNull(boletoLunes4);
            Assert.AreEqual(Colectivo.TarifaBasica, boletoLunes4!.TotalAbonado);
            Assert.AreEqual(10000m - (2 * mitad) - Colectivo.TarifaBasica, tarjeta.Saldo);
            Assert.AreEqual(3, tarjeta.Boletos.Count);
        }

        [Test]
        public void CoberturaMetodosPublicos_TiposDeTarjetaTodos_Y_Excepciones()
        {
            var todos = TiposDeTarjeta.Todos.ToList();
            Assert.AreEqual(4, todos.Count);
            Assert.IsTrue(todos.Any(t => t.Nombre == "Normal"));
            Assert.IsTrue(todos.Any(t => t.Nombre == "Medio boleto estudiantil"));
            Assert.IsTrue(todos.Any(t => t.Nombre == "Boleto gratuito estudiantil"));
            Assert.IsTrue(todos.Any(t => t.Nombre == "Franquicia completa"));

            var ex1 = new LimiteSaldoExcedidoException("Error saldo");
            Assert.AreEqual("Error saldo", ex1.Message);

            var ex2 = new SaldoInsuficienteException("Error fondo");
            Assert.AreEqual("Error fondo", ex2.Message);
        }

        [Test]
        public void CoberturaMetodosPublicos_Colectivo_ConstructoresConExcepciones()
        {
            Assert.Throws<ArgumentNullException>(() => new Colectivo("102", null!));
        }

        [Test]
        public void CoberturaMetodosPublicos_Boleto_PropiedadesParaEFCore()
        {
            var boleto = new Boleto(2150, 5000, new Tarjeta(), new Colectivo("102"));
            boleto.ColectivoId = 10;
            Assert.AreEqual(10, boleto.ColectivoId);
        }
    }
}
