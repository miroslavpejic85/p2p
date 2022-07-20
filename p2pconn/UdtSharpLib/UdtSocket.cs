using System;
using System.Net;
using System.Net.Sockets;

namespace UdtSharp
{
    public class UdtSocket
    {
        public UdtSocket(AddressFamily addressFamily, SocketType socketType)
        {
            UDT.s_UDTUnited.startup();

            try
            {
                mSocketId = UDT.s_UDTUnited.newSocket(addressFamily, socketType);
                mLocalEndPoint = new IPEndPoint(IPAddress.Any, 0);
            }
            catch (UdtException udtException)
            {
                throw new Exception($"Problem when initializing new socket type {socketType}", udtException);
            }
        }

        public int Bind(Socket socket)
        {
            try
            {
                int status = UDT.s_UDTUnited.bind(mSocketId, socket);
                mLocalEndPoint = (IPEndPoint)socket.LocalEndPoint;
                return status;
            }
            catch (UdtException udtException)
            {
                throw new Exception("Problem when binding to existing socket", udtException);
            }
        }

        public int Bind(IPEndPoint serverAddress)
        {
            try
            {
                int status = UDT.s_UDTUnited.bind(mSocketId, serverAddress);
                mLocalEndPoint = serverAddress;
                return status;
            }
            catch (UdtException udtException)
            {
                throw new Exception($"Problem when binding to server address {serverAddress}", udtException);
            }
        }

        public int Listen(int maxConnections)
        {
            try
            {
                return UDT.s_UDTUnited.listen(mSocketId, maxConnections);
            }
            catch (UdtException udtException)
            {
                throw new Exception($"Problem when listening with {maxConnections}", udtException);
            }
        }

        public UdtSocket Accept()
        {
            try
            {
                IPEndPoint clientEndPoint = null;
                int clientSocketId = UDT.s_UDTUnited.accept(mSocketId, ref clientEndPoint);

                return new UdtSocket(clientSocketId, clientEndPoint, mLocalEndPoint);
            }
            catch (UdtException udtException)
            {
                throw new Exception("Problem when accepting socket", udtException);
            }
        }

        public int Connect(IPEndPoint server)
        {
            try
            {
                int status = UDT.s_UDTUnited.connect(mSocketId, server);
                mRemoteEndPoint = server;
                return status;
            }
            catch (UdtException udtException)
            {
                throw new Exception($"Problem when connecting to server endpoint {server}", udtException);
            }
        }

        public bool IsConnected()
        {
            return UDT.s_UDTUnited.getStatus(mSocketId) == UDTSTATUS.CONNECTED;
        }

        public int Send(byte[] data, int offset, int length)
        {
            try
            {
                UDT udt = UDT.s_UDTUnited.lookup(mSocketId);
                return udt.send(data, offset, length);
            }
            catch (UdtException udtException)
            {
                throw new Exception("Problem when sending data", udtException);
            }
        }

        public int Receive(byte[] data, int offset, int length)
        {
            try
            {
                UDT udt = UDT.s_UDTUnited.lookup(mSocketId);
                return udt.recv(data, offset, length);
            }
            catch (UdtException udtException)
            {
                throw new Exception("Problem when receiving data", udtException);
            }
        }

        public int Close()
        {
            try
            {
                return UDT.s_UDTUnited.close(mSocketId);
            }
            catch (UdtException udtException)
            {
                throw new Exception("Problem when closing socket", udtException);
            }
        }

        public IPEndPoint LocalEndPoint { get { return mLocalEndPoint; } }
        public IPEndPoint RemoteEndPoint { get { return mRemoteEndPoint; } }

        UdtSocket(int iSocketID, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            mSocketId = iSocketID;
            mLocalEndPoint = localEndPoint;
            mRemoteEndPoint = remoteEndPoint;
        }

        int mSocketId;
        IPEndPoint mLocalEndPoint;
        IPEndPoint mRemoteEndPoint;

        delegate int ReceiveDelegate(byte[] buffer, int offset, int count);
        ReceiveDelegate mReceiveDelegate = null;
        delegate int SendDelegate(byte[] buffer, int offset, int count);
        SendDelegate mSendDelegate = null;
    }
}

