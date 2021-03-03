using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoinbasePro;
using CoinbasePro.Services.Products.Types;
using CoinbasePro.WebSocket;
using CoinbasePro.WebSocket.Models.Response;
using CoinbasePro.WebSocket.Types;
using QuoteServer.OrderBook.Partition;
using QuoteServer.OrderBook.Partition.Events;
using QuoteServer.OrderBook.Partition.Model;
using QuoteServer.OrderBook.Primitives;
using QuoteServer.OrderBook.Primitives.OrderBook.Snapshots;
using WebSocket4Net;

namespace Coinbase.QuoteSource
{
    public class CoinbaseQuotesSourceConnection : IQuotesSourceConnection, IDisposable
    {
        private readonly ICoinbaseProClient _coinbaseProClient;
        private readonly List<TradingPair> _subscribedPairs = new();
        private readonly IWebSocket _webSocket;
        private readonly ManualResetEventSlim _webSocketStarted = new(true);

        public CoinbaseQuotesSourceConnection(ICoinbaseProClient coinbaseProClient)
        {
            _coinbaseProClient = coinbaseProClient;
            _webSocket = _coinbaseProClient.WebSocket;
            _webSocket.OnOpenReceived += (sender, args) => Broadcast(args.LastOrder);
            _webSocket.OnDoneReceived += (sender, args) => Broadcast(args.LastOrder);
            _webSocket.OnMatchReceived += (sender, args) => Broadcast(args.LastOrder);
            _webSocket.OnChangeReceived += (sender, args) => Broadcast(args.LastOrder);
            _webSocket.OnReceivedReceived += (sender, args) => Broadcast(args.LastOrder);
            _webSocket.OnWebSocketOpenAndSubscribed += WebSockedOpened;
        }

        public async Task<OrderBookL3Snapshot> GetOrderBookL3Snapshot(TradingPair tradingPair)
        {
            var snapshot = await _coinbaseProClient.ProductsService.GetProductOrderBookAsync(
                CoinbaseTypeMapper.Map(tradingPair),
                ProductLevel.Three
            );
            return CoinbaseTypeMapper.MapToL3Snapshot(snapshot);
        }
        
        public async Task<OrderBookL2Snapshot> GetOrderBookL2Snapshot(TradingPair tradingPair)
        {
            var snapshot = await _coinbaseProClient.ProductsService.GetProductOrderBookAsync(
                CoinbaseTypeMapper.Map(tradingPair),
                ProductLevel.Two
            );
            return CoinbaseTypeMapper.MapToL2Snapshot(snapshot);
        }

        public void SetSubscribedPairs(IEnumerable<TradingPair> pairs)
        {
            if (_webSocket.State != WebSocketState.Open)
                _webSocketStarted.Wait();
            lock (this)
            {
                _subscribedPairs.Clear();
                _subscribedPairs.AddRange(pairs);
                if (_webSocket.State != WebSocketState.Open)
                    _webSocket?.Start(
                        _subscribedPairs.Select(CoinbaseTypeMapper.Map).ToList(),
                        new List<ChannelType> {ChannelType.Full}
                    );
                else
                    _webSocket?.ChangeProducts(_subscribedPairs.Select(CoinbaseTypeMapper.Map).ToList());
            }
        }

        public void Dispose()
        {
            _webSocketStarted.Dispose();
            _webSocket.Stop();
        }

        public event Action<OrderBookModifyiableEvent>? OnEvent;

        private void WebSockedOpened(object? sender, WebfeedEventArgs<EventArgs> e)
        {
            _webSocketStarted.Set();
        }

        private void Broadcast(BaseMessage message)
        {
            OnEvent?.Invoke(CoinbaseTypeMapper.Map(message));
        }
    }
}