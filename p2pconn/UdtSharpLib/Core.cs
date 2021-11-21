using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using UDTSOCKET = System.Int32;

namespace UdtSharp
{
    public enum UDTOpt
    {
        UDT_MSS,             // the Maximum Transfer Unit
        UDT_SNDSYN,          // if sending is blocking
        UDT_RCVSYN,          // if receiving is blocking
        UDT_CC,              // custom congestion control algorithm
        UDT_FC,              // Flight flag size (window size)
        UDT_SNDBUF,          // maximum buffer in sending queue
        UDT_RCVBUF,          // UDT receiving buffer size
        UDT_LINGER,          // waiting for unsent data when closing
        UDP_SNDBUF,          // UDP sending buffer size
        UDP_RCVBUF,          // UDP receiving buffer size
        UDT_MAXMSG,          // maximum datagram message size
        UDT_MSGTTL,          // time-to-live of a datagram message
        UDT_RENDEZVOUS,      // rendezvous connection mode
        UDT_SNDTIMEO,        // send() timeout
        UDT_RCVTIMEO,        // recv() timeout
        UDT_REUSEADDR,       // reuse an existing port or create a new one
        UDT_MAXBW,           // maximum bandwidth (bytes per second) that the connection can use
        UDT_STATE,           // current socket state, see UDTSTATUS, read only
        UDT_EVENT,           // current avalable events associated with the socket
        UDT_SNDDATA,         // size of data in the sending buffer
        UDT_RCVDATA          // size of data available for recv
    };

    public enum UDTSTATUS
    {
        INIT = 1,
        OPENED,
        LISTENING,
        CONNECTING,
        CONNECTED,
        BROKEN,
        CLOSING,
        CLOSED,
        NONEXIST
    };

    enum EPOLLOpt
    {
        // this values are defined same as linux epoll.h
        // so that if system values are used by mistake, they should have the same effect
        UDT_EPOLL_IN = 0x1,
        UDT_EPOLL_OUT = 0x4,
        UDT_EPOLL_ERR = 0x8
    };

    public class PerfMon
    {
        // global measurements
        internal long msTimeStamp;                    // time since the UDT entity is started, in milliseconds
        internal long pktSentTotal;                   // total number of sent data packets, including retransmissions
        internal long pktRecvTotal;                   // total number of received packets
        internal int pktSndLossTotal;                 // total number of lost packets (sender side)
        internal int pktRcvLossTotal;                 // total number of lost packets (receiver side)
        internal int pktRetransTotal;                 // total number of retransmitted packets
        internal int pktSentACKTotal;                 // total number of sent ACK packets
        internal int pktRecvACKTotal;                 // total number of received ACK packets
        internal int pktSentNAKTotal;                 // total number of sent NAK packets
        internal int pktRecvNAKTotal;                 // total number of received NAK packets
        internal long usSndDurationTotal;             // total time duration when UDT is sending data (idle time exclusive)

        // local measurements
        internal long pktSent;                        // number of sent data packets, including retransmissions
        internal long pktRecv;                        // number of received packets
        internal int pktSndLoss;                      // number of lost packets (sender side)
        internal int pktRcvLoss;                      // number of lost packets (receiver side)
        internal int pktRetrans;                      // number of retransmitted packets
        internal int pktSentACK;                      // number of sent ACK packets
        internal int pktRecvACK;                      // number of received ACK packets
        internal int pktSentNAK;                      // number of sent NAK packets
        internal int pktRecvNAK;                      // number of received NAK packets
        internal double mbpsSendRate;                 // sending rate in Mb/s
        internal double mbpsRecvRate;                 // receiving rate in Mb/s
        internal long usSndDuration;                  // busy sending time (i.e., idle time exclusive)

        // instant measurements
        internal double usPktSndPeriod;               // packet sending period, in microseconds
        internal int pktFlowWindow;                   // flow window size, in number of packets
        internal int pktCongestionWindow;             // congestion window size, in number of packets
        internal int pktFlightSize;                   // number of packets on flight
        internal double msRTT;                        // RTT, in milliseconds
        internal double mbpsBandwidth;                // estimated bandwidth, in Mb/s
        internal int byteAvailSndBuf;                 // available UDT sender buffer size
        internal int byteAvailRcvBuf;                 // available UDT receiver buffer size
    };

    public class Unit
    {
        public Packet m_Packet = new Packet();       // packet
        public int m_iFlag;            // 0: free, 1: occupied, 2: msg read but not freed (out-of-order), 3: msg dropped
    };

    public class UDT
    {
        public const UDTSOCKET INVALID_SOCK = -1;
        public const int ERROR = -1;
        const int m_iVersion = 4;

        public static UdtUnited s_UDTUnited = new UdtUnited();               // UDT global management base

        // Identification
        public UDTSOCKET m_SocketID;                        // UDT socket number
        public SocketType m_iSockType;                     // Type of the UDT connection (SOCK_STREAM or SOCK_DGRAM)
        public UDTSOCKET m_PeerID;             // peer id, for multiplexer

        // Packet sizes
        int m_iPktSize;                              // Maximum/regular packet size, in bytes
        public int m_iPayloadSize;                          // Maximum/regular payload size, in bytes

        // Options
        public int m_iMSS;                                  // Maximum Segment Size, in bytes
        bool m_bSynSending;                          // Sending syncronization mode
        public bool m_bSynRecving;                          // Receiving syncronization mode
        public int m_iFlightFlagSize;                       // Maximum number of packets in flight from the peer side
        int m_iSndBufSize;                           // Maximum UDT sender buffer size
        int m_iRcvBufSize;                           // Maximum UDT receiver buffer size
        LingerOption m_Linger;                             // Linger information on close
        public int m_iUDPSndBufSize;                        // UDP sending buffer size
        public int m_iUDPRcvBufSize;                        // UDP receiving buffer size
        public AddressFamily m_iIPversion;                            // IP version
        public bool m_bRendezvous;                          // Rendezvous connection mode
        int m_iSndTimeOut;                           // sending timeout in milliseconds
        int m_iRcvTimeOut;                           // receiving timeout in milliseconds
        public bool m_bReuseAddr;              // reuse an exiting port or not, for UDP multiplexer
        long m_llMaxBW;              // maximum data transfer rate (threshold)

        // congestion control
        CCVirtualFactory m_pCCFactory;             // Factory class to create a specific CC instance
        CC m_pCC;                                  // congestion control class
        public Dictionary<IPAddress,InfoBlock> m_pCache = new Dictionary<IPAddress, InfoBlock>();       // network information cache

        // Status
        volatile bool m_bListening;                  // If the UDT entit is listening to connection
        volatile bool m_bConnecting;            // The short phase when connect() is called but not yet completed
        public volatile bool m_bConnected;                  // Whether the connection is on or off
        public volatile bool m_bClosing;                    // If the UDT entity is closing
        volatile bool m_bShutdown;                   // If the peer side has shutdown the connection
        public volatile bool m_bBroken;                     // If the connection has been broken
        volatile bool m_bPeerHealth;                 // If the peer status is normal
        bool m_bOpened;                              // If the UDT entity has been opened
        public int m_iBrokenCounter;           // a counter (number of GC checks) to let the GC tag this socket as disconnected

        int m_iEXPCount;                             // Expiration counter
        int m_iBandwidth;                            // Estimated bandwidth, number of packets per second
        int m_iRTT;                                  // RTT, in microseconds
        int m_iRTTVar;                               // RTT variance
        int m_iDeliveryRate;                // Packet arrival rate at the receiver side

        public ulong m_ullLingerExpiration;     // Linger expiration time (for GC to close a socket with data in sending buffer)

        public Handshake m_ConnReq = new Handshake();           // connection request
        public Handshake m_ConnRes = new Handshake();           // connection response
        public long m_llLastReqTime;            // last time when a connection request is sent

        // Sending related data
        public SndBuffer m_pSndBuffer;                    // Sender buffer
        SndLossList m_pSndLossList;                // Sender loss list
        PktTimeWindow m_pSndTimeWindow;            // Packet sending time window

        /*volatile*/
        ulong m_ullInterval;             // Inter-packet time, in CPU clock cycles
        ulong m_ullTimeDiff;                      // aggregate difference in inter-packet time

        volatile int m_iFlowWindowSize;              // Flow control window size
        /*volatile*/
        double m_dCongestionWindow;         // congestion window size

        volatile int m_iSndLastAck;              // Last ACK received
        volatile int m_iSndLastDataAck;          // The real last ACK that updates the sender buffer and loss list
        volatile int m_iSndCurrSeqNo;            // The largest sequence number that has been sent
        int m_iLastDecSeq;                       // Sequence number sent last decrease occurs
        int m_iSndLastAck2;                      // Last ACK2 sent back
        ulong m_ullSndLastAck2Time;               // The time when last ACK2 was sent back

        public int m_iISN;                              // Initial Sequence Number

        // Receiving related data
        public RcvBuffer m_pRcvBuffer;                    // Receiver buffer
        RcvLossList m_pRcvLossList;                // Receiver loss list
        ACKWindow m_pACKWindow;                    // ACK history window
        PktTimeWindow m_pRcvTimeWindow;            // Packet arrival time window

        int m_iRcvLastAck;                       // Last sent ACK
        ulong m_ullLastAckTime;                   // Timestamp of last ACK
        int m_iRcvLastAckAck;                    // Last sent ACK that has been acknowledged
        int m_iAckSeqNo;                         // Last ACK sequence number
        int m_iRcvCurrSeqNo;                     // Largest received sequence number

        ulong m_ullLastWarningTime;               // Last time that a warning message is sent

        int m_iPeerISN;                          // Initial Sequence Number of the peer side

        // synchronization: mutexes and conditions
        readonly object m_ConnectionLock = new object();            // used to synchronize connection operation

        readonly EventWaitHandle m_SendBlockCond = new EventWaitHandle(false, EventResetMode.AutoReset);              // used to block "send" call
        readonly object m_SendBlockLock = new object();             // lock associated to m_SendBlockCond

        readonly object m_AckLock = new object();                   // used to protected sender's loss list when processing ACK

        readonly EventWaitHandle m_RecvDataCond = new EventWaitHandle(false, EventResetMode.AutoReset);               // used to block "recv" when there is no data
        readonly object m_RecvDataLock = new object();              // lock associated to m_RecvDataCond

        readonly object m_SendLock = new object();                  // used to synchronize "send" call
        readonly object m_RecvLock = new object();                  // used to synchronize "recv" call

        // Trace
        ulong m_StartTime;                        // timestamp when the UDT entity is started
        long m_llSentTotal;                       // total number of sent data packets, including retransmissions
        long m_llRecvTotal;                       // total number of received packets
        int m_iSndLossTotal;                         // total number of lost packets (sender side)
        int m_iRcvLossTotal;                         // total number of lost packets (receiver side)
        int m_iRetransTotal;                         // total number of retransmitted packets
        int m_iSentACKTotal;                         // total number of sent ACK packets
        int m_iRecvACKTotal;                         // total number of received ACK packets
        int m_iSentNAKTotal;                         // total number of sent NAK packets
        int m_iRecvNAKTotal;                         // total number of received NAK packets
        long m_llSndDurationTotal;       // total real time for sending

