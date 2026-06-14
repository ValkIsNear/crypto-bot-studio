using System.IO;
using Newtonsoft.Json.Linq;

namespace CryptoBot.Services
{
    public class AppConfig
    {
        public string BybitBaseUrl { get; private set; } = "https://api.bybit.com";
        public string ApiKey { get; private set; } = "";
        public string ApiSecret { get; private set; } = "";
        public string RecvWindow { get; private set; } = "5000";
        public string DataFolder { get; private set; } = "Data";

        public static AppConfig Load(string path = "appsettings.json")
        {
            var cfg = new AppConfig();
            if (!File.Exists(path)) return cfg;

            var json = File.ReadAllText(path);
            var root = JObject.Parse(json);

            var bybit = root["Bybit"];
            if (bybit != null)
            {
                cfg.BybitBaseUrl = (string?)bybit["BaseUrl"] ?? cfg.BybitBaseUrl;
                cfg.ApiKey = (string?)bybit["ApiKey"] ?? "";
                cfg.ApiSecret = (string?)bybit["ApiSecret"] ?? "";
                cfg.RecvWindow = (string?)bybit["RecvWindow"] ?? cfg.RecvWindow;
            }

            var csv = root["Csv"];
            if (csv != null)
            {
                cfg.DataFolder = (string?)csv["DataFolder"] ?? cfg.DataFolder;
            }

            Directory.CreateDirectory(cfg.DataFolder);
            return cfg;
        }
    }
}
