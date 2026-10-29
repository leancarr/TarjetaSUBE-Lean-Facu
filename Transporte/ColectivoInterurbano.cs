using System;
using System.Collections.Generic;

namespace Transporte
{
    // OCP / LSP: ColectivoInterurbano extiende Colectivo especializando la tarifa base
    // a $3000 sin modificar la lógica de cobro ni las reglas de viaje existentes.
    public class ColectivoInterurbano : Colectivo
    {
        public const decimal TarifaInterurbana = 3000;

        protected override decimal TarifaBase => TarifaInterurbana;

        public ColectivoInterurbano() : base()
        {
        }

        public ColectivoInterurbano(string linea) : base(linea)
        {
        }

        public ColectivoInterurbano(string linea, IProveedorDeFecha reloj) : base(linea, reloj)
        {
        }

        public ColectivoInterurbano(string linea, IProveedorDeFecha reloj, IEnumerable<IReglaDeViaje> reglas)
            : base(linea, reloj, reglas)
        {
        }
    }
}
