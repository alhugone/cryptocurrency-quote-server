using System;
using System.Collections.Generic;
using System.Linq;
using QuoteServer.OrderBook.Partition.Events;
using QuoteServer.OrderBook.Primitives;

namespace QuoteServer.OrderBook.Partition.Interceptors
{
    public class JoinMultipleStreamsInterceptor : IObservable<OrderBookModifyiableEvent>, IObserver<TradingPair[]>,
        IObserver<OrderBookModifyiableEvent>, IDisposable
    {
        private readonly Func<TradingPair, IObservable<OrderBookModifyiableEvent>> _getStream;
        private readonly Dictionary<TradingPair, IDisposable> _subscriptions = new();
        private IObserver<OrderBookModifyiableEvent>? _observer;

        public JoinMultipleStreamsInterceptor(
            IObservable<TradingPair[]> paris,
            Func<TradingPair, IObservable<OrderBookModifyiableEvent>> getStream)
        {
            _getStream = getStream;
            paris.Subscribe(this);
        }

        public void Dispose()
        {
            ReleaseResources();
            GC.SuppressFinalize(this);
        }

        public IDisposable Subscribe(IObserver<OrderBookModifyiableEvent> observer)
        {
            _observer = observer;
            return this;
        }

        void IObserver<OrderBookModifyiableEvent>.OnCompleted() => _observer?.OnCompleted();
        void IObserver<OrderBookModifyiableEvent>.OnError(Exception error) => _observer?.OnError(error);

        public void OnNext(OrderBookModifyiableEvent value)
        {
            if (_subscriptions.ContainsKey(value.TradingPair))
                _observer?.OnNext(value);
        }

        public void OnCompleted()
        {
        }

        void IObserver<TradingPair[]>.OnError(Exception error)
        {
        }

        public void OnNext(TradingPair[] value)
        {
            var pairsToUnsubscribe = _subscriptions.Keys.Except(value);
            foreach (var tor in pairsToUnsubscribe)
            {
                _subscriptions[tor].Dispose();
                _subscriptions.Remove(tor);
            }
            var pairsToSubscribe = value.Except(_subscriptions.Keys);
            foreach (var tor in pairsToSubscribe)
            {
                var stra = _getStream(tor);
                var str = stra.Subscribe(this);
                _subscriptions.Add(tor, str);
            }
        }

        private void ReleaseResources()
        {
            foreach (var (_, subscription) in _subscriptions) subscription.Dispose();
        }

        ~JoinMultipleStreamsInterceptor()
        {
            ReleaseResources();
        }
    }
}