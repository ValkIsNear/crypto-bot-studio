using System;

namespace CryptoBot.Models
{
    public class OrderRecord
    {
        public string OrderId { get; set; } = "";
        public string Symbol { get; set; } = "";
        public string Category { get; set; } = "spot"; // spot | linear
        public string Side { get; set; } = "Buy";       // Buy | Sell
        public string OrderType { get; set; } = "Market"; // Market | Limit
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
