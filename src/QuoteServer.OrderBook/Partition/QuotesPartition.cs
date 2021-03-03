using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using QuoteServer.OrderBook.Partition.Events;
using QuoteServer.OrderBook.Partition.Exceptions;
using QuoteServer.OrderBook.Partition.Interceptors;
using QuoteServer.OrderBook.Partition.Model;
using QuoteServer.OrderBook.Partition.Readers;
using QuoteServer.OrderBook.Partition.Settings;
using QuoteServer.OrderBook.Primitives;
using QuoteServer.OrderBook.Primitives.OrderBook.Snapshots;

namespace QuoteServer.OrderBook.Partition
{
    public class QuotesPartition : IQuotesPartition
    {
        private readonly List<TradingPairSubscription>[] _activeSubscriptionsToPairs;
        private readonly Dictionary<TradingPair, int> _activeSubscriptionToPairsCounter = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly Channel<OrderBookModifyiableEvent>[] _channelForPairsMap;
        private readonly IQuotesSourceConnection _quotesSourceConnection;
        private readonly Channel<OrderBookModifyiableEvent>[] _readersChannels;
        private readonly IReadersFactory _readersFactory;
        private int _readerSubscriptionRoundRobinIndex;

        public QuotesPartition(
            IQuotesSourceConnection quotesSourceConnection,
            QuotesPartitionSettings? settings = null,
            IReadersFactory? readersFactory = null)
        {
            _readersFactory = readersFactory ?? new DefaultReaderFactory();
            Settings = settings ?? new QuotesPartitionSettings();
            var allPairsCount = Math.Max(Settings.TradingPairs.Count, Enum.GetValues<TradingPair>().Length);
            _channelForPairsMap = new Channel<OrderBookModifyiableEvent>[allPairsCount];
            _readersChannels = new Channel<OrderBookModifyiableEvent>[Settings.NumberOfReaders];
            _activeSubscriptionsToPairs = new List<TradingPairSubscription>[allPairsCount];
            _quotesSourceConnection = quotesSourceConnection;
            for (var i = 0; i < _activeSubscriptionsToPairs.Length; i++)
                _activeSubscriptionsToPairs[i] = new List<TradingPairSubscription>(1000);
            _quotesSourceConnection.OnEvent += OnOrderBookEventReceived;
            StartReaders();
        }

        public QuotesPartitionSettings Settings { get; }

        public IObservable<OrderBookModifyiableEvent> Streams(TradingPair tradingTradingPair)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
                throw new InvalidOperationException("Object is closed");
            if (!Settings.TradingPairs.Contains(tradingTradingPair))
                throw new PartitionDoNotHandleTradingPair(tradingTradingPair);
            lock (this)
            {
                if (!_activeSubscriptionToPairsCounter.ContainsKey(tradingTradingPair))
                {
                    _activeSubscriptionToPairsCounter.Add(tradingTradingPair, 1);
                    _quotesSourceConnection?.SetSubscribedPairs(_activeSubscriptionToPairsCounter.Keys);
                    AssignPairChannelToReader(tradingTradingPair);
                }
                else
                {
                    _activeSubscriptionToPairsCounter[tradingTradingPair] =
                        _activeSubscriptionToPairsCounter[tradingTradingPair] + 1;
                }
                return new TradingPairSubscription(this, tradingTradingPair);
            }
        }

        public IObservable<OrderBookModifyiableEvent> Streams(IObservable<TradingPair[]> paris) =>
            new JoinMultipleStreamsInterceptor(paris, Streams);

        public void Dispose()
        {
            _quotesSourceConnection?.Dispose();
            _cancellationTokenSource.Cancel();
        }

        public Task<OrderBookL3Snapshot> GetOrderBookL3Snapshot(TradingPair tradingTradingPair) =>
            _quotesSourceConnection.GetOrderBookL3Snapshot(tradingTradingPair);

        public Task<OrderBookL2Snapshot> GetOrderBookL2Snapshot(TradingPair tradingTradingPair) =>
            _quotesSourceConnection.GetOrderBookL2Snapshot(tradingTradingPair);

        private void StartReaders()
        {
            for (var i = 0; i < Settings.NumberOfReaders; i++)
            {
                _readersChannels[i] = Channel.CreateUnbounded<OrderBookModifyiableEvent>(
                    new UnboundedChannelOptions
                    {
                        SingleReader = true, SingleWriter = true, AllowSynchronousContinuations = false,
                    }
                );
                var readerId = i;
                _readersFactory.StartNew(
                    readerId,
                    _readersChannels[readerId].Reader,
                    _activeSubscriptionsToPairs,
                    _cancellationTokenSource.Token
                );
            }
        }

        public void OnOrderBookEventReceived(OrderBookModifyiableEvent msg)
        {
            _channelForPairsMap[(int) msg.TradingPair].Writer.WriteAsync(msg);
        }

        private void AssignPairChannelToReader(TradingPair tradingPair)
        {
            _channelForPairsMap[(int) tradingPair] =
                _readersChannels[_readerSubscriptionRoundRobinIndex++ % Settings.NumberOfReaders];
        }

        private void OnUnsubscribed(TradingPairSubscription tradingPairSubscription)
        {
            lock (this)
            {
                _activeSubscriptionsToPairs[(int) tradingPairSubscription.TradingPair].Remove(tradingPairSubscription);
                DecreseCounter(tradingPairSubscription.TradingPair);
            }
        }

        private void OnSubscribed(TradingPairSubscription tradingPairSubscription)
        {
            lock (this)
            {
                _activeSubscriptionsToPairs[(int) tradingPairSubscription.TradingPair].Add(tradingPairSubscription);
            }
        }

        private void DecreseCounter(TradingPair tradingTradingPair)
        {
            lock (this)
            {
                _activeSubscriptionToPairsCounter[tradingTradingPair] =
                    _activeSubscriptionToPairsCounter[tradingTradingPair] - 1;
                if (_activeSubscriptionToPairsCounter[tradingTradingPair] == 0)
                    Task.Run(
                        async () =>
                        {
                            await Task.Delay(Settings.DelayUpdatingConnectionWhenNoSubscribersOnPairTo);
                            if (_activeSubscriptionToPairsCounter[tradingTradingPair] == 0)
                                _quotesSourceConnection?.SetSubscribedPairs(_activeSubscriptionToPairsCounter.Keys);
                        }
                    );
            }
        }

        public class TradingPairSubscription : IObservable<OrderBookModifyiableEvent>, IDisposable
        {
            private readonly QuotesPartition _quotesPartition;
            public readonly TradingPair TradingPair;
            public IObserver<OrderBookModifyiableEvent>? Observer;

            public TradingPairSubscription(QuotesPartition quotesPartition, TradingPair tradingPair)
            {
                _quotesPartition = quotesPartition;
                TradingPair = tradingPair;
            }

            public void Dispose()
            {
                _quotesPartition.OnUnsubscribed(this);
            }

            public IDisposable Subscribe(IObserver<OrderBookModifyiableEvent> observer)
            {
                Observer = observer;
                _quotesPartition.OnSubscribed(this);
                return this;
            }
        }
    }
}