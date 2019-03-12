using System;
using UnityEngine;

namespace TSG.Core.Singleton
{
    public abstract class SingletonMonoBase<T> : MonoBehaviour
        where T : MonoBehaviour
    {
        private static readonly object THREAD_LOCK = new object();

        private static T _instance = null;

        public static T instance
        {
            get
            {
                if( _instance == null )
                {
                    lock( THREAD_LOCK )
                    {
                        if( _instance == null )
                        {
                            GameObject obj = new GameObject( typeof( T ).ToString(), typeof( T ) );
                            _instance = obj.AddMissingComponent<T>();
                        }
                    }
                }

                return _instance;
            }
        }
    }

    public abstract class SingletonBase<T> : IDisposable
        where T : class
    {
        private static readonly object THREAD_LOCK = new object();

        private static T _instance = null;

        public static T instance
        {
            get
            {
                if( _instance == null )
                {
                    lock( THREAD_LOCK )
                    {
                        if( _instance == null )
                        {
                            _instance = default( T );
                        }
                    }
                }

                return _instance;
            }
        }

        virtual public void Dispose() { }
    }

    public abstract class SingletonSceneMonoBase<T> : MonoBehaviour
        where T : MonoBehaviour
    {
        private static readonly object THREAD_LOCK = new object();

        private static T _instance = null;

        public static T instance
        {
            get
            {
                if( _instance == null )
                {
                    lock( THREAD_LOCK )
                    {
                        if( _instance == null )
                        {
                            _instance = FindObjectOfType<T>();
                            if( _instance == null )
                            {
                                GameObject obj = new GameObject( typeof( T ).ToString(), typeof( T ) );
                                _instance = obj.AddMissingComponent<T>();
                            }
                        }
                    }
                }

                return _instance;
            }
        }
    }
}
