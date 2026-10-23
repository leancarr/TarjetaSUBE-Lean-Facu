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
    }
}
