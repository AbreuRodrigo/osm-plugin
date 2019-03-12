using System.Collections.Generic;


namespace TSG.Core.PoolSystem
{
    public class GenericPool<T> : System.IDisposable where T : class, IPoolable<T>
    {
        private System.Action<T> _onAfterCreate;
        private System.Action<T> _onSpawnObject;
        private System.Action<T> _onUnspawnObject;

        private HashSet<T> _objList;
        private Stack<T> _availableObjects;

        private readonly object _spawnThreadLock = new object();
        private readonly object _unspawnThreadLock = new object();

        public GenericPool( int pPoolSize, T pSource, 
            System.Action<T> pOnAfterCreate = null,
            System.Action<T> pOnSpawnObject = null,
            System.Action<T> pOnUnspawnObject = null )
        {
            _onAfterCreate = pOnAfterCreate;
            _onSpawnObject = pOnSpawnObject;
            _onUnspawnObject = pOnUnspawnObject;

            _objList = new HashSet<T>();
            _availableObjects = new Stack<T>( pPoolSize );
            for( int i = 0; i < pPoolSize; ++i )
            {
                T obj = pSource.Create( i );
                _objList.Add( obj );
                _availableObjects.Push( obj );

                _onAfterCreate?.Invoke( obj );
            }
        }

        public void Dispose()
        {
            if( _objList != null )
            {
                _objList.Clear();
                _objList = null;
            }

            if( _availableObjects != null )
            {
                _availableObjects.Clear();
                _availableObjects = null;
            }

            _onAfterCreate = null;
            _onSpawnObject = null;
            _onUnspawnObject = null;
        }

        public T Spawn()
        {
            T result = default( T );

            // this will ensure that calls to spawn are thread-safe
            lock( _spawnThreadLock )
            {
                if( _availableObjects.Count > 0 )
                    result = _availableObjects.Pop();

                _onSpawnObject?.Invoke( result );
            }
            return result;
        }

        public void Unspawn( T pObj )
        {
            // this will ensure we don't try to unspawn the same object more than once
            lock( _unspawnThreadLock )
            {
                if( !_objList.Contains( pObj ) )
                    return;

                _onUnspawnObject?.Invoke( pObj );
                _availableObjects.Push( pObj );
            }
        }
    }
}