using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QuoteServer.OrderBook.Partition.Events;

namespace QuoteServer.OrderBook.Partition.Interceptors
{
    public class DeduplcateEventsBySequenceInterceptor : IObservable<OrderBookModifyiableEvent>,
        IObserver<OrderBookModifyiableEvent>, IDisposable
    {
        private readonly BitArray _lastNEventsOccurrenceBitArray;
        private readonly IEnumerable<IObservable<OrderBookModifyiableEvent>> _streams;
        private readonly int _trackOnlyLastNEvents;
        private long _greatestReceivedSequence;
        private IObserver<OrderBookModifyiableEvent>? _observer;
        private IEnumerable<IDisposable>? _subscriptions;

        public DeduplcateEventsBySequenceInterceptor(
            IEnumerable<IObservable<OrderBookModifyiableEvent>> streams,
            int trackOnlyLastNEvents)
        {
            _streams = streams;
            _trackOnlyLastNEvents = trackOnlyLastNEvents > 0
                ? trackOnlyLastNEvents
                : throw new ArgumentOutOfRangeException(
                    nameof(trackOnlyLastNEvents),
                    trackOnlyLastNEvents,
                    "Must be grater than 0."
                );
            _lastNEventsOccurrenceBitArray = new BitArray(_trackOnlyLastNEvents, false);
        }

        public void Dispose()
        {
            if (_subscriptions != null)
                foreach (var disposable in _subscriptions)
                    disposable.Dispose();
        }

        public IDisposable Subscribe(IObserver<OrderBookModifyiableEvent> observer)
        {
            _observer = observer;
            _subscriptions = _streams.Select(x => x.Subscribe(this)).ToList();
            return this;
        }

        public void OnCompleted()
        {
            /// it should never be called as e.g. Coinbase never completes
        }

        public void OnError(Exception error)
        {
            /// it would require to think about policy, maybe it's just one stream error
        }

        public void OnNext(OrderBookModifyiableEvent value)
        {
            lock (this)
            {
                var distance = (int) (value.Sequence - _greatestReceivedSequence);
                if (0 < distance)
                {
                    if (distance > _trackOnlyLastNEvents)
                        _lastNEventsOccurrenceBitArray.SetAll(false);
                    else
                        _lastNEventsOccurrenceBitArray.LeftShift(distance % _trackOnlyLastNEvents);
                    _lastNEventsOccurrenceBitArray.Set(0, true);
                    _greatestReceivedSequence = value.Sequence;
                    _observer?.OnNext(value);
                }
                else if (distance < 0 && -distance < _trackOnlyLastNEvents)
                {
                    if (_lastNEventsOccurrenceBitArray.Get(-distance) == false)
                    {
                        _lastNEventsOccurrenceBitArray.Set(-distance, true);
                        _observer?.OnNext(value);
                    }
                }
            }
        }
    }
}