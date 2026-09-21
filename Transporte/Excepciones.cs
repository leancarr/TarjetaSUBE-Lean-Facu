using System;

namespace Transporte
{
    public class MontoDeCargaInvalidoException : Exception
    {
        public MontoDeCargaInvalidoException(string message) : base(message) { }
    }

    public class LimiteSaldoExcedidoException : Exception
    {
        public LimiteSaldoExcedidoException(string message) : base(message) { }
    }

    public class SaldoInsuficienteException : Exception
    {
        public SaldoInsuficienteException(string message) : base(message) { }
    }
}
