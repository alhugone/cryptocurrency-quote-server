using System.Collections.Generic;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using QuoteServer.OrderBook.Partition.Events;
using QuoteServer.OrderBook.Primitives.OrderBook.Snapshots;

namespace CoinbaseStreamRecording.SessionRecorder
{
    public class SequentialWriter
    {
        private readonly string _path;

        private readonly JsonSerializerSettings _serializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.All, Formatting = Formatting.Indented,
        };

        private int _l2SnapshotsIndex;
        private int _l3SnapshotsIndex;
        private int _streamsIndex;
        public SequentialWriter(string path) => _path = path;

        public void Write(IEnumerable<OrderBookModifyiableEvent> events)
        {
            var i = Interlocked.Increment(ref _streamsIndex);
            Write(events, BuildFileNameForStreams(i));
        }

        public void Write(OrderBookL2Snapshot snapshot)
        {
            var i = Interlocked.Increment(ref _l2SnapshotsIndex);
            Write(snapshot, BuildFileNameForOrderBookL2Snapshot(i));
        }

        public void Write(OrderBookL3Snapshot snapshot)
        {
            var i = Interlocked.Increment(ref _l3SnapshotsIndex);
            Write(snapshot, BuildFileNameForOrderBookL3Snapshot(i));
        }

        private void Write(object snapshot, string fileName)
        {
            Directory.CreateDirectory(_path);
            File.WriteAllText(
                Path.Combine(_path, fileName),
                JsonConvert.SerializeObject(snapshot, _serializerSettings)
            );
        }

        private static string BuildFileNameForStreams(int i) => $"stream-{i:0000}.json";
        private static string BuildFileNameForOrderBookL3Snapshot(int i) => $"snapshot-L3-{i:0000}.json";
        private static string BuildFileNameForOrderBookL2Snapshot(int i) => $"snapshot-L2-{i:0000}.json";

        public IEnumerable<OrderBookModifyiableEvent> ReadAllEvents()
        {
            var i = 1;
            var file = Path.Combine(_path, BuildFileNameForStreams(i));
            while (File.Exists(file))
            {
                var events = JsonConvert.DeserializeObject<IEnumerable<OrderBookModifyiableEvent>>(
                    File.ReadAllText(file),
                    _serializerSettings
                );
                foreach (var orderBookModifyiableEvent in events) yield return orderBookModifyiableEvent;
                i++;
                file = Path.Combine(_path, BuildFileNameForStreams(i));
            }
        }

        public IEnumerable<OrderBookL3Snapshot> ReadAllOrderBookL3Snapshots()
        {
            var i = 1;
            var file = Path.Combine(_path, BuildFileNameForOrderBookL3Snapshot(i));
            while (File.Exists(file))
            {
                yield return JsonConvert.DeserializeObject<OrderBookL3Snapshot>(
                    File.ReadAllText(file),
                    _serializerSettings
                );
                i++;
                file = Path.Combine(_path, BuildFileNameForOrderBookL3Snapshot(i));
            }
        }

        public IEnumerable<OrderBookL2Snapshot> ReadAllOrderBookL2Snapshots()
        {
            var i = 1;
            var file = Path.Combine(_path, BuildFileNameForOrderBookL2Snapshot(i));
            while (File.Exists(file))
            {
                var snapshot = JsonConvert.DeserializeObject<OrderBookL2Snapshot>(
                    File.ReadAllText(file),
                    _serializerSettings
                );
                yield return snapshot;
                i++;
                file = Path.Combine(_path, BuildFileNameForOrderBookL2Snapshot(i));
            }
        }
    }
}