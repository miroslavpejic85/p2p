using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using UDTSOCKET = System.Int32;

namespace UdtSharp
{
    class Multiplexer
    {
        internal SndQueue m_pSndQueue; // The sending queue
        internal RcvQueue m_pRcvQueue; // The receiving queue
        internal Channel m_pChannel;   // The UDP channel for sending and receiving
        internal Timer m_pTimer;       // The timer

        internal int m_iPort;            // The UDP port number of this multiplexer
        internal AddressFamily m_iIPversion;       // IP version
        internal int m_iMSS;         // Maximum Segment Size
        internal int m_iRefCount;        // number of UDT instances that are associated with this multiplexer
        internal bool m_bReusable;       // if this one can be shared with others

        internal int m_iID;          // multiplexer ID
    }

    internal class UdtSocketInternal
    {
        public UDTSTATUS m_Status;                       // current socket state

        public ulong m_TimeStamp;                     // time when the socket is closed

        public AddressFamily m_iIPversion;                         // IP version
        public IPEndPoint m_pSelfAddr;                    // pointer to the local address of the socket
        public IPEndPoint m_pPeerAddr;                    // pointer to the peer address of the socket

        public UDTSOCKET m_SocketID;                     // socket ID
        public UDTSOCKET m_ListenSocket;                 // ID of the listener socket; 0 means this is an independent socket

        public UDTSOCKET m_PeerID;                       // peer socket ID
        public int m_iISN;                           // initial sequence number, used to tell different connection from same IP:port

        public UDT m_pUDT;                             // pointer to the UDT entity

        public HashSet<UDTSOCKET> m_pQueuedSockets;    // set of connections waiting for accept()
        public HashSet<UDTSOCKET> m_pAcceptSockets;    // set of accept()ed connections

        public EventWaitHandle m_AcceptCond = new EventWaitHandle(false, EventResetMode.AutoReset);// used to block "accept" call
        public object m_AcceptLock = new object();             // mutex associated to m_AcceptCond

        public uint m_uiBackLog;                 // maximum number of connections in queue

        public int m_iMuxID;                             // multiplexer ID

        public object m_ControlLock = new object();            // lock this socket exclusively for control APIs: bind/listen/connect

        public UdtSocketInternal()
        {
            m_Status = UDTSTATUS.INIT;
            m_iMuxID = -1;
        }

        public void Close()
        {
            m_AcceptCond.Close();
        }
    }

    public class UdtUnited
    {
        Dictionary<UDTSOCKET, UdtSocketInternal> m_Sockets = new Dictionary<UDTSOCKET, UdtSocketInternal>();       // stores all the socket structures

        object m_ControlLock = new object();                    // used to synchronize UDT API

        object m_IDLock = new object();                         // used to synchronize ID generation
        UDTSOCKET m_SocketID;                             // seed to generate a new unique socket ID

        Dictionary<long, HashSet<UDTSOCKET>> m_PeerRec = new Dictionary<long, HashSet<UDTSOCKET>>();// record sockets from peers to avoid repeated connection request, int64_t = (socker_id << 30) + isn

        //pthread_key_t m_TLSError;                         // thread local error record (last error)
        Dictionary<uint, UdtException> m_mTLSRecord;
        object m_TLSLock = new object();

        Dictionary<int, Multiplexer> m_mMultiplexer = new Dictionary<UDTSOCKET, Multiplexer>();      // UDP multiplexer
        object m_MultiplexerLock = new object();

        Dictionary<IPAddress, InfoBlock> m_pCache = new Dictionary<IPAddress, InfoBlock>();            // UDT network information cache

        volatile bool m_bClosing;
        object m_GCStopLock = new object();
        EventWaitHandle m_GCStopCond = new EventWaitHandle(false, EventResetMode.AutoReset);

        object m_InitLock = new object();
        int m_iInstanceCount;               // number of startup() called by application

        Dictionary<UDTSOCKET, UdtSocketInternal> m_ClosedSockets = new Dictionary<UDTSOCKET, UdtSocketInternal>();   // temporarily store closed sockets

