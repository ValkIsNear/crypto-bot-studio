# CryptoBot — C# WinForms projekat za Bybit

Desktop aplikacija u **C# (.NET 8, WinForms)** koja se povezuje na **Bybit V5 REST API** i implementira sve zahteve iz projektnog opisa.

## Kako otvoriti u Visual Studiju

1. Instaliraj **Visual Studio 2022** (Community je besplatan) sa workload-om **.NET desktop development**.
2. Iskopiraj ceo folder `CryptoBot/` na svoj računar.
3. U Visual Studiju: **File → Open → Project/Solution…** pa izaberi `CryptoBot/CryptoBot.csproj`.
4. Otvori `appsettings.json` i unesi svoj **Bybit API Key i Secret** (napravi ih na bybit.com → Profile → API).
   - Za testiranje koristi **testnet** ključeve i postavi `BaseUrl` na `https://api-testnet.bybit.com`.
5. **F5** (Start) — pokreće se `MainForm` sa dugmadima Market / Wallet / Trade / Istorija.

## Struktura projekta

```
CryptoBot/
├── CryptoBot.csproj          # .NET 8 WinForms projekat (Newtonsoft.Json paket)
├── appsettings.json          # Bybit BaseUrl, ApiKey, ApiSecret, RecvWindow, DataFolder
├── Program.cs                # Entry point
├── Models/
│   ├── Symbol.cs             # Valutni par
│   ├── Ticker.cs             # Trenutna cena + 24h statistika
│   ├── Kline.cs              # OHLCV "sveca"
│   ├── WalletBalance.cs      # Stanje sredstava
│   └── OrderRecord.cs        # Zapis o nalogu
├── Services/
│   ├── AppConfig.cs          # Učitavanje appsettings.json
│   ├── BybitApiService.cs    # REST klijent + HMAC-SHA256 potpis (V5)
│   └── CsvService.cs         # Čitanje/pisanje CSV (Symbols, Klines, Wallet, Orders)
└── Forms/
    ├── MainForm.cs           # Glavni meni
    ├── MarketForm.cs         # Parovi, cene, likvidnost, učitavanje istorije
    ├── WalletForm.cs         # Wallet stanje + CSV snapshot
    └── TradeForm.cs          # Otvori/zatvori trejd (Spot ili Futures/linear)
```

## Pokrivenost zahteva iz PDF-a

| Zahtev | Gde je implementirano |
|---|---|
| Prikaz svih valutnih parova | `MarketForm` + `BybitApiService.GetSymbolsAsync` |
| Prikaz trenutnih cena | `MarketForm` + `GetTickersAsync` |
| Prikaz likvidnosti | `BybitApiService.GetOrderBookAsync` |
| Otvaranje / zatvaranje trejdova | `TradeForm` + `PlaceOrderAsync` / `ClosePositionAsync` |
| Spot i Futures | `category = "spot"` ili `"linear"` u svim pozivima |
| Wallet stanje | `WalletForm` + `GetWalletBalancesAsync` |
| CSV istorijski podaci | `CsvService` (Symbols, Klines, Wallet, Orders) |
| HttpClient + Newtonsoft.Json | `BybitApiService` |
| Bezbednost (API ključ van koda) | `appsettings.json` + `AppConfig` |

## Važno — bezbednost i rizik

- **Nikad ne commit-uj `appsettings.json` sa pravim ključevima** u javni repo. Dodaj ga u `.gitignore`.
- Za prvo testiranje **OBAVEZNO koristi Bybit testnet** (`https://api-testnet.bybit.com`) da slučajno ne otvoriš pravi nalog.
- Na API ključu uključi samo dozvole koje su ti potrebne (npr. Read + Spot Trade), bez Withdraw.
- Automatizovano trgovanje nosi realan rizik gubitka kapitala — pre svake strategije uradi backtest na CSV podacima.

## Sledeći koraci (proširenja)

- Grafik svećama (npr. `ScottPlot` ili `LiveCharts2`) iznad `Klines` podataka
- Indikatori (SMA, EMA, RSI, MACD) na osnovu `Klines.csv`
- Backtester koji prolazi kroz CSV i simulira strategiju
- Position management za futures (leverage, margin mode, position side)
