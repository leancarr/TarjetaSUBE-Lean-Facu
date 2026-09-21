using System;

namespace Transporte
{
    // SRP: Colectivo se encarga de representar la unidad y orquestar el cobro del pasaje,
    // delegando la deducción a la tarjeta y emitiendo el comprobante correspondiente.
    // OCP: En esta iteración la tarifa básica es fija ($1580). Para futuras iteraciones (medio boleto,
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

        public Boleto PagarCon(Tarjeta tarjeta)
        {
            if (tarjeta == null)
            {
                throw new ArgumentNullException(nameof(tarjeta));
            }

            tarjeta.Debitar(TarifaBasica);

            return new Boleto(TarifaBasica, tarjeta.Saldo, tarjeta, this);
        }

        public Boleto pagarCon(Tarjeta tarjeta)
        {
            return PagarCon(tarjeta);
        }
    }
}
