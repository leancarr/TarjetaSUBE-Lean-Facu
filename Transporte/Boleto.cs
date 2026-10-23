using System;

namespace Transporte
{
    // SRP: Boleto funciona como un comprobante histórico inmutable de la transacción.
    // Almacena datos copiados del momento del viaje (fecha, tipo de tarjeta, línea,
    // total abonado, saldo restante, tarjetaId y si fue viaje plus).
    public class Boleto
    {
        public int Id { get; set; }
        public DateTime Fecha { get; private set; }
        public string TipoTarjeta { get; private set; } = string.Empty;
        public string Linea { get; private set; } = string.Empty;
        public decimal TotalAbonado { get; private set; }
        public decimal SaldoRestante { get; private set; }
        public bool EsViajePlus { get; private set; }

        public int TarjetaId { get; set; }
        public Tarjeta Tarjeta { get; set; } = null!;

        public int ColectivoId { get; set; }
        public Colectivo Colectivo { get; set; } = null!;

        // Propiedades para retrocompatibilidad con tests anteriores
        public DateTime FechaHora => Fecha;
        public decimal Monto => TotalAbonado;

        protected Boleto()
        {
        }

        public Boleto(DateTime fecha, string tipoTarjeta, string linea, decimal totalAbonado, decimal saldoRestante, int tarjetaId, Tarjeta tarjeta, Colectivo colectivo)
        {
            Fecha = fecha;
            TipoTarjeta = tipoTarjeta;
            Linea = linea;
            TotalAbonado = totalAbonado;
            SaldoRestante = saldoRestante;
            TarjetaId = tarjetaId;
            Tarjeta = tarjeta;
            Colectivo = colectivo;
            EsViajePlus = saldoRestante < 0;
        }

        // Constructores auxiliares para retrocompatibilidad con tests existentes
        public Boleto(decimal monto, decimal saldoRestante, Tarjeta tarjeta, Colectivo colectivo)
            : this(DateTime.Now, tarjeta?.TarjetaTipo?.Nombre ?? "Normal", colectivo?.Linea ?? string.Empty, monto, saldoRestante, tarjeta?.Id ?? 0, tarjeta!, colectivo!)
        {
        }

        public Boleto(decimal monto, decimal saldoRestante, Tarjeta tarjeta, Colectivo colectivo, DateTime fechaHora)
            : this(fechaHora, tarjeta?.TarjetaTipo?.Nombre ?? "Normal", colectivo?.Linea ?? string.Empty, monto, saldoRestante, tarjeta?.Id ?? 0, tarjeta!, colectivo!)
        {
        }
    }
}
