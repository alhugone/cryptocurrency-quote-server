using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using QuoteServer.OrderBook.Partition.Events;
using QuoteServer.OrderBook.Partition.Interceptors;
using Xunit;

namespace QuoteServer.OrderBook.Tests
{
    public class DeduplcateEventsBySequenceInterceptor_OnNext_Should_DeduplicateEventsBySequence
    {
        [Fact]
        public void WhenRecivesContionusGrowingOnlySequences()
        {
            var expectedEvents = 100;
            var mock = new Mock<IObservable<OrderBookModifyiableEvent>>();
            var mock2 = new Mock<IObserver<OrderBookModifyiableEvent>>();
            var mocked = new List<IObservable<OrderBookModifyiableEvent>>();
            mocked.Add(mock.Object);
            var cut = new DeduplcateEventsBySequenceInterceptor(mocked, 100);
            cut.Subscribe(mock2.Object);
            var rand = new Random(100);
            var initSequence = rand.Next(0, 100_000);
            var recived = new HashSet<long>();
            var even = new Opened();
            mock2.Setup(x => x.OnNext(It.IsAny<OrderBookModifyiableEvent>()))
                 .Callback((OrderBookModifyiableEvent e) => { recived.Add(e.Sequence); });
            var i = initSequence;
            while (recived.Count < expectedEvents)
            {
                even.Sequence = i++;
                cut.OnNext(even);
            }
        }

        [Fact]
        public void WhenRecivesSequencePastTheTrackedSequencesIgnoresIt()
        {
            var expectedEvents = 100;
            var mock = new Mock<IObservable<OrderBookModifyiableEvent>>();
            var mock2 = new Mock<IObserver<OrderBookModifyiableEvent>>();
            var mocked = new List<IObservable<OrderBookModifyiableEvent>>();
            mocked.Add(mock.Object);
            var cut = new DeduplcateEventsBySequenceInterceptor(mocked, 100);
            cut.Subscribe(mock2.Object);
            var rand = new Random(100);
            var initSequence = rand.Next(0, 100_000);
            var recived = new HashSet<long>();
            var even = new Opened();
            mock2.Setup(x => x.OnNext(It.IsAny<OrderBookModifyiableEvent>()))
                 .Callback((OrderBookModifyiableEvent e) => { recived.Add(e.Sequence); });
            even.Sequence = initSequence;
            cut.OnNext(even);
            // act
            even.Sequence = initSequence - (expectedEvents + 1);
            cut.OnNext(even);
            even.Sequence = initSequence - (expectedEvents + 100);
            cut.OnNext(even);
            // assert
            recived.Should().ContainSingle();
            recived.First().Should().Be(initSequence);
        }

        [Fact]
        public void WhenRecivesRandomSequencesInRangeOfTrackedEvents()
        {
            var expectedEvents = 100;
            var mock = new Mock<IObservable<OrderBookModifyiableEvent>>();
            var mock2 = new Mock<IObserver<OrderBookModifyiableEvent>>();
            var mocked = new List<IObservable<OrderBookModifyiableEvent>>();
            mocked.Add(mock.Object);
            var cut = new DeduplcateEventsBySequenceInterceptor(mocked, expectedEvents);
            cut.Subscribe(mock2.Object);
            var rand = new Random(100);
            var initSequence = rand.Next(0, 100_000);
            var recived = new HashSet<long>();
            var even = new Opened();
            mock2.Setup(x => x.OnNext(It.IsAny<OrderBookModifyiableEvent>()))
                 .Callback((OrderBookModifyiableEvent e) => { recived.Add(e.Sequence); });
            while (recived.Count < expectedEvents)
            {
                even.Sequence = rand.Next(initSequence, initSequence + expectedEvents);
                cut.OnNext(even);
            }
        }
    }
}