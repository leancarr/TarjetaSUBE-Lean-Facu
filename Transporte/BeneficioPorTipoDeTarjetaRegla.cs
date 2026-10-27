using System;
using System.Linq;

namespace Transporte
{
    public class BeneficioPorTipoDeTarjetaRegla : IReglaDeViaje
    {
        public int Orden => 40;

        public bool AplicaA(ContextoDeViaje c) => !c.EsTrasbordo;

        public bool Evaluar(ContextoDeViaje c)
        {
            var tipo = c.Tarjeta?.TarjetaTipo;
            if (tipo == null)
            {
                return true;
            }

            decimal porcentajeDescuento = tipo.PorcentajeDescuento;

            if (tipo.MaxViajesConBeneficioPorDia.HasValue && ViajesDeHoy(c) >= tipo.MaxViajesConBeneficioPorDia.Value)
            {
                porcentajeDescuento = 0m;
            }

            c.Tarifa *= (1m - porcentajeDescuento);
            return true;
        }

        private int ViajesDeHoy(ContextoDeViaje c)
        {
            if (c.Tarjeta?.Boletos == null)
            {
                return 0;
            }

            return c.Tarjeta.Boletos.Count(b => b.Fecha.Date == c.Ahora.Date);
        }
    }
}
