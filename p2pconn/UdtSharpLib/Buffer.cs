using System;
using System.Collections.Generic;

namespace UdtSharp
{
    public class SndBuffer
    {
        object m_BufLock = new object();           // used to synchronize buffer operation

        class Block
        {
            internal byte[] m_pcData;                   // pointer to the data block
            internal int m_iLength;                    // length of the block

            internal uint m_iMsgNo;                 // message number
            internal ulong m_OriginTime;            // original request time
            internal int m_iTTL;                       // time to live (milliseconds)
        }

        List<Block> mBlockList = new List<Block>();
        int m_iLastBlock = 0;
        int m_iCurrentBlock = 0;
        int m_iFirstBlock = 0;

        uint m_iNextMsgNo;                // next message number

        int m_iSize;                // buffer size (number of packets)
        int m_iMSS;                          // maximum seqment/packet size

        int m_iCount;           // number of used blocks

        public SndBuffer(int size, int mss)
        {
            m_iSize = size;
            m_iMSS = mss;

            // circular linked list for out bound packets

            for (int i = 0; i < m_iSize; ++i)
            {
                Block block = new Block();
                block.m_iMsgNo = 0;
                block.m_pcData = new byte[m_iMSS];
                mBlockList.Add(block);
            }

        }

        // Functionality:
        //    Insert a user buffer into the sending list.
        // Parameters:
        //    0) [in] data: pointer to the user data block.
        //    1) [in] len: size of the block.
        //    2) [in] ttl: time to live in milliseconds
        //    3) [in] order: if the block should be delivered in order, for DGRAM only
        // Returned value:
        //    None.
        public void addBuffer(byte[] data, int offset, int len, int ttl = -1, bool order = false)
        {
            int size = len / m_iMSS;
            if ((len % m_iMSS) != 0)
                size++;

            // dynamically increase sender buffer
            while (size + m_iCount >= m_iSize)
                increase();

            ulong time = Timer.getTime();
            uint inorder = Convert.ToUInt32(order);
            inorder <<= 29;

            for (int i = 0; i < size; ++i)
            {
                Block s = mBlockList[m_iLastBlock];
                IncrementBlockIndex(ref m_iLastBlock);
                int pktlen = len - i * m_iMSS;
                if (pktlen > m_iMSS)
                    pktlen = m_iMSS;

                Array.Copy(data, i * m_iMSS + offset, s.m_pcData, 0, pktlen);
                s.m_iLength = pktlen;
                s.m_iMsgNo = m_iNextMsgNo | inorder;
                if (i == 0)
                    s.m_iMsgNo |= 0x80000000;
                if (i == size - 1)
                    s.m_iMsgNo |= 0x40000000;

                s.m_OriginTime = time;
                s.m_iTTL = ttl;
            }

            lock (m_BufLock)
            {
                m_iCount += size;
            }

            m_iNextMsgNo++;
            if (m_iNextMsgNo == MessageNumber.m_iMaxMsgNo)
                m_iNextMsgNo = 1;
        }

        public int readData(ref byte[] data, ref uint msgno)
        {
            // No data to read
            if (m_iCurrentBlock == m_iLastBlock)
                return 0;

            data = mBlockList[m_iCurrentBlock].m_pcData;
            int readlen = mBlockList[m_iCurrentBlock].m_iLength;
            msgno = mBlockList[m_iCurrentBlock].m_iMsgNo;

            IncrementBlockIndex(ref m_iCurrentBlock);

            return readlen;
        }

        public int readData(ref byte[] data, int offset, ref uint msgno, out int msglen)
        {
            msglen = 0;
            lock (m_BufLock)
            {
                int blockIndex = m_iFirstBlock;
                IncrementBlockIndex(ref blockIndex, offset);
                Block p = mBlockList[blockIndex];

                if ((p.m_iTTL >= 0) && ((Timer.getTime() - p.m_OriginTime) / 1000 > (ulong)p.m_iTTL))
                {
                    msgno = p.m_iMsgNo & 0x1FFFFFFF;

                    msglen = 1;

                    IncrementBlockIndex(ref blockIndex);
                    p = mBlockList[blockIndex];

                    bool move = false;
                    while (msgno == (p.m_iMsgNo & 0x1FFFFFFF))
                    {
                        if (blockIndex == m_iCurrentBlock)
                            move = true;

                        IncrementBlockIndex(ref blockIndex);
                        p = mBlockList[blockIndex];

                        if (move)
                            m_iCurrentBlock = blockIndex;
                        msglen++;
                    }

                    return -1;
                }

                data = p.m_pcData;
                int readlen = p.m_iLength;
                msgno = p.m_iMsgNo;

                return readlen;
            }
        }