        static Random m_random = new Random();

        public UdtUnited()
        {
            // Socket ID MUST start from a random value
            m_SocketID = 1 + (int)((1 << 30) * m_random.NextDouble());

            //m_TLSError = TlsAlloc();
        }

        ~UdtUnited()
        {
            //TlsFree(m_TLSError);
        }

        public int startup()
        {
            lock (m_InitLock)
            {
                ++m_iInstanceCount;
                return 0;
            }
        }

        public int cleanup()
        {
            lock (m_InitLock)
            {
                if (--m_iInstanceCount > 0)
                    return 0;
            }

            m_bClosing = true;
            return 0;
        }

        public UDTSOCKET newSocket(AddressFamily af, SocketType type)
        {
            if ((type != SocketType.Stream) && (type != SocketType.Dgram))
                throw new UdtException(5, 3, 0);

            UdtSocketInternal ns = new UdtSocketInternal();
            ns.m_pUDT = new UDT();
            ns.m_pSelfAddr = new IPEndPoint(IPAddress.Any, 0);

            lock (m_IDLock)
            {
                ns.m_SocketID = --m_SocketID;
            }

            ns.m_Status = UDTSTATUS.INIT;
            ns.m_ListenSocket = 0;
            ns.m_pUDT.m_SocketID = ns.m_SocketID;
            ns.m_pUDT.m_iSockType = type;
            ns.m_pUDT.m_iIPversion = af;
            ns.m_pUDT.m_pCache = m_pCache;

            // protect the m_Sockets structure.
            lock (m_ControlLock)
            {
                m_Sockets[ns.m_SocketID] = ns;
            }

            return ns.m_SocketID;
        }

        public int newConnection(UDTSOCKET listen, IPEndPoint peer, Handshake hs)
        {
            UdtSocketInternal ns = null;
            UdtSocketInternal ls = locate(listen);

            if (null == ls)
                return -1;

            // if this connection has already been processed
            if (null != (ns = locate(peer, hs.m_iID, hs.m_iISN)))
            {
                if (ns.m_pUDT.m_bBroken)
                {
                    // last connection from the "peer" address has been broken
                    ns.m_Status = UDTSTATUS.CLOSED;
                    ns.m_TimeStamp = Timer.getTime();

                    lock (ls.m_AcceptLock)
                    {
                        ls.m_pQueuedSockets.Remove(ns.m_SocketID);
                        ls.m_pAcceptSockets.Remove(ns.m_SocketID);
                    }
                }
                else
                {
                    // connection already exist, this is a repeated connection request
                    // respond with existing HS information

                    hs.m_iISN = ns.m_pUDT.m_iISN;
                    hs.m_iMSS = ns.m_pUDT.m_iMSS;
                    hs.m_iFlightFlagSize = ns.m_pUDT.m_iFlightFlagSize;
                    hs.m_iReqType = -1;
                    hs.m_iID = ns.m_SocketID;

                    return 0;

                    //except for this situation a new connection should be started
                }
            }

            // exceeding backlog, refuse the connection request
            if (ls.m_pQueuedSockets.Count >= ls.m_uiBackLog)
                return -1;

            ns = new UdtSocketInternal();
            ns.m_pUDT = new UDT(ls.m_pUDT);
            ns.m_pSelfAddr = new IPEndPoint(IPAddress.Any, 0);
            ns.m_pPeerAddr = peer;

            lock (m_IDLock)
            {
                ns.m_SocketID = --m_SocketID;
            }

            ns.m_ListenSocket = listen;
            ns.m_iIPversion = ls.m_iIPversion;
            ns.m_pUDT.m_SocketID = ns.m_SocketID;
            ns.m_PeerID = hs.m_iID;
            ns.m_iISN = hs.m_iISN;

            int error = 0;

            try
            {
                // bind to the same addr of listening socket
                ns.m_pUDT.open();
                updateMux(ns, ls);
                ns.m_pUDT.connect(peer, hs);
            }
            catch (Exception e)
            {
                error = 1;
                goto ERR_ROLLBACK;
            }

            ns.m_Status = UDTSTATUS.CONNECTED;

            // copy address information of local node
            ns.m_pUDT.m_pSndQueue.m_pChannel.getSockAddr(ref ns.m_pSelfAddr);
            ConvertIPAddress.ToUintArray(ns.m_pSelfAddr.Address, ref ns.m_pUDT.m_piSelfIP);

            // protect the m_Sockets structure.
            lock (m_ControlLock)
            {
                m_Sockets[ns.m_SocketID] = ns;
                HashSet<int> sockets;
                if (!m_PeerRec.TryGetValue((ns.m_PeerID << 30) + ns.m_iISN, out sockets))
                {
                    sockets = new HashSet<int>();
                    m_PeerRec.Add((ns.m_PeerID << 30) + ns.m_iISN, sockets);
                }

                sockets.Add(ns.m_SocketID);
            }

            lock (ls.m_AcceptLock)
            {
                ls.m_pQueuedSockets.Add(ns.m_SocketID);
            }

            // acknowledge users waiting for new connections on the listening socket
            //m_EPoll.update_events(listen, ls.m_pUDT.m_sPollID, UDT_EPOLL_IN, true);

            Timer.triggerEvent();

        ERR_ROLLBACK:
            if (error > 0)
            {
                ns.m_pUDT.close();
                ns.m_Status = UDTSTATUS.CLOSED;
                ns.m_TimeStamp = Timer.getTime();

                return -1;
            }

            // wake up a waiting accept() call
            ls.m_AcceptCond.Set();

            return 1;
        }

