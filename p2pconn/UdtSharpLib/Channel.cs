using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace UdtSharp
{
    public class Channel
    {
        AddressFamily m_iIPversion;      // IP version

        Socket m_socket;                 // socket descriptor

        int m_iSndBufSize;               // UDP sending buffer size
        int m_iRcvBufSize;

        public Channel()
        {
            m_iIPversion = AddressFamily.InterNetwork;
            m_iSndBufSize = 65536;
            m_iRcvBufSize = 65536;
        }

        public Channel(AddressFamily addressFamily)
        {
            m_iIPversion = addressFamily;
            m_iSndBufSize = 65536;
            m_iRcvBufSize = 65536;
        }

        public void open(IPEndPoint addr)
        {
            // construct a socket
            try
            {
                m_socket = new Socket(m_iIPversion, SocketType.Dgram, ProtocolType.Udp);
            }
            catch (SocketException e)
            {
                throw new UdtException(1, 0, e.ErrorCode);
            }

            if (null != addr)
            {
                try
                {
                    m_socket.Bind(addr);
                }
                catch (SocketException e)
                {
                    throw new UdtException(1, 3, e.ErrorCode);
                }
            }
            else
            {
                try
                {
                    m_socket.Bind(new IPEndPoint(IPAddress.Any, 0));
                }
                catch (SocketException e)
                {
                    throw new UdtException(1, 3, e.ErrorCode);
                }
            }

            setUDPSockOpt();
        }

        public void open(Socket udpsock)
        {
            m_socket = udpsock;
            setUDPSockOpt();
        }

        void setUDPSockOpt()
        {
            bool isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

            bool isBSD =
#if NETSTANDARD2_0
                false;
#else
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
#endif

            if (isMac || isBSD)
            {
                // BSD system will fail setsockopt if the requested buffer size exceeds system maximum value
                int maxsize = 64000;
                m_socket.ReceiveBufferSize = maxsize;
                m_socket.SendBufferSize = maxsize;
            }
            else
            {
                m_socket.ReceiveBufferSize = m_iRcvBufSize;
                m_socket.SendBufferSize = m_iSndBufSize;
            }
        }

        public void close()
        {
            m_socket.Close();
        }

        int getSndBufSize()
        {
            m_iSndBufSize = (int)m_socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer);
            return m_iSndBufSize;
        }

        int getRcvBufSize()
        {
            m_iRcvBufSize = (int)m_socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);
            return m_iRcvBufSize;
        }

        public void setSndBufSize(int size)
        {
            m_iSndBufSize = size;
        }

        public void setRcvBufSize(int size)
        {
            m_iRcvBufSize = size;
        }

        public void getSockAddr(ref IPEndPoint addr)
        {
            addr = (IPEndPoint)m_socket.LocalEndPoint;
        }

        void getPeerAddr(ref IPEndPoint addr)
        {
            addr = (IPEndPoint)m_socket.RemoteEndPoint;
        }

        public int sendto(IPEndPoint addr, Packet packet)
        {
            TraceSend(addr, packet);

            // convert control information into network order
            packet.ConvertControlInfoToNetworkOrder();

            // convert packet header into network order
            packet.ConvertHeaderToNetworkOrder();

            byte[] data = packet.GetBytes();
            int res = m_socket.SendTo(data, addr);

            // convert back into local host order
            packet.ConvertHeaderToHostOrder();
            packet.ConvertControlInfoToHostOrder();

            return res;
        }

        void TraceSend(IPEndPoint destination, Packet packet)
        {
            return;
            StringBuilder sb = new StringBuilder();
            sb.Append(DateTime.Now.ToString("hh:mm:ss.fff"));
            sb.AppendFormat(" SND {0} => {1}", m_socket.LocalEndPoint, destination);
            sb.AppendLine();
            sb.AppendLine(packet.ToString());
            sb.AppendLine();
            Console.WriteLine(sb.ToString());
        }

        void TraceRecv(IPEndPoint source, Packet packet)
        {
            return;
            StringBuilder sb = new StringBuilder();
            sb.Append(DateTime.Now.ToString("hh:mm:ss.fff"));
            sb.AppendFormat(" RCV {0} <= {1}", m_socket.LocalEndPoint, source);
            sb.AppendLine();
            sb.AppendLine(packet.ToString());
            sb.AppendLine();
            Console.WriteLine(sb.ToString());
        }

        public int recvfrom(ref IPEndPoint addr, Packet packet)
        {
            try
            {
            if (!m_socket.Poll(10000, SelectMode.SelectRead))
                return -1;
            }
            catch (SocketException sex)
            {
                return -1;
            }
            catch (ObjectDisposedException odex)
            {
                return -1;
            }

            byte[] bytes = new byte[Packet.m_iPktHdrSize + packet.getLength()];

            EndPoint source = addr;

            int res;
            try
            {
                res = m_socket.ReceiveFrom(bytes, ref source);
            }
            catch (SocketException sex)
            {
                return -1;
            }
            catch (ObjectDisposedException odex)
            {
                return -1;
            }

            addr = source as IPEndPoint;

            if (res <= 0)
            {
                return -1;
            }

            bool success = packet.SetHeaderAndDataFromBytes(bytes, res);
            if (!success)
                return -1;

            // convert back into local host order
            packet.ConvertHeaderToHostOrder();
            packet.ConvertControlInfoToHostOrder();

            TraceRecv(addr, packet);

            return packet.getLength();
        }
    }
}