        ulong m_LastSampleTime;                   // last performance sample time
        long m_llTraceSent;                       // number of pakctes sent in the last trace interval
        long m_llTraceRecv;                       // number of pakctes received in the last trace interval
        int m_iTraceSndLoss;                         // number of lost packets in the last trace interval (sender side)
        int m_iTraceRcvLoss;                         // number of lost packets in the last trace interval (receiver side)
        int m_iTraceRetrans;                         // number of retransmitted packets in the last trace interval
        int m_iSentACK;                              // number of ACKs sent in the last trace interval
        int m_iRecvACK;                              // number of ACKs received in the last trace interval
        int m_iSentNAK;                              // number of NAKs sent in the last trace interval
        int m_iRecvNAK;                              // number of NAKs received in the last trace interval
        long m_llSndDuration;            // real time for sending
        long m_llSndDurationCounter;     // timers to record the sending duration

        // Timers
        ulong m_ullCPUFrequency;                  // CPU clock frequency, used for Timer, ticks per microsecond

        public const int m_iSYNInterval = 10000;             // Periodical Rate Control Interval, 10000 microsecond
        const int m_iSelfClockInterval = 64;       // ACK interval for self-clocking

        ulong m_ullNextACKTime;          // Next ACK time, in CPU clock cycles, same below
        ulong m_ullNextNAKTime;          // Next NAK time

        /*volatile*/
        ulong m_ullSYNInt;      // SYN interval
        /*volatile*/
        ulong m_ullACKInt;      // ACK interval
        /*volatile*/
        ulong m_ullNAKInt;      // NAK interval
        /*volatile*/
        ulong m_ullLastRspTime;     // time stamp of last response from the peer

        ulong m_ullMinNakInt;            // NAK timeout lower bound; too small value can cause unnecessary retransmission
        ulong m_ullMinExpInt;            // timeout lower bound threshold: too small timeout can cause problem

        int m_iPktCount;                // packet counter for ACK
        int m_iLightACKCount;           // light ACK counter

        ulong m_ullTargetTime;           // scheduled time of next packet sending

        // for UDP multiplexer
        public SndQueue m_pSndQueue;          // packet sending queue
        public RcvQueue m_pRcvQueue;         // packet receiving queue
        public IPEndPoint m_pPeerAddr;          // peer address
        public uint[] m_piSelfIP = new uint[4];         // local UDP IP address
        public SNode m_pSNode;               // node information for UDT list used in snd queue
        public RNode m_pRNode;               // node information for UDT list used in rcv queue

        public UDT()
        {
            // Default UDT configurations
            m_iMSS = 1500;
            m_bSynSending = true;
            m_bSynRecving = true;
            m_iFlightFlagSize = 204800;
            m_iSndBufSize = 65536;
            m_iRcvBufSize = 65536; //Rcv buffer MUST NOT be bigger than Flight Flag size
            m_Linger = new LingerOption(true, 180);
            m_iUDPSndBufSize = 524288;
            m_iUDPRcvBufSize = m_iRcvBufSize * m_iMSS;
            m_iSockType = SocketType.Stream;
            m_iIPversion = AddressFamily.InterNetwork;
            m_bRendezvous = true;
            m_iSndTimeOut = -1;
            m_iRcvTimeOut = -1;
            m_bReuseAddr = true;
            m_llMaxBW = -1;

            m_pCCFactory = new CCFactory<UDTCC>();

            // Initial status
            m_bOpened = false;
            m_bListening = false;
            m_bConnecting = false;
            m_bConnected = false;
            m_bClosing = false;
            m_bShutdown = false;
            m_bBroken = false;
            m_bPeerHealth = true;
            m_ullLingerExpiration = 0;
        }

        public UDT(UDT ancestor)
        {
            // Default UDT configurations
            m_iMSS = ancestor.m_iMSS;
            m_bSynSending = ancestor.m_bSynSending;
            m_bSynRecving = ancestor.m_bSynRecving;
            m_iFlightFlagSize = ancestor.m_iFlightFlagSize;
            m_iSndBufSize = ancestor.m_iSndBufSize;
            m_iRcvBufSize = ancestor.m_iRcvBufSize;
            m_Linger = ancestor.m_Linger;
            m_iUDPSndBufSize = ancestor.m_iUDPSndBufSize;
            m_iUDPRcvBufSize = ancestor.m_iUDPRcvBufSize;
            m_iSockType = ancestor.m_iSockType;
            m_iIPversion = ancestor.m_iIPversion;
            m_bRendezvous = ancestor.m_bRendezvous;
            m_iSndTimeOut = ancestor.m_iSndTimeOut;
            m_iRcvTimeOut = ancestor.m_iRcvTimeOut;
            m_bReuseAddr = true;    // this must be true, because all accepted sockets shared the same port with the listener
            m_llMaxBW = ancestor.m_llMaxBW;

            m_pCCFactory = ancestor.m_pCCFactory.clone();
            m_pCC = null;
            m_pCache = ancestor.m_pCache;

            // Initial status
            m_bOpened = false;
            m_bListening = false;
            m_bConnecting = false;
            m_bConnected = false;
            m_bClosing = false;
            m_bShutdown = false;
            m_bBroken = false;
            m_bPeerHealth = true;
            m_ullLingerExpiration = 0;
        }

        ~UDT()
        {
            // release mutex/condtion variables
            destroySynch();

        }

        public unsafe void setOpt(UDTOpt optName, int optval, UDTSOCKET socket)
        {
            setOpt(optName, &optval, socket);
        }

        public unsafe void setOpt(UDTOpt optName, void* optval, UDTSOCKET socket)
        {
            if (m_bBroken || m_bClosing)
                throw new UdtException(2, 1, 0);

            lock (m_ConnectionLock) lock(m_SendLock) lock(m_RecvLock)
            {
                setOpt_unsafe(optName, optval, socket);
            }
        }

        unsafe void setOpt_unsafe(UDTOpt optName, void* optval, UDTSOCKET socket)
        {
            switch (optName)
            {
                case UDTOpt.UDT_MSS:
                    if (m_bOpened)
                        throw new UdtException(5, 1, 0);

                    if (*(int*)optval < (int)(28 + Handshake.m_iContentSize))
                        throw new UdtException(5, 3, 0);

                    m_iMSS = *(int*)optval;

                    // Packet size cannot be greater than UDP buffer size
                    if (m_iMSS > m_iUDPSndBufSize)
                        m_iMSS = m_iUDPSndBufSize;
                    if (m_iMSS > m_iUDPRcvBufSize)
                        m_iMSS = m_iUDPRcvBufSize;

                    break;

                case UDTOpt.UDT_SNDSYN:
                    m_bSynSending = *(bool*)optval;
                    break;

                case UDTOpt.UDT_RCVSYN:
                    m_bSynRecving = *(bool*)optval;
                    break;

                case UDTOpt.UDT_CC:
                    if (m_bConnecting || m_bConnected)
                        throw new UdtException(5, 1, 0);
                    //m_pCCFactory = (&(CCCVirtualFactory*)optval).clone();

                    break;

                case UDTOpt.UDT_FC:
                    if (m_bConnecting || m_bConnected)
                        throw new UdtException(5, 2, 0);

                    if (*(int*)optval < 1)
                        throw new UdtException(5, 3);

                    // Mimimum recv flight flag size is 32 packets
                    if (*(int*)optval > 32)
                        m_iFlightFlagSize = *(int*)optval;
                    else
                        m_iFlightFlagSize = 32;

                    break;

                case UDTOpt.UDT_SNDBUF:
                    if (m_bOpened)
                        throw new UdtException(5, 1, 0);

                    if (*(int*)optval <= 0)
                        throw new UdtException(5, 3, 0);

                    m_iSndBufSize = *(int*)optval / (m_iMSS - 28);

                    break;

                case UDTOpt.UDT_RCVBUF:
                    if (m_bOpened)
                        throw new UdtException(5, 1, 0);

                    if (*(int*)optval <= 0)
                        throw new UdtException(5, 3, 0);

                    // Mimimum recv buffer size is 32 packets
                    if (*(int*)optval > (m_iMSS - 28) * 32)
                        m_iRcvBufSize = *(int*)optval / (m_iMSS - 28);
                    else
                        m_iRcvBufSize = 32;

                    // recv buffer MUST not be greater than FC size
                    if (m_iRcvBufSize > m_iFlightFlagSize)
                        m_iRcvBufSize = m_iFlightFlagSize;

                    break;

                case UDTOpt.UDT_LINGER:
                    m_Linger = ConvertLingerOption.FromVoidPointer(optval);
                    break;

                case UDTOpt.UDP_SNDBUF:
                    if (m_bOpened)
                        throw new UdtException(5, 1, 0);

                    m_iUDPSndBufSize = *(int*)optval;

                    if (m_iUDPSndBufSize < m_iMSS)
                        m_iUDPSndBufSize = m_iMSS;

                    break;

                case UDTOpt.UDP_RCVBUF:
                    if (m_bOpened)
                        throw new UdtException(5, 1, 0);

                    m_iUDPRcvBufSize = *(int*)optval;

                    if (m_iUDPRcvBufSize < m_iMSS)
                        m_iUDPRcvBufSize = m_iMSS;

                    break;

                case UDTOpt.UDT_RENDEZVOUS:
                    if (m_bConnecting || m_bConnected)
                        throw new UdtException(5, 1, 0);
                    m_bRendezvous = *(bool*)optval;
                    break;

                case UDTOpt.UDT_SNDTIMEO:
                    m_iSndTimeOut = *(int*)optval;
                    break;

                case UDTOpt.UDT_RCVTIMEO:
                    m_iRcvTimeOut = *(int*)optval;
                    break;

                case UDTOpt.UDT_REUSEADDR:
                    if (m_bOpened)
                        throw new UdtException(5, 1, 0);
                    m_bReuseAddr = *(bool*)optval;
                    break;

                case UDTOpt.UDT_MAXBW:
                    m_llMaxBW = *(long*)optval;
                    break;

                default:
                    throw new UdtException(5, 0, 0);
            }
        }

        public unsafe void getOpt(UDTOpt optName, void* optval, ref int optlen)
        {
            lock (m_ConnectionLock)
            {
                getOpt_unsafe(optName, optval, ref optlen);
            }
        }

