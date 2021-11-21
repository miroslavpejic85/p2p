using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace StreamLibrary.src
{
    public class LzwCompression
    {
        private EncoderParameter parameter;
        private ImageCodecInfo encoderInfo;
        private EncoderParameters encoderParams;

        public LzwCompression(int Quality)
        {
            this.parameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)Quality);
            this.encoderInfo = GetEncoderInfo("image/jpeg");
            this.encoderParams = new EncoderParameters(2);
            this.encoderParams.Param[0] = parameter;
            this.encoderParams.Param[1] = new EncoderParameter(System.Drawing.Imaging.Encoder.Compression, (long)EncoderValue.CompressionLZW);
        }

        public byte[] Compress(Bitmap bmp, byte[] AdditionInfo = null)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                if (AdditionInfo != null)
                    stream.Write(AdditionInfo, 0, AdditionInfo.Length);
                bmp.Save(stream, encoderInfo, encoderParams);
                return stream.ToArray();
            }
        }
        public void Compress(Bitmap bmp, Stream stream, byte[] AdditionInfo = null)
        {
            if (AdditionInfo != null)
                stream.Write(AdditionInfo, 0, AdditionInfo.Length);
            bmp.Save(stream, encoderInfo, encoderParams);
        }

        private ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            ImageCodecInfo[] imageEncoders = ImageCodecInfo.GetImageEncoders();
            for (int i = 0; i < imageEncoders.Length; i++)
            {
                if (imageEncoders[i].MimeType == mimeType)
                {
                    return imageEncoders[i];
                }
            }
            return null;
        }
    }
}