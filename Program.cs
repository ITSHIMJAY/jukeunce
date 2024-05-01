using System;

namespace Estate_Example
{
    public class Program
    {
        static void Main(string[] args)
        {
            Settings.LoadSettings();
            Console.Title = "OTP | Call logs";

            Estate.Initialize(Settings.settings.Estate_Username, Settings.settings.Estate_APIKey);
            Telegram.Initialize(Settings.settings.Telegram_Bot_Token);

            Authentication.LoadUsers();
            KeySystem.LoadKeys();

            Console.ReadLine();
        }
    }
}