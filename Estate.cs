using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8601 // Possible null reference assignment.

namespace Estate_Example
{
    internal class Estate
    {
        public static HttpClient client = new HttpClient();
        public static string BaseUrl = "https://api.estate.red/";
        public static string Username = "";
        public static string APIKey = "";

        public static List<EstateScript> Scripts = new List<EstateScript>();

        public static void Initialize(string username, string apikey)
        {
            Username = username;
            APIKey = apikey;

            // test Estate
            string? calllogresponse = Estate.GetBalance().Result;
            EstateResponse balance = JsonConvert.DeserializeObject<EstateResponse>(calllogresponse);
            if (balance.Status == "success")
                Console.WriteLine("[ESTATE]: Available Balance: " + balance.Message + " $");
            else
                Console.WriteLine("[ESTATE]: " + balance.Message);

            LoadScripts();
            Console.WriteLine("[ESTATE]: Loaded " + Scripts.Count + " scripts.");
        }

        public static void LoadScripts()
        {
            if (!File.Exists("scripts.json"))
            {
                Scripts.Add(new EstateScript()
                {
                    Name = "Example Script",
                    Id = "<Copy from script editor>",
                    Description = "This is an example script.",
                });
                SaveScripts();
                LoadScripts();
            }
            else
            {
                string json = File.ReadAllText("scripts.json");
                Scripts = JsonConvert.DeserializeObject<List<EstateScript>>(json);
            }
        }

        public static void SaveScripts()
        {
            string json = JsonConvert.SerializeObject(Scripts, Formatting.Indented);
            File.WriteAllText("scripts.json", json);
        }

        public static async Task<string> StartCall(string to, string displayname, string scriptid, string vars)
        {
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "username", Username },
                { "apikey", APIKey },
                { "to", to },
                { "displayname", displayname },
                { "scriptid", scriptid },
                { "vars", vars }
            };

            var response = client.PostAsync(BaseUrl + "/call", new FormUrlEncodedContent(data)).Result;
            return await response.Content.ReadAsStringAsync();
        }

        public static async Task<string> TriggerEvent(string eventname, string callid)
        {
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "username", Username },
                { "apikey", APIKey },
                { "event", eventname },
                { "callid", callid }
            };

            var response = client.PostAsync(BaseUrl + "/event", new FormUrlEncodedContent(data)).Result;
            return await response.Content.ReadAsStringAsync();
        }

        public static async Task<string> GetCallLog(string id) => await client.GetAsync(BaseUrl + $"/get_call_log?username={Username}&apikey={APIKey}&id={id}").Result.Content.ReadAsStringAsync();
        public static async Task<string> GetScriptVariables(string scriptid) => await client.GetAsync(BaseUrl + $"/get_script_variables?username={Username}&apikey={APIKey}&scriptid={scriptid}").Result.Content.ReadAsStringAsync();
        public static async Task<string> GetBalance() => await client.GetAsync(BaseUrl + $"/balance?username={Username}&apikey={APIKey}").Result.Content.ReadAsStringAsync();
    }

    public class EstateResponse
    {
        [JsonProperty("status")]
        public string? Status { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("id")]
        public string? Id { get; set; }
    }

    public partial class EstateCallLog
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("phone_to")]
        public string PhoneTo { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("transcript")]
        public string Transcript { get; set; }

        [JsonProperty("call_control_id")]
        public string CallControlId { get; set; }

        [JsonProperty("script_id")]
        public string ScriptId { get; set; }

        [JsonProperty("vars")]
        public string Vars { get; set; }

        [JsonProperty("status_log")]
        public string StatusLog { get; set; }

        [JsonProperty("caller_username")]
        public string CallerUsername { get; set; }

        [JsonProperty("date")]
        public DateTimeOffset Date { get; set; }
    }

    public partial class EstateScript
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}