using System;

namespace Transporte
{
    // SRP: Colectivo se encarga de representar la unidad y orquestar el cobro del pasaje,
    // delegando la deducción a la tarjeta y emitiendo el comprobante correspondiente.
    // OCP: En esta iteración la tarifa básica es fija (). Para futuras iteraciones (medio boleto,
    // transbordos) este esquema permite extender el cálculo de tarifas sin modificar la estructura interna de Tarjeta.
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

            if (!tarjeta.PuedeDescontar(TarifaBasica))
            {
                boleto = null;
                return false;
            }

            tarjeta.Descontar(TarifaBasica);
            boleto = new Boleto(TarifaBasica, tarjeta.Saldo, tarjeta, this);
            return true;
        }

        public bool pagarCon(Tarjeta tarjeta, out Boleto? boleto)
        {
            return PagarCon(tarjeta, out boleto);
        }
    }
}
