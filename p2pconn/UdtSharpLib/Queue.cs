using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace UdtSharp
{
    public class SNode
    {
        public UDT m_pUDT;       // Pointer to the instance of CUDT socket
        public ulong m_llTimeStamp;      // Time Stamp

        public int m_iHeapLoc;     // location on the heap, -1 means not on the heap
    };

    public class RNode
    {
        public UDT m_pUDT;                // Pointer to the instance of CUDT socket
        public ulong m_llTimeStamp;      // Time Stamp

        public bool m_bOnList;              // if the node is already on the list
    };

    class UnitQueue
    {
        struct QEntry
        {
            internal Unit[] m_pUnit;     // unit queue
            internal byte[][] m_pBuffer;        // data buffer
            internal int m_iSize;        // size of each queue
        }
        List<QEntry> mEntries = new List<QEntry>();

        int m_iCurrEntry = 0;
        int m_iLastEntry = 0;

        int m_iAvailUnit;         // recent available unit
        int m_iAvailableQueue;

        int m_iSize;            // total size of the unit queue, in number of packets
        public int m_iCount;       // total number of valid packets in the queue

        int m_iMSS;         // unit buffer size
        AddressFamily m_iIPversion;  // IP version

        UnitQueue()
        {
            m_iSize = 0;
            m_iCount = 0;
            m_iMSS = 0;
            m_iIPversion = 0;
        }

        ~UnitQueue()
        {
        }

        int init(int size, int mss, AddressFamily version)
        {
            QEntry tempq = new QEntry();
            Unit[] tempu = new Unit[size];
            byte[][] tempb = new byte[size][];

            for (int i = 0; i < size; ++i)
            {
                tempb[i] = new byte[mss];
                tempu[i] = new Unit();
                tempu[i].m_iFlag = 0;

                tempu[i].m_Packet.SetDataFromBytes(tempb[i]);
            }
            tempq.m_pUnit = tempu;
            tempq.m_pBuffer = tempb;
            tempq.m_iSize = size;

            m_iSize = size;
            m_iMSS = mss;
            m_iIPversion = version;

            mEntries.Add(tempq);

            return 0;
        }

        int increase()
        {
            // adjust/correct m_iCount
            int real_count = 0;
            for (int q = 0; q < mEntries.Count; ++q)
            {
                Unit[] units = mEntries[q].m_pUnit;
                for (int u = mEntries[q].m_iSize; u < units.Length; ++u)
                    if (units[u].m_iFlag != 0)
                        ++real_count;
            }
            m_iCount = real_count;
            if ((double)m_iCount / m_iSize < 0.9)
                return -1;

            // all queues have the same size
            int size = mEntries[0].m_iSize;

            QEntry tempq = new QEntry();
            Unit[] tempu = new Unit[size];
            byte[][] tempb = new byte[size][];

            for (int i = 0; i < size; ++i)
            {
                tempb[i] = new byte[m_iMSS];
                tempu[i].m_iFlag = 0;
                tempu[i].m_Packet.SetDataFromBytes(tempb[i]);
            }
            tempq.m_pUnit = tempu;
            tempq.m_pBuffer = tempb;
            tempq.m_iSize = size;

            mEntries.Add(tempq);

            m_iSize += size;

            return 0;
        }

        int shrink()
        {
            // currently queue cannot be shrunk.
            return -1;
        }

        Unit getNextAvailUnit()
        {
            if (m_iCount * 10 > m_iSize * 9)
                increase();

            if (m_iCount >= m_iSize)
                return null;

            QEntry entrance = mEntries[m_iCurrEntry];

            //do
            //{
            //    QEntry currentEntry = mEntries[m_iCurrEntry];
            //    Unit sentinel = currentEntry.m_pUnit[currentEntry.m_iSize - 1];
            //    for (CUnit* sentinel = m_pCurrQueue.m_pUnit + m_pCurrQueue.m_iSize - 1; m_pAvailUnit != sentinel; ++m_pAvailUnit)
            //        if (m_pAvailUnit.m_iFlag == 0)
            //            return m_pAvailUnit;



            //    if (m_pCurrQueue.m_pUnit.m_iFlag == 0)
            //    {
            //        m_pAvailUnit = m_pCurrQueue.m_pUnit;
            //        return m_pAvailUnit;
            //    }

            //    m_pCurrQueue = m_pCurrQueue.m_pNext;
            //    m_pAvailUnit = m_pCurrQueue.m_pUnit;
            //} while (m_pCurrQueue != entrance);

            increase();

            return null;
        }
    }

    public class SndUList
    {
        object m_ListLock = new object();

        public object m_pWindowLock;
        public EventWaitHandle m_pWindowCond;

        SNode[] m_pHeap;           // The heap array
        int m_iArrayLength;         // physical length of the array
        int m_iLastEntry;           // position of last entry on the heap array

        public Timer m_pTimer;

        public SndUList()
        {
            m_iArrayLength = 4096;
            m_iLastEntry = -1;

            m_pHeap = new SNode[m_iArrayLength];
        }

        public void insert(ulong ts, UDT u)
        {
            lock (m_ListLock)
            {
                // increase the heap array size if necessary
                if (m_iLastEntry == m_iArrayLength - 1)
                {
                    Array.Resize(ref m_pHeap, m_iArrayLength * 2);
                    m_iArrayLength *= 2;
                }
                
                insert_(ts, u);
            }
        }

        public void update(UDT u, bool reschedule = true)
        {
            lock (m_ListLock)
            {
                SNode n = u.m_pSNode;

                if (n.m_iHeapLoc >= 0)
                {
                    if (!reschedule)
                        return;

                    if (n.m_iHeapLoc == 0)
                    {
                        n.m_llTimeStamp = 1;
                        m_pTimer.interrupt();
                        return;
                    }

                    remove_(u);
                }

                insert_(1, u);
            }
        }

        public int pop(ref IPEndPoint addr, ref Packet pkt)
        {
            lock (m_ListLock)
            {
                if (-1 == m_iLastEntry)
                    return -1;

                // no pop until the next schedulled time
                ulong ts = Timer.rdtsc();
                if (ts < m_pHeap[0].m_llTimeStamp)
                    return -1;

                UDT u = m_pHeap[0].m_pUDT;
                remove_(u);

                if (!u.m_bConnected || u.m_bBroken)
                    return -1;

                // pack a packet from the socket
                if (u.packData(pkt, ref ts) <= 0)
                    return -1;

                addr = u.m_pPeerAddr;

                // insert a new entry, ts is the next processing time
                if (ts > 0)
                    insert_(ts, u);

                return 1;
            }
        }

        public void remove(UDT u)
        {
            lock (m_ListLock)
            {
                remove_(u);
            }
        }

        public ulong getNextProcTime()
        {
            lock (m_ListLock)
            {
                if (-1 == m_iLastEntry)
                    return 0;

                return m_pHeap[0].m_llTimeStamp;
            }
        }

        void insert_(ulong ts, UDT u)
        {
            SNode n = u.m_pSNode;

            // do not insert repeated node
            if (n.m_iHeapLoc >= 0)
                return;

            m_iLastEntry++;
            m_pHeap[m_iLastEntry] = n;
            n.m_llTimeStamp = ts;

            int q = m_iLastEntry;
            int p = q;
            while (p != 0)
            {
                p = (q - 1) >> 1;
                if (m_pHeap[p].m_llTimeStamp > m_pHeap[q].m_llTimeStamp)
                {
                    SNode t = m_pHeap[p];
                    m_pHeap[p] = m_pHeap[q];
                    m_pHeap[q] = t;
                    t.m_iHeapLoc = q;
                    q = p;
                }
                else
                    break;
            }

            n.m_iHeapLoc = q;

            // an earlier event has been inserted, wake up sending worker
            if (n.m_iHeapLoc == 0)
                m_pTimer.interrupt();

            // first entry, activate the sending queue
            if (0 == m_iLastEntry)
            {
                m_pWindowCond.Set();
            }
        }

        void remove_(UDT u)
        {
            SNode n = u.m_pSNode;

            if (n.m_iHeapLoc >= 0)
            {
                // remove the node from heap
                m_pHeap[n.m_iHeapLoc] = m_pHeap[m_iLastEntry];
                m_iLastEntry--;
                m_pHeap[n.m_iHeapLoc].m_iHeapLoc = n.m_iHeapLoc;

                int q = n.m_iHeapLoc;
                int p = q * 2 + 1;
                while (p <= m_iLastEntry)
                {
                    if ((p + 1 <= m_iLastEntry) && (m_pHeap[p].m_llTimeStamp > m_pHeap[p + 1].m_llTimeStamp))
                        p++;

                    if (m_pHeap[q].m_llTimeStamp > m_pHeap[p].m_llTimeStamp)
                    {
                        SNode t = m_pHeap[p];
                        m_pHeap[p] = m_pHeap[q];
                        m_pHeap[p].m_iHeapLoc = p;
                        m_pHeap[q] = t;
                        m_pHeap[q].m_iHeapLoc = q;

                        q = p;
                        p = q * 2 + 1;
                    }
                    else
                        break;
                }

                n.m_iHeapLoc = -1;
            }

            // the only event has been deleted, wake up immediately
            if (0 == m_iLastEntry)
                m_pTimer.interrupt();
        }
    }

    public class RendezvousQueue
    {
        struct CRL
        {
            internal int m_iID;            // UDT socket ID (self)
            internal UDT m_pUDT;           // UDT instance
            internal AddressFamily m_iIPversion;                 // IP version
            internal IPEndPoint m_pPeerAddr;      // UDT sonnection peer address
            internal ulong m_ullTTL;          // the time that this request expires
        };
        List<CRL> m_lRendezvousID = new List<CRL>();      // The sockets currently in rendezvous mode

        object m_RIDVectorLock = new object();

        public void insert(int id, UDT u, AddressFamily ipv, IPEndPoint addr, ulong ttl)
        {
            CRL r;
            r.m_iID = id;
            r.m_pUDT = u;
            r.m_iIPversion = ipv;
            r.m_pPeerAddr = addr;
            r.m_ullTTL = ttl;

            lock (m_RIDVectorLock)
            {
                m_lRendezvousID.Add(r);
            }
        }

        public void remove(int id)
        {
            lock (m_RIDVectorLock)
            {
                for (int i = 0; i < m_lRendezvousID.Count; ++i)
                {
                    if (m_lRendezvousID[i].m_iID == id)
                    {
                        m_lRendezvousID.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        public UDT retrieve(IPEndPoint addr, ref int id)
        {
            lock (m_RIDVectorLock)
            {
                foreach (CRL crl in m_lRendezvousID)
                {
                    if (crl.m_pPeerAddr.Equals(addr) && (id == 0) || (id == crl.m_iID))
                    {
                        id = crl.m_iID;
                        return crl.m_pUDT;
                    }
                }

                return null;
            }
        }

        public void updateConnStatus()
        {
            if (m_lRendezvousID.Count == 0)
                return;

            lock (m_RIDVectorLock)
            {

                foreach (CRL crl in m_lRendezvousID)
                {
                    // avoid sending too many requests, at most 1 request per 250ms
                    if (Timer.getTime() - (ulong)crl.m_pUDT.m_llLastReqTime > 250000)
                    {
                        //if (Timer.getTime() >= crl.m_ullTTL)
                        //{
                        //    // connection timer expired, acknowledge app via epoll
                        //    i->m_pUDT->m_bConnecting = false;
                        //    CUDT::s_UDTUnited.m_EPoll.update_events(i->m_iID, i->m_pUDT->m_sPollID, UDT_EPOLL_ERR, true);
                        //    continue;
                        //}

                        Packet request = new Packet();
                        request.pack(crl.m_pUDT.m_ConnReq);
                        // ID = 0, connection request
                        request.SetId(!crl.m_pUDT.m_bRendezvous ? 0 : crl.m_pUDT.m_ConnRes.m_iID);
                        crl.m_pUDT.m_pSndQueue.sendto(crl.m_pPeerAddr, request);
                        crl.m_pUDT.m_llLastReqTime = (long)Timer.getTime();
                    }
                }
            }
        }

    }

    public class SndQueue
    {
        public SndUList m_pSndUList;     // List of UDT instances for data sending
        public Channel m_pChannel;                // The UDP channel for data sending
        Timer m_pTimer;           // Timing facility

        object m_WindowLock;
        EventWaitHandle m_WindowCond;

        volatile bool m_bClosing;       // closing the worker
        EventWaitHandle m_ExitCond;

        Thread m_WorkerThread;

        public SndQueue()
        {
            m_WindowLock = new object();
            m_WindowCond = new EventWaitHandle(false, EventResetMode.AutoReset);
            m_ExitCond = new EventWaitHandle(false, EventResetMode.AutoReset);
        }

        public void Close()
        {
            m_bClosing = true;

            m_WindowCond.Set();
            if (null != m_WorkerThread)
                m_ExitCond.WaitOne(Timeout.Infinite);

            m_WindowCond.Close();
            m_ExitCond.Close();
        }

        public void init(Channel c, Timer t)
        {
            m_pChannel = c;
            m_pTimer = t;
            m_pSndUList = new SndUList();
            m_pSndUList.m_pWindowLock = m_WindowLock;
            m_pSndUList.m_pWindowCond = m_WindowCond;
            m_pSndUList.m_pTimer = m_pTimer;

            m_WorkerThread = new Thread(worker);
            m_WorkerThread.IsBackground = true;
            m_WorkerThread.Start(this);
        }

        static void worker(object param)
        {
            SndQueue self = param as SndQueue;
            if (self == null)
                return;

            while (!self.m_bClosing)
            {
                ulong ts = self.m_pSndUList.getNextProcTime();

                if (ts > 0)
                {
                    // wait until next processing time of the first socket on the list
                    ulong currtime = Timer.rdtsc();
                    if (currtime < ts)
                        self.m_pTimer.sleepto(ts);

                    // it is time to send the next pkt
                    IPEndPoint addr = null;
                    Packet pkt = new Packet();
                    if (self.m_pSndUList.pop(ref addr, ref pkt) < 0)
                        continue;

                    self.m_pChannel.sendto(addr, pkt);
                }
                else
                {
                    // wait here if there is no sockets with data to be sent
                    self.m_WindowCond.WaitOne(Timeout.Infinite);
                }
            }

            self.m_ExitCond.Set();
        }

        public int sendto(IPEndPoint addr, Packet packet)
        {
            // send out the packet immediately (high priority), this is a control packet
            m_pChannel.sendto(addr, packet);
            return packet.getLength();
        }
    }

    public class RcvUList
    {
        public List<RNode> m_nodeList = new List<RNode>();

        public void insert(UDT u)
        {
            RNode n = u.m_pRNode;
            n.m_llTimeStamp = Timer.rdtsc();

            // always insert at the end for RcvUList
            m_nodeList.Add(n);
        }

        public void remove(UDT u)
        {
            RNode n = u.m_pRNode;

            if (!n.m_bOnList)
                return;

            m_nodeList.Remove(n);
        }

        public void update(UDT u)
        {
            RNode n = u.m_pRNode;

            if (!n.m_bOnList)
                return;

            RNode match = m_nodeList.Find(x => x.Equals(n));
            if (match.Equals(default(RNode)))
                return;

            match.m_llTimeStamp = Timer.rdtsc();
        }
    }

    public class RcvQueue
    {
        RcvUList m_pRcvUList = new RcvUList();     // List of UDT instances that will read packets from the queue
        Channel m_pChannel;       // UDP channel for receving packets
        Timer m_pTimer;           // shared timer with the snd queue

        int m_iPayloadSize;                  // packet payload size

        volatile bool m_bClosing;            // closing the workder
        EventWaitHandle m_ExitCond;

        object m_LSLock;
        UDT m_pListener;                                   // pointer to the (unique, if any) listening UDT entity
        RendezvousQueue m_pRendezvousQueue = new RendezvousQueue();                // The list of sockets in rendezvous mode

        List<UDT> m_vNewEntry = new List<UDT>();                      // newly added entries, to be inserted
        object m_IDLock;

        Dictionary<int, Queue<Packet>> m_mBuffer = new Dictionary<int, Queue<Packet>>();  // temporary buffer for rendezvous connection request

        object m_PassLock;
        EventWaitHandle m_PassCond;

        Thread m_WorkerThread;

        Dictionary<int, UDT> m_hash = new Dictionary<int, UDT>();

        public RcvQueue()
        {
            m_PassLock = new object();
            m_PassCond = new EventWaitHandle(false, EventResetMode.AutoReset);
            m_LSLock = new object();
            m_IDLock = new object();
            m_ExitCond = new EventWaitHandle(false, EventResetMode.AutoReset);
        }

        public void Close()
        {
            m_bClosing = true;

            if (null != m_WorkerThread)
                m_ExitCond.WaitOne(Timeout.Infinite);

            m_PassCond.Close();
            m_ExitCond.Close();
        }

        public void init(int qsize, int payload, AddressFamily version, int hsize, Channel cc, Timer t)
        {
            m_iPayloadSize = payload;

            m_pChannel = cc;
            m_pTimer = t;

            m_WorkerThread = new Thread(worker);
            m_WorkerThread.IsBackground = true;
            m_WorkerThread.Start(this);
        }

        static void worker(object param)
        {
            RcvQueue self = param as RcvQueue;
            if (self == null)
                return;

            IPEndPoint addr = new IPEndPoint(IPAddress.Any, 0);
            UDT u = null;
            int id;

            while (!self.m_bClosing)
            {
                self.m_pTimer.tick();

                // check waiting list, if new socket, insert it to the list
                while (self.ifNewEntry())
                {
                    UDT ne = self.getNewEntry();
                    if (null != ne)
                    {
                        self.m_pRcvUList.insert(ne);
                        self.m_hash.Add(ne.m_SocketID, ne);
                    }
                }

                // find next available slot for incoming packet
                Unit unit = new Unit();
                unit.m_Packet.setLength(self.m_iPayloadSize);

                // reading next incoming packet, recvfrom returns -1 is nothing has been received
                if (self.m_pChannel.recvfrom(ref addr, unit.m_Packet) < 0)
                    goto TIMER_CHECK;

                id = unit.m_Packet.GetId();

                // ID 0 is for connection request, which should be passed to the listening socket or rendezvous sockets
                if (0 == id)
                {
                    if (null != self.m_pListener)
                        self.m_pListener.listen(addr, unit.m_Packet);
                    else if (null != (u = self.m_pRendezvousQueue.retrieve(addr, ref id)))
                    {
                        // asynchronous connect: call connect here
                        // otherwise wait for the UDT socket to retrieve this packet
                        if (!u.m_bSynRecving)
                            u.connect(unit.m_Packet);
                        else
                        {
                            Packet newPacket = new Packet();
                            newPacket.Clone(unit.m_Packet);
                            self.storePkt(id, newPacket);
                        }
                    }
                }
                else if (id > 0)
                {
                    if (self.m_hash.TryGetValue(id, out u))
                    {
                        if (addr.Equals(u.m_pPeerAddr))
                        {
                            if (u.m_bConnected && !u.m_bBroken && !u.m_bClosing)
                            {
                                if (0 == unit.m_Packet.getFlag())
                                    u.processData(unit);
                                else
                                    u.processCtrl(unit.m_Packet);

                                u.checkTimers();
                                self.m_pRcvUList.update(u);
                            }
                        }
                    }
                    else if (null != (u = self.m_pRendezvousQueue.retrieve(addr, ref id)))
                    {
                        if (!u.m_bSynRecving)
                            u.connect(unit.m_Packet);
                        else
                        {
                            Packet newPacket = new Packet();
                            newPacket.Clone(unit.m_Packet);
                            self.storePkt(id, newPacket);
                        }
                    }
                }

            TIMER_CHECK:
                // take care of the timing event for all UDT sockets

                ulong currtime = Timer.rdtsc();

                ulong ctime = currtime - 100000 * Timer.getCPUFrequency();
                for (int i = 0; i < self.m_pRcvUList.m_nodeList.Count; ++i)
                {
                    RNode ul = self.m_pRcvUList.m_nodeList[i];
                    if (ul.m_llTimeStamp >= ctime)
                        break;

                    u = ul.m_pUDT;

                    if (u.m_bConnected && !u.m_bBroken && !u.m_bClosing)
                    {
                        u.checkTimers();
                        self.m_pRcvUList.update(u);
                    }
                    else
                    {
                        // the socket must be removed from Hash table first, then RcvUList
                        self.m_hash.Remove(u.m_SocketID);
                        self.m_pRcvUList.remove(u);
                        u.m_pRNode.m_bOnList = false;
                    }
                }

                // Check connection requests status for all sockets in the RendezvousQueue.
                self.m_pRendezvousQueue.updateConnStatus();
            }


            self.m_ExitCond.Set();
        }

        public int recvfrom(int id, Packet packet)
        {
            bool gotLock = false;
            Monitor.Enter(m_PassLock, ref gotLock);

            Queue<Packet> packetQueue;
            if (!m_mBuffer.TryGetValue(id, out packetQueue))
            {
                if (gotLock)
                    Monitor.Exit(m_PassLock);
                m_PassCond.WaitOne(1000);

                lock (m_PassLock)
                {

                    if (!m_mBuffer.TryGetValue(id, out packetQueue))
                    {
                        packet.setLength(-1);
                        return -1;
                    }
                }
            }

            if (gotLock && Monitor.IsEntered(m_PassLock))
                Monitor.Exit(m_PassLock);

            // retrieve the earliest packet
            Packet newpkt = packetQueue.Peek();

            if (packet.getLength() < newpkt.getLength())
            {
                packet.setLength(-1);
                return -1;
            }

            // copy packet content

            packet.Clone(newpkt);

            packetQueue.Dequeue();
            if (packetQueue.Count == 0)
            {
                lock (m_PassLock)
                {
                    m_mBuffer.Remove(id);
                }
            }

            return packet.getLength();
        }

        public int setListener(UDT u)
        {
            lock (m_LSLock)
            {

                if (null != m_pListener)
                    return -1;

                m_pListener = u;
                return 0;
            }
        }

        public void removeListener(UDT u)
        {
            lock (m_LSLock)
            {
                if (u == m_pListener)
                    m_pListener = null;
            }
        }

        public void registerConnector(int id, UDT u, AddressFamily ipv, IPEndPoint addr, ulong ttl)
        {
            m_pRendezvousQueue.insert(id, u, ipv, addr, ttl);
        }

        public void removeConnector(int id)
        {
            m_pRendezvousQueue.remove(id);
            lock (m_PassLock)
            {
                m_mBuffer.Remove(id);
            }
        }

        public void setNewEntry(UDT u)
        {
            lock (m_IDLock)
            {
                m_vNewEntry.Add(u);
            }
        }

        bool ifNewEntry()
        {
            return !(m_vNewEntry.Count == 0);
        }

        UDT getNewEntry()
        {
            lock (m_IDLock)
            {
                if (m_vNewEntry.Count == 0)
                    return null;

                UDT u = m_vNewEntry[0];
                m_vNewEntry.RemoveAt(0);
                return u;
            }
        }

        void storePkt(int id, Packet pkt)
        {
            lock (m_PassLock)
            {
                Queue<Packet> packetQueue;
                if (!m_mBuffer.TryGetValue(id, out packetQueue))
                {
                    packetQueue = new Queue<Packet>();
                    packetQueue.Enqueue(pkt);
                    m_mBuffer.Add(id, packetQueue);

                    m_PassCond.Set();
                }
                else
                {
                    //avoid storing too many packets, in case of malfunction or attack
                    if (packetQueue.Count > 16)
                        return;

                    packetQueue.Enqueue(pkt);
                }
            }
        }
    }
}
