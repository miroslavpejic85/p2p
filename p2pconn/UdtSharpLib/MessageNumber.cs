// UDT Message Number: 0 - (2^29 - 1)

using System;

namespace UdtSharp
{
    static class MessageNumber
    {
        public static int msgcmp(int msgno1, int msgno2)
        {
            return (Math.Abs(msgno1 - msgno2) < m_iMsgNoTH) ? (msgno1 - msgno2) : (msgno2 - msgno1);
        }

        public static int msglen(int msgno1, int msgno2)
        {
            return (msgno1 <= msgno2) ? (msgno2 - msgno1 + 1) : (msgno2 - msgno1 + m_iMaxMsgNo + 2);
        }

        public static int msgoff(int msgno1, int msgno2)
        {
            if (Math.Abs(msgno1 - msgno2) < m_iMsgNoTH)
                return msgno2 - msgno1;

            if (msgno1 < msgno2)
                return msgno2 - msgno1 - m_iMaxMsgNo - 1;

            return msgno2 - msgno1 + m_iMaxMsgNo + 1;
        }

        public static int incmsg(int msgno)
        {
            return (msgno == m_iMaxMsgNo) ? 0 : msgno + 1;
        }

        static int m_iMsgNoTH = 0xFFFFFFF;             // threshold for comparing msg. no.
        public static int m_iMaxMsgNo = 0x1FFFFFFF;           // maximum message number used in UDT
    }
}