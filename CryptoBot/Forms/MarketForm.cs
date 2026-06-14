using System;
using System.Linq;
using System.Windows.Forms;
using CryptoBot.Services;

namespace CryptoBot.Forms
{
    public class MarketForm : Form
    {
        private readonly BybitApiService _api;
        private readonly CsvService _csv;
        private ComboBox _cmbCategory = null!;
        private DataGridView _grid = null!;
        private TextBox _txtFilter = null!;
        private Button _btnRefresh = null!;
        private Button _btnSaveCsv = null!;
        private Button _btnKlines = null!;
        private Label _status = null!;

        public MarketForm(BybitApiService api, CsvService csv)
        {
            _api = api; _csv = csv;
            Text = "Market — parovi i cene";
            Width = 900; Height = 600;
            StartPosition = FormStartPosition.CenterParent;
            BuildUi();
        }

        private void BuildUi()
        {
            _cmbCategory = new ComboBox { Left = 10, Top = 10, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbCategory.Items.AddRange(new object[] { "spot", "linear" });
            _cmbCategory.SelectedIndex = 0;

            _txtFilter = new TextBox { Left = 140, Top = 10, Width = 180, PlaceholderText = "Filter (npr. BTC)" };
            _btnRefresh = new Button { Left = 330, Top = 8, Width = 110, Text = "Učitaj" };
            _btnSaveCsv = new Button { Left = 450, Top = 8, Width = 150, Text = "Sačuvaj parove (CSV)" };
            _btnKlines = new Button { Left = 610, Top = 8, Width = 170, Text = "Istorija (Klines → CSV)" };

            _grid = new DataGridView
            {
                Left = 10, Top = 45, Width = 860, Height = 480,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            _status = new Label { Left = 10, Top = 535, Width = 860, Text = "Spreman." };

            _btnRefresh.Click += async (_, _) => await LoadAsync();
            _btnSaveCsv.Click += async (_, _) =>
            {
                try
                {
                    var symbols = await _api.GetSymbolsAsync(_cmbCategory.Text);
                    _csv.SaveSymbols(symbols);
                    _status.Text = $"Sačuvano {symbols.Count} parova u Symbols.csv";
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            };
            _btnKlines.Click += async (_, _) =>
            {
                if (_grid.CurrentRow == null) { MessageBox.Show("Izaberi par."); return; }
                var symbol = _grid.CurrentRow.Cells["Symbol"].Value?.ToString() ?? "";
                try
                {
                    var klines = await _api.GetKlinesAsync(symbol, "60", 200, _cmbCategory.Text);
                    _csv.AppendKlines(klines);
                    _status.Text = $"Sačuvano {klines.Count} sveća za {symbol} (1h) u Klines.csv";
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            };

            Controls.AddRange(new Control[] { _cmbCategory, _txtFilter, _btnRefresh, _btnSaveCsv, _btnKlines, _grid, _status });
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            try
            {
                _status.Text = "Učitavam tickere…";
                var tickers = await _api.GetTickersAsync(_cmbCategory.Text);
                var filter = _txtFilter.Text?.Trim().ToUpperInvariant() ?? "";
                var rows = tickers
                    .Where(t => string.IsNullOrEmpty(filter) || t.Symbol.Contains(filter))
                    .OrderByDescending(t => t.Turnover24h)
                    .Select(t => new
                    {
                        t.Symbol,
                        LastPrice = t.LastPrice,
                        Bid = t.Bid1Price,
                        Ask = t.Ask1Price,
                        High24h = t.HighPrice24h,
                        Low24h = t.LowPrice24h,
                        Vol24h = t.Volume24h,
                        ChangePct = Math.Round(t.PriceChangePercent24h, 2)
                    })
                    .ToList();
                _grid.DataSource = rows;
                _status.Text = $"Prikazano {rows.Count} parova ({_cmbCategory.Text}).";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                _status.Text = "Greška.";
            }
        }
    }
}
