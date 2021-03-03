using System;
using System.Buffers;
using QuoteServer.OrderBook.Partition.Events;
using QuoteServer.OrderBook.Primitives.OrderBook;
using QuoteServer.OrderBook.Primitives.OrderBook.Snapshots;

namespace QuoteServer.OrderBook.OrderBookComputing
{
    internal class OrderBookSynchronizer : IDisposable
    {
        public enum SynchronizationResult
        {
            Succeed,
            SnapshotHasBeenTakenTooEarlyIHaveNoEventsWithThatSequence,
        }

        private const int ArrayPoolRentSize = 50_000;
        private readonly IOrderBook _orderBook;
        private readonly TimeSpan _orderBookCacheDuration = TimeSpan.FromMilliseconds(250);
        private readonly IDisposable _subscription;
        private (IOrderBook orderBook, DateTimeOffset when)? _cachedOrderBook;
        private int _nextEventIndex;
        private bool _synchronizedInitialStateWithEvents;
        private OrderBookModifyiableEvent[]? _tempEventsBufferForSynchronization;

        public OrderBookSynchronizer(IOrderBook orderBook, IObservable<OrderBookModifyiableEvent> events)
        {
            _orderBook = orderBook;
            PrepareToSynchronization();
            _subscription = events.Subscribe(OnEventReceived, OnCompleted);
        }

        public void Dispose()
        {
            lock (this)
            {
                _subscription?.Dispose();
                OnCompleted(null);
            }
        }

        private void OnCompleted(Exception? obj)
        {
            lock (this)
            {
                if (_tempEventsBufferForSynchronization != null)
                {
                    ArrayPool<OrderBookModifyiableEvent>.Shared.Return(_tempEventsBufferForSynchronization);
                    _tempEventsBufferForSynchronization = null;
                }
            }
        }

        private void OnEventReceived(OrderBookModifyiableEvent orderBookModifyiableEvent)
        {
            lock (this)
            {
                if (_synchronizedInitialStateWithEvents)
                    _orderBook.Apply(orderBookModifyiableEvent);
                if (!_synchronizedInitialStateWithEvents)
                {
                    _tempEventsBufferForSynchronization![_nextEventIndex] = orderBookModifyiableEvent;
                    _nextEventIndex = (_nextEventIndex + 1) % _tempEventsBufferForSynchronization.Length;
                }
            }
        }

        public void PrepareToSynchronization()
        {
            lock (this)
            {
                _synchronizedInitialStateWithEvents = false;
                _nextEventIndex = 0;
                _tempEventsBufferForSynchronization =
                    ArrayPool<OrderBookModifyiableEvent>.Shared.Rent(ArrayPoolRentSize);
            }
        }

        public IOrderBook TakeOrderBookCopy()
        {
            var orderBook = _cachedOrderBook;
            if (orderBook.HasValue && DateTimeOffset.Now - orderBook.Value.when < _orderBookCacheDuration)
                return orderBook.Value.orderBook;
            lock (this)
            {
                orderBook = _cachedOrderBook;
                if (orderBook.HasValue && DateTimeOffset.Now - orderBook.Value.when < _orderBookCacheDuration)
                    return orderBook.Value.Item1;
                _cachedOrderBook = (new HashBasedOrderBook(_orderBook.Sequence, _orderBook.Asks, _orderBook.Bids),
                    DateTimeOffset.Now);
                return _cachedOrderBook.Value.orderBook;
            }
        }

        public OrderBookL3Snapshot TakeOrderBookL3Snapshot() =>
            OrderBookSnapshotsFactory.L3Snapshot(TakeOrderBookCopy());

        public SynchronizationResult SynchronizeOrderBookInitialStateWith(OrderBookL3Snapshot snapshot)
        {
            if (_synchronizedInitialStateWithEvents == false)
                throw new InvalidOperationException(
                    $"{nameof(OrderBookSynchronizer)} is in synchronized state already. You must first call {nameof(PrepareToSynchronization)}"
                );
            lock (this)
            {
                if (_tempEventsBufferForSynchronization == null || _nextEventIndex == 0 ||
                    _tempEventsBufferForSynchronization[0].Sequence > snapshot.Sequence)
                    return SynchronizationResult.SnapshotHasBeenTakenTooEarlyIHaveNoEventsWithThatSequence;
                _orderBook.ResetTo(snapshot.Sequence, snapshot.Asks, snapshot.Bids);
                for (var i = 0; i < _nextEventIndex; i++)
                {
                    if (_tempEventsBufferForSynchronization[i].Sequence <= snapshot.Sequence)
                        continue;
                    _orderBook.Apply(_tempEventsBufferForSynchronization[i]);
                }
                ArrayPool<OrderBookModifyiableEvent>.Shared.Return(_tempEventsBufferForSynchronization);
                _tempEventsBufferForSynchronization = null;
                _synchronizedInitialStateWithEvents = false;
                return SynchronizationResult.Succeed;
            }
        }

        public OrderBookL2Snapshot TakeOrderBookL2Snapshot() =>
            OrderBookSnapshotsFactory.L2Snapshot(TakeOrderBookCopy());
    }
}