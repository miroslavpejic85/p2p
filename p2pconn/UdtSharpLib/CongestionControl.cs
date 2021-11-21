using System;
using UDTSOCKET = System.Int32;

namespace UdtSharp
{
    public class CC
    {
        protected const int m_iSYNInterval = UDT.m_iSYNInterval;	// UDT constant parameter, SYN

        public double m_dPktSndPeriod;              // Packet sending period, in microseconds
        public double m_dCWndSize;                  // Congestion window size, in packets

        protected int m_iBandwidth;           // estimated bandwidth, packets per second
        protected double m_dMaxCWndSize;               // maximum cwnd size, in packets

        protected int m_iMSS;             // Maximum Packet Size, including all packet headers
        protected int m_iSndCurrSeqNo;        // current maximum seq no sent out
        protected int m_iRcvRate;         // packet arrive rate at receiver side, packets per second
        protected int m_iRTT;             // current estimated RTT, microsecond

        protected string m_pcParam;            // user defined parameter

        public UDTSOCKET m_UDT;                     // The UDT entity that this congestion control algorithm is bound to

        public int m_iACKPeriod;                    // Periodical timer to send an ACK, in milliseconds
        public int m_iACKInterval;                  // How many packets to send one ACK, in packets

        public bool m_bUserDefinedRTO;              // if the RTO value is defined by users
        public int m_iRTO;                          // RTO value, microseconds

        PerfMon m_PerfInfo = new PerfMon();                 // protocol statistics information

        public CC()
        {
            m_dPktSndPeriod = 1.0;
            m_dCWndSize = 16.0;
            m_pcParam = null;
            m_iACKPeriod = 0;
            m_iACKInterval = 0;
            m_bUserDefinedRTO = false;
            m_iRTO = -1;
        }

        // Functionality:
        //    Callback function to be called (only) at the start of a UDT connection.
        //    note that this is different from CCC(), which is always called.
        // Parameters:
        //    None.
        // Returned value:
        //    None.

        public virtual void init() { }

        // Functionality:
        //    Callback function to be called when a UDT connection is closed.
        // Parameters:
        //    None.
        // Returned value:
        //    None.

        public virtual void close() { }

        // Functionality:
        //    Callback function to be called when an ACK packet is received.
        // Parameters:
        //    0) [in] ackno: the data sequence number acknowledged by this ACK.
        // Returned value:
        //    None.

        public virtual void onACK(int seqno) { }

        // Functionality:
        //    Callback function to be called when a loss report is received.
        // Parameters:
        //    0) [in] losslist: list of sequence number of packets, in the format describled in packet.cpp.
        //    1) [in] size: length of the loss list.
        // Returned value:
        //    None.

        public virtual void onLoss(int[] loss, int length) { }

        // Functionality:
        //    Callback function to be called when a timeout event occurs.
        // Parameters:
        //    None.
        // Returned value:
        //    None.

        public virtual void onTimeout() { }

        // Functionality:
        //    Callback function to be called when a data is sent.
        // Parameters:
        //    0) [in] seqno: the data sequence number.
        //    1) [in] size: the payload size.
        // Returned value:
        //    None.

        public virtual void onPktSent(Packet packet) { }

        // Functionality:
        //    Callback function to be called when a data is received.
        // Parameters:
        //    0) [in] seqno: the data sequence number.
        //    1) [in] size: the payload size.
        // Returned value:
        //    None.

        public virtual void onPktReceived(Packet packet) { }

        // Functionality:
        //    Callback function to Process a user defined packet.
        // Parameters:
        //    0) [in] pkt: the user defined packet.
        // Returned value:
        //    None.

        public virtual void processCustomMsg(Packet packet) { }


        protected void setACKTimer(int msINT)
        {
            m_iACKPeriod = msINT > m_iSYNInterval ? m_iSYNInterval : msINT;
        }

        protected void setACKInterval(int pktINT)
        {
            m_iACKInterval = pktINT;
        }

        protected void setRTO(int usRTO)
        {
            m_bUserDefinedRTO = true;
            m_iRTO = usRTO;
        }

        protected void sendCustomMsg(Packet pkt)
        {
            UDT u = UDT.s_UDTUnited.lookup(m_UDT);

            if (null != u)
            {
                pkt.SetId(u.m_PeerID);
                u.m_pSndQueue.sendto(u.m_pPeerAddr, pkt);
            }
        }

        protected PerfMon getPerfInfo()
        {
            try
            {
                UDT u = UDT.s_UDTUnited.lookup(m_UDT);
                if (null != u)
                    u.sample(m_PerfInfo, false);
            }
            catch (Exception e)
            {
                return null;
            }

            return m_PerfInfo;
        }

        public void setMSS(int mss)
        {
            m_iMSS = mss;
        }

        public void setBandwidth(int bw)
        {
            m_iBandwidth = bw;
        }

        public void setSndCurrSeqNo(int seqno)
        {
            m_iSndCurrSeqNo = seqno;
        }

        public void setRcvRate(int rcvrate)
        {
            m_iRcvRate = rcvrate;
        }

        public void setMaxCWndSize(int cwnd)
        {
            m_dMaxCWndSize = cwnd;
        }

        public void setRTT(int rtt)
        {
            m_iRTT = rtt;
        }

        protected void setUserParam(string param)
        {
            m_pcParam = param;
        }
    }