        unsafe void getOpt_unsafe(UDTOpt optName, void* optval, ref int optlen)
        {
            switch (optName)
            {
                case UDTOpt.UDT_MSS:
                    *(int*)optval = m_iMSS;
                    optlen = sizeof(int);
                    break;

                case UDTOpt.UDT_SNDSYN:
                    *(bool*)optval = m_bSynSending;
                    optlen = sizeof(bool);
                    break;

                case UDTOpt.UDT_RCVSYN:
                    *(bool*)optval = m_bSynRecving;
                    optlen = sizeof(bool);
                    break;

                case UDTOpt.UDT_CC:
                    if (!m_bOpened)
                        throw new UdtException(5, 5, 0);
                    //*(CC**)optval = m_pCC;
                    //optlen = sizeof(CC*);

                    break;

                case UDTOpt.UDT_FC:
                    *(int*)optval = m_iFlightFlagSize;
                    optlen = sizeof(int);
                    break;

                case UDTOpt.UDT_SNDBUF:
                    *(int*)optval = m_iSndBufSize * (m_iMSS - 28);
                    optlen = sizeof(int);
                    break;

                case UDTOpt.UDT_RCVBUF:
                    *(int*)optval = m_iRcvBufSize * (m_iMSS - 28);
                    optlen = sizeof(int);
                    break;

                case UDTOpt.UDT_LINGER:
                    if (optlen < 5) //?? (int)(sizeof(LingerOption)))
                        throw new UdtException(5, 3, 0);
                    ConvertLingerOption.ToVoidPointer(m_Linger, optval);
                    optlen = 5; //??
                    break;

                case UDTOpt.UDP_SNDBUF:
                    *(int*)optval = m_iUDPSndBufSize;
                    optlen = sizeof(int);
                    break;

                case UDTOpt.UDP_RCVBUF:
                    *(int*)optval = m_iUDPRcvBufSize;
                    optlen = sizeof(int);
                    break;

                case UDTOpt.UDT_RENDEZVOUS:
                    *(bool*)optval = m_bRendezvous;
                    optlen = sizeof(bool);
                    break;

                case UDTOpt.UDT_SNDTIMEO:
                    *(int*)optval = m_iSndTimeOut;
                    optlen = sizeof(int);
                    break;

                case UDTOpt.UDT_RCVTIMEO:
                    *(int*)optval = m_iRcvTimeOut;
                    optlen = sizeof(int);
                    break;

                case UDTOpt.UDT_REUSEADDR:
                    *(bool*)optval = m_bReuseAddr;
                    optlen = sizeof(bool);
                    break;

                case UDTOpt.UDT_MAXBW:
                    *(long*)optval = m_llMaxBW;
                    optlen = sizeof(long);
                    break;

                case UDTOpt.UDT_STATE:
                    *(int*)optval = (int)s_UDTUnited.getStatus(m_SocketID);
                    optlen = sizeof(int);
                    break;

                case UDTOpt.UDT_EVENT:
                    {
                        int udtevent = 0;
                        if (m_bBroken)
                            udtevent |= (int)EPOLLOpt.UDT_EPOLL_ERR;
                        else
                        {
                            if (m_pRcvBuffer != null && (m_pRcvBuffer.getRcvDataSize() > 0))
                                udtevent |= (int)EPOLLOpt.UDT_EPOLL_IN;
                            if (m_pSndBuffer != null && (m_iSndBufSize > m_pSndBuffer.getCurrBufSize()))
                                udtevent |= (int)EPOLLOpt.UDT_EPOLL_OUT;
                        }
                        *(int*)optval = udtevent;
                        optlen = sizeof(int);
                    }
                    break;

                case UDTOpt.UDT_SNDDATA:
                    if (m_pSndBuffer != null)
                        *(int*)optval = m_pSndBuffer.getCurrBufSize();
                    else
                        *(int*)optval = 0;
                    optlen = sizeof(int);
                    break;

                case UDTOpt.UDT_RCVDATA:
                    if (m_pRcvBuffer != null)
                        *(int*)optval = m_pRcvBuffer.getRcvDataSize();
                    else
                        *(int*)optval = 0;
                    optlen = sizeof(int);
                    break;

                default:
                    throw new UdtException(5, 0, 0);
            }
        }

        public unsafe void open()
        {
            lock (m_ConnectionLock)
            {
                open_unsafe();
            }
        }

        unsafe void open_unsafe()
        {
            // Initial sequence number, loss, acknowledgement, etc.
            m_iPktSize = m_iMSS - 28;
            m_iPayloadSize = m_iPktSize - Packet.m_iPktHdrSize;

            m_iEXPCount = 1;
            m_iBandwidth = 1;
            m_iDeliveryRate = 16;
            m_iAckSeqNo = 0;
            m_ullLastAckTime = 0;

            // trace information
            
            m_StartTime = Timer.getTime();
            m_llSentTotal = m_llRecvTotal = m_iSndLossTotal = m_iRcvLossTotal = m_iRetransTotal = m_iSentACKTotal = m_iRecvACKTotal = m_iSentNAKTotal = m_iRecvNAKTotal = 0;
            m_LastSampleTime = Timer.getTime();
            m_llTraceSent = m_llTraceRecv = m_iTraceSndLoss = m_iTraceRcvLoss = m_iTraceRetrans = m_iSentACK = m_iRecvACK = m_iSentNAK = m_iRecvNAK = 0;
            m_llSndDuration = m_llSndDurationTotal = 0;

            // structures for queue
            if (null == m_pSNode)
                m_pSNode = new SNode();
            m_pSNode.m_pUDT = this;
            m_pSNode.m_llTimeStamp = 1;
            m_pSNode.m_iHeapLoc = -1;

            if (null == m_pRNode)
                m_pRNode = new RNode();
            m_pRNode.m_pUDT = this;
            m_pRNode.m_llTimeStamp = 1;
            //m_pRNode.m_pPrev = m_pRNode.m_pNext = null;
            m_pRNode.m_bOnList = false;

            m_iRTT = 10 * m_iSYNInterval;
            m_iRTTVar = m_iRTT >> 1;

            m_ullCPUFrequency = Timer.getCPUFrequency();

            // set up the timers
            m_ullSYNInt = m_iSYNInterval * m_ullCPUFrequency;

            // set minimum NAK and EXP timeout to 100ms
            m_ullMinNakInt = 300000 * m_ullCPUFrequency;
            m_ullMinExpInt = 300000 * m_ullCPUFrequency;

            m_ullACKInt = m_ullSYNInt;
            m_ullNAKInt = m_ullMinNakInt;


            ulong currtime = Timer.rdtsc();
            m_ullLastRspTime = currtime;
            m_ullNextACKTime = currtime + m_ullSYNInt;
            m_ullNextNAKTime = currtime + m_ullNAKInt;

            m_iPktCount = 0;
            m_iLightACKCount = 1;

            m_ullTargetTime = 0;
            m_ullTimeDiff = 0;

            // Now UDT is opened.
            m_bOpened = true;
        }

        public void listen()
        {
            lock (m_ConnectionLock)
            {
                listen_unsafe();
            }
        }

        void listen_unsafe()
        {
            if (!m_bOpened)
                throw new UdtException(5, 0, 0);

            if (m_bConnecting || m_bConnected)
                throw new UdtException(5, 2, 0);

            // listen can be called more than once
            if (m_bListening)
                return;

            // if there is already another socket listening on the same port
            if (m_pRcvQueue.setListener(this) < 0)
                throw new UdtException(5, 11, 0);

            m_bListening = true;
        }

        public unsafe void connect(IPEndPoint serv_addr)
        {
            lock (m_ConnectionLock)
            {
                connect_unsafe(serv_addr);
            }
        }

        unsafe void connect_unsafe(IPEndPoint serv_addr)
        {
            if (!m_bOpened)
                throw new UdtException(5, 0, 0);

            if (m_bListening)
                throw new UdtException(5, 2, 0);

            if (m_bConnecting || m_bConnected)
                throw new UdtException(5, 2, 0);

            m_bConnecting = true;

            // record peer/server address
            m_pPeerAddr = serv_addr;

            // register this socket in the rendezvous queue
            // RendezevousQueue is used to temporarily store incoming handshake, non-rendezvous connections also require this function
            ulong ttl = 3000000;
            if (m_bRendezvous)
                ttl *= 10;
            ttl += Timer.getTime();
            m_pRcvQueue.registerConnector(m_SocketID, this, m_iIPversion, serv_addr, ttl);

            // This is my current configurations
            m_ConnReq.m_iVersion = m_iVersion;
            m_ConnReq.m_iType = m_iSockType;
            m_ConnReq.m_iMSS = m_iMSS;
            m_ConnReq.m_iFlightFlagSize = (m_iRcvBufSize < m_iFlightFlagSize) ? m_iRcvBufSize : m_iFlightFlagSize;
            m_ConnReq.m_iReqType = (!m_bRendezvous) ? 1 : 0;
            m_ConnReq.m_iID = m_SocketID;
            ConvertIPAddress.ToUintArray(serv_addr.Address, ref m_ConnReq.m_piPeerIP);

            // Random Initial Sequence Number
            Random rnd = new Random();
            m_iISN = m_ConnReq.m_iISN = rnd.Next(1, SequenceNumber.m_iMaxSeqNo);

            m_iLastDecSeq = m_iISN - 1;
            m_iSndLastAck = m_iISN;
            m_iSndLastDataAck = m_iISN;
            m_iSndCurrSeqNo = m_iISN - 1;
            m_iSndLastAck2 = m_iISN;
            m_ullSndLastAck2Time = Timer.getTime();

            // Inform the server my configurations.
            Packet request = new Packet();
            request.pack(m_ConnReq);
            // ID = 0, connection request
            request.SetId(0);

            m_pSndQueue.sendto(serv_addr, request);
            m_llLastReqTime = (long)Timer.getTime();

            // asynchronous connect, return immediately
            if (!m_bSynRecving)
            {
                return;
            }

            // Wait for the negotiated configurations from the peer side.
            Packet response = new Packet();
            byte[] resdata = new byte[m_iPayloadSize];
            response.pack(0, resdata);

            UdtException e = new UdtException(0, 0);

            while (!m_bClosing)
            {
                // avoid sending too many requests, at most 1 request per 250ms
                if ((long)Timer.getTime() - m_llLastReqTime > 250000)
                {
                    request.pack(m_ConnReq);
                    if (m_bRendezvous)
                        request.SetId(m_ConnRes.m_iID);
                    m_pSndQueue.sendto(serv_addr, request);
                    m_llLastReqTime = (long)Timer.getTime();
                }

                response.setLength(m_iPayloadSize);
                if (m_pRcvQueue.recvfrom(m_SocketID, response) > 0)
                {
                    if (connect(response) <= 0)
                        break;

                    // new request/response should be sent out immediately on receving a response
                    m_llLastReqTime = 0;
                }

                if (Timer.getTime() > ttl)
                {
                    // timeout
                    e = new UdtException(1, 1, 0);
                    break;
                }
            }

            if (e.getErrorCode() == 0)
            {
                if (m_bClosing)                                                 // if the socket is closed before connection...
                    e = new UdtException(1);
                else if (1002 == m_ConnRes.m_iReqType)                          // connection request rejected
                    e = new UdtException(1, 2, 0);
                else if ((!m_bRendezvous) && (m_iISN != m_ConnRes.m_iISN))      // secuity check
                    e = new UdtException(1, 4, 0);
            }

            if (e.getErrorCode() != 0)
                throw e;
        }

