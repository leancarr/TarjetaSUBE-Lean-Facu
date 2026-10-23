using System;

namespace Transporte
{
    public class ContextoDeViaje
    {
        public Tarjeta Tarjeta { get; set; }
        public Colectivo Colectivo { get; set; }
        public DateTime Ahora { get; set; }
        public decimal Tarifa { get; set; }
        public bool EsTrasbordo { get; set; }

        public ContextoDeViaje(Tarjeta tarjeta, Colectivo colectivo, DateTime ahora, decimal tarifaInicial)
        {
            Tarjeta = tarjeta;
            Colectivo = colectivo;
            Ahora = ahora;
            Tarifa = tarifaInicial;
            EsTrasbordo = false;
        }
    }
}
