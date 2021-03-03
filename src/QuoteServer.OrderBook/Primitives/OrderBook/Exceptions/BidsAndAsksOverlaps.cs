namespace QuoteServer.OrderBook.Primitives.OrderBook.Exceptions
{
    public class BidsAndAsksOverlaps : OrderBookBaseException
    {
        public BidsAndAsksOverlaps(decimal askPrice, decimal bidPrice) : base(
            $"Bid and Asks overlaps: Ask Price {askPrice} <= {bidPrice} Bid price"
        )
        {
        }
    }
}