        public int connect(Packet response)
        {
           // this is the 2nd half of a connection request. If the connection is setup successfully this returns 0.
           // returning -1 means there is an error.
           // returning 1 or 2 means the connection is in process and needs more handshake

           if (!m_bConnecting)
              return -1;

           if (m_bRendezvous && ((0 == response.getFlag()) || (1 == response.getType())) && (0 != m_ConnRes.m_iType))
           {
              //a data packet or a keep-alive packet comes, which means the peer side is already connected
              // in this situation, the previously recorded response will be used
              goto POST_CONNECT;
           }

           if ((1 != response.getFlag()) || (0 != response.getType()))
              return -1;

           m_ConnRes.deserialize(response.GetDataBytes(), response.getLength());

           if (m_bRendezvous)
           {
              // regular connect should NOT communicate with rendezvous connect
              // rendezvous connect require 3-way handshake
              if (1 == m_ConnRes.m_iReqType)
                 return -1;

              if ((0 == m_ConnReq.m_iReqType) || (0 == m_ConnRes.m_iReqType))
              {
                 m_ConnReq.m_iReqType = -1;
                 // the request time must be updated so that the next handshake can be sent out immediately.
                 m_llLastReqTime = 0;
                 return 1;
              }
           }
           else
           {
              // set cookie
              if (1 == m_ConnRes.m_iReqType)
              {
                 m_ConnReq.m_iReqType = -1;
                 m_ConnReq.m_iCookie = m_ConnRes.m_iCookie;
                 m_llLastReqTime = 0;
                 return 1;
              }
           }

            POST_CONNECT:

            // Remove from rendezvous queue
            m_pRcvQueue.removeConnector(m_SocketID);

            // Re-configure according to the negotiated values.
            m_iMSS = m_ConnRes.m_iMSS;
            m_iFlowWindowSize = m_ConnRes.m_iFlightFlagSize;
            m_iPktSize = m_iMSS - 28;
            m_iPayloadSize = m_iPktSize - Packet.m_iPktHdrSize;
            m_iPeerISN = m_ConnRes.m_iISN;
            m_iRcvLastAck = m_ConnRes.m_iISN;
            m_iRcvLastAckAck = m_ConnRes.m_iISN;
            m_iRcvCurrSeqNo = m_ConnRes.m_iISN - 1;
            m_PeerID = m_ConnRes.m_iID;
            Array.Copy(m_ConnRes.m_piPeerIP, m_piSelfIP, 4);

            // Prepare all data structures
            try
            {
                m_pSndBuffer = new SndBuffer(32, m_iPayloadSize);
                m_pRcvBuffer = new RcvBuffer(m_iRcvBufSize);
                // after introducing lite ACK, the sndlosslist may not be cleared in time, so it requires twice space.
                m_pSndLossList = new SndLossList(m_iFlowWindowSize* 2);
                m_pRcvLossList = new RcvLossList(m_iFlightFlagSize);
                m_pACKWindow = new ACKWindow(1024);
                m_pRcvTimeWindow = new PktTimeWindow(16, 64);
                m_pSndTimeWindow = new PktTimeWindow();
            }
            catch (Exception e)
            {
                throw new UdtException(3, 2, 0);
            }

            InfoBlock ib;
            if (m_pCache.TryGetValue(m_pPeerAddr.Address, out ib))
            {
                m_iRTT = ib.m_iRTT;
                m_iBandwidth = ib.m_iBandwidth;
            }

            m_pCC = m_pCCFactory.create();
            m_pCC.m_UDT = m_SocketID;
            m_pCC.setMSS(m_iMSS);
            m_pCC.setMaxCWndSize(m_iFlowWindowSize);
            m_pCC.setSndCurrSeqNo(m_iSndCurrSeqNo);
            m_pCC.setRcvRate(m_iDeliveryRate);
            m_pCC.setRTT(m_iRTT);
            m_pCC.setBandwidth(m_iBandwidth);
            m_pCC.init();

            m_ullInterval = (ulong) (m_pCC.m_dPktSndPeriod* m_ullCPUFrequency);
            m_dCongestionWindow = m_pCC.m_dCWndSize;

            // And, I am connected too.
            m_bConnecting = false;
            m_bConnected = true;

           // register this socket for receiving data packets
           m_pRNode.m_bOnList = true;
           m_pRcvQueue.setNewEntry(this);

            // acknowledge the management module.
            s_UDTUnited.connect_complete(m_SocketID);

           // acknowledde any waiting epolls to write
           //s_UDTUnited.m_EPoll.update_events(m_SocketID, m_sPollID, EPOLLOpt.UDT_EPOLL_OUT, true);

           return 0;
        }

        public unsafe void connect(IPEndPoint peer, Handshake hs)
        {
            lock (m_ConnectionLock)
            {
                connect_unsafe(peer, hs);
            }
        }

        unsafe void connect_unsafe(IPEndPoint peer, Handshake hs)
        {
            // Uses the smaller MSS between the peers
            if (hs.m_iMSS > m_iMSS)
                hs.m_iMSS = m_iMSS;
            else
                m_iMSS = hs.m_iMSS;

            // exchange info for maximum flow window size
            m_iFlowWindowSize = hs.m_iFlightFlagSize;
            hs.m_iFlightFlagSize = (m_iRcvBufSize < m_iFlightFlagSize) ? m_iRcvBufSize : m_iFlightFlagSize;

            m_iPeerISN = hs.m_iISN;

            m_iRcvLastAck = hs.m_iISN;
            m_iRcvLastAckAck = hs.m_iISN;
            m_iRcvCurrSeqNo = hs.m_iISN - 1;

            m_PeerID = hs.m_iID;
            hs.m_iID = m_SocketID;

            // use peer's ISN and send it back for security check
            m_iISN = hs.m_iISN;

            m_iLastDecSeq = m_iISN - 1;
            m_iSndLastAck = m_iISN;
            m_iSndLastDataAck = m_iISN;
            m_iSndCurrSeqNo = m_iISN - 1;
            m_iSndLastAck2 = m_iISN;
            m_ullSndLastAck2Time = Timer.getTime();

            // this is a reponse handshake
            hs.m_iReqType = -1;

            // get local IP address and send the peer its IP address (because UDP cannot get local IP address)
            Array.Copy(hs.m_piPeerIP, m_piSelfIP, 4);
            ConvertIPAddress.ToUintArray(peer.Address, ref hs.m_piPeerIP);

            m_iPktSize = m_iMSS - 28;
            m_iPayloadSize = m_iPktSize - Packet.m_iPktHdrSize;

            // Prepare all structures
            try
            {
                m_pSndBuffer = new SndBuffer(32, m_iPayloadSize);
                m_pRcvBuffer = new RcvBuffer(m_iRcvBufSize);
                m_pSndLossList = new SndLossList(m_iFlowWindowSize * 2);
                m_pRcvLossList = new RcvLossList(m_iFlightFlagSize);
                m_pACKWindow = new ACKWindow(1024);
                m_pRcvTimeWindow = new PktTimeWindow(16, 64);
                m_pSndTimeWindow = new PktTimeWindow();
            }
            catch (Exception e)
            {
                throw new UdtException(3, 2, 0);
            }

            InfoBlock ib;
            if (m_pCache.TryGetValue(peer.Address, out ib))
            {
                m_iRTT = ib.m_iRTT;
                m_iBandwidth = ib.m_iBandwidth;
            }

            m_pCC = m_pCCFactory.create();
            m_pCC.m_UDT = m_SocketID;
            m_pCC.setMSS(m_iMSS);
            m_pCC.setMaxCWndSize(m_iFlowWindowSize);
            m_pCC.setSndCurrSeqNo(m_iSndCurrSeqNo);
            m_pCC.setRcvRate(m_iDeliveryRate);
            m_pCC.setRTT(m_iRTT);
            m_pCC.setBandwidth(m_iBandwidth);
            m_pCC.init();

            m_ullInterval = (ulong)(m_pCC.m_dPktSndPeriod * m_ullCPUFrequency);
            m_dCongestionWindow = m_pCC.m_dCWndSize;

            m_pPeerAddr = peer;

            // And of course, it is connected.
            m_bConnected = true;

            // register this socket for receiving data packets
            m_pRNode.m_bOnList = true;
            m_pRcvQueue.setNewEntry(this);

            //send the response to the peer, see listen() for more discussions about this
            Packet response = new Packet();
            response.pack(hs);
            response.SetId(m_PeerID);
            m_pSndQueue.sendto(peer, response);
        }

        public unsafe void close()
        {
            if (!m_bOpened)
                return;

            if (m_Linger.Enabled)
            {
                ulong entertime = Timer.getTime();

                while (!m_bBroken && m_bConnected && (m_pSndBuffer.getCurrBufSize() > 0) && (Timer.getTime() - entertime < (ulong)m_Linger.LingerTime * 1000000))
                {
                    // linger has been checked by previous close() call and has expired
                    if (m_ullLingerExpiration >= entertime)
                        break;

                    if (!m_bSynSending)
                    {
                        // if this socket enables asynchronous sending, return immediately and let GC to close it later
                        if (0 == m_ullLingerExpiration)
                            m_ullLingerExpiration = entertime + (ulong)m_Linger.LingerTime * 1000000;

                        return;
                    }

                    System.Threading.Thread.Sleep(1);
                }
            }

            // remove this socket from the snd queue
            if (m_bConnected)
                m_pSndQueue.m_pSndUList.remove(this);

            // trigger any pending IO events.
            //s_UDTUnited.m_EPoll.update_events(m_SocketID, m_sPollID, EPOLLOpt.UDT_EPOLL_ERR, true);
            // then remove itself from all epoll monitoring
            //try
            //{
            //    for (set<int>.iterator i = m_sPollID.begin(); i != m_sPollID.end(); ++i)
            //        s_UDTUnited.m_EPoll.remove_usock(* i, m_SocketID);
            //}
            //catch (Exception e)
            //{
            //}

            if (!m_bOpened)
                return;

            // Inform the threads handler to stop.
            m_bClosing = true;

            lock (m_ConnectionLock)
            {
                close_unsafe();
            }

            // waiting all send and recv calls to stop
            lock (m_SendLock) lock(m_RecvLock)
            { }

            // CLOSED.
            m_bOpened = false;
        }

        unsafe void close_unsafe()
        {
            // Signal the sender and recver if they are waiting for data.
            releaseSynch();

            if (m_bListening)
            {
                m_bListening = false;
                m_pRcvQueue.removeListener(this);
            }
            else if (m_bConnecting)
            {
                m_pRcvQueue.removeConnector(m_SocketID);
            }

            if (m_bConnected)
            {
                if (!m_bShutdown)
                    sendCtrl(5);

                m_pCC.close();

                // Store current connection information.
                InfoBlock ib;
                if (!m_pCache.TryGetValue(m_pPeerAddr.Address, out ib))
                {
                    ib = new InfoBlock(m_pPeerAddr.Address);
                    m_pCache[m_pPeerAddr.Address] = ib;
                }
                ib.m_iRTT = m_iRTT;
                ib.m_iBandwidth = m_iBandwidth;

                m_bConnected = false;
            }

        }

        public int send(byte[] data, int offset, int len)
        {   /* error with major 5 and minor 10 meant "This operation is not supported in SOCK_DGRAM mode"
            if (SocketType.Dgram == m_iSockType)
                throw new UdtException(5, 10, 0);
            */
            // throw an exception if not connected
            if (m_bBroken || m_bClosing)
                throw new UdtException(2, 1, 0);
            else if (!m_bConnected)
                throw new UdtException(2, 2, 0);

            if (len <= 0)
                return 0;

            if (offset + len > data.Length)
                len = data.Length - offset;

            lock (m_SendLock)
            {
                return send_unsafe(data, offset, len);
            }
        }

