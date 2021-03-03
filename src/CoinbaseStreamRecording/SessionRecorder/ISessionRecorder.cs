using System;
using System.Collections.Generic;
using QuoteServer.OrderBook.Partition.Events;
using QuoteServer.OrderBook.Primitives.OrderBook.Snapshots;

namespace CoinbaseStreamRecording.SessionRecorder
{
    public interface ISessionRecorder
    {
        List<OrderBookL3Snapshot> OrderBookL3Snapshots { get; set; }
        List<OrderBookL2Snapshot> OrderBookL2Snapshots { get; set; }
        Statistics Statistics { get; }
        decimal MinSequence { get; }
        List<OrderBookModifyiableEvent> Events { get; }
        void Record(OrderBookL3Snapshot snapshot);
        void ReloadWithLastRecordedSessionData();
        IEnumerable<OrderBookModifyiableEvent> FindEventsBetweenSequences(long start, long end);
        IEnumerable<OrderBookModifyiableEvent> FindEventsBetweenSnapshots(int fromSnapshot, int toSnapshot);
        string PersistSession();
        void WhenSequence(decimal sequence, Action action);
        void Load(string pathToPersistedSession);
        void Record(OrderBookModifyiableEvent snapshot);
        void Record(OrderBookL2Snapshot orderBookL2Snapshot);
    }
}