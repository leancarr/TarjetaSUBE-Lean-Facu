using System;

namespace Transporte
{
    // SRP: RetiroDeBici representa el comprobante y ciclo de vida de un retiro de bicicleta pública,
    // registrando la tarifa cobrada, multas por demora, fechas de retiro/devolución y estado de mora.
    public class RetiroDeBici
    {
        public int Id { get; set; }

        public int TarjetaId { get; set; }
        public Tarjeta Tarjeta { get; set; } = null!;

        public int EstacionId { get; set; }
        public EstacionBicicletas Estacion { get; set; } = null!;

        public DateTime FechaRetiro { get; private set; }
        public DateTime? FechaDevolucion { get; private set; }
        public decimal TarifaCobrada { get; private set; }
        public decimal MultaCobrada { get; private set; }
        public bool TuvoDemora { get; private set; }
        public bool MultaSaldada { get; private set; }

        public bool EstaEnCurso => !FechaDevolucion.HasValue;

        protected RetiroDeBici()
        {
        }

        public RetiroDeBici(DateTime fechaRetiro, decimal tarifaCobrada, Tarjeta tarjeta, EstacionBicicletas estacion, decimal multaCobrada = 0m)
        {
            FechaRetiro = fechaRetiro;
            TarifaCobrada = tarifaCobrada;
            Tarjeta = tarjeta ?? throw new ArgumentNullException(nameof(tarjeta));
            TarjetaId = tarjeta.Id;
            Estacion = estacion ?? throw new ArgumentNullException(nameof(estacion));
            EstacionId = estacion.Id;
            FechaDevolucion = null;
            TuvoDemora = false;
            MultaCobrada = multaCobrada;
            MultaSaldada = false;
        }

        public void Devolver(DateTime fecha, TimeSpan tiempoMaximo)
        {
            FechaDevolucion = fecha;
            TuvoDemora = (fecha - FechaRetiro) > tiempoMaximo;
        }

        public void SaldarMulta()
        {
            MultaSaldada = true;
        }
    }
}