    public class UDTCC : CC
    {
        int m_iRCInterval;          // UDT Rate control interval
        ulong m_LastRCTime;      // last rate increase time
        bool m_bSlowStart;          // if in slow start phase
        int m_iLastAck;         // last ACKed seq no
        bool m_bLoss;           // if loss happened since last rate increase
        int m_iLastDecSeq;      // max pkt seq no sent out when last decrease happened
        double m_dLastDecPeriod;        // value of pktsndperiod when last decrease happened
        int m_iNAKCount;                     // NAK counter
        int m_iDecRandom;                    // random threshold on decrease by number of loss events
        int m_iAvgNAKNum;                    // average number of NAKs per congestion
        int m_iDecCount;            // number of decreases in a congestion epoch

        static Random m_random = new Random();


        public override void init()
        {
            m_iRCInterval = m_iSYNInterval;
            m_LastRCTime = Timer.getTime();
            setACKTimer(m_iRCInterval);

            m_bSlowStart = true;
            m_iLastAck = m_iSndCurrSeqNo;
            m_bLoss = false;
            m_iLastDecSeq = SequenceNumber.decseq(m_iLastAck);
            m_dLastDecPeriod = 1;
            m_iAvgNAKNum = 0;
            m_iNAKCount = 0;
            m_iDecRandom = 1;

            m_dCWndSize = 16;
            m_dPktSndPeriod = 1;
        }

        public override void onACK(int ack)
        {
            long B = 0;
            double inc = 0;
            // Note: 1/24/2012
            // The minimum increase parameter is increased from "1.0 / m_iMSS" to 0.01
            // because the original was too small and caused sending rate to stay at low level
            // for long time.
            const double min_inc = 0.01;

            ulong currtime = Timer.getTime();
            if (currtime - m_LastRCTime < (ulong)m_iRCInterval)
                return;

            m_LastRCTime = currtime;

            if (m_bSlowStart)
            {
                m_dCWndSize += SequenceNumber.seqlen(m_iLastAck, ack);
                m_iLastAck = ack;

                if (m_dCWndSize > m_dMaxCWndSize)
                {
                    m_bSlowStart = false;
                    if (m_iRcvRate > 0)
                        m_dPktSndPeriod = 1000000.0 / m_iRcvRate;
                    else
                        m_dPktSndPeriod = (m_iRTT + m_iRCInterval) / m_dCWndSize;
                }
            }
            else
                m_dCWndSize = m_iRcvRate / 1000000.0 * (m_iRTT + m_iRCInterval) + 16;

            // During Slow Start, no rate increase
            if (m_bSlowStart)
                return;

            if (m_bLoss)
            {
                m_bLoss = false;
                return;
            }

            B = (long)(m_iBandwidth - 1000000.0 / m_dPktSndPeriod);
            if ((m_dPktSndPeriod > m_dLastDecPeriod) && ((m_iBandwidth / 9) < B))
                B = m_iBandwidth / 9;
            if (B <= 0)
                inc = min_inc;
            else
            {
                // inc = max(10 ^ ceil(log10( B * MSS * 8 ) * Beta / MSS, 1/MSS)
                // Beta = 1.5 * 10^(-6)

                inc = Math.Pow(10.0, Math.Ceiling(Math.Log10(B * m_iMSS * 8.0))) * 0.0000015 / m_iMSS;

                if (inc < min_inc)
                    inc = min_inc;
            }

            m_dPktSndPeriod = (m_dPktSndPeriod * m_iRCInterval) / (m_dPktSndPeriod * inc + m_iRCInterval);
        }

        public override void onLoss(int[] losslist, int length)
        {
            //Slow Start stopped, if it hasn't yet
            if (m_bSlowStart)
            {
                m_bSlowStart = false;
                if (m_iRcvRate > 0)
                {
                    // Set the sending rate to the receiving rate.
                    m_dPktSndPeriod = 1000000.0 / m_iRcvRate;
                    return;
                }
                // If no receiving rate is observed, we have to compute the sending
                // rate according to the current window size, and decrease it
                // using the method below.
                m_dPktSndPeriod = m_dCWndSize / (m_iRTT + m_iRCInterval);
            }

            m_bLoss = true;

            if (SequenceNumber.seqcmp(losslist[0] & 0x7FFFFFFF, m_iLastDecSeq) > 0)
            {
                m_dLastDecPeriod = m_dPktSndPeriod;
                m_dPktSndPeriod = Math.Ceiling(m_dPktSndPeriod * 1.125);

                m_iAvgNAKNum = (int)Math.Ceiling(m_iAvgNAKNum * 0.875 + m_iNAKCount * 0.125);
                m_iNAKCount = 1;
                m_iDecCount = 1;

                m_iLastDecSeq = m_iSndCurrSeqNo;

                // remove global synchronization using randomization
                m_iDecRandom = (int)Math.Ceiling(m_iAvgNAKNum * m_random.NextDouble());
                if (m_iDecRandom < 1)
                    m_iDecRandom = 1;
            }
            else if ((m_iDecCount++ < 5) && (0 == (++m_iNAKCount % m_iDecRandom)))
            {
                // 0.875^5 = 0.51, rate should not be decreased by more than half within a congestion period
                m_dPktSndPeriod = Math.Ceiling(m_dPktSndPeriod * 1.125);
                m_iLastDecSeq = m_iSndCurrSeqNo;
            }
        }

        public override void onTimeout()
        {
            if (m_bSlowStart)
            {
                m_bSlowStart = false;
                if (m_iRcvRate > 0)
                    m_dPktSndPeriod = 1000000.0 / m_iRcvRate;
                else
                    m_dPktSndPeriod = m_dCWndSize / (m_iRTT + m_iRCInterval);
            }
            else
            {
                /*
                m_dLastDecPeriod = m_dPktSndPeriod;
                m_dPktSndPeriod = ceil(m_dPktSndPeriod * 2);
                m_iLastDecSeq = m_iLastAck;
                */
            }
        }
    }
}