        public UDT lookup(UDTSOCKET u)
        {
            // protects the m_Sockets structure
            lock (m_ControlLock)
            {
                UdtSocketInternal socket;
                if (!m_Sockets.TryGetValue(u, out socket) || socket.m_Status == UDTSTATUS.CLOSED)
                    throw new UdtException(5, 4, 0);

                return socket.m_pUDT;
            }
        }

        public UDTSTATUS getStatus(UDTSOCKET u)
        {
            // protects the m_Sockets structure
            lock (m_ControlLock)
            {
                UdtSocketInternal socket;
                if (m_Sockets.TryGetValue(u, out socket))
                {
                    if (socket.m_pUDT.m_bBroken)
                        return UDTSTATUS.BROKEN;

                    return socket.m_Status;
                }

                if (m_ClosedSockets.ContainsKey(u))
                    return UDTSTATUS.CLOSED;

                return UDTSTATUS.NONEXIST;
            }
        }

        public int bind(UDTSOCKET u, IPEndPoint name)
        {
            UdtSocketInternal s = locate(u);
            if (null == s)
                throw new UdtException(5, 4, 0);

            lock (s.m_ControlLock)
            {
                // cannot bind a socket more than once
                if (UDTSTATUS.INIT != s.m_Status)
                    throw new UdtException(5, 0, 0);

                s.m_pUDT.open();
                updateMux(s, name);
                s.m_Status = UDTSTATUS.OPENED;

                // copy address information of local node
                s.m_pUDT.m_pSndQueue.m_pChannel.getSockAddr(ref s.m_pSelfAddr);

                return 0;
            }
        }


        public int bind(UDTSOCKET u, Socket udpsock)
        {
            UdtSocketInternal s = locate(u);
            if (null == s)
                throw new UdtException(5, 4, 0);

            lock (s.m_ControlLock)
            {

                // cannot bind a socket more than once
                if (UDTSTATUS.INIT != s.m_Status)
                    throw new UdtException(5, 0, 0);

                IPEndPoint name = (IPEndPoint)udpsock.LocalEndPoint;
                s.m_pUDT.open();
                updateMux(s, name, udpsock);
                s.m_Status = UDTSTATUS.OPENED;
                // copy address information of local node
                s.m_pUDT.m_pSndQueue.m_pChannel.getSockAddr(ref s.m_pSelfAddr);
                return 0;
            }
        }

