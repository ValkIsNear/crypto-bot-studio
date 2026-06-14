using System;

namespace CryptoBot.Models
{
    public class WalletBalance
    {
        public string BalanceId { get; set; } = Guid.NewGuid().ToString();
        public string Asset { get; set; } = "";
        public decimal TotalBalance { get; set; }
        public decimal AvailableBalance { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