        int send_unsafe(byte[] data, int offset, int len)
        {
            if (m_pSndBuffer.getCurrBufSize() == 0)
            {
                // delay the EXP timer to avoid mis-fired timeout
                ulong currtime = Timer.rdtsc();
                m_ullLastRspTime = currtime;
            }

            if (m_iSndBufSize <= m_pSndBuffer.getCurrBufSize())
            {
                if (!m_bSynSending)
                    throw new UdtException(6, 1, 0);
                else
                {
                    // wait here during a blocking sending

                    if (m_iSndTimeOut < 0)
                    {
                        while (!m_bBroken && m_bConnected && !m_bClosing && (m_iSndBufSize <= m_pSndBuffer.getCurrBufSize()) && m_bPeerHealth)
                            m_SendBlockCond.WaitOne(Timeout.Infinite);
                    }
                    else
                    {
                        ulong exptime = Timer.getTime() + (ulong)m_iSndTimeOut * 1000;

                        while (!m_bBroken && m_bConnected && !m_bClosing && (m_iSndBufSize <= m_pSndBuffer.getCurrBufSize()) && m_bPeerHealth && (Timer.getTime() < exptime))
                            m_SendBlockCond.WaitOne((int)(exptime - Timer.getTime()) / 1000);
                    }

                    // check the connection status
                    if (m_bBroken || m_bClosing)
                        throw new UdtException(2, 1, 0);
                    else if (!m_bConnected)
                        throw new UdtException(2, 2, 0);
                    else if (!m_bPeerHealth)
                    {
                        m_bPeerHealth = true;
                        throw new UdtException(7);
                    }
                }
            }

            if (m_iSndBufSize <= m_pSndBuffer.getCurrBufSize())
            {
                if (m_iSndTimeOut >= 0)
                    throw new UdtException(6, 3, 0);

                return 0;
            }

            int size = (m_iSndBufSize - m_pSndBuffer.getCurrBufSize()) * m_iPayloadSize;
            if (size > len)
                size = len;

            // record total time used for sending
            if (0 == m_pSndBuffer.getCurrBufSize())
                m_llSndDurationCounter = (long)Timer.getTime();

            // insert the user buffer into the sening list
            m_pSndBuffer.addBuffer(data, offset, size);

            // insert this socket to snd list if it is not on the list yet
            m_pSndQueue.m_pSndUList.update(this, false);

            //if (m_iSndBufSize <= m_pSndBuffer.getCurrBufSize())
            //{
            //    // write is not available any more
            //    s_UDTUnited.m_EPoll.update_events(m_SocketID, m_sPollID, EPOLLOpt.UDT_EPOLL_OUT, false);
            //}

            return size;
        }

        public int recv(byte[] data, int offset, int len)
        {   /* error with major 5 and minor 10 meant "This operation is not supported in SOCK_DGRAM mode"
            if (SocketType.Dgram == m_iSockType)
                throw new UdtException(5, 10, 0);
            */
            // throw an exception if not connected
            if (!m_bConnected)
                throw new UdtException(2, 2, 0);
            else if ((m_bBroken || m_bClosing) && (0 == m_pRcvBuffer.getRcvDataSize()))
                throw new UdtException(2, 1, 0);

            if (len <= 0)
                return 0;

            lock (m_RecvLock)
            {
                return recv_unsafe(data, offset, len);
            }
        }

        int recv_unsafe(byte[] data, int offset, int len)
        {
            if (0 == m_pRcvBuffer.getRcvDataSize())
            {
                if (!m_bSynRecving)
                    throw new UdtException(6, 2, 0);
                else
                {
                    if (m_iRcvTimeOut < 0)
                    {
                        while (!m_bBroken && m_bConnected && !m_bClosing && (0 == m_pRcvBuffer.getRcvDataSize()))
                            m_RecvDataCond.WaitOne(Timeout.Infinite);
                    }
                    else
                    {
                        ulong enter_time = Timer.getTime();

                        while (!m_bBroken && m_bConnected && !m_bClosing && (0 == m_pRcvBuffer.getRcvDataSize()))
                        {
                            int diff = (int)(Timer.getTime() - enter_time) / 1000;
                            if (diff >= m_iRcvTimeOut)
                                break;
                            m_RecvDataCond.WaitOne(m_iRcvTimeOut - diff);
                        }
                    }
                }
            }

            // throw an exception if not connected
            if (!m_bConnected)
                throw new UdtException(2, 2, 0);
            else if ((m_bBroken || m_bClosing) && (0 == m_pRcvBuffer.getRcvDataSize()))
                throw new UdtException(2, 1, 0);

            int res = m_pRcvBuffer.readBuffer(data, offset, len);

            //if (m_pRcvBuffer.getRcvDataSize() <= 0)
            //{
            //    // read is not available any more
            //    s_UDTUnited.m_EPoll.update_events(m_SocketID, m_sPollID, UDT_EPOLL_IN, false);
            //}

            if ((res <= 0) && (m_iRcvTimeOut >= 0))
                throw new UdtException(6, 3, 0);

            return res;
        }

        public int sendmsg(byte[] data, int offset, int len, int msttl, bool inorder)
        {
            if (SocketType.Stream == m_iSockType)
                throw new UdtException(5, 9, 0);

            // throw an exception if not connected
            if (m_bBroken || m_bClosing)
                throw new UdtException(2, 1, 0);
            else if (!m_bConnected)
                throw new UdtException(2, 2, 0);

            if (len <= 0)
                return 0;

            if (len + offset > data.Length)
                len = data.Length - offset;

            if (len > m_iSndBufSize * m_iPayloadSize)
                throw new UdtException(5, 12, 0);

            lock (m_SendLock)
            {
                return sendmsg_unsafe(data, offset, len, msttl, inorder);
            }
        }

        int sendmsg_unsafe(byte[] data, int offset, int len, int msttl, bool inorder)
        {
            if (m_pSndBuffer.getCurrBufSize() == 0)
            {
                // delay the EXP timer to avoid mis-fired timeout
                m_ullLastRspTime = Timer.rdtsc();
            }

            if ((m_iSndBufSize - m_pSndBuffer.getCurrBufSize()) * m_iPayloadSize < len)
            {
                if (!m_bSynSending)
                    throw new UdtException(6, 1, 0);
                else
                {
                    // wait here during a blocking sending
                    if (m_iSndTimeOut < 0)
                    {
                        while (!m_bBroken && m_bConnected && !m_bClosing && ((m_iSndBufSize - m_pSndBuffer.getCurrBufSize()) * m_iPayloadSize < len))
                            m_SendBlockCond.WaitOne(Timeout.Infinite);
                    }
                    else
                    {
                        ulong exptime = Timer.getTime() + (ulong)m_iSndTimeOut * 1000;

                        while (!m_bBroken && m_bConnected && !m_bClosing && ((m_iSndBufSize - m_pSndBuffer.getCurrBufSize()) * m_iPayloadSize < len) && (Timer.getTime() < exptime))
                            m_SendBlockCond.WaitOne((int)(exptime - Timer.getTime()) / 1000);
                    }

                    // check the connection status
                    if (m_bBroken || m_bClosing)
                        throw new UdtException(2, 1, 0);
                    else if (!m_bConnected)
                        throw new UdtException(2, 2, 0);
                }
            }

            if ((m_iSndBufSize - m_pSndBuffer.getCurrBufSize()) * m_iPayloadSize < len)
            {
                if (m_iSndTimeOut >= 0)
                    throw new UdtException(6, 3, 0);

                return 0;
            }

            // record total time used for sending
            if (0 == m_pSndBuffer.getCurrBufSize())
                m_llSndDurationCounter = (long)Timer.getTime();

            // insert the user buffer into the sening list
            m_pSndBuffer.addBuffer(data, offset, len, msttl, inorder);

            // insert this socket to the snd list if it is not on the list yet
            m_pSndQueue.m_pSndUList.update(this, false);

            //if (m_iSndBufSize <= m_pSndBuffer.getCurrBufSize())
            //{
            //    // write is not available any more
            //    s_UDTUnited.m_EPoll.update_events(m_SocketID, m_sPollID, UDT_EPOLL_OUT, false);
            //}

            return len;
        }

        public int recvmsg(byte[] data, int len)
        {
            if (SocketType.Stream == m_iSockType)
                throw new UdtException(5, 9, 0);

            // throw an exception if not connected
            if (!m_bConnected)
                throw new UdtException(2, 2, 0);

            if (len <= 0)
                return 0;

            lock (m_RecvLock)
            {
                return recvmsg_unsafe(data, len);
            }
        }

        int recvmsg_unsafe(byte[] data, int len)
        {
            int res = 0;
            if (m_bBroken || m_bClosing)
            {
                res = m_pRcvBuffer.readMsg(data, len);

                //if (m_pRcvBuffer.getRcvMsgNum() <= 0)
                //{
                //    // read is not available any more
                //    s_UDTUnited.m_EPoll.update_events(m_SocketID, m_sPollID, EPOLLOpt.UDT_EPOLL_IN, false);
                //}

                if (0 == res)
                    throw new UdtException(2, 1, 0);
                else
                    return res;
            }

            if (!m_bSynRecving)
            {
                res = m_pRcvBuffer.readMsg(data, len);
                if (0 == res)
                    throw new UdtException(6, 2, 0);
                else
                    return res;
            }

            bool timeout = false;

            do
            {
                if (m_iRcvTimeOut < 0)
                {
                    while (!m_bBroken && m_bConnected && !m_bClosing && (0 == (res = m_pRcvBuffer.readMsg(data, len))))
                        m_RecvDataCond.WaitOne(Timeout.Infinite);
                }
                else
                {
                    timeout = !m_RecvDataCond.WaitOne(m_iRcvTimeOut);
                    res = m_pRcvBuffer.readMsg(data, len);
                }

                if (m_bBroken || m_bClosing)
                    throw new UdtException(2, 1, 0);
                else if (!m_bConnected)
                    throw new UdtException(2, 2, 0);
            }
            while ((0 == res) && !timeout);

            //if (m_pRcvBuffer.getRcvMsgNum() <= 0)
            //{
            //    // read is not available any more
            //    s_UDTUnited.m_EPoll.update_events(m_SocketID, m_sPollID, UDT_EPOLL_IN, false);
            //}

            if ((res <= 0) && (m_iRcvTimeOut >= 0))
                throw new UdtException(6, 3, 0);

            return res;
        }

        public void sample(PerfMon perf, bool clear)
        {
            if (!m_bConnected)
                throw new UdtException(2, 2, 0);
            if (m_bBroken || m_bClosing)
                throw new UdtException(2, 1, 0);

            ulong currtime = Timer.getTime();
            perf.msTimeStamp = (long)(currtime - m_StartTime) / 1000;

            perf.pktSent = m_llTraceSent;
            perf.pktRecv = m_llTraceRecv;
            perf.pktSndLoss = m_iTraceSndLoss;
            perf.pktRcvLoss = m_iTraceRcvLoss;
            perf.pktRetrans = m_iTraceRetrans;
            perf.pktSentACK = m_iSentACK;
            perf.pktRecvACK = m_iRecvACK;
            perf.pktSentNAK = m_iSentNAK;
            perf.pktRecvNAK = m_iRecvNAK;
            perf.usSndDuration = m_llSndDuration;

            perf.pktSentTotal = m_llSentTotal;
            perf.pktRecvTotal = m_llRecvTotal;
            perf.pktSndLossTotal = m_iSndLossTotal;
            perf.pktRcvLossTotal = m_iRcvLossTotal;
            perf.pktRetransTotal = m_iRetransTotal;
            perf.pktSentACKTotal = m_iSentACKTotal;
            perf.pktRecvACKTotal = m_iRecvACKTotal;
            perf.pktSentNAKTotal = m_iSentNAKTotal;
            perf.pktRecvNAKTotal = m_iRecvNAKTotal;
            perf.usSndDurationTotal = m_llSndDurationTotal;

            double interval = (double)(currtime - m_LastSampleTime);

            perf.mbpsSendRate = (double)(m_llTraceSent) * m_iPayloadSize * 8.0 / interval;
            perf.mbpsRecvRate = (double)(m_llTraceRecv) * m_iPayloadSize * 8.0 / interval;

            perf.usPktSndPeriod = m_ullInterval / (double)m_ullCPUFrequency;
            perf.pktFlowWindow = m_iFlowWindowSize;
            perf.pktCongestionWindow = (int)m_dCongestionWindow;
            perf.pktFlightSize = SequenceNumber.seqlen(m_iSndLastAck, SequenceNumber.incseq(m_iSndCurrSeqNo)) - 1;
            perf.msRTT = m_iRTT / 1000.0;
            perf.mbpsBandwidth = m_iBandwidth * m_iPayloadSize * 8.0 / 1000000.0;

            if (Monitor.TryEnter(m_ConnectionLock))
            {
                perf.byteAvailSndBuf = (null == m_pSndBuffer) ? 0 : (m_iSndBufSize - m_pSndBuffer.getCurrBufSize()) * m_iMSS;
                perf.byteAvailRcvBuf = (null == m_pRcvBuffer) ? 0 : m_pRcvBuffer.getAvailBufSize() * m_iMSS;

                Monitor.Exit(m_ConnectionLock);
            }
            else
            {
                perf.byteAvailSndBuf = 0;
                perf.byteAvailRcvBuf = 0;
            }

            if (clear)
            {
                m_llTraceSent = m_llTraceRecv = m_iTraceSndLoss = m_iTraceRcvLoss = m_iTraceRetrans = m_iSentACK = m_iRecvACK = m_iSentNAK = m_iRecvNAK = 0;
                m_llSndDuration = 0;
                m_LastSampleTime = currtime;
            }
        }

