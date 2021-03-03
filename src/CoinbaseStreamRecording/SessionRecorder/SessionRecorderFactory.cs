namespace CoinbaseStreamRecording.SessionRecorder
{
    public static class SessionRecorderFactory
    {
        public static ISessionRecorder GetStreamRecorder() => new SessionRecorderStream("d:\\CoinbaseData1");
    }
}