        void IncrementBlockIndex(ref int blockIndex, int offset = 1)
        {
            blockIndex = (blockIndex + offset) % mBlockList.Count;
        }

        public void ackData(int offset)
        {
            lock (m_BufLock)
            {
                IncrementBlockIndex(ref m_iFirstBlock, offset);

                m_iCount -= offset;

                Timer.triggerEvent();
            }
        }

        public int getCurrBufSize()
        {
           return m_iCount;
        }

        void increase()
        {
            int unitsize = m_iSize;

            for (int i = 0; i < unitsize; ++i)
            {
                Block block = new Block();
                block.m_iMsgNo = 0;
                block.m_pcData = new byte[m_iMSS];
                mBlockList.Add(block);
            }

            m_iSize += unitsize;
        }
    }

    public class RcvBuffer
    {
        Unit[] m_pUnit;                     // pointer to the protocol buffer
        int m_iSize;                         // size of the protocol buffer

        int m_iStartPos;                     // the head position for I/O (inclusive)
        int m_iLastAckPos;                   // the last ACKed position (exclusive)
                                             // EMPTY: m_iStartPos = m_iLastAckPos   FULL: m_iStartPos = m_iLastAckPos + 1
        int m_iMaxPos;          // the furthest data position

        int m_iNotch;           // the starting read point of the first unit

        public RcvBuffer(int bufsize)
        {
            m_iSize = bufsize;
            m_iStartPos = 0;
            m_iLastAckPos = 0;
            m_iMaxPos = 0;
            m_iNotch = 0;
            m_pUnit = new Unit[m_iSize];
            for (int i = 0; i < m_iSize; ++i)
                m_pUnit[i] = null;
        }

        ~RcvBuffer()
        {
            for (int i = 0; i < m_iSize; ++i)
            {
                if (null != m_pUnit[i])
                {
                    m_pUnit[i].m_iFlag = 0;
                }
            }
        }

        public int addData(Unit unit, int offset)
        {
            int pos = (m_iLastAckPos + offset) % m_iSize;
            if (offset > m_iMaxPos)
                m_iMaxPos = offset;

            if (null != m_pUnit[pos])
                return -1;

            m_pUnit[pos] = unit;

            unit.m_iFlag = 1;

            return 0;
        }

        public int readBuffer(byte[] data, int offset, int len)
        {
            int p = m_iStartPos;
            int lastack = m_iLastAckPos;
            int rs = len;

            while ((p != lastack) && (rs > 0))
            {
                int unitsize = m_pUnit[p].m_Packet.getLength() - m_iNotch;
                if (unitsize > rs)
                    unitsize = rs;

                unitsize = m_pUnit[p].m_Packet.GetDataBytes(m_iNotch, data, offset, unitsize);

                offset += unitsize;

                if ((rs > unitsize) || (rs == m_pUnit[p].m_Packet.getLength() - m_iNotch))
                {
                    Unit tmp = m_pUnit[p];
                    m_pUnit[p] = null;
                    tmp.m_iFlag = 0;

                    if (++p == m_iSize)
                        p = 0;

                    m_iNotch = 0;
                }
                else
                    m_iNotch += rs;

                rs -= unitsize;
            }

            m_iStartPos = p;
            return len - rs;
        }

        public void ackData(int len)
        {
            m_iLastAckPos = (m_iLastAckPos + len) % m_iSize;
            m_iMaxPos -= len;
            if (m_iMaxPos < 0)
                m_iMaxPos = 0;

            Timer.triggerEvent();
        }

        public int getAvailBufSize()
        {
            // One slot must be empty in order to tell the difference between "empty buffer" and "full buffer"
            return m_iSize - getRcvDataSize() - 1;
        }

        public int getRcvDataSize()
        {
            if (m_iLastAckPos >= m_iStartPos)
                return m_iLastAckPos - m_iStartPos;

            return m_iSize + m_iLastAckPos - m_iStartPos;
        }