        void CCUpdate()
        {
            m_ullInterval = (ulong)(m_pCC.m_dPktSndPeriod * m_ullCPUFrequency);
            m_dCongestionWindow = m_pCC.m_dCWndSize;

            if (m_llMaxBW <= 0)
                return;
            double minSP = 1000000.0 / ((double)m_llMaxBW / m_iMSS) * m_ullCPUFrequency;
            if (m_ullInterval < minSP)
                m_ullInterval = (ulong)minSP;
        }


        void destroySynch()
        {
            m_SendBlockCond.Close();
            m_RecvDataCond.Close();
        }

        void releaseSynch()
        {
            m_SendBlockCond.Set();

            bool gotLock = false;
            try
            {
                Monitor.Enter(m_SendLock, ref gotLock);
            }
            finally
            {
                if (gotLock)
                    Monitor.Exit(m_SendLock);
            }

            m_RecvDataCond.Set();

            gotLock = false;
            try
            {
                Monitor.Enter(m_RecvLock, ref gotLock);
            }
            finally
            {
                if (gotLock)
                    Monitor.Exit(m_RecvLock);
            }
        }

        unsafe void sendCtrl(int pkttype, void* lparam = null, void* rparam = null, int size = 0)
        {
            Packet ctrlpkt = new Packet();

            switch (pkttype)
            {
                case 2: //010 - Acknowledgement
                    {
                        int ack;

                        // If there is no loss, the ACK is the current largest sequence number plus 1;
                        // Otherwise it is the smallest sequence number in the receiver loss list.
                        if (0 == m_pRcvLossList.getLossLength())
                            ack = SequenceNumber.incseq(m_iRcvCurrSeqNo);
                        else
                            ack = m_pRcvLossList.getFirstLostSeq();

                        if (ack == m_iRcvLastAckAck)
                            break;

                        // send out a lite ACK
                        // to save time on buffer processing and bandwidth/AS measurement, a lite ACK only feeds back an ACK number
                        if (4 == size)
                        {
                            ctrlpkt.pack(pkttype, null, &ack, size);
                            ctrlpkt.SetId(m_PeerID);
                            m_pSndQueue.sendto(m_pPeerAddr, ctrlpkt);

                            break;
                        }

                        ulong currtime = Timer.rdtsc();

                        // There are new received packets to acknowledge, update related information.
                        if (SequenceNumber.seqcmp(ack, m_iRcvLastAck) > 0)
                        {
                            int acksize = SequenceNumber.seqoff(m_iRcvLastAck, ack);

                            m_iRcvLastAck = ack;

                            m_pRcvBuffer.ackData(acksize);

                            // signal a waiting "recv" call if there is any data available
                            if (m_bSynRecving)
                                m_RecvDataCond.Set();

                            // acknowledge any waiting epolls to read
                            //s_UDTUnited.m_EPoll.update_events(m_SocketID, m_sPollID, EPOLLOpt.UDT_EPOLL_IN, true);
                        }
                        else if (ack == m_iRcvLastAck)
                        {
                            if ((currtime - m_ullLastAckTime) < ((ulong)(m_iRTT + 4 * m_iRTTVar) * m_ullCPUFrequency))
                                break;
                        }
                        else
                            break;

                        // Send out the ACK only if has not been received by the sender before
                        if (SequenceNumber.seqcmp(m_iRcvLastAck, m_iRcvLastAckAck) > 0)
                        {
                            int[] data = new int[6];

                            m_iAckSeqNo = AckNumber.incack(m_iAckSeqNo);
                            data[0] = m_iRcvLastAck;
                            data[1] = m_iRTT;
                            data[2] = m_iRTTVar;
                            data[3] = m_pRcvBuffer.getAvailBufSize();
                            // a minimum flow window of 2 is used, even if buffer is full, to break potential deadlock
                            if (data[3] < 2)
                                data[3] = 2;

                            if (currtime - m_ullLastAckTime > m_ullSYNInt)
                            {
                                data[4] = m_pRcvTimeWindow.getPktRcvSpeed();
                                data[5] = m_pRcvTimeWindow.getBandwidth();
                                ctrlpkt.pack(pkttype, m_iAckSeqNo, data);

                                m_ullLastAckTime = Timer.rdtsc();
                            }
                            else
                            {
                                ctrlpkt.pack(pkttype, m_iAckSeqNo, data, 4);
                            }

                            ctrlpkt.SetId(m_PeerID);
                            m_pSndQueue.sendto(m_pPeerAddr, ctrlpkt);

                            m_pACKWindow.store(m_iAckSeqNo, m_iRcvLastAck);

                            ++m_iSentACK;
                            ++m_iSentACKTotal;
                        }

                        break;
                    }

                case 6: //110 - Acknowledgement of Acknowledgement
                    ctrlpkt.pack(pkttype, lparam);
                    ctrlpkt.SetId(m_PeerID);
                    m_pSndQueue.sendto(m_pPeerAddr, ctrlpkt);

                    break;

                case 3: //011 - Loss Report
                    {
                        if (null != rparam)
                        {
                            if (1 == size)
                            {
                                // only 1 loss packet
                                ctrlpkt.pack(pkttype, null, (int*)rparam + 1, 4);
                            }
                            else
                            {
                                // more than 1 loss packets
                                ctrlpkt.pack(pkttype, null, rparam, 8);
                            }

                            ctrlpkt.SetId(m_PeerID);
                            m_pSndQueue.sendto(m_pPeerAddr, ctrlpkt);

                            ++m_iSentNAK;
                            ++m_iSentNAKTotal;
                        }
                        else if (m_pRcvLossList.getLossLength() > 0)
                        {
                            // this is periodically NAK report; make sure NAK cannot be sent back too often

                            // read loss list from the local receiver loss list
                            int[] data = new int[m_iPayloadSize / 4];
                            int losslen;
                            m_pRcvLossList.getLossArray(data, out losslen, m_iPayloadSize / 4);

                            if (0 < losslen)
                            {
                                ctrlpkt.pack(pkttype, data, losslen);
                                ctrlpkt.SetId(m_PeerID);
                                m_pSndQueue.sendto(m_pPeerAddr, ctrlpkt);

                                ++m_iSentNAK;
                                ++m_iSentNAKTotal;
                            }
                        }

                        // update next NAK time, which should wait enough time for the retansmission, but not too long
                        m_ullNAKInt = (ulong)(m_iRTT + 4 * m_iRTTVar) * m_ullCPUFrequency;
                        int rcv_speed = m_pRcvTimeWindow.getPktRcvSpeed();
                        if (rcv_speed > 0)
                            m_ullNAKInt += (ulong)(m_pRcvLossList.getLossLength() * 1000000 / rcv_speed) *m_ullCPUFrequency;
                        if (m_ullNAKInt < m_ullMinNakInt)
                            m_ullNAKInt = m_ullMinNakInt;

                        break;
                    }

                case 4: //100 - Congestion Warning
                    ctrlpkt.pack(pkttype);
                    ctrlpkt.SetId(m_PeerID);
                    m_pSndQueue.sendto(m_pPeerAddr, ctrlpkt);

                    m_ullLastWarningTime = Timer.rdtsc();
                    break;

                case 1: //001 - Keep-alive
                    ctrlpkt.pack(pkttype);
                    ctrlpkt.SetId(m_PeerID);
                    m_pSndQueue.sendto(m_pPeerAddr, ctrlpkt);

                    break;

                case 0: //000 - Handshake
                    ctrlpkt.pack(pkttype, null, rparam, Handshake.m_iContentSize);
                    ctrlpkt.SetId(m_PeerID);
                    m_pSndQueue.sendto(m_pPeerAddr, ctrlpkt);

                    break;

                case 5: //101 - Shutdown
                    ctrlpkt.pack(pkttype);
                    ctrlpkt.SetId(m_PeerID);
                    m_pSndQueue.sendto(m_pPeerAddr, ctrlpkt);

                    break;

                case 7: //111 - Msg drop request
                    ctrlpkt.pack(pkttype, lparam, rparam, 8);
                    ctrlpkt.SetId(m_PeerID);
                    m_pSndQueue.sendto(m_pPeerAddr, ctrlpkt);

                    break;

                case 8: //1000 - acknowledge the peer side a special error
                    ctrlpkt.pack(pkttype, lparam);
                    ctrlpkt.SetId(m_PeerID);
                    m_pSndQueue.sendto(m_pPeerAddr, ctrlpkt);

                    break;

                case 32767: //0x7FFF - Resevered for future use
                    break;

                default:
                    break;
            }
        }

