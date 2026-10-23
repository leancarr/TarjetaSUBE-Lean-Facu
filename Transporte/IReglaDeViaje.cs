namespace Transporte
{
    public interface IReglaDeViaje
    {
        int Orden { get; }
        bool AplicaA(ContextoDeViaje c);
        bool Evaluar(ContextoDeViaje c);
    }
}
