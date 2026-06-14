using System;
using System.Linq;
using System.Windows.Forms;
using CryptoBot.Services;
using ScottPlot;
using ScottPlot.WinForms;

namespace CryptoBot.Forms
{
    /// <summary>
    /// Grafik uživo — povlači klines + tickere sa Bybit-a i osvežava na svakih par sekundi.
    /// </summary>
    public class ChartForm : Form
    {
        private readonly BybitApiService _api;
        private readonly string _symbol;
        private readonly string _category;

        private FormsPlot _plot = null!;
        private ComboBox _cmbInterval = null!;
        private ComboBox _cmbType = null!;
        private Label _lblPrice = null!;
        private Label _status = null!;
        private System.Windows.Forms.Timer _timer = null!;

        public ChartForm(BybitApiService api, string symbol, string category)
        {
            _api = api; _symbol = symbol; _category = category;
            Text = $"Grafik uživo — {symbol} ({category})";
            Width = 1000; Height = 640;
            StartPosition = FormStartPosition.CenterParent;
            BuildUi();
        }

        private void BuildUi()
        {
            _cmbInterval = new ComboBox { Left = 10, Top = 10, Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbInterval.Items.AddRange(new object[] { "1", "5", "15", "60", "240", "D" });
            _cmbInterval.SelectedItem = "5";

            _cmbType = new ComboBox { Left = 100, Top = 10, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbType.Items.AddRange(new object[] { "Sveće (OHLC)", "Linija (Close)" });
            _cmbType.SelectedIndex = 0;

            _lblPrice = new Label { Left = 240, Top = 14, Width = 300, Text = "Cena: —", Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold) };

            _plot = new FormsPlot { Left = 10, Top = 45, Width = 960, Height = 520, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            _status = new Label { Left = 10, Top = 575, Width = 960, Text = "Učitavam…", Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };

            _cmbInterval.SelectedIndexChanged += async (_, _) => await RefreshChartAsync();
            _cmbType.SelectedIndexChanged += async (_, _) => await RefreshChartAsync();

            Controls.AddRange(new Control[] { _cmbInterval, _cmbType, _lblPrice, _plot, _status });

            _timer = new System.Windows.Forms.Timer { Interval = 3000 };
            _timer.Tick += async (_, _) => await TickAsync();

            Shown += async (_, _) => { await RefreshChartAsync(); _timer.Start(); };
            FormClosed += (_, _) => _timer.Stop();
        }

        private async System.Threading.Tasks.Task TickAsync()
        {
            try
            {
                var tickers = await _api.GetTickersAsync(_category);
                var t = tickers.FirstOrDefault(x => x.Symbol == _symbol);
                if (t != null)
                {
                    _lblPrice.Text = $"Cena: {t.LastPrice}  |  24h: {Math.Round(t.PriceChangePercent24h, 2)}%  |  Vol: {t.Volume24h}";
                }
                await RefreshChartAsync();
            }
            catch (Exception ex) { _status.Text = "Greška: " + ex.Message; }
        }

        private async System.Threading.Tasks.Task RefreshChartAsync()
        {
            try
            {
                var interval = _cmbInterval.SelectedItem?.ToString() ?? "5";
                var klines = await _api.GetKlinesAsync(_symbol, interval, 200, _category);
                if (klines.Count == 0) { _status.Text = "Nema podataka."; return; }

                // Bybit vraća najnoviju prvu — okreni hronološki
                var ordered = klines.OrderBy(k => k.TimestampMs).ToList();

                _plot.Plot.Clear();

                if (_cmbType.SelectedIndex == 0)
                {
                    var ohlcs = ordered.Select(k => new OHLC(
                        (double)k.Open, (double)k.High, (double)k.Low, (double)k.Close,
                        DateTimeOffset.FromUnixTimeMilliseconds(k.TimestampMs).LocalDateTime,
                        TimeSpan.FromMinutes(IntervalMinutes(interval))
                    )).ToList();
                    _plot.Plot.Add.Candlestick(ohlcs);
                }
                else
                {
                    var xs = ordered.Select(k => DateTimeOffset.FromUnixTimeMilliseconds(k.TimestampMs).LocalDateTime.ToOADate()).ToArray();
                    var ys = ordered.Select(k => (double)k.Close).ToArray();
                    var sp = _plot.Plot.Add.Scatter(xs, ys);
                    sp.LineWidth = 2;
                    _plot.Plot.Axes.DateTimeTicksBottom();
                }

                _plot.Plot.Title($"{_symbol} — {interval}");
                _plot.Plot.YLabel("Cena");
                _plot.Plot.Axes.AutoScale();
                _plot.Refresh();

                _status.Text = $"Osveženo {DateTime.Now:HH:mm:ss}  ({ordered.Count} sveća)";
            }
            catch (Exception ex) { _status.Text = "Greška: " + ex.Message; }
        }

        private static int IntervalMinutes(string iv) => iv switch
        {
            "1" => 1, "3" => 3, "5" => 5, "15" => 15, "30" => 30,
            "60" => 60, "120" => 120, "240" => 240, "360" => 360, "720" => 720,
            "D" => 1440, "W" => 10080, _ => 5
        };
    }
}
