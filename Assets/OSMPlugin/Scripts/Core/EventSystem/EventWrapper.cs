namespace TSG.Core.EventSystem
{
    // Wrappers for events
    public class EventWrapper : IEventWrapper
    {
    }

    public class EventWrapper<TData> : IEventWrapper
    {
        public TData data;

        public EventWrapper( TData pData )
        {
            data = pData;
        }
    }

    public class EventWrapper<TData1, TData2> : IEventWrapper
    {
        public TData1 data1;
        public TData2 data2;

        public EventWrapper( TData1 pData1, TData2 pData2 )
        {
            data1 = pData1;
            data2 = pData2;
        }
    }

    public class EventWrapper<TData1, TData2, TData3> : IEventWrapper
    {
        public TData1 data1;
        public TData2 data2;
        public TData3 data3;

        public EventWrapper( TData1 pData1, TData2 pData2, TData3 pData3 )
        {
            data1 = pData1;
            data2 = pData2;
            data3 = pData3;
        }
    }

    public class EventWrapper<TData1, TData2, TData3, TData4> : IEventWrapper
    {
        public TData1 data1;
        public TData2 data2;
        public TData3 data3;
        public TData4 data4;

        public EventWrapper(TData1 pData1, TData2 pData2, TData3 pData3, TData4 pData4)
        {
            data1 = pData1;
            data2 = pData2;
            data3 = pData3;
            data4 = pData4;
        }
    }

    public class EventWrapper<TData1, TData2, TData3, TData4, TData5> : IEventWrapper
    {
        public TData1 data1;
        public TData2 data2;
        public TData3 data3;
        public TData4 data4;
        public TData5 data5;

        public EventWrapper(TData1 pData1, TData2 pData2, TData3 pData3, TData4 pData4, TData5 pData5)
        {
            data1 = pData1;
            data2 = pData2;
            data3 = pData3;
            data4 = pData4;
            data5 = pData5;
        }
    }
}