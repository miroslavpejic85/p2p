using System;
using System.IO;

namespace UdtSharp
{
    public class UdtNetworkStream : Stream
    {
        public UdtNetworkStream(UdtSocket socket)
        {
            mSocket = socket;
        }

        public override bool CanRead { get { return true; } }

        public override bool CanSeek { get { return false; } }

        public override bool CanWrite { get { return true; } }

        public override long Length { get { throw new NotImplementedException(); } }

        public override long Position { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return mSocket.Receive(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            mSocket.Send(buffer, offset, count);
        }

        UdtSocket mSocket;
    }
}
