using System.Collections.Generic;

namespace Transporte
{
    public static class TiposDeTarjeta
    {
        public static TarjetaTipo Normal => new TarjetaTipo(1, "Normal", 0m, 0, null);
        public static TarjetaTipo MedioBoleto => new TarjetaTipo(2, "Medio boleto estudiantil", 0.5m, 5, 2);
        public static TarjetaTipo BoletoGratuito => new TarjetaTipo(3, "Boleto gratuito estudiantil", 1.0m, 0, 2);
        public static TarjetaTipo FranquiciaCompleta => new TarjetaTipo(4, "Franquicia completa", 1.0m, 0, null);

        public static IEnumerable<TarjetaTipo> Todos => new[]
        {
            Normal,
            MedioBoleto,
            BoletoGratuito,
            FranquiciaCompleta
        };
    }
}
