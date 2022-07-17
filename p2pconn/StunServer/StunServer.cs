using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace p2p.StunServer
{
    public class StunServer
    {
        // Special Thanks to https://github.com/johngagefaulkner for this class!

        public string Server { get; set; }
        public int Port { get; set; }

        /// <summary>
        /// Loads list of Stun Servers from a local file path.
        /// </summary>
        /// <param name="filePath">Full path to the local Stun Server list (.JSON). Example: C:\Users\Administrator\Downloads\StunServers.json</param>
        /// <returns>An array of 'StunServer' objects.</returns>
        public static StunServer[] GetStunServersFromFile(string filePath)
        {
            string _json = System.IO.File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<StunServer[]>(_json);
        }

        /// <summary>
        /// Save the list of Stun Servers to the local file path.
        /// </summary>
        /// <param name="StunServer">Stun Server lists</param>
        /// <param name="filePath">Full path to the local Stun Server list (.JSON). Example: C:\Users\Administrator\Downloads\StunServers.json</param>
        /// <returns>Bool true/false</returns>
        public static bool WriteStunServersToFile(List<StunServer> StunServer, string filePath)
        {
            string JSONString = JsonConvert.SerializeObject(StunServer);
            File.WriteAllText(filePath, JSONString);
            return true;
        }

        /// <summary>
        /// Loads list of Stun Servers from an HTTP/HTTPS URL.
        /// </summary>
        /// <param name="fileUrl">Full URL to the Stun Server list (.JSON). Example: "https://raw.github.com/username/repo/files/StunServers.json"</param>
        /// <returns>An array of 'StunServer' objects.</returns>
        public static StunServer[] GetStunServersFromUrl(string fileUrl)
        {
            string _json;
            using (System.Net.WebClient wc = new System.Net.WebClient())
            {
                _json = wc.DownloadString(fileUrl);
            }
            return JsonConvert.DeserializeObject<StunServer[]>(_json);
        }

        /// <summary>
        /// Loads list of Stun Servers from a pre-populated JSON string.
        /// </summary>
        /// <param name="json">A string, in JSON format, containing an array of Stun Server objects.</param>
        /// <returns>An array of 'StunServer' objects.</returns>
        public static StunServer[] GetStunServersFromJson(string json)
        {
            return JsonConvert.DeserializeObject<StunServer[]>(json);
        }
    }
}
