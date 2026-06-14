using System;
using System.Linq;
using System.Windows.Forms;
using CryptoBot.Services;

namespace CryptoBot.Forms
{
    public class WalletForm : Form
    {
        private readonly BybitApiService _api;
        private readonly CsvService _csv;
        private DataGridView _grid = null!;
        private Button _btnLoad = null!;
        private Button _btnSnapshot = null!;
        private Label _total = null!;

        public WalletForm(BybitApiService api, CsvService csv)
        {
            _api = api; _csv = csv;
            Text = "Wallet — stanje";
            Width = 700; Height = 500;
            StartPosition = FormStartPosition.CenterParent;

            _btnLoad = new Button { Text = "Učitaj stanje", Left = 10, Top = 10, Width = 140 };
            _btnSnapshot = new Button { Text = "Snimi snapshot (CSV)", Left = 160, Top = 10, Width = 180 };
            _total = new Label { Left = 360, Top = 14, Width = 300, Text = "" };

            _grid = new DataGridView
            {
                Left = 10, Top = 50, Width = 660, Height = 400,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            _btnLoad.Click += async (_, _) => await LoadAsync();
            _btnSnapshot.Click += async (_, _) =>
            {
                try
                {
                    var bal = await _api.GetWalletBalancesAsync();
                    _csv.AppendWalletSnapshot(bal);
                    MessageBox.Show($"Snimljeno {bal.Count} stavki u Wallet.csv");
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            };

            Controls.AddRange(new Control[] { _btnLoad, _btnSnapshot, _total, _grid });
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            try
            {
                var bal = await _api.GetWalletBalancesAsync();
                _grid.DataSource = bal.Select(b => new
                {
                    b.Asset,
                    Total = b.TotalBalance,
                    Available = b.AvailableBalance
                }).ToList();
                _total.Text = $"Stavki: {bal.Count}  |  Ukupno (zbir Total): {bal.Sum(x => x.TotalBalance):0.########}";
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }
}
