using System;
using System.Linq;

namespace Transporte
{
    // SRP / OCP: TrasbordoRegla aplica beneficio de trasbordo sin costo entre líneas distintas
    // dentro de una ventana de 1 hora desde el último boleto pagado, de lunes a sábado de 07:00 a 22:00.
    public class TrasbordoRegla : IReglaDeViaje
    {
        public int Orden => 30;

        public const int HoraInicio = 7;
        public const int HoraFin = 22;
        public static readonly TimeSpan VentanaMaxima = TimeSpan.FromHours(1);

        public bool AplicaA(ContextoDeViaje c)
        {
            return c.Tarjeta?.Boletos != null && c.Tarjeta.Boletos.Count > 0;
        }

        public bool Evaluar(ContextoDeViaje c)
        {
            if (c.Tarjeta?.Boletos == null || c.Tarjeta.Boletos.Count == 0)
            {
                return true;
            }

            var boletosOrdenados = c.Tarjeta.Boletos.OrderByDescending(b => b.Fecha).ToList();
            var ultimoBoleto = boletosOrdenados.FirstOrDefault();
            var ultimoBoletoPagado = boletosOrdenados.FirstOrDefault(b => !b.EsTrasbordo);

            if (ultimoBoleto == null || ultimoBoletoPagado == null)
            {
                return true;
            }

            // Ventana máxima de 1 hora desde el último boleto pagado (no se renueva con trasbordos)
            TimeSpan tiempoDesdePago = c.Ahora - ultimoBoletoPagado.Fecha;
            if (tiempoDesdePago < TimeSpan.Zero || tiempoDesdePago > VentanaMaxima)
            {
                return true;
            }

            // La línea debe ser distinta a la del boleto anterior
            if (string.Equals(ultimoBoleto.Linea, c.Colectivo?.Linea, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Días permitidos: Lunes a Sábado (no Domingo)
            if (c.Ahora.DayOfWeek == DayOfWeek.Sunday)
            {
                return true;
            }

            // Horario permitido: [07:00:00, 22:00:00) -> 7 <= hora < 22
            if (c.Ahora.Hour < HoraInicio || c.Ahora.Hour >= HoraFin)
            {
                return true;
            }

            // Aplica trasbordo gratuito
            c.Tarifa = 0m;
            c.EsTrasbordo = true;
            return true;
        }
    }
}
