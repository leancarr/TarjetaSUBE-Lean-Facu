using System;
using System.Collections.Generic;
using System.Linq;

namespace Transporte
{
    // SRP: EstacionBicicletas representa la estación del sistema Mi Bici Tu Bici,
    // gestionando el retiro, cobro de pase diario, liquidación de multas acumuladas
    // y devolución con control de tiempo de uso.
    public class EstacionBicicletas
    {
        public const decimal TarifaDiaria = 2418.75m;
        public const decimal MultaPorDemora = 1250m;
        public static readonly TimeSpan TiempoMaximoDeUso = TimeSpan.FromHours(1);

        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;

        private readonly IProveedorDeFecha reloj;

        public EstacionBicicletas() : this(string.Empty, new ProveedorDeFechaSistema())
        {
        }

        public EstacionBicicletas(string nombre) : this(nombre, new ProveedorDeFechaSistema())
        {
        }

        public EstacionBicicletas(string nombre, IProveedorDeFecha reloj)
        {
            Nombre = nombre;
            this.reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        }

        public decimal MultaPendiente(Tarjeta tarjeta)
        {
            if (tarjeta?.RetirosDeBici == null || tarjeta.RetirosDeBici.Count == 0)
            {
                return 0m;
            }

            int cantidadPendientes = tarjeta.RetirosDeBici.Count(r => r.TuvoDemora && !r.MultaSaldada);
            return cantidadPendientes * MultaPorDemora;
        }

        public bool RetirarBici(Tarjeta tarjeta, out RetiroDeBici? retiro)
        {
            if (tarjeta == null)
            {
                throw new ArgumentNullException(nameof(tarjeta));
            }

            // (a) Si la tarjeta ya tiene una bici en curso -> false
            if (tarjeta.RetirosDeBici != null && tarjeta.RetirosDeBici.Any(r => r.EstaEnCurso))
            {
                retiro = null;
                return false;
            }

            // (b) Determinar si corresponde cobrar tarifa y multas acumuladas
            if (CorrespondeCobrarTarifa(tarjeta, reloj.Ahora))
            {
                decimal tarifa = TarifaDiaria;
                decimal multa = MultaPendiente(tarjeta);
                decimal total = tarifa + multa;

                // (c) Si no alcanza para tarifa + multas -> false y nada cambia
                if (!tarjeta.TieneSaldoPara(total))
                {
                    retiro = null;
                    return false;
                }

                // Descontar total (tarifa + multas)
                tarjeta.Descontar(total);

                // Marcar multas pendientes como saldadas
                if (tarjeta.RetirosDeBici != null)
                {
                    var pendientes = tarjeta.RetirosDeBici.Where(r => r.TuvoDemora && !r.MultaSaldada).ToList();
                    foreach (var p in pendientes)
                    {
                        p.SaldarMulta();
                    }
                }

                retiro = new RetiroDeBici(reloj.Ahora, tarifa, tarjeta, this, multa);
                tarjeta.RetirosDeBici ??= new List<RetiroDeBici>();
                tarjeta.RetirosDeBici.Add(retiro);
                return true;
            }
            else
            {
                // Si el día ya está pago, no se cobra nada y las multas siguen pendientes
                retiro = new RetiroDeBici(reloj.Ahora, 0m, tarjeta, this, 0m);
                tarjeta.RetirosDeBici ??= new List<RetiroDeBici>();
                tarjeta.RetirosDeBici.Add(retiro);
                return true;
            }
        }

        public bool DevolverBici(Tarjeta tarjeta, out RetiroDeBici? retiro)
        {
            if (tarjeta == null)
            {
                throw new ArgumentNullException(nameof(tarjeta));
            }

            var retiroEnCurso = tarjeta.RetirosDeBici?.FirstOrDefault(r => r.EstaEnCurso);
            if (retiroEnCurso == null)
            {
                retiro = null;
                return false;
            }

            retiroEnCurso.Devolver(reloj.Ahora, TiempoMaximoDeUso);
            retiro = retiroEnCurso;
            return true;
        }

        private bool CorrespondeCobrarTarifa(Tarjeta tarjeta, DateTime fecha)
        {
            if (tarjeta.RetirosDeBici == null || tarjeta.RetirosDeBici.Count == 0)
            {
                return true;
            }

            bool yaPagoHoy = tarjeta.RetirosDeBici.Any(r => r.FechaRetiro.Date == fecha.Date && r.TarifaCobrada > 0);
            return !yaPagoHoy;
        }
    }
}
