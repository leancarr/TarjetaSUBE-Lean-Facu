using System;

namespace Transporte
{
    // SRP / OCP: TarjetaTipo modela los distintos tipos de franquicia como datos parametrizados.
    // Permite agregar nuevos tipos de tarjeta sin necesidad de crear nuevas subclases en el dominio.
    public class TarjetaTipo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal PorcentajeDescuento { get; set; }
        public int MinutosEntreViajes { get; set; }
        public int? MaxViajesConBeneficioPorDia { get; set; }

        public TarjetaTipo()
        {
        }

        public TarjetaTipo(int id, string nombre, decimal porcentajeDescuento, int minutosEntreViajes = 0, int? maxViajesConBeneficioPorDia = null)
        {
            Id = id;
            Nombre = nombre;
            PorcentajeDescuento = porcentajeDescuento;
            MinutosEntreViajes = minutosEntreViajes;
            MaxViajesConBeneficioPorDia = maxViajesConBeneficioPorDia;
        }
    }
}
