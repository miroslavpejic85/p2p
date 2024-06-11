﻿using p2pcopy;
using StreamLibrary.UnsafeCodecs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using UdtSharp;
using System.IO;
using p2p.StunServer;
using p2p.p2p;

namespace p2pconn
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            GlobalVariables.Root = this;
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        static Random r = new Random();
        public string myname = "Me";
        public string peername = "Peer";
        bool bConnected = false;

        Thread thread;

        Socket socket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Dgram, ProtocolType.Udp);

        UdtSocket connection;

        private readonly string StunServersJson = Directory.GetCurrentDirectory() + "\\StunServers.json";

        private void Form1_Load(object sender, EventArgs e)
        {
            myname = Environment.UserName;
            dspeed.SelectedIndex = 2;
            CheckDataGridView();
            GetEndPoint();
        }

        private void CheckDataGridView()
        {
            if (File.Exists(StunServersJson))
            {
                // Read data from StunServersJson
                Array StunServers = StunServer.GetStunServersFromFile(StunServersJson);
                if (StunServers.Length > 0)
                {
                    int i = 0;
                    foreach (var _stun in StunServer.GetStunServersFromFile(StunServersJson))
                    {
                        this.dataGridView1.Rows.Insert(i, _stun.Server, _stun.Port);
                        i++;
                    }
                } else
                {
                    this.dataGridView1.Rows.Insert(0, "stun.l.google.com", 19302);
                }
            }
            else
            {
                // Save data from dataGridView to StunServersJson
                this.dataGridView1.Rows.Insert(0, "stun.l.google.com", 19302);
                SaveDataToJsoin();
            }
        }

        private void GetEndPoint()
        {
            P2pEndPoint p2pEndPoint = GetP2pEndPoint();

            if (p2pEndPoint == null)
                return;

            // txtmyHost.Text = Functions.Base64Encode(p2pEndPoint.External.ToString());
            txtmyHost.Text = p2pEndPoint.External.ToString();
            Clipboard.SetText(p2pEndPoint.External.ToString());
            string localendpoint = p2pEndPoint.Internal.ToString();
            string[] words = localendpoint.Split(':');
            // txtLocalHost.Text = Functions.Base64Encode(GetPhysicalIPAdress() + ":" + words[1]);
            txtLocalHost.Text = GetPhysicalIPAdress() + ":" + words[1];

            P2pEndPoint GetP2pEndPoint()
            {
                string storedPort = RegistryFuncs.GetFromRegistry(P2PKeys.LastPort);
                int newPort = r.Next(49152, 65535);
                if (!string.IsNullOrEmpty(storedPort))
                    newPort = Convert.ToInt32(storedPort);
                socket.Bind(new IPEndPoint(IPAddress.Any, newPort));
                P2pEndPoint tmpEndPoint = GetExternalEndPoint(socket);
                if (tmpEndPoint != null)
                    RegistryFuncs.SetToRegistry(P2PKeys.LastPort, newPort.ToString());
                return tmpEndPoint;
            }
        }

        private string GetPhysicalIPAdress()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                var addr = ni.GetIPProperties().GatewayAddresses.FirstOrDefault();
                if (addr != null && !addr.Address.ToString().Equals("0.0.0.0"))
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                return ip.Address.ToString();
                            }
                        }
                    }
                }
            }
            return String.Empty;
        }
        private void button2_Click(object sender, EventArgs e)
        {
            thread = new Thread(() => connect());
            thread.Start();
            txtRemoteIP.ReadOnly = true;
            button2.Enabled = false;
        }

        private void connect()
        {
            try
            {
                string remoteIp;
                int remotePort;

                // string peer = Functions.Base64Decode(txtRemoteIP.Text);
                string peer = txtRemoteIP.Text;
                if (string.IsNullOrEmpty(peer))
                {
                    MessageBox.Show("Invalid ip:port entered");
                    button2.Enabled = true;
                    return;
                }
                // try again to connect to external to "reopen" port
                GetExternalEndPoint(socket);
                ParseRemoteAddr(peer, out remoteIp, out remotePort);
                connection = PeerConnect(socket, remoteIp, remotePort);

                if (connection == null)
                {
                    label4.Invoke((MethodInvoker)(() => label4.ForeColor = Color.Red));
                    label4.Invoke((MethodInvoker)(() => label4.Text = "Failed to establish P2P conn to " + remoteIp)); 
                    return;
                }
                try
                {
                    Thread t = new Thread(new ParameterizedThreadStart(SenderReceiver.Run));
                    t.Start(connection);
                }
                catch (System.IO.IOException e1)
                {
                    r_chat.Invoke((MethodInvoker)(() => r_chat.ForeColor = Color.Red));
                    r_chat.Invoke((MethodInvoker)(() => r_chat.Text = "Connection Error: " + e1.Message));
                }
            }
            catch (System.IO.IOException e2)
            {
                r_chat.Invoke((MethodInvoker)(() => r_chat.ForeColor = Color.Red));
                r_chat.Invoke((MethodInvoker)(() => r_chat.Text = "Connection Error: " + e2.Message));
            }
        }

        static void ParseRemoteAddr(string addr, out string remoteIp, out int port)
        {
            string[] split = addr.Split(':');

            remoteIp = split[0];
            port = int.Parse(split[1]);
        }

        class P2pEndPoint
        {
            internal IPEndPoint External;
            internal IPEndPoint Internal;
        }

        private P2pEndPoint GetExternalEndPoint(Socket socket)
        {
            List<Tuple<string, int>> stunServers = new List<Tuple<string, int>>();

            // Read Stun Servers from dataGridView
            for (int rows = 0; rows < this.dataGridView1.Rows.Count; rows++)
            {
                if (this.dataGridView1.Rows[rows].Cells["Server"].Value != null && this.dataGridView1.Rows[rows].Cells["Port"].Value != null)
                {
                    string Server = Convert.ToString(this.dataGridView1.Rows[rows].Cells["Server"].Value);
                    int Port = Convert.ToInt32(this.dataGridView1.Rows[rows].Cells["Port"].Value);

                    stunServers.Add(new Tuple<string, int>(Server, Port));
                }
            }

            // https://gist.github.com/zziuni/3741933

            // stunServers.Add(new Tuple<string, int>("stun.l.google.com", 19302));

            Console.WriteLine("Contacting STUN servers to obtain your IP");

            foreach (Tuple<string, int> server in stunServers)
            {
                string host = server.Item1;
                int port = server.Item2;

                StunResult externalEndPoint = StunClient.Query(host, port, socket);

                if (externalEndPoint.NetType == StunNetType.UdpBlocked)
                {
                    continue;
                }

                Console.WriteLine("Your firewall is {0}", externalEndPoint.NetType.ToString());

                return new P2pEndPoint()
                {
                    External = externalEndPoint.PublicEndPoint,
                    Internal = (socket.LocalEndPoint as IPEndPoint)
                };
            }

            MessageBox.Show("Your external IP can't be obtained. Could not find a working STUN server :-( ");
            return null;
        }

        public UdtSocket PeerConnect(Socket socket, string remoteAddr, int remotePort)
        {
            bConnected = false;
            SenderReceiver.isConnected = false;
            int retry = 0;

            UdtSocket client = null;

            while (!bConnected)
            {
                try
                {
                    int sleepTimeToSync = 1;

                    label4.Invoke((MethodInvoker)(() => label4.ForeColor = Color.Black));
                    label4.Invoke((MethodInvoker)(() => label4.Text = "Waiting " + sleepTimeToSync + "  sec to sync with other peer"));
                    Thread.Sleep(sleepTimeToSync * 1000);

                    GetExternalEndPoint(socket);

                    if (client != null)
                        client.Close();

                    client = new UdtSocket(socket.AddressFamily, socket.SocketType);
                    client.Bind(socket);

                    retry++;
                    label4.Invoke((MethodInvoker)(() => label4.ForeColor = Color.Black));
                    label4.Invoke((MethodInvoker)(() => label4.Text = retry + " Trying to connect to " + remoteAddr + ":" + remotePort));

                    client.Connect(new IPEndPoint(IPAddress.Parse(remoteAddr), remotePort));

                    label4.Invoke((MethodInvoker)(() => label4.ForeColor = Color.DarkGreen));
                    label4.Invoke((MethodInvoker)(() => label4.Text = "Connected successfully to " + remoteAddr + ":" + remotePort));

                    SenderReceiver.isConnected = true;
                    bConnected = true;
                }
                catch (Exception e)
                {
                    label4.Invoke((MethodInvoker)(() => label4.ForeColor = Color.Red));
                    label4.Invoke((MethodInvoker)(() => label4.Text = e.Message.Replace(Environment.NewLine, ". ")));
                }
            }
            return client;
        }

        private void CloseAll()
        {
            if (bConnected == true)
            { 
                SenderReceiver.SendMessage("end|");
                thread.Abort();
                SenderReceiver.netStream.Close();
                SenderReceiver.isConnected = false;
                SenderReceiver.client.Close();
                connection.Close();
                socket.Close();
            }
            Process.GetCurrentProcess().Kill();
        }

        public void EnableStreach(bool sino)
        {
            checkBox1.Enabled = sino;
        }

        public void EnableDSpeed(bool sino)
        {
            dspeed.Enabled = sino;
        }

        private delegate void PlaceString(string item);
        public void WriteFPS(string item)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new PlaceString(this.WriteFPS), new object[] { item });
                }
                else
                {
                    this.lblFPS.Text = item;
                }
            }
            catch
            {
            }
        }
        public void WriteKB(string item)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new PlaceString(this.WriteKB), new object[] { item });
                }
                else
                {
                    this.lblkb.Text = item;
                }
            }
            catch
            {
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (txtnsg.Text != "")
            {
                if(bConnected == true)
                {
                    Writetxtchatrom("Blue", txtnsg.Text);
                    SenderReceiver.SendMessage("c|" + txtnsg.Text);
                    txtnsg.Text = "";
                }
                else
                {
                    MessageBox.Show("You are not connected to peer endpoint");
                }
            }
            else
            {
                MessageBox.Show("Nothing write to send!");
            }
        }

        private void btn_paste_Click(object sender, EventArgs e)
        {
            txtRemoteIP.Text = Clipboard.GetText();
            Clipboard.SetText(txtmyHost.Text);
            if(txtmyHost.Text == txtRemoteIP.Text || txtLocalHost.Text == txtRemoteIP.Text)
            {
                txtRemoteIP.Text = "";
                MessageBox.Show("Please paste peer remote host:port not your!");
            }
        }

        public void EnableButtonRdp(bool truefalse)
        {
            this.btnRdp.Enabled = truefalse;
        }
        public void Writetxtchatrom(string color, string msg)
        {
            try
            {
                string time = DateTime.Now.ToString("hh:mm");
                this.r_chat.Select(r_chat.TextLength, 0);

                if (color == "Blue")
                {
                    //Set the formatting and color text
                    this.r_chat.SelectionFont = new Font(r_chat.Font, FontStyle.Bold);
                    this.r_chat.SelectionColor = Color.Blue;
                    this.r_chat.AppendText(myname + " [" + time + "]: ");

                    // Revert the formatting back 
                    this.r_chat.SelectionFont = r_chat.Font;
                    this.r_chat.SelectionColor = r_chat.ForeColor;
                    this.r_chat.AppendText(msg + Environment.NewLine);
                }
                else if (color == "Green")
                {
                    //Set the formatting and color text
                    this.r_chat.SelectionFont = new Font(r_chat.Font, FontStyle.Bold);
                    this.r_chat.SelectionColor = Color.Green;
                    this.r_chat.AppendText(peername + " [" + time + "]: ");

                    // Revert the formatting back 
                    this.r_chat.SelectionFont = r_chat.Font;
                    this.r_chat.SelectionColor = r_chat.ForeColor;
                    this.r_chat.AppendText(msg + Environment.NewLine);
                    //red
                }
                else if (color == "Red")
                {
                    //Set the formatting and color text
                    this.r_chat.SelectionFont = new Font(r_chat.Font, FontStyle.Bold);
                    this.r_chat.SelectionColor = Color.Red;
                    this.r_chat.AppendText(peername + " [" + time + "]: " + msg + Environment.NewLine);
                }
            }
            catch
            {
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                switch (e.CloseReason)
                {
                    case CloseReason.ApplicationExitCall:
                        CloseAll();
                        break;
                    case CloseReason.FormOwnerClosing:
                        CloseAll();
                        break;
                    case CloseReason.MdiFormClosing:
                        CloseAll();
                        break;
                    case CloseReason.None:
                        CloseAll();
                        break;
                    case CloseReason.TaskManagerClosing:
                        CloseAll();
                        break;
                    case CloseReason.UserClosing:
                        CloseAll();
                        break;
                    case CloseReason.WindowsShutDown:
                        CloseAll();
                        break;
                    default:
                        CloseAll();
                        break;
                }
            }
            catch
            {
            }
        }

        private void r_chat_TextChanged(object sender, EventArgs e)
        {
            r_chat.SelectionStart = r_chat.TextLength;
            r_chat.ScrollToCaret();
        }

        private void r_chat_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                Process.Start(e.LinkText);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (txtmyHost.Text != "")
            {
                Clipboard.SetText(txtmyHost.Text);
            }
        }

        private void btnRdp_Click(object sender, EventArgs e)
        {
            if (bConnected == true)
            {
                var myForm = new pDesktop();
                myForm.Show();

                RemoteDesktop.UnsafeMotionCodec = new UnsafeStreamCodec(RemoteDesktop.DesktopQuality, true);
                SenderReceiver.SendMessage("openp2pDesktop|");
                GlobalVariables.Root.EnableButtonRdp(false);
            }
            else
            {
                MessageBox.Show("You are not connected to peer endpoint");
            }
        }

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start("https://www.pocketsolution.net/");
            }
            catch
            {
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(RemoteDesktop.DesktopRunning == true)
            {
                if (checkBox1.Checked == true)
                {
                    GlobalVariables.p2pDesktop.ControlStreach(true);
                }
                else
                {
                    GlobalVariables.p2pDesktop.ControlStreach(false);
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (txtLocalHost.Text != "")
            {
                Clipboard.SetText(txtLocalHost.Text);
            }
        }

        private void dspeed_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (bConnected == true)
            {
                SenderReceiver.SendMessage("ds|" + dspeed.GetItemText(dspeed.SelectedItem));
            }
        }

        private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (this.dataGridView1.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in this.dataGridView1.SelectedRows)
                {
                    if (row.IsNewRow) continue;
                    this.dataGridView1.Rows.RemoveAt(row.Index);
                    CheckJsonFile();                }
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            SaveDataToJsoin();
        }

        private void SaveDataToJsoin()
        {
            if (this.dataGridView1.Rows.Count == 1 && File.Exists(StunServersJson))
            {
                File.Delete(StunServersJson);
                this.dataGridView1.Rows.Insert(0, "stun.l.google.com", 19302);
            }

            var ListJsonStunServers = new List<StunServer>();

            for (int rows = 0; rows < this.dataGridView1.Rows.Count; rows++)
            {
                if (this.dataGridView1.Rows[rows].Cells["Server"].Value != null && this.dataGridView1.Rows[rows].Cells["Port"].Value != null)
                {
                    string Server = Convert.ToString(this.dataGridView1.Rows[rows].Cells["Server"].Value);
                    int Port = Convert.ToInt32(this.dataGridView1.Rows[rows].Cells["Port"].Value);

                    var _stun = new StunServer
                    {
                        Server = Server,
                        Port = Port,
                    };
                    ListJsonStunServers.Add(_stun);
                }
            }

            StunServer.WriteStunServersToFile(ListJsonStunServers, StunServersJson);
           
            if (this.dataGridView1.Rows.Count > 1)
            {
                MessageBox.Show("Stun Servers List saved to: " + StunServersJson);
            }
        }

        private void CheckJsonFile()
        {
            if (this.dataGridView1.Rows.Count == 1 && File.Exists(StunServersJson))
            {
                File.Delete(StunServersJson);
            }
        }
    }

    public class GlobalVariables
    {
        public static Form1 Root;
        public static pDesktop p2pDesktop;
    }
}

