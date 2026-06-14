using System;
using System.Windows.Forms;
using CryptoBot.Forms;

namespace CryptoBot
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
