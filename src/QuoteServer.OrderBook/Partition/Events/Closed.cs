namespace QuoteServer.OrderBook.Partition.Events
{
    public class Closed : StateEvent
    {
        public DoneReasonType Reason { get; set; }
    }
}