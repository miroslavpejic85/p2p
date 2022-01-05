using p2pconn;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using UdtSharp;
using Cryptography;

namespace p2pcopy
{
    public static class SenderReceiver
    {
        #region "declare"
        public static bool isConnected = false;
        public static UdtSocket client = null;
        public static UdtNetworkStream netStream;
        public static BinaryWriter swriter;
        static BinaryReader sreader;
        public static Bitmap _decodeBitmap;
        public static Rectangle[] rect;
        private static int FPS = 0;
        private static Stopwatch sfps = Stopwatch.StartNew();
        private static Stopwatch RenderSW = Stopwatch.StartNew();
        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);
        private static string ENCRYPTIONKEY = "SPXGPU3UPSIWSX5NLKFTIVN5RHXZW1F2H8CC2ORE";
        static readonly Aes256 aes = new Aes256(ENCRYPTIONKEY);
        #endregion

        #region "recive data <======"
        static internal void Run(Object conn)
        {

            client = (UdtSocket)conn;
            netStream = new UdtNetworkStream(client); 
            sreader = new BinaryReader(netStream); 

            Rectangle rect = Screen.AllScreens[RemoteDesktop.MonitorIndex].WorkingArea;

            SendMessage("peer|" + GlobalVariables.Root.myname + "|" +
                                    Screen.PrimaryScreen.Bounds.Width + "|" +
                                    Screen.PrimaryScreen.Bounds.Height);

            while (isConnected && netStream.CanRead) 
            {
                try
                {
                    string message = sreader.ReadString(); // err

                    // aes 256 bit decode
                    message = aes.Decrypt(message);

                    if (message != null && message.Length > 0)
                    {

                        string[] words = message.Split('|');
                        switch (words[0])
                        {
                            case "peer":
                                GlobalVariables.Root.peername = words[1];
                                GlobalVariables.Root.Text = "Connected to => " + words[1];
                                RemoteDesktop.RScreenWidth = int.Parse(words[2]);
                                RemoteDesktop.RScreenHeight = int.Parse(words[3]);
                                break;

                            case "c":
                                GlobalVariables.Root.Writetxtchatrom("Green", words[1]);
                                break;

                            case "openp2pDesktop":
                                RemoteDesktop.StartDesktop();
                                GlobalVariables.Root.EnableButtonRdp(false);
                                break;

                            case "ds":
                                if (RemoteDesktop.DesktopRunning == true)
                                {
                                    RemoteDesktop.DesktopSpeed = Int32.Parse(words[1]);
                                    RemoteDesktop.stream.FrameInterval = RemoteDesktop.DesktopSpeed;
                                }
                                break;

                            // desktop_streaming
                            case "b":
                                try
                                {
                                    GlobalVariables.p2pDesktop.ReceiveMouseCursor(words[1].ToString()); 
                                    int value;
                                    if (int.TryParse(words[2], out value))
                                    {
                                        int toRecv = Convert.ToInt32(words[2]);
                                        byte[] tempBytes = sreader.ReadBytes(toRecv);
                                        if (tempBytes != null && tempBytes.Length > 0)
                                        {
                                            // GlobalVariables.Root.Writetxtchatrom("Green", "compressed: " + tempBytes.Length);
                                            GlobalVariables.Root.WriteKB("FSIZE: " + Functions.FormatFileSize(tempBytes.Length));
                                            Bitmap decoded = RemoteDesktop.UnsafeMotionCodec.DecodeData(new MemoryStream(QuickLZ.Decompress(tempBytes)));
                                            if (RenderSW.ElapsedMilliseconds >= (1000 / 20))
                                            {
                                                GlobalVariables.p2pDesktop.DecodeImage1((Bitmap)decoded.Clone());
                                                RenderSW = Stopwatch.StartNew();
                                            }
                                            FPS++;
                                            if (sfps.ElapsedMilliseconds >= 1000)
                                            {
                                                GlobalVariables.Root.WriteFPS("FPS: " + FPS);
                                                FPS = 0;
                                                sfps = Stopwatch.StartNew();
                                            }
                                            Array.Clear(tempBytes, 0, tempBytes.Length);
                                            GC.Collect();
                                        }
                                    }   
                                }
                                catch (Exception ex)
                                {
                                    GlobalVariables.Root.Writetxtchatrom("Red", "Receive Streaming: " + ex.ToString());
                                    SendMessage("endp2pDesktop|");
                                    GC.Collect();
                                }
                                break;

                            // mouse_control
                            case "m":
                                try
                                {
                                    InputControl obj1 = new InputControl();
                                    obj1.MoveMouse(int.Parse(words[1]), int.Parse(words[2]));
                                }
                                catch (Exception ex)
                                {
                                    GlobalVariables.Root.Writetxtchatrom("Red", "Mouse move: " + ex.Message);
                                }
                                break;

                            case "mw":
                                try
                                {
                                    InputControl obj3 = new InputControl();
                                    obj3.MouseWheel(int.Parse(words[1]));
                                }
                                catch (Exception ex)
                                {
                                    GlobalVariables.Root.Writetxtchatrom("Red", "Mouse weel: " + ex.Message);
                                }
                                break;

                            // keyboard_control
                            case "ku":
                                try
                                {
                                    InputControl obj4 = new InputControl();
                                    obj4.SendKeystroke(Convert.ToByte(words[1]), Convert.ToByte(MapVirtualKey(Convert.ToUInt32(words[1]), 0)), false, false);
                                }
                                catch (Exception ex)
                                {
                                    GlobalVariables.Root.Writetxtchatrom("Red", "Key up: " + ex.Message.ToString());
                                }
                                break;

                            case "kd":
                                try
                                {
                                    InputControl obj5 = new InputControl();
                                    obj5.SendKeystroke(Convert.ToByte(words[1]), Convert.ToByte(MapVirtualKey(Convert.ToUInt32(words[1]), 0)), true, false);
                                }
                                catch (Exception ex)
                                {
                                    GlobalVariables.Root.Writetxtchatrom("Red", "key down: " + ex.Message.ToString());
                                }
                                break;

                            case "endp2pDesktop":
                                RemoteDesktop.StopDesktop();
                                GlobalVariables.Root.EnableButtonRdp(true);
                                break;

                            case "end":
                                if (RemoteDesktop.DesktopRunning == true)
                                {
                                    RemoteDesktop.StopDesktop();
                                }
                                netStream.Close();
                                isConnected = false;
                                Process.GetCurrentProcess().Kill();
                                break;

                        }

                        if (words[0] == "mu" || words[0] == "md")
                        {
                            try
                            {
                                InputControl obj2 = new InputControl();
                                bool isleft = false;
                                if (int.Parse(words[3]) == 0)
                                    isleft = true;
                            
                                if (words[4] == "MouseUp")
                                {
                                    obj2.PressOrReleaseMouseButton(false, isleft, int.Parse(words[1]), int.Parse(words[2]));
                                }
                                else
                                {
                                    obj2.PressOrReleaseMouseButton(true, isleft, int.Parse(words[1]), int.Parse(words[2]));
                                }
                            }
                            catch (Exception ex)
                            {
                                GlobalVariables.Root.Writetxtchatrom("Red", "Mouse Up/Down: " + ex.Message);
                            }
                        } 
                    }

                }
                catch (IOException e)
                {
                    GlobalVariables.Root.Writetxtchatrom("Red", "Get data: " + e.Message);
                }
            }
        }
    #endregion

        #region "send data =====>"
    static internal void SendMessage(string message)
        {
            try
            {
                if (isConnected && netStream.CanWrite)
                {
                    // aes 256 bit encode
                    message = aes.Encrypt(message);
                    swriter = new BinaryWriter(netStream);
                    swriter.Write(message);
                    swriter.Flush();
                }
            }
            catch (Exception ex)
            {
                GlobalVariables.Root.Writetxtchatrom("Red", "SendMessage: " + ex.Message);
            }
        }
        #endregion

    }
}
