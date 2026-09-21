using System;

namespace Transporte
{
    // SRP: Boleto funciona como un comprobante histórico de la transacción.
    // Almacena los datos del momento del pago (tarifa cobrada, saldo resultante, fecha/hora y entidades relacionadas)
    // sin contener lógica de negocio ni modificar el estado de otros objetos.
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

        public Boleto()
        {
        }

        public Boleto(decimal monto, decimal saldoRestante, Tarjeta tarjeta, Colectivo colectivo)
        {
            FechaHora = DateTime.Now;
            Monto = monto;
            SaldoRestante = saldoRestante;
            Tarjeta = tarjeta;
            Colectivo = colectivo;
        }

        public Boleto(decimal monto, decimal saldoRestante, Tarjeta tarjeta, Colectivo colectivo, DateTime fechaHora)
        {
            FechaHora = fechaHora;
            Monto = monto;
            SaldoRestante = saldoRestante;
            Tarjeta = tarjeta;
            Colectivo = colectivo;
        }
    }
}