        public unsafe void processCtrl(Packet ctrlpkt)
        {
            // Just heard from the peer, reset the expiration count.
            m_iEXPCount = 1;
            ulong currtime = Timer.rdtsc();
            m_ullLastRspTime = currtime;

            switch (ctrlpkt.getType())
            {
                case 2: //010 - Acknowledgement
                    {
                        int ack;

                        // process a lite ACK
                        if (4 == ctrlpkt.getLength())
                        {
                            ack = ctrlpkt.GetIntFromData(0);
                            if (SequenceNumber.seqcmp(ack, m_iSndLastAck) >= 0)
                            {
                                m_iFlowWindowSize -= SequenceNumber.seqoff(m_iSndLastAck, ack);
                                m_iSndLastAck = ack;
                            }

                            break;
                        }

                        // read ACK seq. no.
                        ack = ctrlpkt.getAckSeqNo();

                        // send ACK acknowledgement
                        // number of ACK2 can be much less than number of ACK
                        ulong now = Timer.getTime();
                        if ((now - m_ullSndLastAck2Time > m_iSYNInterval) || (ack == m_iSndLastAck2))
                        {
                            sendCtrl(6, &ack);
                            m_iSndLastAck2 = ack;
                            m_ullSndLastAck2Time = now;
                        }

                        // Got data ACK
                        ack = ctrlpkt.GetIntFromData(0);

                        // check the validation of the ack
                        if (SequenceNumber.seqcmp(ack, SequenceNumber.incseq(m_iSndCurrSeqNo)) > 0)
                        {
                            //this should not happen: attack or bug
                            m_bBroken = true;
                            m_iBrokenCounter = 0;
                            break;
                        }

                        if (SequenceNumber.seqcmp(ack, m_iSndLastAck) >= 0)
                        {
                            // Update Flow Window Size, must update before and together with m_iSndLastAck
                            m_iFlowWindowSize = ctrlpkt.GetIntFromData(3);
                            m_iSndLastAck = ack;
                        }

                        // protect packet retransmission
                        bool bLockTaken = false;
                        Monitor.Enter(m_AckLock, ref bLockTaken);

                        int offset = SequenceNumber.seqoff(m_iSndLastDataAck, ack);
                        if (offset <= 0)
                        {
                            // discard it if it is a repeated ACK
                            if (bLockTaken)
                                Monitor.Exit(m_AckLock);
                            break;
                        }

                        // acknowledge the sending buffer
                        m_pSndBuffer.ackData(offset);

                        // record total time used for sending
                        m_llSndDuration += (long)now - m_llSndDurationCounter;
                        m_llSndDurationTotal += (long)now - m_llSndDurationCounter;
                        m_llSndDurationCounter = (long)now;

                        // update sending variables
                        m_iSndLastDataAck = ack;
                        m_pSndLossList.remove(SequenceNumber.decseq(m_iSndLastDataAck));

                        if (bLockTaken)
                            Monitor.Exit(m_AckLock);

                        if (m_bSynSending)
                            m_SendBlockCond.Set();

                        // acknowledde any waiting epolls to write
                        //s_UDTUnited.m_EPoll.update_events(m_SocketID, m_sPollID, EPOLLOpt.UDT_EPOLL_OUT, true);

                        // insert this socket to snd list if it is not on the list yet
                        m_pSndQueue.m_pSndUList.update(this, false);

                        // Update RTT
                        int rtt = ctrlpkt.GetIntFromData(1);
                        m_iRTTVar = (m_iRTTVar * 3 + Math.Abs(rtt - m_iRTT)) >> 2;
                        m_iRTT = (m_iRTT * 7 + rtt) >> 3;

                        m_pCC.setRTT(m_iRTT);

                        if (ctrlpkt.getLength() > 16)
                        {
                            // Update Estimated Bandwidth and packet delivery rate
                            if (ctrlpkt.GetIntFromData(4) > 0)
                                m_iDeliveryRate = (m_iDeliveryRate * 7 + ctrlpkt.GetIntFromData(4)) >> 3;

                            if (ctrlpkt.GetIntFromData(5) > 0)
                                m_iBandwidth = (m_iBandwidth * 7 + ctrlpkt.GetIntFromData(5)) >> 3;

                            m_pCC.setRcvRate(m_iDeliveryRate);
                            m_pCC.setBandwidth(m_iBandwidth);
                        }

                        m_pCC.onACK(ack);
                        CCUpdate();

                        ++m_iRecvACK;
                        ++m_iRecvACKTotal;

                        break;
                    }

                case 6: //110 - Acknowledgement of Acknowledgement
                    {
                        int ack = -1;
                        int rtt = -1;

                        // update RTT
                        rtt = m_pACKWindow.acknowledge(ctrlpkt.getAckSeqNo(), ref ack);
                        if (rtt <= 0)
                            break;

                        //if increasing delay detected...
                        //   sendCtrl(4);

                        // RTT EWMA
                        m_iRTTVar = (m_iRTTVar * 3 + Math.Abs(rtt - m_iRTT)) >> 2;
                        m_iRTT = (m_iRTT * 7 + rtt) >> 3;

                        m_pCC.setRTT(m_iRTT);

                        // update last ACK that has been received by the sender
                        if (SequenceNumber.seqcmp(ack, m_iRcvLastAckAck) > 0)
                            m_iRcvLastAckAck = ack;

                        break;
                    }

                case 3: //011 - Loss Report
                    {
                        int[] losslist = new int[ctrlpkt.getLength() / 4];
                        Buffer.BlockCopy(ctrlpkt.GetDataBytes(), 0, losslist, 0, ctrlpkt.getLength());

                        m_pCC.onLoss(losslist, ctrlpkt.getLength() / 4);
                        CCUpdate();

                        bool secure = true;

                        // decode loss list message and insert loss into the sender loss list
                        for (int i = 0; i < losslist.Length; ++i)
                        {
                            if (0 != (losslist[i] & 0x80000000))
                            {
                                if ((SequenceNumber.seqcmp(losslist[i] & 0x7FFFFFFF, losslist[i + 1]) > 0) || (SequenceNumber.seqcmp(losslist[i + 1], m_iSndCurrSeqNo) > 0))
                                {
                                    // seq_a must not be greater than seq_b; seq_b must not be greater than the most recent sent seq
                                    secure = false;
                                    break;
                                }

                                int num = 0;
                                if (SequenceNumber.seqcmp(losslist[i] & 0x7FFFFFFF, m_iSndLastAck) >= 0)
                                    num = m_pSndLossList.insert(losslist[i] & 0x7FFFFFFF, losslist[i + 1]);
                                else if (SequenceNumber.seqcmp(losslist[i + 1], m_iSndLastAck) >= 0)
                                    num = m_pSndLossList.insert(m_iSndLastAck, losslist[i + 1]);

                                m_iTraceSndLoss += num;
                                m_iSndLossTotal += num;

                                ++i;
                            }
                            else if (SequenceNumber.seqcmp(losslist[i], m_iSndLastAck) >= 0)
                            {
                                if (SequenceNumber.seqcmp(losslist[i], m_iSndCurrSeqNo) > 0)
                                {
                                    //seq_a must not be greater than the most recent sent seq
                                    secure = false;
                                    break;
                                }

                                int num = m_pSndLossList.insert(losslist[i], losslist[i]);

                                m_iTraceSndLoss += num;
                                m_iSndLossTotal += num;
                            }
                        }

                        if (!secure)
                        {
                            //this should not happen: attack or bug
                            m_bBroken = true;
                            m_iBrokenCounter = 0;
                            break;
                        }

                        // the lost packet (retransmission) should be sent out immediately
                        m_pSndQueue.m_pSndUList.update(this);

                        ++m_iRecvNAK;
                        ++m_iRecvNAKTotal;

                        break;
                    }

                case 4: //100 - Delay Warning
                        // One way packet delay is increasing, so decrease the sending rate
                    m_ullInterval = (ulong)Math.Ceiling(m_ullInterval * 1.125);
                    m_iLastDecSeq = m_iSndCurrSeqNo;

                    break;

                case 1: //001 - Keep-alive
                        // The only purpose of keep-alive packet is to tell that the peer is still alive
                        // nothing needs to be done.

                    break;

                case 0: //000 - Handshake
                    {
                        Handshake req = new Handshake();
                        req.deserialize(ctrlpkt.GetDataBytes(), ctrlpkt.getLength());
                        if ((req.m_iReqType > 0) || (m_bRendezvous && (req.m_iReqType != -2)))
                        {
                            // The peer side has not received the handshake message, so it keeps querying
                            // resend the handshake packet

                            Handshake initdata = new Handshake();
                            initdata.m_iISN = m_iISN;
                            initdata.m_iMSS = m_iMSS;
                            initdata.m_iFlightFlagSize = m_iFlightFlagSize;
                            initdata.m_iReqType = (!m_bRendezvous) ? -1 : -2;
                            initdata.m_iID = m_SocketID;

                            byte[] hs = new byte[m_iPayloadSize];
                            int hs_size = m_iPayloadSize;
                            initdata.serialize(hs);
                            fixed (byte* pHS = hs)
                            {
                                sendCtrl(0, null, pHS, hs_size);
                            }
                        }

                        break;
                    }

                case 5: //101 - Shutdown
                    m_bShutdown = true;
                    m_bClosing = true;
                    m_bBroken = true;
                    m_iBrokenCounter = 60;

                    // Signal the sender and recver if they are waiting for data.
                    releaseSynch();

                    Timer.triggerEvent();

                    break;

                case 7: //111 - Msg drop request
                    m_pRcvBuffer.dropMsg(ctrlpkt.getMsgSeq());
                    m_pRcvLossList.remove(ctrlpkt.GetIntFromData(0), ctrlpkt.GetIntFromData(1));

                    // move forward with current recv seq no.
                    if ((SequenceNumber.seqcmp(ctrlpkt.GetIntFromData(0), SequenceNumber.incseq(m_iRcvCurrSeqNo)) <= 0)
                       && (SequenceNumber.seqcmp(ctrlpkt.GetIntFromData(1), m_iRcvCurrSeqNo) > 0))
                    {
                        m_iRcvCurrSeqNo = ctrlpkt.GetIntFromData(1);
                    }

                    break;

                case 8: // 1000 - An error has happened to the peer side
                        //int err_type = packet.getAddInfo();

                    // currently only this error is signalled from the peer side
                    // if recvfile() failes (e.g., due to disk fail), blcoked sendfile/send should return immediately
                    // giving the app a chance to fix the issue

                    m_bPeerHealth = false;

                    break;

                case 32767: //0x7FFF - reserved and user defined messages
                    m_pCC.processCustomMsg(ctrlpkt);
                    CCUpdate();

                    break;

                default:
                    break;
            }
        }

        public unsafe int packData(Packet packet, ref ulong ts)
        {
            int payload = 0;
            bool probe = false;

            ulong entertime = Timer.rdtsc();

            if ((0 != m_ullTargetTime) && (entertime > m_ullTargetTime))
                m_ullTimeDiff += entertime - m_ullTargetTime;

            // Loss retransmission always has higher priority.
            packet.SetSequenceNumber(m_pSndLossList.getLostSeq());
            if (packet.GetSequenceNumber() >= 0)
            {
                // protect m_iSndLastDataAck from updating by ACK processing
                lock (m_AckLock)
                {
                    int offset = SequenceNumber.seqoff(m_iSndLastDataAck, packet.GetSequenceNumber());
                    if (offset < 0)
                        return 0;

                    int msglen = 0;
                    byte[] data = null;
                    uint msgNo = 0;
                    payload = m_pSndBuffer.readData(ref data, offset, ref msgNo, out msglen);
                    packet.SetDataFromBytes(data, 0, payload);
                    packet.SetMessageNumber(msgNo);

                    if (-1 == payload)
                    {
                        int[] seqpair = new int[2];
                        seqpair[0] = packet.GetSequenceNumber();
                        seqpair[1] = SequenceNumber.incseq(seqpair[0], msglen);
                        msgNo = packet.GetMessageNumber();
                        fixed (int* pSeqpair = seqpair)
                        {
                            sendCtrl(7, &msgNo, pSeqpair, 8);
                        }

                        // only one msg drop request is necessary
                        m_pSndLossList.remove(seqpair[1]);

                        // skip all dropped packets
                        if (SequenceNumber.seqcmp(m_iSndCurrSeqNo, SequenceNumber.incseq(seqpair[1])) < 0)
                            m_iSndCurrSeqNo = SequenceNumber.incseq(seqpair[1]);

                        return 0;
                    }
                    else if (0 == payload)
                        return 0;

                    ++m_iTraceRetrans;
                    ++m_iRetransTotal;
                }
            }
            else
            {
                // If no loss, pack a new packet.

                // check congestion/flow window limit
                int cwnd = (m_iFlowWindowSize < (int)m_dCongestionWindow) ? m_iFlowWindowSize : (int)m_dCongestionWindow;
                if (cwnd >= SequenceNumber.seqlen(m_iSndLastAck, SequenceNumber.incseq(m_iSndCurrSeqNo)))
                {
                    byte[] data = null;
                    uint msgNo = 0;
                    payload = m_pSndBuffer.readData(ref data, ref msgNo);
                    if (0 != payload)
                    {
                        packet.SetDataFromBytes(data, 0, payload);
                        packet.SetMessageNumber(msgNo);

                        m_iSndCurrSeqNo = SequenceNumber.incseq(m_iSndCurrSeqNo);
                        m_pCC.setSndCurrSeqNo(m_iSndCurrSeqNo);

                        packet.SetSequenceNumber(m_iSndCurrSeqNo);

                        // every 16 (0xF) packets, a packet pair is sent
                        if (0 == (packet.GetSequenceNumber() & 0xF))
                            probe = true;
                    }
                    else
                    {
                        m_ullTargetTime = 0;
                        m_ullTimeDiff = 0;
                        ts = 0;
                        return 0;
                    }
                }
                else
                {
                    m_ullTargetTime = 0;
                    m_ullTimeDiff = 0;
                    ts = 0;
                    return 0;
                }
            }

            packet.SetTimestamp((int)(Timer.getTime() - m_StartTime));
            packet.SetId(m_PeerID);
            packet.setLength(payload);

            m_pCC.onPktSent(packet);
            m_pSndTimeWindow.onPktSent(packet.GetTimestamp());

            ++m_llTraceSent;
            ++m_llSentTotal;

            if (probe)
            {
                // sends out probing packet pair
                ts = entertime;
                probe = false;
            }
            else
            {
                if (m_ullTimeDiff >= m_ullInterval)
                {
                    ts = entertime;
                    m_ullTimeDiff -= m_ullInterval;
                }
                else
                {
                    ts = entertime + m_ullInterval - m_ullTimeDiff;
                    m_ullTimeDiff = 0;
                }
            }

            m_ullTargetTime = ts;

            return payload;
        }

