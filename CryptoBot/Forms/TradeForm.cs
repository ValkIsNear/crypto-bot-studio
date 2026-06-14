using System;
using System.Globalization;
using System.Windows.Forms;
using CryptoBot.Services;

namespace CryptoBot.Forms
{
    public class TradeForm : Form
    {
        private readonly BybitApiService _api;
        private readonly CsvService _csv;

        private ComboBox _cmbCategory = null!;
        private ComboBox _cmbSide = null!;
        private ComboBox _cmbType = null!;
        private TextBox _txtSymbol = null!;
        private TextBox _txtQty = null!;
        private TextBox _txtPrice = null!;
        private Button _btnSend = null!;
        private Button _btnClose = null!;
        private TextBox _log = null!;

        public TradeForm(BybitApiService api, CsvService csv)
        {
            _api = api; _csv = csv;
            Text = "Trade — otvori / zatvori poziciju";
            Width = 560; Height = 480;
            StartPosition = FormStartPosition.CenterParent;
            BuildUi();
        }

        private void BuildUi()
        {
            int y = 15;
            Controls.Add(new Label { Text = "Tržište:", Left = 15, Top = y + 3, Width = 70 });
            _cmbCategory = new ComboBox { Left = 90, Top = y, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbCategory.Items.AddRange(new object[] { "spot", "linear" });
            _cmbCategory.SelectedIndex = 0;
            Controls.Add(_cmbCategory);

            y += 35;
            Controls.Add(new Label { Text = "Symbol:", Left = 15, Top = y + 3, Width = 70 });
            _txtSymbol = new TextBox { Left = 90, Top = y, Width = 150, Text = "BTCUSDT" };
            Controls.Add(_txtSymbol);

            y += 35;
            Controls.Add(new Label { Text = "Strana:", Left = 15, Top = y + 3, Width = 70 });
            _cmbSide = new ComboBox { Left = 90, Top = y, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbSide.Items.AddRange(new object[] { "Buy", "Sell" });
            _cmbSide.SelectedIndex = 0;
            Controls.Add(_cmbSide);

            y += 35;
            Controls.Add(new Label { Text = "Tip:", Left = 15, Top = y + 3, Width = 70 });
            _cmbType = new ComboBox { Left = 90, Top = y, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbType.Items.AddRange(new object[] { "Market", "Limit" });
            _cmbType.SelectedIndex = 0;
            Controls.Add(_cmbType);

            y += 35;
            Controls.Add(new Label { Text = "Količina:", Left = 15, Top = y + 3, Width = 70 });
            _txtQty = new TextBox { Left = 90, Top = y, Width = 120, Text = "0.001" };
            Controls.Add(_txtQty);

            y += 35;
            Controls.Add(new Label { Text = "Cena (Limit):", Left = 15, Top = y + 3, Width = 90 });
            _txtPrice = new TextBox { Left = 110, Top = y, Width = 120, Text = "" };
            Controls.Add(_txtPrice);

            y += 45;
            _btnSend = new Button { Text = "Pošalji nalog", Left = 15, Top = y, Width = 180, Height = 35 };
            _btnClose = new Button { Text = "Zatvori poziciju (suprotni Market)", Left = 210, Top = y, Width = 290, Height = 35 };
            Controls.Add(_btnSend);
            Controls.Add(_btnClose);

            y += 45;
            _log = new TextBox
            {
                Left = 15, Top = y, Width = 510, Height = 180,
                Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            Controls.Add(_log);

            _btnSend.Click += async (_, _) => await SendAsync(false);
            _btnClose.Click += async (_, _) => await SendAsync(true);
        }

        private async System.Threading.Tasks.Task SendAsync(bool close)
        {
            try
            {
                if (!decimal.TryParse(_txtQty.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var qty))
                {
                    MessageBox.Show("Neispravna količina."); return;
                }
                decimal? price = null;
                if (_cmbType.Text == "Limit" &&
                    decimal.TryParse(_txtPrice.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var p))
                    price = p;

                OrderRecordResult res;
                if (close)
                {
                    var o = await _api.ClosePositionAsync(_txtSymbol.Text.Trim().ToUpperInvariant(),
                        _cmbSide.Text, qty, _cmbCategory.Text);
                    res = new OrderRecordResult(o);
                }
                else
                {
                    var o = await _api.PlaceOrderAsync(_txtSymbol.Text.Trim().ToUpperInvariant(),
                        _cmbSide.Text, qty, _cmbType.Text, price, _cmbCategory.Text);
                    res = new OrderRecordResult(o);
                }

                _csv.AppendOrder(res.Order);
                _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {res.Order.Side} {res.Order.OrderType} {res.Order.Qty} {res.Order.Symbol} → {res.Order.Status} (id={res.Order.OrderId}){Environment.NewLine}");
            }
            catch (Exception ex)
            {
                _log.AppendText($"[GREŠKA] {ex.Message}{Environment.NewLine}");
            }
        }

        private class OrderRecordResult
        {
            public Models.OrderRecord Order { get; }
            public OrderRecordResult(Models.OrderRecord o) { Order = o; }
        }
    }
}