        public int listen(UDTSOCKET u, int backlog)
        {
            UdtSocketInternal s = locate(u);
            if (null == s)
                throw new UdtException(5, 4, 0);

            lock (s.m_ControlLock)
            {

                // do nothing if the socket is already listening
                if (UDTSTATUS.LISTENING == s.m_Status)
                    return 0;

                // a socket can listen only if is in UDTSTATUS.OPENED status
                if (UDTSTATUS.OPENED != s.m_Status)
                    throw new UdtException(5, 5, 0);

                // listen is not supported in rendezvous connection setup
                if (s.m_pUDT.m_bRendezvous)
                    throw new UdtException(5, 7, 0);

                if (backlog <= 0)
                    throw new UdtException(5, 3, 0);

                s.m_uiBackLog = (uint)backlog;

                s.m_pQueuedSockets = new HashSet<UDTSOCKET>();
                s.m_pAcceptSockets = new HashSet<UDTSOCKET>();

                s.m_pUDT.listen();

                s.m_Status = UDTSTATUS.LISTENING;

                return 0;
            }
        }

        public UDTSOCKET accept(UDTSOCKET listen, ref IPEndPoint addr)
        {
            if (null != addr)
                throw new UdtException(5, 3, 0);

            UdtSocketInternal ls = locate(listen);

            if (ls == null)
                throw new UdtException(5, 4, 0);

            // the "listen" socket must be in UDTSTATUS.LISTENING status
            if (UDTSTATUS.LISTENING != ls.m_Status)
                throw new UdtException(5, 6, 0);

            // no "accept" in rendezvous connection setup
            if (ls.m_pUDT.m_bRendezvous)
                throw new UdtException(5, 7, 0);

            UDTSOCKET u = UDT.INVALID_SOCK;
            bool accepted = false;

            // !!only one conection can be set up each time!!
            while (!accepted)
            {
                lock (ls.m_AcceptLock)
                {
                    if (ls.m_pQueuedSockets.Count > 0)
                    {
                        HashSet<UDTSOCKET>.Enumerator e = ls.m_pQueuedSockets.GetEnumerator();
                        e.MoveNext();
                        u = e.Current;
                        ls.m_pAcceptSockets.Add(u);
                        ls.m_pQueuedSockets.Remove(u);

                        accepted = true;
                    }
                    else if (!ls.m_pUDT.m_bSynRecving)
                        accepted = true;
                }

                if (!accepted & (UDTSTATUS.LISTENING == ls.m_Status))
                    ls.m_AcceptCond.WaitOne(Timeout.Infinite);

                if ((UDTSTATUS.LISTENING != ls.m_Status) || ls.m_pUDT.m_bBroken)
                {
                    // Send signal to other threads that are waiting to accept.
                    ls.m_AcceptCond.Set();
                    accepted = true;
                }

                //if (ls.m_pQueuedSockets.Count == 0)
                //    m_EPoll.update_events(listen, ls.m_pUDT.m_sPollID, UDT_EPOLL_IN, false);
            }

            if (u == UDT.INVALID_SOCK)
            {
                // non-blocking receiving, no connection available
                if (!ls.m_pUDT.m_bSynRecving)
                    throw new UdtException(6, 2, 0);

                // listening socket is closed
                throw new UdtException(5, 6, 0);
            }

            addr = locate(u).m_pPeerAddr;

            return u;
        }

