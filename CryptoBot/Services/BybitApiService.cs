using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CryptoBot.Models;
using Newtonsoft.Json.Linq;

namespace CryptoBot.Services
{
    /// <summary>
    /// Klijent za Bybit V5 REST API.
    /// Javni endpointi (market data) ne zahtevaju potpis.
    /// Privatni endpointi (wallet, order) koriste HMAC-SHA256 potpis (V5 sema):
    ///   prehash = timestamp + apiKey + recvWindow + (queryString ili telo)
    /// </summary>
    public class BybitApiService
    {
        private readonly HttpClient _http;
        private readonly AppConfig _cfg;

        public BybitApiService(AppConfig cfg)
        {
            _cfg = cfg;
            _http = new HttpClient { BaseAddress = new Uri(cfg.BybitBaseUrl) };
            _http.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        // ============== JAVNI ENDPOINTI ==============

        public async Task<List<Symbol>> GetSymbolsAsync(string category = "spot")
        {
            var url = $"/v5/market/instruments-info?category={category}";
            var json = await _http.GetStringAsync(url);
            var root = JObject.Parse(json);

            var list = new List<Symbol>();
            var arr = root["result"]?["list"] as JArray;
            if (arr == null) return list;

            foreach (var item in arr)
            {
                list.Add(new Symbol
                {
                    Name = (string?)item["symbol"] ?? "",
                    BaseCoin = (string?)item["baseCoin"] ?? "",
                    QuoteCoin = (string?)item["quoteCoin"] ?? "",
                    Status = (string?)item["status"] ?? "",
                    Category = category
                });
            }
            return list;
        }

        public async Task<List<Ticker>> GetTickersAsync(string category = "spot")
        {
            var url = $"/v5/market/tickers?category={category}";
            var json = await _http.GetStringAsync(url);
            var root = JObject.Parse(json);

            var list = new List<Ticker>();
            var arr = root["result"]?["list"] as JArray;
            if (arr == null) return list;

            foreach (var item in arr)
            {
                list.Add(new Ticker
                {
                    Symbol = (string?)item["symbol"] ?? "",
                    LastPrice = ParseDec(item["lastPrice"]),
                    Bid1Price = ParseDec(item["bid1Price"]),
                    Ask1Price = ParseDec(item["ask1Price"]),
                    HighPrice24h = ParseDec(item["highPrice24h"]),
                    LowPrice24h = ParseDec(item["lowPrice24h"]),
                    Volume24h = ParseDec(item["volume24h"]),
                    Turnover24h = ParseDec(item["turnover24h"]),
                    PriceChangePercent24h = ParseDec(item["price24hPcnt"]) * 100m
                });
            }
            return list;
        }

        /// <summary>
        /// Istorijske svece (OHLCV) za zadati simbol i interval.
        /// interval: 1, 3, 5, 15, 30, 60, 120, 240, 360, 720, D, W, M
        /// </summary>
        public async Task<List<Kline>> GetKlinesAsync(string symbol, string interval = "60", int limit = 200, string category = "spot")
        {
            var url = $"/v5/market/kline?category={category}&symbol={symbol}&interval={interval}&limit={limit}";
            var json = await _http.GetStringAsync(url);
            var root = JObject.Parse(json);

            var list = new List<Kline>();
            var arr = root["result"]?["list"] as JArray;
            if (arr == null) return list;

            foreach (var row in arr)
            {
                // Format: [start, open, high, low, close, volume, turnover]
                list.Add(new Kline
                {
                    Symbol = symbol,
                    Interval = interval,
                    TimestampMs = long.Parse((string)row[0]!, CultureInfo.InvariantCulture),
                    Open = ParseDec(row[1]),
                    High = ParseDec(row[2]),
                    Low = ParseDec(row[3]),
                    Close = ParseDec(row[4]),
                    Volume = ParseDec(row[5])
                });
            }
            return list;
        }

        /// <summary>
        /// Knjiga naloga (likvidnost / dubina trzista).
        /// </summary>
        public async Task<JObject> GetOrderBookAsync(string symbol, int limit = 25, string category = "spot")
        {
            var url = $"/v5/market/orderbook?category={category}&symbol={symbol}&limit={limit}";
            var json = await _http.GetStringAsync(url);
            return JObject.Parse(json);
        }

        // ============== PRIVATNI ENDPOINTI ==============

        public async Task<List<WalletBalance>> GetWalletBalancesAsync(string accountType = "UNIFIED")
        {
            var query = $"accountType={accountType}";
            var json = await SignedGetAsync("/v5/account/wallet-balance", query);
            var root = JObject.Parse(json);

            var list = new List<WalletBalance>();
            var coins = root["result"]?["list"]?[0]?["coin"] as JArray;
            if (coins == null) return list;

            foreach (var c in coins)
            {
                list.Add(new WalletBalance
                {
                    Asset = (string?)c["coin"] ?? "",
                    TotalBalance = ParseDec(c["walletBalance"]),
                    AvailableBalance = ParseDec(c["availableToWithdraw"]),
                    Timestamp = DateTime.UtcNow
                });
            }
            return list;
        }

        /// <summary>
        /// Postavlja nalog (market ili limit). category: spot ili linear (futures).
        /// </summary>
        public async Task<OrderRecord> PlaceOrderAsync(string symbol, string side, decimal qty,
            string orderType = "Market", decimal? price = null, string category = "spot")
        {
            var body = new JObject
            {
                ["category"] = category,
                ["symbol"] = symbol,
                ["side"] = side,                  // "Buy" ili "Sell"
                ["orderType"] = orderType,        // "Market" ili "Limit"
                ["qty"] = qty.ToString(CultureInfo.InvariantCulture)
            };
            if (orderType == "Limit" && price.HasValue)
                body["price"] = price.Value.ToString(CultureInfo.InvariantCulture);

            var json = await SignedPostAsync("/v5/order/create", body.ToString(Newtonsoft.Json.Formatting.None));
            var root = JObject.Parse(json);

            return new OrderRecord
            {
                OrderId = (string?)root["result"]?["orderId"] ?? "",
                Symbol = symbol,
                Category = category,
                Side = side,
                OrderType = orderType,
                Qty = qty,
                Price = price ?? 0m,
                Status = (string?)root["retMsg"] ?? "",
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// "Zatvara" poziciju jednostavnim suprotnim market nalogom.
        /// </summary>
        public Task<OrderRecord> ClosePositionAsync(string symbol, string currentSide, decimal qty, string category = "linear")
        {
            var opposite = currentSide.Equals("Buy", StringComparison.OrdinalIgnoreCase) ? "Sell" : "Buy";
            return PlaceOrderAsync(symbol, opposite, qty, "Market", null, category);
        }

        // ============== POTPISIVANJE (HMAC-SHA256) ==============

        private async Task<string> SignedGetAsync(string path, string query)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var prehash = timestamp + _cfg.ApiKey + _cfg.RecvWindow + query;
            var sign = Sign(prehash, _cfg.ApiSecret);

            var req = new HttpRequestMessage(HttpMethod.Get, path + "?" + query);
            AddAuthHeaders(req, timestamp, sign);

            var resp = await _http.SendAsync(req);
            return await resp.Content.ReadAsStringAsync();
        }

        private async Task<string> SignedPostAsync(string path, string jsonBody)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var prehash = timestamp + _cfg.ApiKey + _cfg.RecvWindow + jsonBody;
            var sign = Sign(prehash, _cfg.ApiSecret);

            var req = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };
            AddAuthHeaders(req, timestamp, sign);

            var resp = await _http.SendAsync(req);
            return await resp.Content.ReadAsStringAsync();
        }

        private void AddAuthHeaders(HttpRequestMessage req, string timestamp, string sign)
        {
            req.Headers.Add("X-BAPI-API-KEY", _cfg.ApiKey);
            req.Headers.Add("X-BAPI-SIGN", sign);
            req.Headers.Add("X-BAPI-SIGN-TYPE", "2");
            req.Headers.Add("X-BAPI-TIMESTAMP", timestamp);
            req.Headers.Add("X-BAPI-RECV-WINDOW", _cfg.RecvWindow);
        }

        private static string Sign(string payload, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static decimal ParseDec(JToken? t)
        {
            if (t == null) return 0m;
            var s = t.ToString();
            if (string.IsNullOrWhiteSpace(s)) return 0m;
            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m;
        }
    }
}
