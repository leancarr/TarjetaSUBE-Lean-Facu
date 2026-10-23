using System;
using System.Collections.Generic;
using System.Linq;

namespace Transporte
{
    // SRP: Colectivo se encarga de representar la unidad y orquestar el cobro del pasaje,
    // delegando la deducción a la tarjeta y emitiendo el comprobante correspondiente.
    // OCP: Las reglas de cobro (franquicias, límites de uso, trasbordos) se evalúan mediante
    // la lista inyectable de IReglaDeViaje ordenadas por Orden.
    public class Colectivo
    {
        public const decimal TarifaBasica = 1580;

        public int Id { get; set; }
        public string Linea { get; set; } = string.Empty;

        private readonly IProveedorDeFecha reloj;
        private readonly List<IReglaDeViaje> reglas;

        protected virtual decimal TarifaBase => TarifaBasica;

        public Colectivo() : this(string.Empty, new ProveedorDeFechaSistema(), ReglasDeViaje.PorDefecto())
        {
        }

        public Colectivo(string linea) : this(linea, new ProveedorDeFechaSistema(), ReglasDeViaje.PorDefecto())
        {
        }

        public Colectivo(string linea, IProveedorDeFecha reloj) : this(linea, reloj, ReglasDeViaje.PorDefecto())
        {
        }

        public Colectivo(string linea, IProveedorDeFecha reloj, IEnumerable<IReglaDeViaje> reglas)
        {
            Linea = linea;
            this.reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
            this.reglas = (reglas ?? ReglasDeViaje.PorDefecto()).OrderBy(r => r.Orden).ToList();
        }

        public bool PagarCon(Tarjeta tarjeta, out Boleto? boleto)
        {
            if (tarjeta == null)
            {
                throw new ArgumentNullException(nameof(tarjeta));
            }

            var contexto = new ContextoDeViaje(tarjeta, this, reloj.Ahora, TarifaBase);

            foreach (var regla in reglas)
            {
                if (regla.AplicaA(contexto))
                {
                    if (!regla.Evaluar(contexto))
                    {
                        boleto = null;
                        return false;
                    }
                }
            }

            if (!tarjeta.PuedeDescontar(contexto.Tarifa))
            {
                boleto = null;
                return false;
            }

            tarjeta.Descontar(contexto.Tarifa);
            string tipoTarjeta = tarjeta.TarjetaTipo?.Nombre ?? "Normal";
            boleto = new Boleto(contexto.Ahora, tipoTarjeta, Linea, contexto.Tarifa, tarjeta.Saldo, tarjeta.Id, tarjeta, this);
            tarjeta.Boletos.Add(boleto);
            return true;
        }

        public bool pagarCon(Tarjeta tarjeta, out Boleto? boleto)
        {
            return PagarCon(tarjeta, out boleto);
        }
    }
}
