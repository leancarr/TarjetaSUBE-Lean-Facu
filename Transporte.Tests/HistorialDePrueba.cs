using System;

namespace Transporte.Tests
{
    public static class HistorialDePrueba
    {
        public static void AgregarViajes(Tarjeta tarjeta, int cantidad, DateTime fecha, Colectivo? colectivo = null)
        {
            var cole = colectivo ?? new Colectivo("Linea 122");
            string tipoTarjeta = tarjeta.TarjetaTipo?.Nombre ?? "Normal";

            for (int i = 0; i < cantidad; i++)
            {
                // Agregamos boletos espaciados por minutos
                DateTime fechaBoleto = fecha.AddMinutes(i * 10);
                var boleto = new Boleto(fechaBoleto, tipoTarjeta, cole.Linea, Colectivo.TarifaBasica, tarjeta.Saldo, tarjeta.Id, tarjeta, cole);
                tarjeta.Boletos.Add(boleto);
            }
        }
    }
}
