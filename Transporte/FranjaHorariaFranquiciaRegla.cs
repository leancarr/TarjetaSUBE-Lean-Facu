using System;

namespace Transporte
{
    // SRP / OCP: FranjaHorariaFranquiciaRegla valida que las tarjetas con franquicia
    // (cualquier tipo con EsFranquicia = true) solo viajen de lunes a viernes entre las 06:00 y las 22:00.
    public class FranjaHorariaFranquiciaRegla : IReglaDeViaje
    {
        public int Orden => 10;

        public const int HoraInicio = 6;
        public const int HoraFin = 22;

        public bool AplicaA(ContextoDeViaje c)
        {
            return c.Tarjeta?.TarjetaTipo != null && c.Tarjeta.TarjetaTipo.EsFranquicia;
        }

        public bool Evaluar(ContextoDeViaje c)
        {
            // Lunes a viernes únicamente
            if (c.Ahora.DayOfWeek == DayOfWeek.Saturday || c.Ahora.DayOfWeek == DayOfWeek.Sunday)
            {
                return false;
            }

            // Horario: [06:00:00, 22:00:00)
            if (c.Ahora.Hour < HoraInicio || c.Ahora.Hour >= HoraFin)
            {
                return false;
            }

            return true;
        }
    }
}