        public int connect(UDTSOCKET u, IPEndPoint name)
        {
            UdtSocketInternal s = locate(u);
            if (null == s)
                throw new UdtException(5, 4, 0);

            lock (s.m_ControlLock)
            {
                // a socket can "connect" only if it is in INIT or UDTSTATUS.OPENED status
                if (UDTSTATUS.INIT == s.m_Status)
                {
                    if (!s.m_pUDT.m_bRendezvous)
                    {
                        s.m_pUDT.open();
                        updateMux(s);
                        s.m_Status = UDTSTATUS.OPENED;
                    }
                    else
                        throw new UdtException(5, 8, 0);
                }

                // connect_complete() may be called before connect() returns.
                // So we need to update the status before connect() is called,
                // otherwise the status may be overwritten with wrong value (CONNECTED vs. CONNECTING).
                s.m_Status = UDTSTATUS.CONNECTING;
                try
                {
                    s.m_pUDT.connect(name);
                }
                catch (UdtException e)
                {
                    s.m_Status = UDTSTATUS.OPENED;
                    throw e;
                }

                // record peer address
                s.m_pPeerAddr = name;

                return 0;
            }
        }

        public void connect_complete(UDTSOCKET u)
        {
            UdtSocketInternal s = locate(u);
            if (null == s)
                throw new UdtException(5, 4, 0);

            // copy address information of local node
            // the local port must be correctly assigned BEFORE CUDT.connect(),
            // otherwise if connect() fails, the multiplexer cannot be located by garbage collection and will cause leak
            s.m_pUDT.m_pSndQueue.m_pChannel.getSockAddr(ref s.m_pSelfAddr);
            ConvertIPAddress.ToUintArray(s.m_pSelfAddr.Address, ref s.m_pUDT.m_piSelfIP);

            s.m_Status = UDTSTATUS.CONNECTED;
        }

        public int close(UDTSOCKET u)
        {
            UdtSocketInternal s = locate(u);
            if (null == s)
                throw new UdtException(5, 4, 0);

            lock (s.m_ControlLock)
            {

                if (s.m_Status == UDTSTATUS.LISTENING)
                {
                    if (s.m_pUDT.m_bBroken)
                        return 0;

                    s.m_TimeStamp = Timer.getTime();
                    s.m_pUDT.m_bBroken = true;

                    // broadcast all "accept" waiting
                    s.m_AcceptCond.Set();

                    return 0;
                }

                s.m_pUDT.close();

                // synchronize with garbage collection.
                lock (m_ControlLock)
                {

                    // since "s" is located before m_ControlLock, locate it again in case it became invalid
                    if (!m_Sockets.TryGetValue(u, out s) || s.m_Status == UDTSTATUS.CLOSED)
                    {
                        return 0;
                    }

                    s.m_Status = UDTSTATUS.CLOSED;

                    // a socket will not be immediated removed when it is closed
                    // in order to prevent other methods from accessing invalid address
                    // a timer is started and the socket will be removed after approximately 1 second
                    s.m_TimeStamp = Timer.getTime();

                    m_Sockets.Remove(s.m_SocketID);
                    m_ClosedSockets.Add(s.m_SocketID, s);

                    Timer.triggerEvent();

                    return 0;
                }
            }
        }

        //            int CUDTUnited.getpeername(const UDTSOCKET u, sockaddr*name, int* namelen)
        //{
        //                if (CONNECTED != getStatus(u))
        //                    throw new UdtException(2, 2, 0);

        //                UdtSocket* s = locate(u);

        //                if (null == s)
        //                    throw new UdtException(5, 4, 0);

        //                if (!s.m_pUDT.m_bConnected || s.m_pUDT.m_bBroken)
        //                    throw new UdtException(2, 2, 0);

        //                if (AF_INET == s.m_iIPversion)
        //                    *namelen = sizeof(sockaddr_in);
        //                else
        //                    *namelen = sizeof(sockaddr_in6);

        //                // copy address information of peer node
        //                memcpy(name, s.m_pPeerAddr, *namelen);

        //                return 0;
        //            }

        //            int CUDTUnited.getsockname(const UDTSOCKET u, sockaddr*name, int* namelen)
        //{
        //                UdtSocket* s = locate(u);

        //                if (null == s)
        //                    throw new UdtException(5, 4, 0);

        //                if (s.m_pUDT.m_bBroken)
        //                    throw new UdtException(5, 4, 0);

        //                if (INIT == s.m_Status)
        //                    throw new UdtException(2, 2, 0);

