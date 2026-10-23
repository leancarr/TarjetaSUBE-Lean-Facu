using System;

namespace Transporte
{
    // SRP: Colectivo se encarga de representar la unidad y orquestar el cobro del pasaje,
    // delegando la deducción a la tarjeta y emitiendo el comprobante correspondiente.
    // OCP: El costo del pasaje se calcula a partir del PorcentajeDescuento de TarjetaTipo,
    // permitiendo incorporar nuevas franquicias sin modificar la lógica de cobro.
    public class Colectivo
    {
        public const decimal TarifaBasica = 1580;

        public int Id { get; set; }
        public string Linea { get; set; } = string.Empty;

        public Colectivo()
        {
        }

        public Colectivo(string linea)
        {
            Linea = linea;
        }

        public bool PagarCon(Tarjeta tarjeta, out Boleto? boleto)
        {
            if (tarjeta == null)
            {
                throw new ArgumentNullException(nameof(tarjeta));
            }

            decimal porcentajeDescuento = tarjeta.TarjetaTipo != null ? tarjeta.TarjetaTipo.PorcentajeDescuento : 0m;
            decimal montoAPagar = TarifaBasica * (1m - porcentajeDescuento);

            if (!tarjeta.PuedeDescontar(montoAPagar))
            {
                boleto = null;
                return false;
            }

            tarjeta.Descontar(montoAPagar);
            boleto = new Boleto(montoAPagar, tarjeta.Saldo, tarjeta, this);
            return true;
        }

        public bool pagarCon(Tarjeta tarjeta, out Boleto? boleto)
        {
            return PagarCon(tarjeta, out boleto);
        }
    }
}
