using System;
using NUnit.Framework;

namespace Transporte.Tests
{
    public class ProveedorDeFechaTest
    {
        [Test]
        public void ProveedorDeFechaSistema_RetornaHoraActualAproximada()
        {
            var proveedor = new ProveedorDeFechaSistema();
            var ahora = DateTime.Now;

            Assert.IsTrue((proveedor.Ahora - ahora).TotalSeconds < 2);
        }

        [Test]
        public void ProveedorDeFechaFalso_AvanzaTiempoCorrectamente()
        {
            var fechaInicial = new DateTime(2026, 10, 12, 10, 0, 0);
            var reloj = new ProveedorDeFechaFalso(fechaInicial);

            Assert.AreEqual(fechaInicial, reloj.Ahora);

            reloj.Avanzar(TimeSpan.FromMinutes(5));
            Assert.AreEqual(fechaInicial.AddMinutes(5), reloj.Ahora);
        }
    }
}
