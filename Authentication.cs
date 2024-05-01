using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.InlineMode;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8601 // Possible null reference assignment.

namespace Estate_Example
{
    internal class Authentication
    {
        public static List<JsonUser> Users = new List<JsonUser>();

        public static void LoadUsers()
        {
            if (!System.IO.File.Exists("users.json"))
            {
                SaveUsers();
                LoadUsers();
            }
            else
            {
                string json = System.IO.File.ReadAllText("users.json");
                Users = JsonConvert.DeserializeObject<List<JsonUser>>(json);
            }
        }

        public static void SaveUsers()
        {
            // clone users list to a new list
            List<JsonUser> users = new List<JsonUser>();
            users.AddRange(Users);

            redo:
            try
            {
                string json = JsonConvert.SerializeObject(users, Formatting.Indented);
                System.IO.File.WriteAllText("users.json", json);
            }
            catch
            {
                Thread.Sleep(2000);
                goto redo;
            }
        }

        public static int CheckUser(User telegramuser, bool requiresAdmin, bool requiresSubscription)
        {
            long id = telegramuser.Id;
            string name = telegramuser.FirstName;

            JsonUser? user = Users.Where(x => x.Id == id).FirstOrDefault();
            if (user != null)
            {
                if (user.Banned)
                    return 3;

                if (requiresAdmin && user.Admin)
                    return 1;
                else if (requiresAdmin && !user.Admin)
                    return 0;

                if (requiresSubscription)
                {
                    // check expiry timestamp utc now
                    if (user.Expiry > DateTime.UtcNow)
                        return 1;
                    else
                        return 2;
                }

                return 1;
            }
            else
            {
                JsonUser newuser = new JsonUser()
                {
                    Username = name,
                    Id = id,
                    Email = "",
                    Admin = false,
                    Banned = false,
                    Expiry = DateTime.UnixEpoch, // 1/1/1970
                };

                // add user
                Users.Add(newuser);
                SaveUsers();
                return 4;
            }
        }
    }

    public class JsonUser
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("admin")]
        public bool Admin { get; set; }

        [JsonProperty("banned")]
        public bool Banned { get; set; }

        [JsonProperty("expiry")]
        public DateTime Expiry { get; set; }

        [JsonProperty("keys")]
        public List<string> Keys { get; set; }
    }
}