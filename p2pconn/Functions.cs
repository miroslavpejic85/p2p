namespace p2pconn
{
    static class Functions
    {
        #region "encode decode string"
        //Encode String
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        //Decode String
        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
        #endregion
    
        #region "byte-s s-byte"
        //byte-to-string
        public static string ByteToString(byte[] byteArray)
        {
            string result = System.Text.Encoding.UTF8.GetString(byteArray);
            return result;
        }

        //string-to-byte
        public static byte[] StringToByte(string bytes)
        {
            byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(bytes);
            return utf8Bytes;
        }
        #endregion
   
        #region "get frame size"
        // get sizes
        public static string FormatFileSize(long fileSizeBytes)
        {
            string[] strArray = new string[] 
            {
                "Byte/s",
                "KB/s",
                "MB/s",
                "GB/s"
            };
            decimal num = new decimal(fileSizeBytes);
            int index = 0;
            while ((decimal.Compare(num, 1024) > 0))
            {
                num = decimal.Round(decimal.Divide(num, 1024), 2);
                index += 1;
                if ((index >= (strArray.Length - 1)))
                {
                    break;
                }
            }
            return (num.ToString() + " " + strArray[index]);
        }
    }
    #endregion
}
