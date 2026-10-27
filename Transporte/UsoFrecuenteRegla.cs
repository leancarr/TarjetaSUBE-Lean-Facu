using System.Linq;

namespace Transporte
{
    // SRP / OCP: UsoFrecuenteRegla aplica descuentos por uso frecuente a tarjetas de tipo Normal
    // según la cantidad de viajes realizados en el mes calendario.
    public class UsoFrecuenteRegla : IReglaDeViaje
    {
        public int Orden => 50;

        public const int UmbralDescuentoVeinte = 30;
        public const int UmbralDescuentoVeinticinco = 80;
        public const decimal DescuentoVeinte = 0.20m;
        public const decimal DescuentoVeinticinco = 0.25m;

        public bool AplicaA(ContextoDeViaje c)
        {
            bool esFranquicia = c.Tarjeta?.TarjetaTipo != null && c.Tarjeta.TarjetaTipo.EsFranquicia;
            return !esFranquicia && !c.EsTrasbordo;
        }

        public bool Evaluar(ContextoDeViaje c)
        {
            if (c.Tarjeta?.Boletos == null)
            {
                return true;
            }

            int viajesMes = c.Tarjeta.Boletos.Count(b =>
                b.Fecha.Year == c.Ahora.Year &&
                b.Fecha.Month == c.Ahora.Month &&
                !b.EsTrasbordo);

            int numeroDeViaje = viajesMes + 1;

            if (numeroDeViaje >= UmbralDescuentoVeinticinco)
            {
                c.Tarifa *= (1m - DescuentoVeinticinco);
            }
            else if (numeroDeViaje >= UmbralDescuentoVeinte)
            {
                c.Tarifa *= (1m - DescuentoVeinte);
            }

            return true;
        }
    }
}
