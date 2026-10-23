using System;

namespace Transporte
{
    public class ProveedorDeFechaSistema : IProveedorDeFecha
    {
        public DateTime Ahora => DateTime.Now;
    }
}
