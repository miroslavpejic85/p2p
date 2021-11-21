using System;

namespace UdtSharp
{
    public class ACKWindow
    {
        public ACKWindow(int size = 1024)
        {
            m_iSize = size;
            m_piACKSeqNo = new int[m_iSize];
            m_piACK = new int[m_iSize];
            m_pTimeStamp = new ulong[m_iSize];

            m_piACKSeqNo[0] = -1;
        }

        // Functionality:
        //    Write an ACK record into the window.
        // Parameters:
        //    0) [in] seq: ACK seq. no.
        //    1) [in] ack: DATA ACK no.
        // Returned value:
        //    None.

        public void store(int seq, int ack)
        {
            m_piACKSeqNo[m_iHead] = seq;
            m_piACK[m_iHead] = ack;
            m_pTimeStamp[m_iHead] = Timer.getTime();

            m_iHead = (m_iHead + 1) % m_iSize;

            // overwrite the oldest ACK since it is not likely to be acknowledged
            if (m_iHead == m_iTail)
                m_iTail = (m_iTail + 1) % m_iSize;
        }

        // Functionality:
        //    Search the ACK-2 "seq" in the window, find out the DATA "ack" and caluclate RTT .
        // Parameters:
        //    0) [in] seq: ACK-2 seq. no.
        //    1) [out] ack: the DATA ACK no. that matches the ACK-2 no.
        // Returned value:
        //    RTT.

        public int acknowledge(int seq, ref int ack)
        {
            if (m_iHead >= m_iTail)
            {
                // Head has not exceeded the physical boundary of the window

                for (int i = m_iTail, n = m_iHead; i < n; ++i)
                {
                    // looking for indentical ACK Seq. No.
                    if (seq == m_piACKSeqNo[i])
                    {
                        // return the Data ACK it carried
                        ack = m_piACK[i];

                        // calculate RTT
                        int rtt = (int)(Timer.getTime() - m_pTimeStamp[i]);

                        if (i + 1 == m_iHead)
                        {
                            m_iTail = m_iHead = 0;
                            m_piACKSeqNo[0] = -1;
                        }
                        else
                            m_iTail = (i + 1) % m_iSize;

                        return rtt;
                    }
                }

                // Bad input, the ACK node has been overwritten
                return -1;
            }

            // Head has exceeded the physical window boundary, so it is behind tail
            for (int j = m_iTail, n = m_iHead + m_iSize; j < n; ++j)
            {
                // looking for indentical ACK seq. no.
                if (seq == m_piACKSeqNo[j % m_iSize])
                {
                    // return Data ACK
                    j %= m_iSize;
                    ack = m_piACK[j];

                    // calculate RTT
                    int rtt = (int)(Timer.getTime() - m_pTimeStamp[j]);

                    if (j == m_iHead)
                    {
                        m_iTail = m_iHead = 0;
                        m_piACKSeqNo[0] = -1;
                    }
                    else
                        m_iTail = (j + 1) % m_iSize;

                    return rtt;
                }
            }

            // bad input, the ACK node has been overwritten
            return -1;
        }

        int[] m_piACKSeqNo;       // Seq. No. for the ACK packet
        int[] m_piACK;            // Data Seq. No. carried by the ACK packet
        ulong[] m_pTimeStamp;      // The timestamp when the ACK was sent

        int m_iSize;                 // Size of the ACK history window
        int m_iHead;                 // Pointer to the lastest ACK record
        int m_iTail;                 // Pointer to the oldest ACK record
    }

    ////////////////////////////////////////////////////////////////////////////////

    public class PktTimeWindow
    {
        public PktTimeWindow(int asize = 16, int psize = 16)
        {
            m_iAWSize = asize;
            m_iPWSize = psize;
            m_iMinPktSndInt = 1000000;
            m_piPktWindow = new int[m_iAWSize];
            m_piPktReplica = new int[m_iAWSize];
            m_piProbeWindow = new int[m_iPWSize];
            m_piProbeReplica = new int[m_iPWSize];

            m_LastArrTime = Timer.getTime();

            for (int i = 0; i < m_iAWSize; ++i)
                m_piPktWindow[i] = 1000000;

            for (int k = 0; k < m_iPWSize; ++k)
                m_piProbeWindow[k] = 1000;
        }

        // Functionality:
        //    read the minimum packet sending interval.
        // Parameters:
        //    None.
        // Returned value:
        //    minimum packet sending interval (microseconds).

        public int getMinPktSndInt()
        {
            return m_iMinPktSndInt;
        }

        // Functionality:
        //    Calculate the packes arrival speed.
        // Parameters:
        //    None.
        // Returned value:
        //    Packet arrival speed (packets per second).

