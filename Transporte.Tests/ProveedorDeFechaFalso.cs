using System;

namespace Transporte.Tests
{
    public class ProveedorDeFechaFalso : IProveedorDeFecha
    {
        public DateTime Ahora { get; set; }

        public ProveedorDeFechaFalso(DateTime fechaInicial)
        {
            Ahora = fechaInicial;
        }

        public void Avanzar(TimeSpan tiempo)
        {
            Ahora = Ahora.Add(tiempo);
        }
    }
}
