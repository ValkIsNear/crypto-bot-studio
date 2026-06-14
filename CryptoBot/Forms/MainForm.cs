using System;
using System.Windows.Forms;
using CryptoBot.Services;

namespace CryptoBot.Forms
{
    public class MainForm : Form
    {
        private readonly AppConfig _cfg;
        private readonly BybitApiService _api;
        private readonly CsvService _csv;

        public MainForm()
        {
            _cfg = AppConfig.Load();
            _api = new BybitApiService(_cfg);
            _csv = new CsvService(_cfg);

            Text = "CryptoBot — Bybit klijent";
            Width = 520;
            Height = 360;
            StartPosition = FormStartPosition.CenterScreen;

            var lbl = new Label
            {
                Text = "CryptoBot",
                Font = new System.Drawing.Font("Segoe UI", 22, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Left = 30,
                Top = 20
            };

            var btnMarket = MakeButton("Market (parovi, cene, grafik)", 30, 90);
            var btnWallet = MakeButton("Wallet (stanje + profit)", 30, 140);
            var btnTrade = MakeButton("Trade (otvori / zatvori)", 30, 190);
            var btnHistory = MakeButton("Istorija (CSV)", 30, 240);

            btnMarket.Click += (_, _) => new MarketForm(_api, _csv).Show();
            btnWallet.Click += (_, _) => new WalletForm(_api, _csv).Show();
            btnTrade.Click += (_, _) => new TradeForm(_api, _csv).Show();
            btnHistory.Click += (_, _) =>
            {
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", System.IO.Path.GetFullPath(_cfg.DataFolder));
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            };

            Controls.AddRange(new Control[] { lbl, btnMarket, btnWallet, btnTrade, btnHistory });
        }

        private static Button MakeButton(string text, int x, int y)
        {
            return new Button
            {
                Text = text,
                Left = x,
                Top = y,
                Width = 440,
                Height = 40,
                Font = new System.Drawing.Font("Segoe UI", 11)
            };
        }
    }
}
