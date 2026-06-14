using System;

namespace CryptoBot.Models
{
    // OHLCV "sveca" iz Bybit kline endpointa
    public class Kline
    {
        public long TimestampMs { get; set; }
        public DateTime TimeUtc => DateTimeOffset.FromUnixTimeMilliseconds(TimestampMs).UtcDateTime;
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }

        public string Symbol { get; set; } = "";
        public string Interval { get; set; } = "";
    }
}
