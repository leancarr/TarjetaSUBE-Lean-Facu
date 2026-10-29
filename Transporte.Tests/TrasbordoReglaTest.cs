using System;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Transporte.Tests
{
    public class TrasbordoReglaTest
    {
        [Test]
        public void TrasbordoRegla_OrdenYAplicaA()
        {
            var regla = new TrasbordoRegla();
            Assert.AreEqual(30, regla.Orden);

            var tarjetaSinHistorial = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            var colectivo = new Colectivo("102");
            var ctxSinHistorial = new ContextoDeViaje(tarjetaSinHistorial, colectivo, DateTime.Now, Colectivo.TarifaBasica);

            // Sin historial no aplica trasbordo
            Assert.IsFalse(regla.AplicaA(ctxSinHistorial));

            // Con historial sí aplica
            tarjetaSinHistorial.Boletos.Add(new Boleto(DateTime.Now, "Normal", "102", Colectivo.TarifaBasica, 5000m, 1, tarjetaSinHistorial, colectivo));
            Assert.IsTrue(regla.AplicaA(ctxSinHistorial));
        }

        [Test]
        public void CasoA_PagarCon_LineaDistintaDentroDeUnaHora_EsTrasbordoSinCosto()
        {
            // Lunes 12 de Octubre de 2026, 10:00 (lunes a sábado, 7 <= hora < 22)
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var linea102 = new Colectivo("102", reloj);
            var linea133 = new Colectivo("133", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(5000);

            // 1er viaje: Línea 102 paga tarifa completa
            bool v1 = linea102.PagarCon(tarjeta, out var primero);
            Assert.IsTrue(v1);
            Assert.IsNotNull(primero);
            Assert.AreEqual(Colectivo.TarifaBasica, primero!.TotalAbonado);
            Assert.IsFalse(primero.EsTrasbordo);
            Assert.AreEqual(5000m - Colectivo.TarifaBasica, tarjeta.Saldo);

            // 30 minutos después: Línea 133 (distinta línea dentro de 1 hora) -> trasbordo gratis
            reloj.Avanzar(TimeSpan.FromMinutes(30));
            bool v2 = linea133.PagarCon(tarjeta, out var segundo);
            Assert.IsTrue(v2);
            Assert.IsNotNull(segundo);
            Assert.AreEqual(0m, segundo!.TotalAbonado);
            Assert.IsTrue(segundo.EsTrasbordo);
            Assert.AreEqual(5000m - Colectivo.TarifaBasica, tarjeta.Saldo); // Saldo no cambia
        }

        [Test]
        public void CasoB_PagarCon_MismaLineaDentroDeUnaHora_SeCobra()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var linea102 = new Colectivo("102", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(10000);

            // 1er viaje: Línea 102 paga
            linea102.PagarCon(tarjeta, out var primero);
            Assert.IsFalse(primero!.EsTrasbordo);

            // 15 minutos después en la MISMA línea -> no es trasbordo, se cobra
            reloj.Avanzar(TimeSpan.FromMinutes(15));
            bool v2 = linea102.PagarCon(tarjeta, out var segundo);

            Assert.IsTrue(v2);
            Assert.IsNotNull(segundo);
            Assert.AreEqual(Colectivo.TarifaBasica, segundo!.TotalAbonado);
            Assert.IsFalse(segundo.EsTrasbordo);
            Assert.AreEqual(10000m - (2 * Colectivo.TarifaBasica), tarjeta.Saldo);
        }

        [Test]
        public void CasoC_PagarCon_BordeDeUnaHora_SesentaMinutosExactosEsTrasbordo_PasadaUnaHoraSeCobra()
        {
            // Borde exacto: 60 minutos (≤ 1 hora) es trasbordo
            var relojExacto = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var coleA = new Colectivo("102", relojExacto);
            var coleB = new Colectivo("133", relojExacto);
            var tarjeta1 = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta1.Cargar(10000);

            coleA.PagarCon(tarjeta1, out _);
            relojExacto.Avanzar(TimeSpan.FromHours(1)); // Exactamente 1 hora (11:00:00)
            bool okExacto = coleB.PagarCon(tarjeta1, out var boletoExacto);

            Assert.IsTrue(okExacto);
            Assert.IsNotNull(boletoExacto);
            Assert.AreEqual(0m, boletoExacto!.TotalAbonado);
            Assert.IsTrue(boletoExacto.EsTrasbordo);

            // Pasada 1 hora (61 minutos) -> se cobra
            var relojVencido = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var coleC = new Colectivo("102", relojVencido);
            var coleD = new Colectivo("133", relojVencido);
            var tarjeta2 = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta2.Cargar(10000);

            coleC.PagarCon(tarjeta2, out _);
            relojVencido.Avanzar(TimeSpan.FromMinutes(61)); // 1 hora y 1 minuto (11:01:00)
            bool okVencido = coleD.PagarCon(tarjeta2, out var boletoVencido);

            Assert.IsTrue(okVencido);
            Assert.IsNotNull(boletoVencido);
            Assert.AreEqual(Colectivo.TarifaBasica, boletoVencido!.TotalAbonado);
            Assert.IsFalse(boletoVencido.EsTrasbordo);
            Assert.AreEqual(10000m - (2 * Colectivo.TarifaBasica), tarjeta2.Saldo);
        }

        [Test]
        public void CasoD_PagarCon_CadenaTresColectivos_DentroDeUnaHoraDesdeElPrimerBoletoPagado()
        {
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var lineaA = new Colectivo("102", reloj);
            var lineaB = new Colectivo("133", reloj);
            var lineaC = new Colectivo("144", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(10000);

            // A (10:00) -> paga tarifa completa
            lineaA.PagarCon(tarjeta, out var bA);
            Assert.AreEqual(Colectivo.TarifaBasica, bA!.TotalAbonado);
            Assert.IsFalse(bA.EsTrasbordo);

            // B (10:20, a los 20 min de A) -> gratis (trasbordo)
            reloj.Avanzar(TimeSpan.FromMinutes(20));
            lineaB.PagarCon(tarjeta, out var bB);
            Assert.AreEqual(0m, bB!.TotalAbonado);
            Assert.IsTrue(bB.EsTrasbordo);

            // C (10:45, a los 45 min de A) -> gratis (trasbordo)
            reloj.Avanzar(TimeSpan.FromMinutes(25));
            lineaC.PagarCon(tarjeta, out var bC);
            Assert.AreEqual(0m, bC!.TotalAbonado);
            Assert.IsTrue(bC.EsTrasbordo);

            // Saldo debitado únicamente por el primer viaje A
            Assert.AreEqual(10000m - Colectivo.TarifaBasica, tarjeta.Saldo);

            // D (11:10, a los 70 min de A y 25 min de C) -> la hora no se renueva con B ni con C, se cobra
            reloj.Avanzar(TimeSpan.FromMinutes(25));
            lineaA.PagarCon(tarjeta, out var bD);
            Assert.AreEqual(Colectivo.TarifaBasica, bD!.TotalAbonado);
            Assert.IsFalse(bD.EsTrasbordo);
            Assert.AreEqual(10000m - (2 * Colectivo.TarifaBasica), tarjeta.Saldo);
        }

        [Test]
        public void CasoE_PagarCon_Domingo_0659_Y_2200_SeCobran()
        {
            // Domingo a las 10:00
            var relojDomingo = new ProveedorDeFechaFalso(new DateTime(2026, 10, 18, 10, 0, 0)); // Domingo
            var coleA = new Colectivo("102", relojDomingo);
            var coleB = new Colectivo("133", relojDomingo);
            var tarjetaDom = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjetaDom.Cargar(10000);

            coleA.PagarCon(tarjetaDom, out _);
            relojDomingo.Avanzar(TimeSpan.FromMinutes(20));
            coleB.PagarCon(tarjetaDom, out var boletoDom);
            Assert.IsFalse(boletoDom!.EsTrasbordo, "En domingo no aplica trasbordo sin costo");
            Assert.AreEqual(Colectivo.TarifaBasica, boletoDom.TotalAbonado);

            // Lunes a las 06:59 (hora < 7)
            var relojManana = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 6, 45, 0));
            var coleC = new Colectivo("102", relojManana);
            var coleD = new Colectivo("133", relojManana);
            var tarjetaManana = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjetaManana.Cargar(10000);

            coleC.PagarCon(tarjetaManana, out _);
            relojManana.Avanzar(TimeSpan.FromMinutes(14)); // 06:59:00
            coleD.PagarCon(tarjetaManana, out var boletoManana);
            Assert.IsFalse(boletoManana!.EsTrasbordo, "A las 06:59 no aplica trasbordo sin costo");
            Assert.AreEqual(Colectivo.TarifaBasica, boletoManana.TotalAbonado);

            // Lunes a las 22:00 (hora >= 22)
            var relojNoche = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 21, 45, 0));
            var coleE = new Colectivo("102", relojNoche);
            var coleF = new Colectivo("133", relojNoche);
            var tarjetaNoche = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjetaNoche.Cargar(10000);

            coleE.PagarCon(tarjetaNoche, out _);
            relojNoche.Avanzar(TimeSpan.FromMinutes(15)); // 22:00:00
            coleF.PagarCon(tarjetaNoche, out var boletoNoche);
            Assert.IsFalse(boletoNoche!.EsTrasbordo, "A las 22:00 no aplica trasbordo sin costo");
            Assert.AreEqual(Colectivo.TarifaBasica, boletoNoche.TotalAbonado);
        }

        [Test]
        public void CasoF_PagarCon_Sabado_A_Las_0700_EsTrasbordo()
        {
            // Sábado 17 de Octubre de 2026
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 17, 6, 45, 0));
            var cole102 = new Colectivo("102", reloj);
            var cole133 = new Colectivo("133", reloj);
            var tarjeta = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjeta.Cargar(5000);

            cole102.PagarCon(tarjeta, out _);
            reloj.Avanzar(TimeSpan.FromMinutes(15)); // 07:00:00 exactas de un sábado
            bool ok = cole133.PagarCon(tarjeta, out var boleto);

            Assert.IsTrue(ok);
            Assert.IsNotNull(boleto);
            Assert.IsTrue(boleto!.EsTrasbordo);
            Assert.AreEqual(0m, boleto.TotalAbonado);
            Assert.AreEqual(5000m - Colectivo.TarifaBasica, tarjeta.Saldo);
        }

        [Test]
        public void CasoG_PagarCon_FuncionaConTodosLosTiposDeTarjeta_Y_LineasInterurbanas()
        {
            // 1. Medio boleto: debe respetar los 5 minutos mínimos
            var relojMedio = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var cole1 = new Colectivo("102", relojMedio);
            var cole2 = new Colectivo("133", relojMedio);
            var tarjetaMedio = new Tarjeta { TarjetaTipo = TiposDeTarjeta.MedioBoleto };
            tarjetaMedio.Cargar(5000);

            cole1.PagarCon(tarjetaMedio, out _);

            // A los 4 minutos: rechazado por IntervaloMinimoEntreViajesRegla
            relojMedio.Avanzar(TimeSpan.FromMinutes(4));
            bool rechazado4Min = cole2.PagarCon(tarjetaMedio, out var bRechazado);
            Assert.IsFalse(rechazado4Min);
            Assert.IsNull(bRechazado);

            // A los 5 minutos: aceptado como trasbordo gratuito
            relojMedio.Avanzar(TimeSpan.FromMinutes(1));
            bool aceptado5Min = cole2.PagarCon(tarjetaMedio, out var bAceptado);
            Assert.IsTrue(aceptado5Min);
            Assert.IsNotNull(bAceptado);
            Assert.IsTrue(bAceptado!.EsTrasbordo);
            Assert.AreEqual(0m, bAceptado.TotalAbonado);

            // 2. Boleto gratuito estudiantil
            var relojGratuito = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var cole3 = new Colectivo("102", relojGratuito);
            var cole4 = new Colectivo("133", relojGratuito);
            var tarjetaGratuita = new Tarjeta { TarjetaTipo = TiposDeTarjeta.BoletoGratuito };
            tarjetaGratuita.Cargar(5000);

            cole3.PagarCon(tarjetaGratuita, out var bGratuito1);
            Assert.IsFalse(bGratuito1!.EsTrasbordo);

            relojGratuito.Avanzar(TimeSpan.FromMinutes(20));
            cole4.PagarCon(tarjetaGratuita, out var bGratuito2);
            Assert.IsTrue(bGratuito2!.EsTrasbordo);
            Assert.AreEqual(0m, bGratuito2.TotalAbonado);

            // 3. Franquicia completa (jubilados)
            var relojJubilado = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var cole5 = new Colectivo("102", relojJubilado);
            var cole6 = new Colectivo("133", relojJubilado);
            var tarjetaJubilado = new Tarjeta { TarjetaTipo = TiposDeTarjeta.FranquiciaCompleta };
            tarjetaJubilado.Cargar(5000);

            cole5.PagarCon(tarjetaJubilado, out var bJubilado1);
            Assert.IsFalse(bJubilado1!.EsTrasbordo);

            relojJubilado.Avanzar(TimeSpan.FromMinutes(20));
            cole6.PagarCon(tarjetaJubilado, out var bJubilado2);
            Assert.IsTrue(bJubilado2!.EsTrasbordo);
            Assert.AreEqual(0m, bJubilado2.TotalAbonado);

            // 4. Trasbordo urbano a interurbano
            var relojInter = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var coleUrbano = new Colectivo("102", relojInter);
            var coleInterurbano = new ColectivoInterurbano("Rosario–Gálvez", relojInter);
            var tarjetaNormal = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjetaNormal.Cargar(10000);

            coleUrbano.PagarCon(tarjetaNormal, out var bUrb);
            Assert.AreEqual(Colectivo.TarifaBasica, bUrb!.TotalAbonado);
            Assert.IsFalse(bUrb.EsTrasbordo);

            relojInter.Avanzar(TimeSpan.FromMinutes(20));
            coleInterurbano.PagarCon(tarjetaNormal, out var bInter);
            Assert.AreEqual(0m, bInter!.TotalAbonado);
            Assert.IsTrue(bInter.EsTrasbordo);

            // 5. Trasbordo interurbano a urbano
            var relojInter2 = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 10, 0, 0));
            var coleInter2 = new ColectivoInterurbano("Rosario–Baigorria", relojInter2);
            var coleUrb2 = new Colectivo("102", relojInter2);
            var tarjetaNormal2 = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjetaNormal2.Cargar(10000);

            coleInter2.PagarCon(tarjetaNormal2, out var bInter2);
            Assert.AreEqual(ColectivoInterurbano.TarifaInterurbana, bInter2!.TotalAbonado);
            Assert.IsFalse(bInter2.EsTrasbordo);

            relojInter2.Avanzar(TimeSpan.FromMinutes(20));
            coleUrb2.PagarCon(tarjetaNormal2, out var bUrb2);
            Assert.AreEqual(0m, bUrb2!.TotalAbonado);
            Assert.IsTrue(bUrb2.EsTrasbordo);
        }

        [Test]
        public void CasoH_PagarCon_TrasbordoNoConsumeTopeDiarioDeMedioBoleto_NiSumaAlUsoFrecuente()
        {
            // 1. Tope diario de medio boleto
            var reloj = new ProveedorDeFechaFalso(new DateTime(2026, 10, 12, 8, 0, 0));
            var coleA = new Colectivo("102", reloj);
            var coleB = new Colectivo("133", reloj);
            var tarjetaMedio = new Tarjeta { TarjetaTipo = TiposDeTarjeta.MedioBoleto };
            tarjetaMedio.Cargar(15000);
            decimal mitad = Colectivo.TarifaBasica / 2m;

            // 08:00: 1er viaje con descuento
            coleA.PagarCon(tarjetaMedio, out var b1);
            Assert.AreEqual(mitad, b1!.TotalAbonado);
            Assert.IsFalse(b1.EsTrasbordo);

            // 08:20: Trasbordo a línea distinta -> $0 (no debe consumir el segundo beneficio diario)
            reloj.Avanzar(TimeSpan.FromMinutes(20));
            coleB.PagarCon(tarjetaMedio, out var bTrasbordo);
            Assert.AreEqual(0m, bTrasbordo!.TotalAbonado);
            Assert.IsTrue(bTrasbordo.EsTrasbordo);

            // 12:00: 2do viaje con descuento del día (sigue teniendo descuento)
            reloj.Avanzar(TimeSpan.FromHours(3).Add(TimeSpan.FromMinutes(40)));
            coleA.PagarCon(tarjetaMedio, out var b2);
            Assert.AreEqual(mitad, b2!.TotalAbonado);
            Assert.IsFalse(b2.EsTrasbordo);

            // 16:00: 3er viaje regular del día -> se agotaron los 2 viajes con beneficio, paga completo
            reloj.Avanzar(TimeSpan.FromHours(4));
            coleA.PagarCon(tarjetaMedio, out var b3);
            Assert.AreEqual(Colectivo.TarifaBasica, b3!.TotalAbonado);
            Assert.IsFalse(b3.EsTrasbordo);

            // 2. Uso frecuente: un trasbordo no suma al conteo del mes
            var relojUso = new ProveedorDeFechaFalso(new DateTime(2026, 10, 20, 9, 0, 0));
            var cole102 = new Colectivo("102", relojUso);
            var cole133 = new Colectivo("133", relojUso);
            var tarjetaNormal = new Tarjeta { TarjetaTipo = TiposDeTarjeta.Normal };
            tarjetaNormal.Cargar(30000);

            // 28 viajes previos
            HistorialDePrueba.AgregarViajes(tarjetaNormal, 28, new DateTime(2026, 10, 1, 8, 0, 0), cole102);

            // Viaje 29 regular a las 09:00: cobra tarifa normal
            cole102.PagarCon(tarjetaNormal, out var b29);
            Assert.AreEqual(Colectivo.TarifaBasica, b29!.TotalAbonado);
            Assert.IsFalse(b29.EsTrasbordo);

            // Trasbordo a las 09:20: gratis, no suma al conteo mensual
            relojUso.Avanzar(TimeSpan.FromMinutes(20));
            cole133.PagarCon(tarjetaNormal, out var bTrasbordoUso);
            Assert.AreEqual(0m, bTrasbordoUso!.TotalAbonado);
            Assert.IsTrue(bTrasbordoUso.EsTrasbordo);

            // Siguiente viaje regular a las 12:00: es el viaje 30 regular del mes -> aplica 20% de descuento
            relojUso.Avanzar(TimeSpan.FromHours(2).Add(TimeSpan.FromMinutes(40)));
            cole102.PagarCon(tarjetaNormal, out var b30);
            Assert.IsFalse(b30!.EsTrasbordo);
            Assert.AreEqual(Colectivo.TarifaBasica * 0.80m, b30.TotalAbonado);
        }

        [Test]
        public void DbContext_MapeaPropiedadEsTrasbordoEnBoleto()
        {
            var options = new DbContextOptionsBuilder<TransporteDbContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;

            using var context = new TransporteDbContext(options);
            var property = context.Model.FindEntityType(typeof(Boleto))?.FindProperty("EsTrasbordo");

            Assert.IsNotNull(property);
            Assert.AreEqual(typeof(bool), property!.ClrType);
        }
    }
}