        //                if (AF_INET == s.m_iIPversion)
        //                    *namelen = sizeof(sockaddr_in);
        //                else
        //                    *namelen = sizeof(sockaddr_in6);

        //                // copy address information of local node
        //                memcpy(name, s.m_pSelfAddr, *namelen);

        //                return 0;
        //            }




        internal UdtSocketInternal locate(UDTSOCKET u)
        {
            lock (m_ControlLock)
            {
                UdtSocketInternal s;
                if (!m_Sockets.TryGetValue(u, out s) || s.m_Status == UDTSTATUS.CLOSED)
                {
                    return null;
                }

                return s;
            }
        }

        UdtSocketInternal locate(IPEndPoint peer, UDTSOCKET id, int isn)
        {
            lock (m_ControlLock)
            {
                HashSet<int> sockets;
                if (!m_PeerRec.TryGetValue((id << 30) + isn, out sockets))
                    return null;

                foreach (int iSocket in sockets)
                {
                    UdtSocketInternal socket;
                    if (!m_Sockets.TryGetValue(iSocket, out socket))
                        continue;

                    if (socket.m_pPeerAddr.Equals(peer))
                        return socket;
                }

                return null;
            }
        }

        public void checkBrokenSockets()
        {
            lock (m_ControlLock)
            {
                checkBrokenSockets_unsafe();
            }
        }

        void checkBrokenSockets_unsafe()
        {
            // set of sockets To Be Closed and To Be Removed
            List<UDTSOCKET> tbc = new List<UDTSOCKET>();
            List<UDTSOCKET> tbr = new List<UDTSOCKET>();

            foreach (KeyValuePair<UDTSOCKET, UdtSocketInternal> item in m_Sockets)
            {
                // check broken connection
                if (item.Value.m_pUDT.m_bBroken)
                {
                    if (item.Value.m_Status == UDTSTATUS.LISTENING)
                    {
                        // for a listening socket, it should wait an extra 3 seconds in case a client is connecting
                        if (Timer.getTime() - item.Value.m_TimeStamp < 3000000)
                            continue;
                    }
                    else if ((item.Value.m_pUDT.m_pRcvBuffer != null) && (item.Value.m_pUDT.m_pRcvBuffer.getRcvDataSize() > 0) && (item.Value.m_pUDT.m_iBrokenCounter-- > 0))
                    {
                        // if there is still data in the receiver buffer, wait longer
                        continue;
                    }

                    //close broken connections and start removal timer
                    item.Value.m_Status = UDTSTATUS.CLOSED;
                    item.Value.m_TimeStamp = Timer.getTime();
                    tbc.Add(item.Key);
                    m_ClosedSockets[item.Key] = item.Value;

                    // remove from listener's queue
                    UdtSocketInternal listenSocket;
                    if (!m_Sockets.TryGetValue(item.Value.m_ListenSocket, out listenSocket))
                    {
                        if (!m_ClosedSockets.TryGetValue(item.Value.m_ListenSocket, out listenSocket))
                        {
                            continue;
                        }
                    }

                    Monitor.Enter(listenSocket.m_AcceptLock);
                    listenSocket.m_pQueuedSockets.Remove(item.Value.m_SocketID);
                    listenSocket.m_pAcceptSockets.Remove(item.Value.m_SocketID);
                    Monitor.Exit(listenSocket.m_AcceptLock);
                }
            }

            foreach (KeyValuePair<UDTSOCKET, UdtSocketInternal> j in m_ClosedSockets)
            {
                if (j.Value.m_pUDT.m_ullLingerExpiration > 0)
                {
                    // asynchronous close: 
                    if ((null == j.Value.m_pUDT.m_pSndBuffer) || (0 == j.Value.m_pUDT.m_pSndBuffer.getCurrBufSize()) || (j.Value.m_pUDT.m_ullLingerExpiration <= Timer.getTime()))
                    {
                        j.Value.m_pUDT.m_ullLingerExpiration = 0;
                        j.Value.m_pUDT.m_bClosing = true;
                        j.Value.m_TimeStamp = Timer.getTime();
                    }
                }

                // timeout 1 second to destroy a socket AND it has been removed from RcvUList
                if ((Timer.getTime() - j.Value.m_TimeStamp  > 1000000) && ((null == j.Value.m_pUDT.m_pRNode) || !j.Value.m_pUDT.m_pRNode.m_bOnList))
                {
                    tbr.Add(j.Key);
                }
            }

            // move closed sockets to the ClosedSockets structure
            foreach (UDTSOCKET k in tbc)
                m_Sockets.Remove(k);

            // remove those timeout sockets
            foreach (UDTSOCKET l in tbr)
                removeSocket(l);
        }