        public void dropMsg(int msgno)
        {
            for (int i = m_iStartPos, n = (m_iLastAckPos + m_iMaxPos) % m_iSize; i != n; i = (i + 1) % m_iSize)
                if ((null != m_pUnit[i]) && (msgno == m_pUnit[i].m_Packet.GetMessageNumber()))
                    m_pUnit[i].m_iFlag = 3;
        }

        public int readMsg(byte[] data, int len)
        {
            int p = 0;
            int q = 0;
            bool passack = false;
            if (!scanMsg(ref p, ref q, ref passack))
                return 0;

            int rs = len;
            int dataOffset = 0;
            while (p != (q + 1) % m_iSize)
            {
                byte[] allData = m_pUnit[p].m_Packet.GetDataBytes();
                int unitsize = allData.Length;
                if ((rs >= 0) && (unitsize > rs))
                    unitsize = rs;

                if (unitsize > 0)
                {
                    Array.Copy(allData, 0, data, dataOffset, unitsize);
                    dataOffset += unitsize;
                    rs -= unitsize;
                }

                if (!passack)
                {
                    Unit tmp = m_pUnit[p];
                    m_pUnit[p] = null;
                    tmp.m_iFlag = 0;
                }
                else
                    m_pUnit[p].m_iFlag = 2;

                if (++p == m_iSize)
                    p = 0;
            }

            if (!passack)
                m_iStartPos = (q + 1) % m_iSize;

            return len - rs;
        }

        int getRcvMsgNum()
        {
            int p = 0;
            int q = 0;
            bool passack = false;
            return scanMsg(ref p, ref q, ref passack) ? 1 : 0;
        }

        bool scanMsg(ref int p, ref int q, ref bool passack)
        {   
            // empty buffer
            if ((m_iStartPos == m_iLastAckPos) && (m_iMaxPos <= 0))
                return false;

            //skip all bad msgs at the beginning
            while (m_iStartPos != m_iLastAckPos)
            {
                if (null == m_pUnit[m_iStartPos])
                {
                    if (++m_iStartPos == m_iSize)
                        m_iStartPos = 0;
                    continue;
                }

                if ((1 == m_pUnit[m_iStartPos].m_iFlag) && (m_pUnit[m_iStartPos].m_Packet.getMsgBoundary() > 1))
                {
                    bool good = true;

                    // look ahead for the whole message
                    for (int i = m_iStartPos; i != m_iLastAckPos;)
                    {
                        if ((null == m_pUnit[i]) || (1 != m_pUnit[i].m_iFlag))
                        {
                            good = false;
                            break;
                        }

                        if ((m_pUnit[i].m_Packet.getMsgBoundary() == 1) || (m_pUnit[i].m_Packet.getMsgBoundary() == 3))
                            break;

                        if (++i == m_iSize)
                            i = 0;
                    }

                    if (good)
                        break;
                }

                Unit tmp = m_pUnit[m_iStartPos];
                m_pUnit[m_iStartPos] = null;
                tmp.m_iFlag = 0;

                if (++m_iStartPos == m_iSize)
                    m_iStartPos = 0;
            }

            p = -1;                  // message head
            q = m_iStartPos;         // message tail
            passack = m_iStartPos == m_iLastAckPos;
            bool found = false;

            // looking for the first message
            for (int i = 0, n = m_iMaxPos + getRcvDataSize(); i <= n; ++i)
            {
                if ((null != m_pUnit[q]) && (1 == m_pUnit[q].m_iFlag))
                {
                    switch (m_pUnit[q].m_Packet.getMsgBoundary())
                    {
                        case 3: // 11
                            p = q;
                            found = true;
                            break;

                        case 2: // 10
                            p = q;
                            break;

                        case 1: // 01
                            if (p != -1)
                                found = true;
                            break;
                    }
                }
                else
                {
                    // a hole in this message, not valid, restart search
                    p = -1;
                }

                if (found)
                {
                    // the msg has to be ack'ed or it is allowed to read out of order, and was not read before
                    if (!passack || !m_pUnit[q].m_Packet.getMsgOrderFlag())
                        break;

                    found = false;
                }

                if (++q == m_iSize)
                    q = 0;

                if (q == m_iLastAckPos)
                    passack = true;
            }

            // no msg found
            if (!found)
            {
                // if the message is larger than the receiver buffer, return part of the message
                if ((p != -1) && ((q + 1) % m_iSize == p))
                    found = true;
            }

            return found;
        }
    }
}
