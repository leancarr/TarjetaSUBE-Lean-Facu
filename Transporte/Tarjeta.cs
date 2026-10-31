using System;
using System.Collections.Generic;
using System.Linq;

namespace Transporte
{
    // SRP: Tarjeta gestiona exclusivamente el estado y las reglas de su saldo (cargas permitidas, límites, saldos pendientes y débitos)
    // y mantiene la referencia a su tipo de franquicia (TarjetaTipo), el historial de boletos y retiros de bicicletas emitidos.
    public class Tarjeta
    {
        public int Id { get; set; }
        public decimal Saldo { get; private set; }
        public decimal SaldoPendiente { get; private set; }

        public int TarjetaTipoId { get; set; } = 1;
        public TarjetaTipo TarjetaTipo { get; set; } = new TarjetaTipo(1, "Normal", 0m);

        public List<Boleto> Boletos { get; set; } = new List<Boleto>();
        public List<RetiroDeBici> RetirosDeBici { get; set; } = new List<RetiroDeBici>();

        private readonly decimal[] montosAceptados = new decimal[] { 2000, 3000, 4000, 5000, 8000, 10000, 15000, 20000, 25000, 30000 };
        public const decimal SaldoMaximo = 56000;
        public const decimal LimiteNegativo = -2000;

        public void Cargar(decimal monto)
        {
            if (!montosAceptados.Contains(monto))
            {
                throw new MontoDeCargaInvalidoException($"El monto {monto} no es un monto de carga aceptado.");
            }

            SaldoPendiente += monto;
            AcreditarCarga();
        }

        public void AcreditarCarga()
        {
            if (SaldoPendiente <= 0 || Saldo >= SaldoMaximo)
            {
                return;
            }

            decimal disponible = SaldoMaximo - Saldo;
            decimal montoAAcreditar = Math.Min(SaldoPendiente, disponible);

            Saldo += montoAAcreditar;
            SaldoPendiente -= montoAAcreditar;
        }

        public bool PuedeDescontar(decimal monto)
        {
            return (Saldo - monto) >= LimiteNegativo;
        }

        public bool TieneSaldoPara(decimal monto)
        {
            return Saldo >= monto;
        }

        public void Descontar(decimal monto)
        {
            Saldo -= monto;
            AcreditarCarga();
        }
    }
}
