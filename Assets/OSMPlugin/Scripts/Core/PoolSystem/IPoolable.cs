namespace TSG.Core.PoolSystem
{
    public interface IPoolable<T> : System.IDisposable
    {
        T Create( int pIndex );
    }
}