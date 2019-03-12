using System;
using System.Collections.Generic;
using TSG.Core.Singleton;

namespace TSG.Core.EventSystem
{
    public class EventManager : SingletonMonoBase<EventManager>
    {
        private Dictionary<string, Action<IEventWrapper>> _eventDict = new Dictionary<string, Action<IEventWrapper>>();

        // this helps to ensure callbacks are only registered once per event
        private Dictionary<string, Dictionary<Delegate, Action<IEventWrapper>>> _eventSearchDict = new Dictionary<string, Dictionary<Delegate, Action<IEventWrapper>>>();
        
        //===========================================================
        // Add Listener Methods
        //===========================================================
        /// <summary>
        /// Wraps pFunc in an IEventWrapper object and stores in a dictionary of event listeners and delegates
        /// using pKey as the event lookup
        /// </summary>
        /// <param name="pKey"></param>
        /// <param name="pFunc"></param>
        public void AddListener( string pKey, Action pFunc )
        {
            Action<IEventWrapper> wrapperDel = ( p ) => pFunc();

            InternalAddListener( pKey, pFunc, wrapperDel );
        }

        public void AddListener<TData>( string pKey, Action<TData> pFunc )
        {
            Action<IEventWrapper> wrapperDel = ( p ) => pFunc( ( ( EventWrapper<TData> ) p ).data );

            InternalAddListener( pKey, pFunc, wrapperDel );
        }

        public void AddListener<TData1, TData2>( string pKey, Action<TData1, TData2> pFunc )
        {
            Action<IEventWrapper> wrapperDel = ( p ) => pFunc( 
                ( ( EventWrapper<TData1, TData2> ) p ).data1, 
                ( ( EventWrapper<TData1, TData2> ) p ).data2 );

            InternalAddListener( pKey, pFunc, wrapperDel );
        }

        public void AddListener<TData1, TData2, TData3>( string pKey, Action<TData1, TData2, TData3> pFunc )
        {
            Action<IEventWrapper> wrapperDel = ( p ) => pFunc( 
                ( ( EventWrapper<TData1, TData2, TData3> ) p ).data1, 
                ( ( EventWrapper<TData1, TData2, TData3> ) p ).data2, 
                ( ( EventWrapper<TData1, TData2, TData3> ) p ).data3 );

            InternalAddListener( pKey, pFunc, wrapperDel );
        }

        public void AddListener<TData1, TData2, TData3, TData4>( string pKey, Action<TData1, TData2, TData3, TData4> pFunc )
        {
            Action<IEventWrapper> wrapperDel = ( p ) => pFunc( 
                ( ( EventWrapper<TData1, TData2, TData3, TData4> ) p ).data1, 
                ( ( EventWrapper<TData1, TData2, TData3, TData4> ) p ).data2, 
                ( ( EventWrapper<TData1, TData2, TData3, TData4> ) p ).data3,
                ( ( EventWrapper<TData1, TData2, TData3, TData4> ) p ).data4 );

            InternalAddListener( pKey, pFunc, wrapperDel );
        }

        //===========================================================
        // Remove Listener Methods
        //===========================================================
        /// <summary>
        /// Removes listeners and delegates from map
        /// </summary>
        /// <param name="pKey"></param>
        /// <param name="pFunc"></param>
        public void RemoveListener( string pKey, Action pFunc )
        {
            InternalRemoveListener( pKey, pFunc );
        }

        public void RemoveListener<TData>( string pKey, Action<TData> pFunc )
        {
            InternalRemoveListener( pKey, pFunc );
        }

        public void RemoveListener<TData1, TData2>( string pKey, Action<TData1, TData2> pFunc )
        {
            InternalRemoveListener( pKey, pFunc );
        }

        public void RemoveListener<TData1, TData2, TData3>( string pKey, Action<TData1, TData2, TData3> pFunc )
        {
            InternalRemoveListener( pKey, pFunc );
        }

