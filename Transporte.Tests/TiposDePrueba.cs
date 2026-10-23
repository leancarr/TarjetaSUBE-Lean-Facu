using System;

namespace Transporte.Tests
{
    public static class TiposDePrueba
    {
        public static TarjetaTipo Normal => new TarjetaTipo(1, "Normal", 0m);
        public static TarjetaTipo MedioBoleto => new TarjetaTipo(2, "Medio boleto estudiantil", 0.5m);
        public static TarjetaTipo BoletoGratuito => new TarjetaTipo(3, "Boleto gratuito estudiantil", 1.0m);
        public static TarjetaTipo FranquiciaCompleta => new TarjetaTipo(4, "Franquicia completa", 1.0m);
    }
}
