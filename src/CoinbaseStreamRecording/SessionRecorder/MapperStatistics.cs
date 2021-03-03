using System;
using System.Threading;
using QuoteServer.OrderBook.Partition.Events;

namespace CoinbaseStreamRecording.SessionRecorder
{
    public static class MapperStatistics
    {
        public static void Map(OrderBookModifyiableEvent e, Statistics statistics)
        {
            Interlocked.Increment(ref statistics.TotalEvents);
            switch (e)
            {
                case Opened:
                    Interlocked.Increment(ref statistics.Opened);
                    break;
                case Closed closed:
                    Interlocked.Increment(ref statistics.Closed);
                    if (closed.Reason == DoneReasonType.Canceled)
                        Interlocked.Increment(ref statistics.ClosedCancelled);
                    else
                        Interlocked.Increment(ref statistics.ClosedFilled);
                    break;
                case Matched:
                    Interlocked.Increment(ref statistics.Matched);
                    break;
                case Changed:
                    Interlocked.Increment(ref statistics.Changed);
                    break;
                case ReceivedAny:
                    Interlocked.Increment(ref statistics.Received);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(e), e.GetType(), null);
            }
        }
    }
}