using System;
using System.Collections.Generic;

namespace Transporte
{
    public class Boleto
    {
        public int Id { get; set; }
        public DateTime FechaHora { get; set; }
        public decimal Monto { get; set; }
        public decimal SaldoRestante { get; set; }
        
        public int TarjetaId { get; set; }
        public Tarjeta Tarjeta { get; set; } = null!;
        
        public int ColectivoId { get; set; }
        public Colectivo Colectivo { get; set; } = null!;
    }
}
