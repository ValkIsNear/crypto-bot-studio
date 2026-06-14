namespace CryptoBot.Models
{
    public class Symbol
    {
        public string Name { get; set; } = "";        // npr. BTCUSDT
        public string BaseCoin { get; set; } = "";    // BTC
        public string QuoteCoin { get; set; } = "";   // USDT
        public string Status { get; set; } = "";
        public string Category { get; set; } = "spot"; // spot | linear (futures)
    }
}
