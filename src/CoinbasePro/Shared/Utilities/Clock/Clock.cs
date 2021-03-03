using System;

namespace CoinbasePro.Shared.Utilities.Clock
{
    public class Clock : IClock
    {
        public DateTime GetTime() => DateTime.UtcNow;
    }
}