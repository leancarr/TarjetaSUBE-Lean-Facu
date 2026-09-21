using System;

namespace Transporte
{
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
