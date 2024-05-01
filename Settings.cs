using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Estate_Example
{
    public class Settings
    {
        public static SettingsJson settings = new SettingsJson();

        public static void LoadSettings()
        {
            // if settings.json dont exist, save and load
            if (!File.Exists("settings.json"))
            {
                SaveSettings();
                LoadSettings();
            }
            else
            {
                string json = File.ReadAllText("settings.json");
                settings = JsonConvert.DeserializeObject<SettingsJson>(json);
            }
        }

        public static void SaveSettings()
        {
            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText("settings.json", json);
        }
    }

    // DO NOT EDIT THE VALUES HERE, RUN THE PROGRAM ONCE AND EDIT THE settings.json FILE INSTEAD!!!!
    public class SettingsJson
    {
        [JsonProperty("estate_username")]
        public string Estate_Username { get; set; } = "";

        [JsonProperty("estate_apikey")]
        public string Estate_APIKey { get; set; } = "";

        [JsonProperty("telegram_bot_token")]
        public string Telegram_Bot_Token { get; set; } = "";

        [JsonProperty("brand")]
        public Brand Brand { get; set; } = new Brand();
    }

    public class Brand
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("key_prefix")]
        public string key_prefix { get; set; } = "";

        [JsonProperty("store_url")]
        public string Store_URL { get; set; } = "";
    }
}