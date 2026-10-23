using System;

namespace Transporte.Tests
{
    // Mantiene compatibilidad con tests existentes delegando a la definición canónica en dominio (TiposDeTarjeta)
    public static class TiposDePrueba
    {
        public static TarjetaTipo Normal => TiposDeTarjeta.Normal;
        public static TarjetaTipo MedioBoleto => TiposDeTarjeta.MedioBoleto;
        public static TarjetaTipo BoletoGratuito => TiposDeTarjeta.BoletoGratuito;
        public static TarjetaTipo FranquiciaCompleta => TiposDeTarjeta.FranquiciaCompleta;
    }
}