        public int getPktRcvSpeed()
        {
            // get median value, but cannot change the original value order in the window
            Array.Copy(m_piPktWindow, m_piPktReplica, m_iAWSize - 1); // why -1 ???
            Array.Sort(m_piPktReplica); // need -1 here ???
            int median = m_piPktReplica[m_iAWSize / 2];

            int count = 0;
            int sumMicrosecond = 0;
            int upper = median << 3;
            int lower = median >> 3;

            // median filtering
            for (int i = 0, n = m_iAWSize; i < n; ++i)
            {
                if ((m_piPktWindow[i] < upper) && (m_piPktWindow[i] > lower))
                {
                    ++count;
                    sumMicrosecond += m_piPktWindow[i];
                }
            }
            double packetsPerMicrosecond = (double)count / sumMicrosecond;

            // claculate speed, or return 0 if not enough valid value
            if (count > (m_iAWSize >> 1))
                return (int)Math.Ceiling(1000000 * packetsPerMicrosecond);
            else
                return 0;
        }

        // Functionality:
        //    Estimate the bandwidth.
        // Parameters:
        //    None.
        // Returned value:
        //    Estimated bandwidth (packets per second).

        public int getBandwidth()
        {
            // get median value, but cannot change the original value order in the window
            Array.Copy(m_piProbeWindow, m_piProbeReplica, m_iPWSize - 1); // why -1 ???
            Array.Sort(m_piProbeReplica); // need -1 here ???
            int median = m_piProbeReplica[m_iPWSize / 2];

            int count = 1;
            int sum = median;
            int upper = median << 3;
            int lower = median >> 3;

            // median filtering
            for (int i = 0, n = m_iPWSize; i < n; ++i)
            {
                if ((m_piProbeWindow[i] < upper) && (m_piProbeWindow[i] > lower))
                {
                    ++count;
                    sum += m_piProbeWindow[i];
                }
            }

            return (int)Math.Ceiling(1000000.0 / ((double)sum / (double)count));
        }

        // Functionality:
        //    Record time information of a packet sending.
        // Parameters:
        //    0) currtime: timestamp of the packet sending.
        // Returned value:
        //    None.

        public void onPktSent(int currtime)
        {
            int interval = currtime - m_iLastSentTime;

            if ((interval < m_iMinPktSndInt) && (interval > 0))
                m_iMinPktSndInt = interval;

            m_iLastSentTime = currtime;
        }

        // Functionality:
        //    Record time information of an arrived packet.
        // Parameters:
        //    None.
        // Returned value:
        //    None.

        public void onPktArrival()
        {
            m_CurrArrTime = Timer.getTime();

            // record the packet interval between the current and the last one
            m_piPktWindow[m_iPktWindowPtr] = (int)(m_CurrArrTime - m_LastArrTime);

            // the window is logically circular
            ++m_iPktWindowPtr;
            if (m_iPktWindowPtr == m_iAWSize)
                m_iPktWindowPtr = 0;

            // remember last packet arrival time
            m_LastArrTime = m_CurrArrTime;
        }

        // Functionality:
        //    Record the arrival time of the first probing packet.
        // Parameters:
        //    None.
        // Returned value:
        //    None.

        public void probe1Arrival()
        {
            m_ProbeTime = Timer.getTime();
        }

        // Functionality:
        //    Record the arrival time of the second probing packet and the interval between packet pairs.
        // Parameters:
        //    None.
        // Returned value:
        //    None.

        public void probe2Arrival()
        {
            m_CurrArrTime = Timer.getTime();

            // record the probing packets interval
            m_piProbeWindow[m_iProbeWindowPtr] = (int)(m_CurrArrTime - m_ProbeTime);
            // the window is logically circular
            ++m_iProbeWindowPtr;
            if (m_iProbeWindowPtr == m_iPWSize)
                m_iProbeWindowPtr = 0;
        }

        int m_iAWSize;               // size of the packet arrival history window
        int[] m_piPktWindow;          // packet information window
        int[] m_piPktReplica;
        int m_iPktWindowPtr;         // position pointer of the packet info. window.

        int m_iPWSize;               // size of probe history window size
        int[] m_piProbeWindow;        // record inter-packet time for probing packet pairs
        int[] m_piProbeReplica;
        int m_iProbeWindowPtr;       // position pointer to the probing window

        int m_iLastSentTime;         // last packet sending time
        int m_iMinPktSndInt;         // Minimum packet sending interval

        ulong m_LastArrTime;      // last packet arrival time
        ulong m_CurrArrTime;      // current packet arrival time
        ulong m_ProbeTime;        // arrival time of the first probing packet
    }
}
