namespace CryptoBot.Models
{
    public class Ticker
    {
        public string Symbol { get; set; } = "";
        public decimal LastPrice { get; set; }
        public decimal Bid1Price { get; set; }
        public decimal Ask1Price { get; set; }
        public decimal HighPrice24h { get; set; }
        public decimal LowPrice24h { get; set; }
        public decimal Volume24h { get; set; }
        public decimal Turnover24h { get; set; }
        public decimal PriceChangePercent24h { get; set; }
    }
}
