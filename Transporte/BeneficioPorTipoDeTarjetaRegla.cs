namespace Transporte
{
    public class BeneficioPorTipoDeTarjetaRegla : IReglaDeViaje
    {
        public int Orden => 40;

        public bool AplicaA(ContextoDeViaje c) => !c.EsTrasbordo;

        public bool Evaluar(ContextoDeViaje c)
        {
            decimal porcentajeDescuento = c.Tarjeta?.TarjetaTipo != null
                ? c.Tarjeta.TarjetaTipo.PorcentajeDescuento
                : 0m;

            c.Tarifa *= (1m - porcentajeDescuento);
            return true;
        }
    }
}
