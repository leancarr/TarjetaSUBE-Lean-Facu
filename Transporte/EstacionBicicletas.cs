using System;
using System.Collections.Generic;
using System.Linq;

namespace Transporte
{
    // SRP: EstacionBicicletas representa la estación del sistema Mi Bici Tu Bici,
    // gestionando el retiro, cobro de pase diario y devolución con control de tiempo de uso.
    public class EstacionBicicletas
    {
        public const decimal TarifaDiaria = 2418.75m;
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

            // (b) Si hoy ya se pagó el día no se cobra; si no, corresponde cobrar TarifaDiaria
            decimal tarifaACobrar = CorrespondeCobrarTarifa(tarjeta, reloj.Ahora) ? TarifaDiaria : 0m;

            // (c) Si corresponde cobrar y !TieneSaldoPara(tarifa) -> false y nada cambia
            if (tarifaACobrar > 0 && !tarjeta.TieneSaldoPara(tarifaACobrar))
            {
                retiro = null;
                return false;
            }

            // (d) Descontar, crear el RetiroDeBici y agregarlo al historial -> true
            if (tarifaACobrar > 0)
            {
                tarjeta.Descontar(tarifaACobrar);
            }

            retiro = new RetiroDeBici(reloj.Ahora, tarifaACobrar, tarjeta, this);
            tarjeta.RetirosDeBici ??= new List<RetiroDeBici>();
            tarjeta.RetirosDeBici.Add(retiro);
            return true;
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
