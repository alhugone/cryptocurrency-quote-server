using System;

namespace QuoteServer.OrderBook.Primitives.OrderBook.Exceptions
{
    public abstract class OrderBookBaseException : Exception
    {
        protected OrderBookBaseException(string message) : base(message)
        {
        }
    }
}