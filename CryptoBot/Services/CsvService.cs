using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CryptoBot.Models;

namespace CryptoBot.Services
{
    /// <summary>
    /// Citanje i pisanje istorijskih podataka u CSV datoteke.
    /// Struktura iz projektnog opisa:
    ///   Symbols.csv   : Symbol,BaseCoin,QuoteCoin,Status,Category
    ///   Klines.csv    : Symbol,Interval,TimestampMs,Open,High,Low,Close,Volume
    ///   Wallet.csv    : BalanceId,Asset,TotalBalance,AvailableBalance,Timestamp
    ///   Orders.csv    : OrderId,Symbol,Category,Side,OrderType,Qty,Price,Status,Timestamp
    /// </summary>
    public class CsvService
    {
        private readonly string _folder;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public CsvService(AppConfig cfg)
        {
            _folder = cfg.DataFolder;
            Directory.CreateDirectory(_folder);
        }

        private string Path(string name) => System.IO.Path.Combine(_folder, name);

        // -------- SYMBOLS --------
        public void SaveSymbols(IEnumerable<Symbol> symbols)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Symbol,BaseCoin,QuoteCoin,Status,Category");
            foreach (var s in symbols)
                sb.AppendLine($"{s.Name},{s.BaseCoin},{s.QuoteCoin},{s.Status},{s.Category}");
            File.WriteAllText(Path("Symbols.csv"), sb.ToString());
        }

        public List<Symbol> LoadSymbols()
        {
            var list = new List<Symbol>();
            var path = Path("Symbols.csv");
            if (!File.Exists(path)) return list;

            var lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++) // preskoci header
            {
                var p = lines[i].Split(',');
                if (p.Length < 5) continue;
                list.Add(new Symbol
                {
                    Name = p[0],
                    BaseCoin = p[1],
                    QuoteCoin = p[2],
                    Status = p[3],
                    Category = p[4]
                });
            }
            return list;
        }

        // -------- KLINES --------
        public void AppendKlines(IEnumerable<Kline> klines)
        {
            var path = Path("Klines.csv");
            var newFile = !File.Exists(path);
            using var w = new StreamWriter(path, append: true);
            if (newFile)
                w.WriteLine("Symbol,Interval,TimestampMs,Open,High,Low,Close,Volume");
            foreach (var k in klines)
            {
                w.WriteLine(string.Join(",",
                    k.Symbol, k.Interval, k.TimestampMs.ToString(Inv),
                    k.Open.ToString(Inv), k.High.ToString(Inv),
                    k.Low.ToString(Inv), k.Close.ToString(Inv),
                    k.Volume.ToString(Inv)));
            }
        }

        // -------- WALLET --------
        public void AppendWalletSnapshot(IEnumerable<WalletBalance> balances)
        {
            var path = Path("Wallet.csv");
            var newFile = !File.Exists(path);
            using var w = new StreamWriter(path, append: true);
            if (newFile)
                w.WriteLine("BalanceId,Asset,TotalBalance,AvailableBalance,Timestamp");
            foreach (var b in balances)
            {
                w.WriteLine(string.Join(",",
                    b.BalanceId, b.Asset,
                    b.TotalBalance.ToString(Inv),
                    b.AvailableBalance.ToString(Inv),
                    b.Timestamp.ToString("o", Inv)));
            }
        }

        // -------- ORDERS --------
        public void AppendOrder(OrderRecord o)
        {
            var path = Path("Orders.csv");
            var newFile = !File.Exists(path);
            using var w = new StreamWriter(path, append: true);
            if (newFile)
                w.WriteLine("OrderId,Symbol,Category,Side,OrderType,Qty,Price,Status,Timestamp");
            w.WriteLine(string.Join(",",
                o.OrderId, o.Symbol, o.Category, o.Side, o.OrderType,
                o.Qty.ToString(Inv), o.Price.ToString(Inv),
                o.Status?.Replace(',', ';'), o.Timestamp.ToString("o", Inv)));
        }
    }
}
