using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuoteServer.OrderBook.Partition.Events;
using QuoteServer.OrderBook.Primitives.OrderBook.Snapshots;

namespace CoinbaseStreamRecording.SessionRecorder
{
    public class SessionRecorderStream : ISessionRecorder
    {
        private readonly string _basePath;
        private readonly List<OrderBookModifyiableEvent> _events2 = new(1000_000);
        private readonly int _eventsTreashold = 10_000;
        private readonly string _fullPath;
        private Action? _callback;
        private decimal _callbackWhenSequence;
        private SequentialWriter _sequentialWriter;

        public SessionRecorderStream(string basePath, int eventsTreashold = 10_000)
        {
            _basePath = basePath;
            _fullPath = Path.Combine(basePath, DateTimeOffset.Now.ToString("yyyy-MM-dd-hh-mm-ss"));
            _sequentialWriter = new SequentialWriter(_fullPath);
            _eventsTreashold = eventsTreashold;
        }

        public List<OrderBookModifyiableEvent> Events { get; } = new(1000_000);
        public Statistics Statistics { get; } = new();
        public List<OrderBookL3Snapshot> OrderBookL3Snapshots { get; set; } = new(10);
        public List<OrderBookL2Snapshot> OrderBookL2Snapshots { get; set; } = new(10);
        public decimal MinSequence { get; private set; } = decimal.MaxValue;

        public void Record(OrderBookModifyiableEvent @event)
        {
            MapperStatistics.Map(@event, Statistics);
            lock (this)
            {
                Events.Add(@event);
                _events2.Add(@event);
                if (_events2.Count == _eventsTreashold)
                {
                    _sequentialWriter.Write(_events2);
                    _events2.Clear();
                }
                MinSequence = Math.Min(@event.Sequence, MinSequence);
            }
            if (_callback != null && @event.Sequence >= _callbackWhenSequence)
                _callback();
        }

        public void Record(OrderBookL2Snapshot snapshot)
        {
            lock (this)
            {
                var last = OrderBookL2Snapshots.LastOrDefault();
                if (last != null && last.Sequence > snapshot.Sequence)
                    throw new ArgumentException($"Snapshots out of order {last} > {snapshot.Sequence}");
                if (last == null || last != null && last.Sequence < snapshot.Sequence)
                {
                    OrderBookL2Snapshots.Add(snapshot);
                    _sequentialWriter.Write(snapshot);
                }
            }
        }

        public void Record(OrderBookL3Snapshot snapshot)
        {
            lock (this)
            {
                var last = OrderBookL3Snapshots.LastOrDefault();
                if (last != null && last.Sequence > snapshot.Sequence)
                    throw new ArgumentException($"Snapshots out of order {last} > {snapshot.Sequence}");
                if (last == null || last != null && last.Sequence < snapshot.Sequence)
                {
                    OrderBookL3Snapshots.Add(snapshot);
                    _sequentialWriter.Write(snapshot);
                    Statistics.MaxAsks = Math.Max(Statistics.MaxAsks, snapshot.Asks.Count);
                    Statistics.MaxBids = Math.Max(Statistics.MaxBids, snapshot.Bids.Count);
                }
            }
        }

        public void WhenSequence(decimal sequence, Action callback)
        {
            _callbackWhenSequence = sequence;
            _callback += callback;
        }

        public string PersistSession()
        {
            lock (this)
            {
                _sequentialWriter.Write(_events2);
                _events2.Clear();
                return _fullPath;
            }
        }

        public void Load(string data)
        {
            lock (this)
            {
                Events.Clear();
                OrderBookL3Snapshots.Clear();
                OrderBookL2Snapshots.Clear();
                var basePath = Path.Combine(_fullPath, data);
                _sequentialWriter = new SequentialWriter(basePath);
                Events.AddRange(_sequentialWriter.ReadAllEvents());
                MinSequence = Events.Min(x => x.Sequence);
                OrderBookL2Snapshots.AddRange(_sequentialWriter.ReadAllOrderBookL2Snapshots());
                OrderBookL3Snapshots.AddRange(_sequentialWriter.ReadAllOrderBookL3Snapshots());
            }
        }

        public void ReloadWithLastRecordedSessionData()
        {
            var last = Directory.GetDirectories(_basePath).OrderBy(x => x).Last();
            Load(last);
        }

        public IEnumerable<OrderBookModifyiableEvent> FindEventsBetweenSequences(long start, long end)
        {
            return Events.Where(e => start < e.Sequence && e.Sequence <= end);
        }

        public IEnumerable<OrderBookModifyiableEvent> FindEventsBetweenSnapshots(int fromSnapshot, int toSnapshot) =>
            FindEventsBetweenSnapshots(fromSnapshot..toSnapshot);

        public IEnumerable<OrderBookModifyiableEvent> FindEventsBetweenSnapshots(Range range)
        {
            var start = OrderBookL3Snapshots[range.Start.Value].Sequence;
            var end = OrderBookL3Snapshots[range.End.Value].Sequence;
            return FindEventsBetweenSequences(start, end);
        }
    }
}