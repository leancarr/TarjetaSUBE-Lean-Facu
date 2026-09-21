using System;
using System.Collections.Generic;
using System.Linq;

namespace Transporte
{
    // SRP: Tarjeta gestiona exclusivamente el estado y las reglas de su saldo (cargas permitidas, límites y débitos).
    // No conoce conceptos de Colectivo, tarifas de transporte ni emisión de boletos.
    public class Tarjeta
    {
        public int Id { get; set; }
        public decimal Saldo { get; private set; }

        private readonly decimal[] montosAceptados = new decimal[] { 2000, 3000, 4000, 5000, 8000, 10000, 15000, 20000, 25000, 30000 };
        private const decimal LimiteSaldo = 40000;

        public void Cargar(decimal monto)
        {
            if (!montosAceptados.Contains(monto))
            {
                throw new MontoDeCargaInvalidoException($"El monto {monto} no es un monto de carga aceptado.");
            }

            if (Saldo + monto > LimiteSaldo)
            {
                throw new LimiteSaldoExcedidoException($"La carga supera el limite maximo de {LimiteSaldo}.");
            }

            Saldo += monto;
        }

        public void Debitar(decimal monto)
        {
            if (Saldo < monto)
            {
                throw new SaldoInsuficienteException("Saldo insuficiente para realizar el pago.");
            }

            Saldo -= monto;
        }
    }
}