        void removeSocket(UDTSOCKET u)
        {
            UdtSocketInternal closedSocket;
            if (!m_ClosedSockets.TryGetValue(u, out closedSocket))
                return;

            // decrease multiplexer reference count, and remove it if necessary
            int mid = closedSocket.m_iMuxID;

            if (null != closedSocket.m_pQueuedSockets)
            {
                Monitor.Enter(closedSocket.m_AcceptLock);

                // if it is a listener, close all un-accepted sockets in its queue and remove them later
                foreach (UDTSOCKET q in closedSocket.m_pQueuedSockets)
                {
                    m_Sockets[q].m_pUDT.m_bBroken = true;
                    m_Sockets[q].m_pUDT.close();
                    m_Sockets[q].m_TimeStamp = Timer.getTime();
                    m_Sockets[q].m_Status = UDTSTATUS.CLOSED;
                    m_ClosedSockets[q] = m_Sockets[q];
                    m_Sockets.Remove(q);
                }

               Monitor.Exit(closedSocket.m_AcceptLock);
            }

            // remove from peer rec
            HashSet<int> sockets;
            if (m_PeerRec.TryGetValue((closedSocket.m_PeerID << 30) + closedSocket.m_iISN, out sockets))
            {
                sockets.Remove(u);
                if (sockets.Count == 0)
                    m_PeerRec.Remove(closedSocket.m_PeerID);
            }

            // delete this one
            closedSocket.m_pUDT.close();
            closedSocket.Close();
            m_ClosedSockets.Remove(u);

            Multiplexer m;
            if (!m_mMultiplexer.TryGetValue(mid, out m))
            {
                //something is wrong!!!
                return;
            }

            m.m_iRefCount--;
            if (0 == m.m_iRefCount)
            {
                m.m_pChannel.close();
                m.m_pSndQueue.Close();
                m.m_pRcvQueue.Close();
                m.m_pTimer.Stop();

                m_mMultiplexer.Remove(mid);
            }
        }

        //            void setError(UdtException e)
        //{
        //                CGuard tg(m_TLSLock);
        //                delete(UdtException *)TlsGetValue(m_TLSError);
        //                TlsSetValue(m_TLSError, e);
        //                m_mTLSRecord[GetCurrentThreadId()] = e;
        //            }

        //            UdtException getError()
        //{
        //                CGuard tg(m_TLSLock);
        //                if (null == TlsGetValue(m_TLSError))
        //                {
        //                    UdtException* e = new UdtException;
        //                    TlsSetValue(m_TLSError, e);
        //                    m_mTLSRecord[GetCurrentThreadId()] = e;
        //                }
        //                return (UdtException*)TlsGetValue(m_TLSError);
        //            }

        //            void checkTLSValue()
        //{
        //                CGuard tg(m_TLSLock);

        //                vector<DWORD> tbr;
        //                for (map<DWORD, UdtException*>.iterator i = m_mTLSRecord.begin(); i != m_mTLSRecord.end(); ++i)
        //                {
        //                    HANDLE h = OpenThread(THREAD_QUERY_INFORMATION, FALSE, i.first);
        //                    if (null == h)
        //                    {
        //                        tbr.push_back(i.first);
        //                        break;
        //                    }
        //                    if (WAIT_OBJECT_0 == WaitForSingleObject(h, 0))
        //                    {
        //                        delete i.second;
        //                        tbr.push_back(i.first);
        //                    }
        //                    CloseHandle(h);
        //                }
        //                for (vector<DWORD>.iterator j = tbr.begin(); j != tbr.end(); ++j)
        //                    m_mTLSRecord.erase(*j);
        //            }

