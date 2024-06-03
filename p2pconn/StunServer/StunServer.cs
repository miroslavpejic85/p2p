using System.Collections.Generic;
using System.IO;
using System.Text.Json;

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
        public static async Task<StunServer[]> GetStunServersFromFileAsync(string filePath)
        {
            var fileHandle = File.OpenRead(filePath);

            var stunServers = await JsonSerializer.DeserializeAsync<StunServer[]>(fileHandle);
            if (stunServers is null)
            {
                return [];
            }

            return stunServers;
        }

        /// <summary>
        /// Save the list of Stun Servers to the local file path.
        /// </summary>
        /// <param name="StunServer">Stun Server lists</param>
        /// <param name="filePath">Full path to the local Stun Server list (.JSON). Example: C:\Users\Administrator\Downloads\StunServers.json</param>
        /// <returns>Bool true/false</returns>
        public static async Task<bool> WriteStunServersToFileAsync(List<StunServer> StunServer, string filePath)
        {
            string JSONString = JsonSerializer.Serialize(StunServer);
            await File.WriteAllTextAsync(filePath, JSONString);

            return true;
        }

        /// <summary>
        /// Loads list of Stun Servers from an HTTP/HTTPS URL.
        /// </summary>
        /// <param name="fileUrl">Full URL to the Stun Server list (.JSON). Example: "https://raw.github.com/username/repo/files/StunServers.json"</param>
        /// <returns>An array of 'StunServer' objects.</returns>
        public static async Task<StunServer[]> GetStunServersFromUrlAsync(string fileUrl)
        {
            using (var httpClient = new HttpClient())
            {
                var stream = await httpClient.GetStreamAsync(fileUrl);

                var stunServers = await JsonSerializer.DeserializeAsync<StunServer[]>(stream);
                if (stunServers is null)
                {
                    return [];
                }

                return stunServers;
            }
        }

        /// <summary>
        /// Loads list of Stun Servers from a pre-populated JSON string.
        /// </summary>
        /// <param name="json">A string, in JSON format, containing an array of Stun Server objects.</param>
        /// <returns>An array of 'StunServer' objects.</returns>
        public static StunServer[] GetStunServersFromJson(string json)
        {
            var stunServers = JsonSerializer.Deserialize<StunServer[]>(json);
            if (stunServers is null)
            {
                return [];
            }

            return stunServers;
        }
    }
}
