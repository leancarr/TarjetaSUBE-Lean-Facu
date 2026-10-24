using NUnit.Framework;

namespace Transporte.Tests
{
    public class TarjetaTipoTest
    {
        [Test]
        public void Constructor_VacioYPropiedades_AsignanCorrectamente()
        {
            var tipo = new TarjetaTipo
            {
                Id = 10,
                Nombre = "Especial",
                PorcentajeDescuento = 0.25m,
                MinutosEntreViajes = 10,
                MaxViajesConBeneficioPorDia = 4
            };

            Assert.AreEqual(10, tipo.Id);
            Assert.AreEqual("Especial", tipo.Nombre);
            Assert.AreEqual(0.25m, tipo.PorcentajeDescuento);
            Assert.AreEqual(10, tipo.MinutosEntreViajes);
            Assert.AreEqual(4, tipo.MaxViajesConBeneficioPorDia);
        }

        [Test]
        public void Constructor_ConParametros_InicializaPropiedades()
        {
            var tipo = new TarjetaTipo(2, "Medio boleto estudiantil", 0.5m, 5, 2);

            Assert.AreEqual(2, tipo.Id);
            Assert.AreEqual("Medio boleto estudiantil", tipo.Nombre);
            Assert.AreEqual(0.5m, tipo.PorcentajeDescuento);
            Assert.AreEqual(5, tipo.MinutosEntreViajes);
            Assert.AreEqual(2, tipo.MaxViajesConBeneficioPorDia);
        }

        [Test]
        public void Tarjeta_PorDefecto_TieneTipoNormal()
        {
            var tarjeta = new Tarjeta();

            Assert.AreEqual(1, tarjeta.TarjetaTipoId);
            Assert.IsNotNull(tarjeta.TarjetaTipo);
            Assert.AreEqual("Normal", tarjeta.TarjetaTipo.Nombre);
            Assert.AreEqual(0m, tarjeta.TarjetaTipo.PorcentajeDescuento);
            Assert.AreEqual(0, tarjeta.TarjetaTipo.MinutosEntreViajes);
            Assert.IsNull(tarjeta.TarjetaTipo.MaxViajesConBeneficioPorDia);
        }

        [Test]
        public void Tarjeta_AsignarTarjetaTipoYClaveForanea_ActualizaPropiedades()
        {
            var tarjeta = new Tarjeta
            {
                TarjetaTipoId = 2,
                TarjetaTipo = TiposDePrueba.MedioBoleto
            };

            Assert.AreEqual(2, tarjeta.TarjetaTipoId);
            Assert.AreEqual("Medio boleto estudiantil", tarjeta.TarjetaTipo.Nombre);
            Assert.AreEqual(0.5m, tarjeta.TarjetaTipo.PorcentajeDescuento);
            Assert.AreEqual(5, tarjeta.TarjetaTipo.MinutosEntreViajes);
            Assert.AreEqual(2, tarjeta.TarjetaTipo.MaxViajesConBeneficioPorDia);
        }

        [Test]
        public void PagarCon_FranquiciaCompleta_SiemprePuedePagarInclusoSinSaldoOEnLimiteNegativo()
        {
            var tarjeta = new Tarjeta { TarjetaTipoId = 4, TarjetaTipo = TiposDePrueba.FranquiciaCompleta };
            var colectivo = new Colectivo("Linea K");

            // Con saldo 0
            bool resultadoSaldoCero = colectivo.PagarCon(tarjeta, out var boleto1);
            Assert.IsTrue(resultadoSaldoCero);
            Assert.IsNotNull(boleto1);
            Assert.AreEqual(0m, boleto1!.Monto);
            Assert.AreEqual(0m, tarjeta.Saldo);

            // Llevamos la tarjeta al límite negativo exacto (-2000) y validamos que igual puede pagar
            tarjeta.Descontar(2000);
            Assert.AreEqual(Tarjeta.LimiteNegativo, tarjeta.Saldo);

            bool resultadoEnLimite = colectivo.PagarCon(tarjeta, out var boleto2);
            Assert.IsTrue(resultadoEnLimite);
            Assert.IsNotNull(boleto2);
            Assert.AreEqual(0m, boleto2!.Monto);
            Assert.AreEqual(Tarjeta.LimiteNegativo, tarjeta.Saldo);
        }

        [Test]
        public void PagarCon_BoletoGratuitoEstudiantil_CostoCeroYSiemprePuedePagar()
        {
            var tarjeta = new Tarjeta { TarjetaTipoId = 3, TarjetaTipo = TiposDePrueba.BoletoGratuito };
            var colectivo = new Colectivo("Linea 115");

            bool resultado = colectivo.PagarCon(tarjeta, out var boleto);

            Assert.IsTrue(resultado);
            Assert.IsNotNull(boleto);
            Assert.AreEqual(0m, boleto!.Monto);
            Assert.AreEqual(0m, tarjeta.Saldo);
        }

        [Test]
        public void PagarCon_MedioBoleto_MontoEsSiempreLaMitadDelNormal()
        {
            var tarjetaNormal = new Tarjeta { TarjetaTipoId = 1, TarjetaTipo = TiposDePrueba.Normal };
            var tarjetaMedio1 = new Tarjeta { TarjetaTipoId = 2, TarjetaTipo = TiposDePrueba.MedioBoleto };
            var tarjetaMedio2 = new Tarjeta { TarjetaTipoId = 2, TarjetaTipo = TiposDePrueba.MedioBoleto };
            tarjetaNormal.Cargar(5000);
            tarjetaMedio1.Cargar(5000);
            tarjetaMedio2.Cargar(5000);

            var colectivo = new Colectivo("Linea 122");

            bool okNormal = colectivo.PagarCon(tarjetaNormal, out var boletoNormal);
            bool okMedio1 = colectivo.PagarCon(tarjetaMedio1, out var boletoMedio1);
            bool okMedio2 = colectivo.PagarCon(tarjetaMedio2, out var boletoMedio2);

            Assert.IsTrue(okNormal);
            Assert.IsTrue(okMedio1);
            Assert.IsTrue(okMedio2);
            Assert.AreEqual(Colectivo.TarifaBasica / 2m, boletoMedio1!.Monto);
            Assert.AreEqual(boletoNormal!.Monto / 2m, boletoMedio1.Monto);
            Assert.AreEqual(boletoNormal.Monto / 2m, boletoMedio2!.Monto);
            Assert.AreEqual(5000 - (Colectivo.TarifaBasica / 2m), tarjetaMedio1.Saldo);
            Assert.AreEqual(5000 - (Colectivo.TarifaBasica / 2m), tarjetaMedio2.Saldo);
        }
    }
}
