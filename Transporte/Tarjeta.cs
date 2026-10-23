using System;
using System.Collections.Generic;
using System.Linq;

namespace Transporte
{
    // SRP: Tarjeta gestiona exclusivamente el estado y las reglas de su saldo (cargas permitidas, límites y débitos)
    // y mantiene la referencia a su tipo de franquicia (TarjetaTipo).
    public class Tarjeta
    {
        public int Id { get; set; }
        public decimal Saldo { get; private set; }

        public int TarjetaTipoId { get; set; } = 1;
        public TarjetaTipo TarjetaTipo { get; set; } = new TarjetaTipo(1, "Normal", 0m);

        private readonly decimal[] montosAceptados = new decimal[] { 2000, 3000, 4000, 5000, 8000, 10000, 15000, 20000, 25000, 30000 };
        private const decimal LimiteSaldo = 40000;
        public const decimal LimiteNegativo = -2000;

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

        public bool PuedeDescontar(decimal monto)
        {
            return (Saldo - monto) >= LimiteNegativo;
        }

        public void Descontar(decimal monto)
        {
            Saldo -= monto;
        }
    }
}
