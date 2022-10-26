using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace p2p
{
    using Microsoft.Win32;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace p2p
    {
        public enum P2PKeys
        {
            LastIp,
            LastPort,
            LastExternalIP,
            LastExternalPort,
        }

        public static class RegistryFuncs
        {
            public const string P2pPath = @"HKEY_CURRENT_USER\Software\P2pRemoteDesktop\";
            public static string GetFromRegistry(P2PKeys key)
            {
                string keyValue = (string)Registry.GetValue(P2pPath, key.ToString(), null);
                return keyValue;
            }
            public static void SetToRegistry(P2PKeys key, string value)
            {
                Registry.SetValue(P2pPath, key.ToString(), value);
            }
        }
    }
}
