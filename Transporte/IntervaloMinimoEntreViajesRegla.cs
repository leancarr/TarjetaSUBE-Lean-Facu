using System;
using System.Linq;

namespace Transporte
{
    public class IntervaloMinimoEntreViajesRegla : IReglaDeViaje
    {
        public int Orden => 20;

        public bool AplicaA(ContextoDeViaje c)
        {
            return c.Tarjeta?.TarjetaTipo != null && c.Tarjeta.TarjetaTipo.MinutosEntreViajes > 0;
        }

        public bool Evaluar(ContextoDeViaje c)
        {
            if (c.Tarjeta?.Boletos == null || c.Tarjeta.Boletos.Count == 0)
            {
                return true;
            }

            var ultimoBoleto = c.Tarjeta.Boletos
                .OrderByDescending(b => b.Fecha)
                .FirstOrDefault();

            if (ultimoBoleto == null)
            {
                return true;
            }

            TimeSpan tiempoTranscurrido = c.Ahora - ultimoBoleto.Fecha;
            TimeSpan intervaloMinimo = TimeSpan.FromMinutes(c.Tarjeta.TarjetaTipo.MinutosEntreViajes);

            if (tiempoTranscurrido < intervaloMinimo)
            {
                return false;
            }

            return true;
        }
    }
}
