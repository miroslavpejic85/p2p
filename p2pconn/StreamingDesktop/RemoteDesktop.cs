using p2pcopy;
using StreamLibrary;
using StreamLibrary.UnsafeCodecs;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using AForge.Video;
using static p2pconn.Win32Stuff;
using System.Diagnostics;

namespace p2pconn
{
    public static class RemoteDesktop
    {
        #region " declare"
        public static int MonitorIndex = 0; // 1 2.. more monitors
        public static Screen Src = Screen.AllScreens[MonitorIndex]; // 0
        public static IUnsafeCodec UnsafeMotionCodec;
        public static ScreenCaptureStream stream;
        public static BitmapData bmpData = null;
        public static Bitmap DesktopImage = null;
        public static int RScreenWidth = 0;
        public static int RScreenHeight = 0;
        public static int DesktopQuality = 80;
        public static int DesktopSpeed = 100;
        public static bool DesktopRunning = false;
        public static bool AutoSpeed = true;
        public static bool ShowCursor = false;
        public static bool CursorToString = true;
        private static string mode = "[Cursor: Default]";
        private static Stopwatch time = Stopwatch.StartNew(); // test time elapsed
        #endregion
        #region " Start Stop Remote Desktop"
        public static void StartDesktop()
        {
            try
            {
                if (DesktopRunning == true)
                return;
                UnsafeMotionCodec = new UnsafeStreamCodec(DesktopQuality, true);
                DesktopImage = null;
                startAForgeVideo();
                DesktopRunning = true;
                GlobalVariables.Root.Writetxtchatrom("Green", "Start Sharing Desktop");
            }
            catch (Exception ex)
            {
                GlobalVariables.Root.Writetxtchatrom("Red", "StartDesktop(): " +  ex.Message);
            }
        }

        private static void startAForgeVideo()
        {
            // create screen capture video source
            stream = new ScreenCaptureStream(Src.Bounds);

            //  set interval capture default 100ms
            stream.FrameInterval = DesktopSpeed; 

            // set NewFrame event handler
            stream.NewFrame += new NewFrameEventHandler(video_NewFrame);

            // sleep 1 sec
            Thread.Sleep(1000);

            // start the video source
            stream.Start();
        }
        public static void StopDesktop()
        {
            try
            {
                stopAForgeVideo();
                DesktopImage = null;
                DesktopRunning = false;
                GlobalVariables.Root.Writetxtchatrom("Green", "Stop Sharing Desktop " );
            }
            catch (Exception ex)
            {
                GlobalVariables.Root.Writetxtchatrom("Red", " StopDesktop() " + ex.Message);
            }
        }
        private static void stopAForgeVideo()
        {
            try
            {
                stream.NewFrame -= new NewFrameEventHandler(video_NewFrame);
                stream.SignalToStop();
            }
            catch (Exception ex)
            {
                GlobalVariables.Root.Writetxtchatrom("Red", "stopAForgeVideo: " + ex.Message);
            }
        }
        #endregion
        #region " Streaming Desktop"
        private static void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // time = Stopwatch.StartNew();
            ScreenCap = (Bitmap)eventArgs.Frame.Clone(); 
            try
                {
                    GetCursorState();
                    bmpData = ScreenCap.LockBits(new System.Drawing.Rectangle(0, 0, ScreenCap.Width, ScreenCap.Height),
                    ImageLockMode.ReadWrite, ScreenCap.PixelFormat);
                    // 10000000 allocate already enough memory to make it fast
                    using (MemoryStream MotionStream = new MemoryStream(100000000))
                    {
                        if (UnsafeMotionCodec == null) throw new Exception("StreamCodec can not be null.");
                        UnsafeMotionCodec.CodeImage(bmpData.Scan0,
                        new Rectangle(0, 0, ScreenCap.Width, ScreenCap.Height),
                        new Size(ScreenCap.Width, ScreenCap.Height),
                        ScreenCap.PixelFormat, MotionStream);
                        // 4 bytes = inactivity no motion detection
                        if (MotionStream.Length > 4)
                        {
                            // GlobalVariables.Root.Writetxtchatrom("Green", "Before no compressed: " + MotionStream.ToArray().Length);
                            byte[] tempBytes = QuickLZ.Compress(MotionStream.ToArray(), 3);  // Default 3 | Level 1 gives the fastest compression speed while level 3 gives the fastest decompression speed.
                            // GlobalVariables.Root.Writetxtchatrom("Green", "After compressed: " + tempBytes.Length);
                            SenderReceiver.SendMessage("b|" + Tipo + "|" + tempBytes.Length);
                            SenderReceiver.client.Send(tempBytes, 0, tempBytes.Length);
                            Array.Clear(tempBytes, 0, tempBytes.Length);
                        }
                    }
                    ScreenCap.UnlockBits(bmpData);
                    ScreenCap.Dispose();
                    GC.Collect();
                    // GlobalVariables.Root.Writetxtchatrom("Green", "time: " + time.ElapsedMilliseconds + " ms");
                    // time.Stop();
                }
                catch (Exception ex)
                {
                    GlobalVariables.Root.Writetxtchatrom("Red", "Stop Sharing Desktop: " + ex.Message);
                    StopDesktop();
                    GC.Collect();
                }
        }

    private static  Bitmap  ScreenCap
        {
            get { return DesktopImage; }
            set { DesktopImage = value; }
        }
        #endregion
        #region " Cursor To String"
        private static string Tipo
        {
            get {  return mode;  }
            set {  mode = value;  }
        }
        private static void  GetCursorState()
        {
            try
            {
                CURSORINFO pci;
                pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
                if (GetCursorInfo(out pci))
                {
                    IntPtr htcursor = pci.hCursor;
                    using (var micursor = new Cursor(htcursor))
                    {
                        Tipo = micursor.ToString();
                    }
                }
            }
            catch // (Exception ex)
            {
                Tipo = "";
                // ToDo some error here....
                //GlobalVariables.Root.Writetxtchatrom("Red", "GetCursorState(): " + ex.Message); 
            }
        }
        #endregion
    }
}
