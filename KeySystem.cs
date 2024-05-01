using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.BotAPI.AvailableTypes;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8601 // Possible null reference assignment.

namespace Estate_Example
{
    internal class KeySystem
    {
        public static List<License> Keys = new List<License>();

        public static void LoadKeys()
        {
            if (!System.IO.File.Exists("keys.json"))
            {
                SaveKeys();
                LoadKeys();
            }
            else
            {
                string json = System.IO.File.ReadAllText("keys.json");
                Keys = JsonConvert.DeserializeObject<List<License>>(json);
            }
        }

        public static void SaveKeys()
        {
            string json = JsonConvert.SerializeObject(Keys, Formatting.Indented);
            System.IO.File.WriteAllText("keys.json", json);
        }

        public static List<License> GenerateKeys(int amount, double days, double expiryDays)
        {
            List<License> newkeys = new List<License>();

            string brandName = Settings.settings.Brand.key_prefix;

            for (int i = 0; i < amount; i++)
            {
                License key = new License()
                {
                    Key = $"{brandName}-{Guid.NewGuid()}",
                    Days = days,
                    Expiry = DateTime.UtcNow.AddDays(expiryDays),
                    Used = false
                };

                // check if key already exists
                if (Keys.Where(x => x.Key == key.Key).FirstOrDefault() != null)
                {
                    i--;
                    continue; // retry
                }

                Keys.Add(key);
                newkeys.Add(key);
            }

            SaveKeys();
            return newkeys;
        }
    }

    public class License
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("days")]
        public double Days { get; set; }

        [JsonProperty("expiry")]
        public DateTime Expiry { get; set; }

        [JsonProperty("used")]
        public bool Used { get; set; }
    }
}