        void updateMux(UdtSocketInternal s, IPEndPoint addr = null, Socket udpsock = null)
        {
            lock (m_ControlLock)
            {
                Multiplexer m;
                if ((s.m_pUDT.m_bReuseAddr) && (null != addr))
                {
                    int port = addr.Port;

                    // find a reusable address
                    foreach (KeyValuePair<int, Multiplexer> item in m_mMultiplexer)
                    {
                        // reuse the existing multiplexer
                        m = item.Value;
                        if ((m.m_iIPversion == s.m_pUDT.m_iIPversion) && (m.m_iMSS == s.m_pUDT.m_iMSS) && m.m_bReusable)
                        {
                            if (m.m_iPort == port)
                            {
                                // reuse the existing multiplexer
                                ++m.m_iRefCount;
                                s.m_pUDT.m_pSndQueue = m.m_pSndQueue;
                                s.m_pUDT.m_pRcvQueue = m.m_pRcvQueue;
                                s.m_iMuxID = m.m_iID;
                                return;
                            }
                        }
                    }
                }

                // a new multiplexer is needed
                m = new Multiplexer();
                m.m_iMSS = s.m_pUDT.m_iMSS;
                m.m_iIPversion = s.m_pUDT.m_iIPversion;
                m.m_iRefCount = 1;
                m.m_bReusable = s.m_pUDT.m_bReuseAddr;
                m.m_iID = s.m_SocketID;

                m.m_pChannel = new Channel(s.m_pUDT.m_iIPversion);
                m.m_pChannel.setSndBufSize(s.m_pUDT.m_iUDPSndBufSize);
                m.m_pChannel.setRcvBufSize(s.m_pUDT.m_iUDPRcvBufSize);

                try
                {
                    if (null != udpsock)
                        m.m_pChannel.open(udpsock);
                    else
                        m.m_pChannel.open(addr);
                }
                catch (UdtException e)
                {
                    m.m_pChannel.close();
                    throw e;
                }

                IPEndPoint sa = new IPEndPoint(IPAddress.Any, 0);
                m.m_pChannel.getSockAddr(ref sa);
                m.m_iPort = sa.Port;

                m.m_pTimer = new Timer();

                m.m_pSndQueue = new SndQueue();
                m.m_pSndQueue.init(m.m_pChannel, m.m_pTimer);
                m.m_pRcvQueue = new RcvQueue();
                m.m_pRcvQueue.init(32, s.m_pUDT.m_iPayloadSize, m.m_iIPversion, 1024, m.m_pChannel, m.m_pTimer);

                m_mMultiplexer[m.m_iID] = m;

                s.m_pUDT.m_pSndQueue = m.m_pSndQueue;
                s.m_pUDT.m_pRcvQueue = m.m_pRcvQueue;
                s.m_iMuxID = m.m_iID;
            }
        }

        void updateMux(UdtSocketInternal s, UdtSocketInternal ls)
        {
            lock (m_ControlLock)
            {

                int port = ls.m_pSelfAddr.Port;

                // find the listener's address
                foreach (KeyValuePair<int, Multiplexer> item in m_mMultiplexer)
                {
                    if (item.Value.m_iPort == port)
                    {
                        // reuse the existing multiplexer
                        Multiplexer multiplexer = item.Value;
                        ++multiplexer.m_iRefCount;
                        s.m_pUDT.m_pSndQueue = multiplexer.m_pSndQueue;
                        s.m_pUDT.m_pRcvQueue = multiplexer.m_pRcvQueue;
                        s.m_iMuxID = multiplexer.m_iID;
                        return;
                    }
                }
            }
        }
    }

 
}  // namespace UdtSharp
