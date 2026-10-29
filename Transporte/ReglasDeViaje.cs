using System.Collections.Generic;

namespace Transporte
{
    public static class ReglasDeViaje
    {
        public static IEnumerable<IReglaDeViaje> PorDefecto()
        {
            return new List<IReglaDeViaje>
            {
                new FranjaHorariaFranquiciaRegla(),
                new IntervaloMinimoEntreViajesRegla(),
                new TrasbordoRegla(),
                new BeneficioPorTipoDeTarjetaRegla(),
                new UsoFrecuenteRegla()
            };
        }
    }
}
