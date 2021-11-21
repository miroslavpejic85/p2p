using StreamLibrary.src;
using System.Drawing;
using System.IO;

namespace StreamLibrary
{
    public abstract class IVideoCodec
    {
        public delegate void VideoCodeProgress(Stream stream, Rectangle[] MotionChanges);
        public delegate void VideoDecodeProgress(Bitmap bitmap);
        public delegate void VideoDebugScanningDelegate(Rectangle ScanArea);

        public abstract event VideoCodeProgress onVideoStreamCoding;
        public abstract event VideoDecodeProgress onVideoStreamDecoding;
        public abstract event VideoDebugScanningDelegate onCodeDebugScan;
        public abstract event VideoDebugScanningDelegate onDecodeDebugScan;
        protected JpgCompression jpgCompression;
        public abstract ulong CachedSize { get; internal set; }
        public int ImageQuality { get; set; }

        public IVideoCodec(int ImageQuality = 100)
        {
            this.jpgCompression = new JpgCompression(ImageQuality);
            this.ImageQuality = ImageQuality;
        }

        public abstract int BufferCount { get; }
        public abstract CodecOption CodecOptions { get; }
        public abstract void CodeImage(Bitmap bitmap, Stream outStream);
        public abstract Bitmap DecodeData(Stream inStream);
    }
}