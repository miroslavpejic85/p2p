using p2pcopy;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace p2pconn
{
    public partial class pDesktop : Form
    {
        #region "declare"
        private Size Sz;
        private bool Fullscreen = false;
        private Point LocalMousePosition;
        private int cursx;
        private int cursy;
        private int xx;
        private int yy;
        private int Boton = 0;
        public bool ScreenResize = false;
        #endregion
        #region "p2pDesktop load - close"
        public pDesktop()
        {
            GlobalVariables.p2pDesktop = this;
            InitializeComponent();
            p2pScreen.MouseWheel += new MouseEventHandler(p2pScreen_MouseWheel);
            Control.CheckForIllegalCrossThreadCalls = false;
        }
        private void pDesktop_Load(object sender, EventArgs e)
        {

            this.Sz = new Size(RemoteDesktop.RScreenWidth, RemoteDesktop.RScreenHeight);
            Size returnValue = default(Size);
            returnValue = this.SizeFromClientSize(Sz);
            {
                if (RemoteDesktop.RScreenWidth < Screen.PrimaryScreen.Bounds.Width & RemoteDesktop.RScreenHeight < Screen.PrimaryScreen.Bounds.Height - 50)
                {
                    this.panel1.AutoScroll = false;
                    this.StartPosition = FormStartPosition.CenterScreen;
                    this.Width = RemoteDesktop.RScreenWidth + returnValue.Width - this.Sz.Width;
                    this.Height = RemoteDesktop.RScreenHeight + returnValue.Height - this.Sz.Height;
                    System.Drawing.Rectangle Screen = default(System.Drawing.Rectangle);
                    Screen = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
                    this.Top = (Screen.Height / 2) - (this.Height / 2);
                    this.Left = (Screen.Width / 2) - (this.Width / 2);
                    this.MaximumSize = new System.Drawing.Size(RemoteDesktop.RScreenWidth, RemoteDesktop.RScreenHeight + 50);
                    Fullscreen = true;
                }
            }
    
            p2pScreen.Width = RemoteDesktop.RScreenWidth;
            p2pScreen.Height = RemoteDesktop.RScreenHeight;
            p2pScreen.Image = new Bitmap(RemoteDesktop.RScreenWidth, RemoteDesktop.RScreenHeight); //err

            this.Text = "p2p_Desktop: " + RemoteDesktop.RScreenWidth + " x " + RemoteDesktop.RScreenHeight;
            RemoteDesktop.DesktopRunning = true;
            GlobalVariables.Root.EnableStreach(true);
            GlobalVariables.Root.EnableDSpeed(true);
            GlobalVariables.Root.Writetxtchatrom("Green", "Start to receive peer desktop stream");
        }
        private void pDesktop_FormClosing(object sender, FormClosingEventArgs e)
        {
            RemoteDesktop.DesktopRunning = false;
            GlobalVariables.Root.EnableButtonRdp(true);
            SenderReceiver.SendMessage("endp2pDesktop|");
            GlobalVariables.Root.EnableStreach(false);
            GlobalVariables.Root.EnableDSpeed(false);
            GlobalVariables.Root.Writetxtchatrom("Green", "Stop to receive peer desktop stream");
        }
        #endregion
        #region "check screen size"
        public void ControlStreach(bool ScreenResize)
        {
            if (ScreenResize == true)
            {
                this.panel1.AutoScroll = false;
                this.p2pScreen.Dock = DockStyle.Fill;
                this.p2pScreen.SizeMode = PictureBoxSizeMode.StretchImage;
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;

                if (Fullscreen == false)
                {
                    this.Width = Convert.ToInt32(Sz.Width / 2);
                    this.Height = Convert.ToInt32(Sz.Height / 2);
                }
            }
            else
            {
                this.panel1.AutoScroll = true;
                this.p2pScreen.Dock = DockStyle.None;
                this.p2pScreen.SizeMode = PictureBoxSizeMode.Normal;
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            }
        }
        #endregion
        #region "receive cursor to string"
        private delegate void ReceiveCursor(string curs);
        public void ReceiveMouseCursor(string curs)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new ReceiveCursor(this.ReceiveMouseCursor), new object[] { curs });
                }
                else
                {
                    Cursor hcursor = null;
                    //GlobalVariables.Root.Writetxtchatrom("Green", curs);
                    switch (curs)
                    {
                        case "[Cursor: Default]": hcursor = Cursors.Default; break;
                        case "[Cursor: IBeam]": hcursor = Cursors.IBeam;  break;
                        case "[Cursor: WaitCursor]":  hcursor = Cursors.WaitCursor; break;
                        case "[Cursor: SizeNWSE]": hcursor = Cursors.SizeNWSE; break;
                        case "[Cursor: SizeNESW]": hcursor = Cursors.SizeNESW; break;
                        case "[Cursor: SizeWE]": hcursor = Cursors.SizeWE; break;
                        case "[Cursor: SizeNS]": hcursor = Cursors.SizeNS; break;
                        case "[Cursor: SizeAll]": hcursor = Cursors.SizeAll; break;
                        case "[Cursor: Hand]": hcursor = Cursors.Hand; break;
                        case "[Cursor: AppStarting]": hcursor = Cursors.AppStarting; break;
                        case "[Cursor: Arrow]": hcursor = Cursors.Arrow; break;
                        case "[Cursor: Cross]": hcursor = Cursors.Cross; break;
                        case "[Cursor: UpArrow]": hcursor = Cursors.UpArrow; break;
                        case "[Cursor: Help]": hcursor = Cursors.Help; break;
                        case "[Cursor: HSplit]": hcursor = Cursors.HSplit; break;
                        case "[Cursor: VSplit]": hcursor = Cursors.VSplit; break;
                        case "[Cursor: No]": hcursor = Cursors.No; break;
                        default: hcursor = Cursors.Default; break;
                    }
                    this.p2pScreen.Cursor = hcursor;
                }
            }
            catch
            {
            }
        }
        #endregion
        #region "print remote desktop img"
        private delegate void PlaceDeskImageIn(Image dImg);
        public void DecodeImage1(Image img)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new PlaceDeskImageIn(this.DecodeImage1), new object[] { img });
                }
                else
                {
                    Bitmap image = (Bitmap)img.Clone();
                    this.p2pScreen.Image = (Image)image.Clone();
                }
            }
            catch
            {
            }
        }
        #endregion
        #region "mouse and keyboard control"
        private void p2pScreen_MouseMove(object sender, MouseEventArgs e)
        {
            //ok
            try
            {
                LocalMousePosition = p2pScreen.PointToClient(Cursor.Position);
                cursx = LocalMousePosition.X;
                cursy = LocalMousePosition.Y;
                float vertScaleRatio = (float)RemoteDesktop.RScreenWidth / (float)p2pScreen.Size.Width;
                float horzScaleRatio = (float)RemoteDesktop.RScreenHeight / (float)p2pScreen.Size.Height;
                xx = (int)(cursx * vertScaleRatio);
                yy = (int)(cursy * horzScaleRatio); 
                SenderReceiver.SendMessage("m|" + xx + "|" + yy);
                Thread.Sleep(100);
            }
            catch
            {
            }
        }
        private void p2pScreen_MouseUp(object sender, MouseEventArgs e)
        {
            //ok
            try
            {
                switch (e.Button)
                {
                    case MouseButtons.Left:
                        Boton = 0;
                        break;
                    case MouseButtons.Right:
                        Boton = 1;
                        break;
                    case MouseButtons.Middle:
                        Boton = 2;
                        break;
                }
                SenderReceiver.SendMessage("mu|" + xx + "|" + yy + "|" + Boton + "|" + "MouseUp");
            }
            catch
            {
            }
        }
        private void p2pScreen_MouseDown(object sender, MouseEventArgs e)
        {
            //ok
            try
            {
                switch (e.Button)
                {
                    case MouseButtons.Left:
                        Boton = 0;
                        break;
                    case MouseButtons.Right:
                        Boton = 1;
                        break;
                    case MouseButtons.Middle:
                        Boton = 2;
                        break;
                }
                SenderReceiver.SendMessage("md|" + xx + "|" + yy + "|" + Boton + "|" + "MouseDown");
            }
            catch
            {
            }
        }

        void p2pScreen_MouseWheel(object sender, MouseEventArgs e)
        {
            //ok
            try
            {
                p2pScreen.Focus();
                SenderReceiver.SendMessage("mw|" + e.Delta);
            }
            catch
            {
            }
        }
        private void pDesktop_KeyDown(object sender, KeyEventArgs e)
        {
            //ok
            try
            {
                e.Handled = true;
                SenderReceiver.SendMessage("kd|" + e.KeyValue);
            }
            catch
            {
            }
        }

        private void pDesktop_KeyUp(object sender, KeyEventArgs e)
        {
            //ok
            try
            {
                e.Handled = true;
                SenderReceiver.SendMessage("ku|" + e.KeyValue);
            }
            catch
            {
            }
        }
    }
    #endregion
}
