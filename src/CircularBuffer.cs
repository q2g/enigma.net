namespace enigma
{
    using NLog;
    #region Usings
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    #endregion

    public class CircularBuffer<T>
    {
        protected T[] buffer;
        protected long nextFree;

        public CircularBuffer(int length)
        {
            buffer = new T[length];
            nextFree = 0;
        }

        public void Add(T o)
        {
            Interlocked.Increment(ref nextFree);
            var tmpNextFree = nextFree % buffer.Length;
            buffer[tmpNextFree] = o;
        }

        public List<T> GetBuffer()
        {
            return buffer.ToList();
        }

        public void ResetBuffer()
        {
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = default(T);
        }
    }

    public class CircularBufferLong : CircularBuffer<long>
    {
        #region Logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        #endregion

        #region ctor
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="length"></param>
        public CircularBufferLong(int length) : base(length)
        {

        }
        #endregion
        /// <summary>
        /// last Calculated Average
        /// </summary>
        public long LastAverage { get; set; }
        /// <summary>
        /// Calc Average
        /// </summary>
        /// <returns></returns>
        public long CalcAverage()
        {
            long average = -1;
            try
            {
                var values = GetBuffer();
                long sumtimes = values.Sum();
                average = sumtimes / buffer.Length;
                LastAverage = average;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
            }
            return average;
        }
    }
}