        public unsafe int processData(Unit unit)
        {
            Packet packet = unit.m_Packet;

            // Just heard from the peer, reset the expiration count.
            m_iEXPCount = 1;
            ulong currtime = Timer.rdtsc();
            m_ullLastRspTime = currtime;

            m_pCC.onPktReceived(packet);
            ++m_iPktCount;
            // update time information
            m_pRcvTimeWindow.onPktArrival();

            // check if it is probing packet pair
            if (0 == (packet.GetSequenceNumber() & 0xF))
                m_pRcvTimeWindow.probe1Arrival();
            else if (1 == (packet.GetSequenceNumber() & 0xF))
                m_pRcvTimeWindow.probe2Arrival();

            ++m_llTraceRecv;
            ++m_llRecvTotal;

            int offset = SequenceNumber.seqoff(m_iRcvLastAck, packet.GetSequenceNumber());
            if ((offset < 0) || (offset >= m_pRcvBuffer.getAvailBufSize()))
                return -1;

            if (m_pRcvBuffer.addData(unit, offset) < 0)
                return -1;

            // Loss detection.
            if (SequenceNumber.seqcmp(packet.GetSequenceNumber(), SequenceNumber.incseq(m_iRcvCurrSeqNo)) > 0)
            {
                // If loss found, insert them to the receiver loss list
                m_pRcvLossList.insert(SequenceNumber.incseq(m_iRcvCurrSeqNo), SequenceNumber.decseq(packet.GetSequenceNumber()));

                // pack loss list for NAK
                int[] lossdata = new int[2];
                lossdata[0] = (int)(SequenceNumber.incseq(m_iRcvCurrSeqNo) | 0x80000000);
                lossdata[1] = SequenceNumber.decseq(packet.GetSequenceNumber());

                // Generate loss report immediately.
                fixed (int* pLossdata = lossdata)
                {
                    sendCtrl(3, null, pLossdata, (SequenceNumber.incseq(m_iRcvCurrSeqNo) == SequenceNumber.decseq(packet.GetSequenceNumber())) ? 1 : 2);
                }

                int loss = SequenceNumber.seqlen(m_iRcvCurrSeqNo, packet.GetSequenceNumber()) - 2;
                m_iTraceRcvLoss += loss;
                m_iRcvLossTotal += loss;
            }

            // This is not a regular fixed size packet...   
            //an irregular sized packet usually indicates the end of a message, so send an ACK immediately   
            if (packet.getLength() != m_iPayloadSize)
                m_ullNextACKTime = Timer.rdtsc();

            // Update the current largest sequence number that has been received.
            // Or it is a retransmitted packet, remove it from receiver loss list.
            if (SequenceNumber.seqcmp(packet.GetSequenceNumber(), m_iRcvCurrSeqNo) > 0)
                m_iRcvCurrSeqNo = packet.GetSequenceNumber();
            else
                m_pRcvLossList.remove(packet.GetSequenceNumber());

            return 0;
        }

        public int listen(IPEndPoint endPoint, Packet packet)
        {
            if (m_bClosing)
                return 1002;

            if (packet.getLength() != Handshake.m_iContentSize)
                return 1004;

            Handshake hs = new Handshake();
            hs.deserialize(packet.GetDataBytes(), packet.getLength());

            IPHostEntry host = Dns.GetHostEntry(endPoint.Address); //TODO SocketException,ArgumentException

            // SYN cookie
            long timestamp = (long)(Timer.getTime() - m_StartTime) / 60000000;  // secret changes every one minute

            string cookiestr = string.Format("{0}:{1}:{2}", host.HostName, endPoint.Port, timestamp);

            MD5 md5 = MD5.Create();
            byte[] cookie = md5.ComputeHash(System.Text.Encoding.ASCII.GetBytes(cookiestr));

            if (1 == hs.m_iReqType)
            {
                hs.m_iCookie = BitConverter.ToInt32(cookie, 0);
                packet.pack(hs);
                packet.SetId(hs.m_iID);
                m_pSndQueue.sendto(endPoint, packet);
                return 0;
            }
            else
            {
                if (hs.m_iCookie != BitConverter.ToInt32(cookie, 0))
                {
                    timestamp--;
                    cookiestr = string.Format("{0}:{1}:{2}", host.HostName, endPoint.Port, timestamp);
                    cookie = md5.ComputeHash(System.Text.Encoding.ASCII.GetBytes(cookiestr));

                    if (hs.m_iCookie != BitConverter.ToInt32(cookie, 0))
                        return -1;
                }
            }

            int id = hs.m_iID;

            // When a peer side connects in...
            if ((1 == packet.getFlag()) && (0 == packet.getType()))
            {
                if ((hs.m_iVersion != m_iVersion) || (hs.m_iType != m_iSockType))
                {
                    // mismatch, reject the request
                    hs.m_iReqType = 1002;
                    packet.pack(hs);
                    packet.SetId(id);
                    m_pSndQueue.sendto(endPoint, packet);
                }
                else
                {
                    int result = s_UDTUnited.newConnection(m_SocketID, endPoint, hs);

                    if (result == -1)
                        hs.m_iReqType = 1002;

                    // send back a response if connection failed or connection already existed
                    // new connection response should be sent in connect()
                    if (result != 1)
                    {
                        packet.pack(hs);
                        packet.SetId(id);
                        m_pSndQueue.sendto(endPoint, packet);
                    }
                    else
                    {
                        // a new connection has been created, enable epoll for write 
                        //s_UDTUnited.m_EPoll.update_events(m_SocketID, m_sPollID, EPOLLOpt.UDT_EPOLL_OUT, true);
                    }
                }
            }

            return hs.m_iReqType;
        }

        public unsafe void checkTimers()
        {
            // update CC parameters
            CCUpdate();
            //ulong minint = (ulong)(m_ullCPUFrequency * m_pSndTimeWindow.getMinPktSndInt() * 0.9);
            //if (m_ullInterval < minint)
            //   m_ullInterval = minint;

            ulong currtime = Timer.rdtsc();

            if ((currtime > m_ullNextACKTime) || ((m_pCC.m_iACKInterval > 0) && (m_pCC.m_iACKInterval <= m_iPktCount)))
            {
                // ACK timer expired or ACK interval is reached

                sendCtrl(2);
                currtime = Timer.rdtsc();
                if (m_pCC.m_iACKPeriod > 0)
                    m_ullNextACKTime = currtime + (ulong)m_pCC.m_iACKPeriod * m_ullCPUFrequency;
                else
                    m_ullNextACKTime = currtime + m_ullACKInt;

                m_iPktCount = 0;
                m_iLightACKCount = 1;
            }
            else if (m_iSelfClockInterval * m_iLightACKCount <= m_iPktCount)
            {
                //send a "light" ACK
                sendCtrl(2, null, null, 4);
                ++m_iLightACKCount;
            }

            // we are not sending back repeated NAK anymore and rely on the sender's EXP for retransmission
            //if ((m_pRcvLossList.getLossLength() > 0) && (currtime > m_ullNextNAKTime))
            //{
            //   // NAK timer expired, and there is loss to be reported.
            //   sendCtrl(3);
            //
            //   CTimer.rdtsc(currtime);
            //   m_ullNextNAKTime = currtime + m_ullNAKInt;
            //}

            ulong next_exp_time;
            if (m_pCC.m_bUserDefinedRTO)
                next_exp_time = m_ullLastRspTime + (ulong)m_pCC.m_iRTO * m_ullCPUFrequency;
            else
            {
                ulong exp_int = (ulong)(m_iEXPCount * (m_iRTT + 4 * m_iRTTVar) + m_iSYNInterval) * m_ullCPUFrequency;
                if (exp_int < (ulong)m_iEXPCount * m_ullMinExpInt)
                    exp_int = (ulong)m_iEXPCount * m_ullMinExpInt;
                next_exp_time = m_ullLastRspTime + exp_int;
            }

            if (currtime > next_exp_time)
            {
                // Haven't receive any information from the peer, is it dead?!
                // timeout: at least 16 expirations and must be greater than 10 seconds
                if ((m_iEXPCount > 16) && (currtime - m_ullLastRspTime > 5000000 * m_ullCPUFrequency))
                {
                    //
                    // Connection is broken. 
                    // UDT does not signal any information about this instead of to stop quietly.
                    // Application will detect this when it calls any UDT methods next time.
                    //
                    m_bClosing = true;
                    m_bBroken = true;
                    m_iBrokenCounter = 30;

                    // update snd U list to remove this socket
                    m_pSndQueue.m_pSndUList.update(this);

                    releaseSynch();

                    // app can call any UDT API to learn the connection_broken error
                    //s_UDTUnited.m_EPoll.update_events(m_SocketID, m_sPollID, UDT_EPOLL_IN | UDT_EPOLL_OUT | UDT_EPOLL_ERR, true);

                    Timer.triggerEvent();

                    return;
                }

                // sender: Insert all the packets sent after last received acknowledgement into the sender loss list.
                // recver: Send out a keep-alive packet
                if (m_pSndBuffer.getCurrBufSize() > 0)
                {
                    if ((SequenceNumber.incseq(m_iSndCurrSeqNo) != m_iSndLastAck) && (m_pSndLossList.getLossLength() == 0))
                    {
                        // resend all unacknowledged packets on timeout, but only if there is no packet in the loss list
                        int csn = m_iSndCurrSeqNo;
                        int num = m_pSndLossList.insert(m_iSndLastAck, csn);
                        m_iTraceSndLoss += num;
                        m_iSndLossTotal += num;
                    }

                    m_pCC.onTimeout();
                    CCUpdate();

                    // immediately restart transmission
                    m_pSndQueue.m_pSndUList.update(this);
                }
                else
                {
                    sendCtrl(1);
                }

                ++m_iEXPCount;
                // Reset last response time since we just sent a heart-beat.
                m_ullLastRspTime = currtime;
            }
        }
    }
}