        public void RemoveListener<TData1, TData2, TData3, TData4>( string pKey, Action<TData1, TData2, TData3, TData4> pFunc )
        {
            InternalRemoveListener( pKey, pFunc );
        }

        //===========================================================
        // Send Event Methods
        //===========================================================
        // optimize performance for non-data events
        EventWrapper _cachedWrapper = new EventWrapper();
        /// <summary>
        /// Send event with delegates to listeners
        /// </summary>
        /// <param name="pKey"></param>
        public void SendEvent( string pKey )
        {
            InternalSendNotification( pKey, _cachedWrapper );
        }

        public void SendEvent<TData>( string pKey, TData pData )
        {
            InternalSendNotification( pKey, new EventWrapper<TData>( pData ) );
        }

        public void SendEvent<TData1, TData2>( string pKey, TData1 pData1, TData2 pData2 )
        {
            InternalSendNotification( pKey, new EventWrapper<TData1, TData2>( pData1, pData2 ) );
        }

        public void SendEvent<TData1, TData2, TData3>( string pKey, TData1 pData1, TData2 pData2, TData3 pData3 )
        {
            InternalSendNotification( pKey, new EventWrapper<TData1, TData2, TData3>( pData1, pData2, pData3 ) );
        }

        public void SendEvent<TData1, TData2, TData3, TData4>(string pKey, TData1 pData1, TData2 pData2, TData3 pData3, TData4 pData4)
        {
            InternalSendNotification(pKey, new EventWrapper<TData1, TData2, TData3, TData4>(pData1, pData2, pData3, pData4));
        }
        public void SendEvent<TData1, TData2, TData3, TData4, TData5>(string pKey, TData1 pData1, TData2 pData2, TData3 pData3, TData4 pData4, TData5 pData5)
        {
            InternalSendNotification(pKey, new EventWrapper<TData1, TData2, TData3, TData4, TData5>(pData1, pData2, pData3, pData4, pData5));
        }

        //===========================================================
        // Private Methods
        //===========================================================
        private void InternalAddListener( string pKey, Delegate pFunc, Action<IEventWrapper> pAction )
        {
            // if new key and add listener to delegate
            if( !_eventDict.ContainsKey( pKey ) )
            {
                _eventDict.Add( pKey, pAction );
                _eventSearchDict.Add( pKey, new Dictionary<Delegate, Action<IEventWrapper>>() );
                _eventSearchDict[ pKey ].Add( pFunc, pAction );
            }
            // add listener to delegate if needed.
            else if( !_eventSearchDict[ pKey ].ContainsKey( pFunc ) )
            {
                _eventDict[ pKey ] += pAction;
                _eventSearchDict[ pKey ].Add( pFunc, pAction );
            }
        }

        private void InternalRemoveListener( string pKey, Delegate pFunc )
        {
            if( _eventDict.ContainsKey( pKey ) )
            {
                if( _eventSearchDict[ pKey ].ContainsKey( pFunc ) )
                {
                    //Remove the listener from the delegate.
                    _eventDict[ pKey ] -= _eventSearchDict[ pKey ][ pFunc ];

                    //Remove the listener from the search dictionary.
                    _eventSearchDict[ pKey ].Remove( pFunc );

                    //If there is no listener in the delegate, remove the delegate.
                    if( _eventDict[ pKey ] == null )
                    {
                        _eventDict.Remove( pKey );
                        _eventSearchDict.Remove( pKey );
                    }
                }
            }
        }

        private void InternalSendNotification( string pKey, IEventWrapper pEventWrapper )
        {
            if( _eventDict.ContainsKey( pKey ) && _eventDict[ pKey ] != null )
            {
                // send event with delegates to listeners
                _eventDict[ pKey ]( pEventWrapper );
            }
        }

        private void OnDestroy()
        {
            if( _eventDict != null )
            {
                _eventDict.Clear();
                _eventDict = null;
            }

            if( _eventSearchDict != null )
            {
                _eventSearchDict.Clear();
                _eventSearchDict = null;
            }
        }
    }
}