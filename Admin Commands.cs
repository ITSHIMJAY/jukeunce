using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.GettingUpdates;
using Telegram.BotAPI.UpdatingMessages;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously


namespace Estate_Example
{
    internal class Admin_Commands
    {
        [TelegramCommand("ban", true, false)]
        public static async Task Ban(Update update, List<string> args)
        {
            if (args.Count < 1)
            {
                Telegram.client.SendMessage(update.Message.Chat.Id, "Invalid arguments. Usage: /ban <username/id>");
                return;
            }

            string usernameorid = args[0];

            // ban user by id, if none found use the username
            long id = 0;
            long.TryParse(usernameorid, out id);

            JsonUser user = Authentication.Users.Where(x => x.Id == id).FirstOrDefault();
            if (user != null)
            {
                if (user.Id == update.Message.From.Id)
                {
                    Telegram.client.SendMessage(update.Message.Chat.Id, "You cannot ban yourself.");
                    return;
                }

                user.Banned = !user.Banned;
                Authentication.SaveUsers();

                Telegram.client.SendMessage(update.Message.Chat.Id, $"{(user.Banned ? "💥" : "😇")} {user.Username} has been {(user.Banned ? "banned" : "unbanned")}!");
            }
            else
            {
                user = Authentication.Users.Where(x => x.Username == usernameorid).FirstOrDefault();
                if (user != null)
                {
                    if (user.Id == update.Message.From.Id)
                    {
                        Telegram.client.SendMessage(update.Message.Chat.Id, "You cannot ban yourself.");
                        return;
                    }

                    user.Banned = !user.Banned;
                    Authentication.SaveUsers();

                    Telegram.client.SendMessage(update.Message.Chat.Id, $"{(user.Banned ? "💥" : "😇")} {user.Username} has been {(user.Banned ? "banned" : "unbanned")}!");
                }
            }
        }

        [TelegramCommand("genkey", true, false, "genkeys", "keygen", "gen")]
        public static async Task GenKey(Update update, List<string> args)
        {
            if (args.Count < 3)
            {
                Telegram.client.SendMessage(update.Message.Chat.Id, "Invalid arguments. Usage: /genkey <amount> <days> <days-to-expire>");
                return;
            }

            int amount = 0;
            int.TryParse(args[0], out amount);
            int days = 0;
            int.TryParse(args[1], out days);
            int daystoexpire = 0;
            int.TryParse(args[2], out daystoexpire);

            if (amount < 1 || days < 1 || daystoexpire < 1)
            {
                Telegram.client.SendMessage(update.Message.Chat.Id, "Invalid arguments. Usage: /genkey <amount> <days> <days-to-expire>");
                return;
            }

            List<License> keys = KeySystem.GenerateKeys(amount, days, daystoexpire);

            string keylist = "";
            foreach (string key in keys.Select(x => x.Key))
            {
                // add key, with new line, unless its the last key
                keylist += key + "\n";
            }

            // remove if last 
            if (keylist.EndsWith("\n"))
                keylist = keylist.Remove(keylist.Length - 1);

            // send keylist as text file
            InputFile file = new InputFile(Encoding.UTF8.GetBytes(keylist), "keys.txt");
            Telegram.client.SendDocument(update.Message.Chat.Id, file);
        }

        [TelegramCommand("keys", true, false, "exportkeys", "keylist")]
        public static async Task Keys(Update update, List<string> args)
        {
            string usedkeys = "";
            string expiredkeys = "";
            string validkeys = "";

            foreach (License key in KeySystem.Keys)
            {
                if (key.Used)
                    usedkeys += $"{key.Key} | Days: {key.Days} | Expiry: {key.Expiry}\n";
                else if (key.Expiry < DateTime.Now)
                    expiredkeys += $"{key.Key} | Days: {key.Days} | Expiry: {key.Expiry}\n";
                else
                    validkeys += $"{key.Key} | Days: {key.Days} | Expiry: {key.Expiry}\n";
            }
            
            if (!string.IsNullOrWhiteSpace(usedkeys)) Telegram.client.SendDocument(update.Message.Chat.Id, new InputFile(Encoding.UTF8.GetBytes(usedkeys), "usedkeys.txt"));
            if (!string.IsNullOrWhiteSpace(expiredkeys)) Telegram.client.SendDocument(update.Message.Chat.Id, new InputFile(Encoding.UTF8.GetBytes(expiredkeys), "expiredkeys.txt"));
            if (!string.IsNullOrWhiteSpace(validkeys)) Telegram.client.SendDocument(update.Message.Chat.Id, new InputFile(Encoding.UTF8.GetBytes(validkeys), "validkeys.txt"));
        }
        
        [TelegramCommand("balance", true, false)]
        public static async Task Balance(Update update, List<string> args)
        {
            string? balanceresponse = Estate.GetBalance().Result;
            EstateResponse balance = JsonConvert.DeserializeObject<EstateResponse>(balanceresponse);
            if (balance.Status == "success")
                Telegram.client.SendMessage(update.Message.Chat.Id, $"💰 Available Balance: {balance.Message} $");
            else
                Telegram.client.SendMessage(update.Message.Chat.Id, $"❌ Error: {balance.Message}");
        }

        [TelegramCommand("status", true, false, "stats", "statistics")]
        public static async Task Status(Update update, List<string> args)
        {
            // get user statistics
            int usercount = Authentication.Users.Count;
            int activeusers = Authentication.Users.Where(x => x.Expiry > DateTime.Now).Count();
            int bannedusers = Authentication.Users.Where(x => x.Banned).Count();

            // get key statistics
            int keycount = KeySystem.Keys.Count;
            int validkeys = KeySystem.Keys.Where(x => !x.Used && x.Expiry > DateTime.Now).Count();
            int usedkeys = KeySystem.Keys.Where(x => x.Used).Count();
            int expiredkeys = KeySystem.Keys.Where(x => x.Expiry < DateTime.Now).Count();

            // craft message
            string message = $"👥 Users: {usercount} | Active: {activeusers} | Banned: {bannedusers}\n";
            message += $"🔑 Keys: {keycount} | Valid: {validkeys} | Used: {usedkeys} | Expired: {expiredkeys - usedkeys}";

            Telegram.client.SendMessage(update.Message.Chat.Id, message);
        }

        [TelegramCommand("load", true, false)]
        public static async Task Load(Update update, List<string> args)
        {
            Authentication.LoadUsers();
            KeySystem.LoadKeys();
            Estate.LoadScripts();

            Telegram.client.SendMessage(update.Message.Chat.Id, "✅ Loaded users, keys, and scripts.");
        }

        [TelegramCommand("save", true, false)]
        public static async Task Save(Update update, List<string> args)
        {
            Authentication.SaveUsers();
            KeySystem.SaveKeys();
            Estate.SaveScripts();

            Telegram.client.SendMessage(update.Message.Chat.Id, "✅ Saved users, keys, and scripts.");
        }
    }
}