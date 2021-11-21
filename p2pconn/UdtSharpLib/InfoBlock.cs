using System;
using System.Net;
using System.Net.Sockets;

namespace UdtSharp
{
    public class InfoBlock
    {
        uint[] m_piIP = new uint[4];      // IP address, machine read only, not human readable format
        AddressFamily m_iIPversion;       // IP version
        public ulong m_ullTimeStamp;    // last update time
        public int m_iRTT;         // RTT
        public int m_iBandwidth;       // estimated bandwidth
        public int m_iLossRate;        // average loss rate
        public int m_iReorderDistance; // packet reordering distance
        public double m_dInterval;     // inter-packet time, congestion control
        public double m_dCWnd;     // congestion window size, congestion control

        public InfoBlock(IPAddress address)
        {
            m_iIPversion = address.AddressFamily;
            ConvertIPAddress.ToUintArray(address, ref m_piIP);
        }

        public override bool Equals(object value)
        {
            // Is null?
            if (Object.ReferenceEquals(null, value))
            {
                return false;
            }

            // Is the same object?
            if (Object.ReferenceEquals(this, value))
            {
                return true;
            }

            // Is the same type?
            if (value.GetType() != this.GetType())
            {
                return false;
            }

            return IsEqual((InfoBlock)value);
        }

        public bool Equals(InfoBlock infoBlock)
        {
            if (Object.ReferenceEquals(null, infoBlock))
            {
                return false;
            }

            // Is the same object?
            if (Object.ReferenceEquals(this, infoBlock))
            {
                return true;
            }

            return IsEqual(infoBlock);
        }

        public static bool operator ==(InfoBlock infoBlockA, InfoBlock infoBlockB)
        {
            if (Object.ReferenceEquals(infoBlockA, infoBlockB))
            {
                return true;
            }

            // Ensure that "numberA" isn't null
            if (Object.ReferenceEquals(null, infoBlockA))
            {
                return false;
            }

            return (infoBlockA.Equals(infoBlockB));
        }

        public static bool operator !=(InfoBlock infoBlockA, InfoBlock infoBlockB)
        {
            return !(infoBlockA == infoBlockB);
        }

        public override int GetHashCode()
        {
            if (m_iIPversion == AddressFamily.InterNetwork)
                return (int)m_piIP[0];

            return (int)(m_piIP[0] + m_piIP[1] + m_piIP[2] + m_piIP[3]);
        }

        bool IsEqual(InfoBlock infoBlock)
        {
            if (m_iIPversion != infoBlock.m_iIPversion)
                return false;

            else if (m_iIPversion == AddressFamily.InterNetwork)
                return (m_piIP[0] == infoBlock.m_piIP[0]);

            for (int i = 0; i < 4; ++i)
            {
                if (m_piIP[i] != infoBlock.m_piIP[i])
                    return false;
            }

            return true;
        }
    }

}