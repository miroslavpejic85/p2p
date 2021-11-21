// UDT ACK Sub-sequence Number: 0 - (2^31 - 1)

namespace UdtSharp
{
    static class AckNumber
    {
        public static int incack(int ackno)
        {
            return (ackno == m_iMaxAckSeqNo) ? 0 : ackno + 1;
        }

        public static int m_iMaxAckSeqNo = 0x7FFFFFFF;         // maximum ACK sub-sequence number used in UDT
    }
}