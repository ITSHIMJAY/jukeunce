using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.GettingUpdates;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8604 // Possible null reference argument.

namespace Estate_Example
{
    internal class Telegram
    {
        public static BotClient client { get; set; }

        public static void Initialize(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("[TELEGRAM] Please fill out the Telegram token in the settings.json file.");
                return;
            }

            client = new BotClient(token);
            try
            {
                var me = client.GetMe();
                Console.WriteLine($"[TELEGRAM] Bot named: '{me.FirstName}' is now online!");
            }
            catch
            {
                Console.WriteLine("[TELEGRAM] Invalid Telegram token.");
                return;
            }

            new Thread(PollingThread).Start();
        }

        public static void PollingThread()
        {
        redo:
            try
            {
                var updates = client.GetUpdates();
                while (true)
                {
                    try
                    {
                        if (updates.Any())
                        {
                            foreach (Update update in updates)
                            {
                                uint unix = 0;
                                if (update.Message != null) unix = update.Message.Date;

                                // if update is older than now or a callback
                                if (unix + 2 > DateTimeOffset.Now.ToUnixTimeSeconds() || update.CallbackQuery != null)
                                    new Thread(() => HandleUpdate(update)).Start();
                            }

                            var offset = updates.Last().UpdateId + 1;
                            updates = client.GetUpdates(offset);
                        }
                        else
                            updates = client.GetUpdates();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[TELEGRAM] ERROR: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[TELEGRAM] " + ex.Message);
                Thread.Sleep(2000);
                goto redo;
            }
        }

        public static void HandleUpdate(Update update)
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    if (update.Message == null || update.Message.Text == null) return;
                    if (update.Message.Text.StartsWith("/"))
                    {
                        string command = update.Message.Text.Split(' ')[0].Substring(1);
                        List<string> args = update.Message.Text.Split(' ').Skip(1).ToList();
                        HandleCommand(update, command, args);
                    }
                    else
                    {
                        if (update.Message.Text.StartsWith("?"))
                            HandleCommand(update, "?", new List<string>());
                    }
                    break;
                case UpdateType.CallbackQuery:
                    Console.WriteLine("[TELEGRAM] CallbackQuery: " + update.CallbackQuery.Data);
                    var query = update.CallbackQuery;
                    string querydata = query.Data.Split(':')[0];
                    string messageid = query.Data.Split(':')[1];
                    try { Telegram.client.AnswerCallbackQuery(query.Id); } catch { }
                    HandleCommand(update, "", new List<string>() { messageid }, querydata);
                    break;
            }
        }


        public static void HandleCommand(Update update, string command, List<string> args, string button_callback = "")
        {
            string argstext = args.Count > 0 ? "| Args: " + string.Join(", ", args) : "";
            User user = string.IsNullOrWhiteSpace(button_callback) ? update.Message.From : update.CallbackQuery.From;
            Console.WriteLine($"[TELEGRAM] USER: {(string.IsNullOrWhiteSpace(button_callback) ? user.FirstName : user.FirstName)} | {(string.IsNullOrWhiteSpace(button_callback) ? ($"Command: /{command} {argstext}") : $"Button: {button_callback}")} ");
            bool validCommand = false;
            if (!string.IsNullOrWhiteSpace(button_callback))
                command = "BUTTON:" + button_callback;

            var assembly = Assembly.GetExecutingAssembly();
            var types = assembly.GetTypes();

            foreach (var type in types)
            {
                var methods = type.GetMethods();

                foreach (var method in methods)
                {
                    TelegramCommand attribute = method.GetCustomAttribute(typeof(TelegramCommand)) as TelegramCommand;
                    if (attribute != null)
                    {
                        if (attribute.Name.ToLower() == command.ToLower())
                            validCommand = true;

                        // check for aliases
                        if (attribute.Alias.Count > 0)
                        {
                            foreach (string alias in attribute.Alias)
                            {
                                if (alias.ToLower() == command.ToLower())
                                    validCommand = true;
                            }
                        }

                        if (validCommand)
                        {
                        recheck:

                            int status = 0;
                            if (update.Message == null && update.CallbackQuery != null)
                                status = 1;
                            else
                                status = Authentication.CheckUser(update.Message.From, attribute.RequiresAdmin, attribute.RequiresSubscription);

                            switch (status)
                            {
                                case 0: client.SendMessage(update.Message.Chat.Id, "❌ You do not have permission to use this command."); break;
                                case 1: method.Invoke(type, new object[] { update, args }); break; // success
                                case 2: client.SendMessage(update.Message.Chat.Id, $"❌ Your subscription has expired.\n\n🔑 <a href='{Settings.settings.Brand.Store_URL}'>Buy a license key here</a> 🔑\n", parseMode: "HTML"); break;
                                case 3: client.SendMessage(update.Message.Chat.Id, "❌ You have been banned."); break;
                                case 4: goto recheck; // new account, recheck after creating account
                            }

                            return;
                        }
                    }
                }
            }

            if (!validCommand)
                client.SendMessage(update.Message.Chat.Id, "❌ Invalid command. Try /help to list all the commands available.");
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TelegramCommand : Attribute
    {
        public string Name { get; set; }
        public bool RequiresAdmin { get; set; }
        public bool RequiresSubscription { get; set; }
        public List<string> Alias { get; set; } = new List<string>();

        public TelegramCommand(string name, bool requiresAdmin = false, bool requiresSubscription = false, params string[] alias)
        {
            Name = name;
            RequiresAdmin = requiresAdmin;
            RequiresSubscription = requiresSubscription;

            if (alias.Length > 0)
                Alias.AddRange(alias);
        }
    }
}
