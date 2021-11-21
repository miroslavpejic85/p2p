using System.Net;
using System.Net.Sockets;

namespace UdtSharp
{
    public static class ConvertIPAddress
    {
        // TODO use BitConverter/Block.Copy in this class

        public static void ToUintArray(IPAddress ipAddress, ref uint[] outAddress)
        {
            byte[] bytes = ipAddress.GetAddressBytes();
            if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                // TODO addressBytes length must be 4 in this case
                outAddress[0] = (uint)((bytes[3] << 24) + (bytes[2] << 16) + (bytes[1] << 8) + bytes[0]);
                return;
            }

            // TODO addresFamily must by InterNetworkV6
            // addressBytesLenth must be 16
            outAddress[3] = (uint)((bytes[15] << 24) + (bytes[14] << 16) + (bytes[13] << 8) + bytes[12]);
            outAddress[2] = (uint)((bytes[11] << 24) + (bytes[10] << 16) + (bytes[9] << 8) + bytes[8]);
            outAddress[1] = (uint)((bytes[7] << 24) + (bytes[6] << 16) + (bytes[5] << 8) + bytes[4]);
            outAddress[0] = (uint)((bytes[3] << 24) + (bytes[2] << 16) + (bytes[1] << 8) + bytes[0]);
        }
    }

    public static class ConvertLingerOption
    {
        public unsafe static LingerOption FromVoidPointer(void* option)
        {
            bool* pEnabled = (bool*)option;
            bool bEnabled = *pEnabled;

            int* pTime = (int*)(++pEnabled);
            int timeSeconds = *pTime;

            return new LingerOption(bEnabled, timeSeconds);
        }

        public unsafe static void ToVoidPointer(LingerOption lingerOption, void* option)
        {
            bool* pEnabled = (bool*)option;
            *pEnabled = lingerOption.Enabled;

            int* pTime = (int*)(++pEnabled);
            *pTime = lingerOption.LingerTime;
        }
    }
}
