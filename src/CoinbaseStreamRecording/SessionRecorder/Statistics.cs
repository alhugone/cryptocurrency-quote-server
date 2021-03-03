using System;

namespace CoinbaseStreamRecording.SessionRecorder
{
    public class Statistics
    {
        public int Changed;
        public int Closed;
        public int ClosedCancelled;
        public int ClosedFilled;
        public int Matched;
        public int MaxAsks;
        public int MaxBids;
        public int Opened;
        public int Received;
        public DateTimeOffset Started = DateTimeOffset.Now;
        public int TotalEvents;
        public TimeSpan RunningFor => DateTimeOffset.Now - Started;

        public int EventsPerSec
        {
            get
            {
                var sec = (DateTimeOffset.Now - Started).TotalSeconds;
                return TotalEvents / (int) sec;
            }
        }

        public int ChangingEventsPerSec
        {
            get
            {
                var sec = (DateTimeOffset.Now - Started).TotalSeconds;
                return (Opened + Matched + Changed + Closed) / (int) sec;
            }
        }
    }
}