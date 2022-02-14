using p2pcopy;
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

        private void Form1_Load(object sender, EventArgs e)
        {
            myname = Environment.UserName;
            dspeed.SelectedIndex = 2;
            GetEndPoint();
        }

        private void GetEndPoint()
        {
            int newPort = r.Next(49152, 65535);
            socket.Bind(new IPEndPoint(IPAddress.Any, newPort));

            P2pEndPoint p2pEndPoint = GetExternalEndPoint(socket);

            if (p2pEndPoint == null)
                return;

            // txtmyHost.Text = Functions.Base64Encode(p2pEndPoint.External.ToString());
            txtmyHost.Text = p2pEndPoint.External.ToString();
            Clipboard.SetText(p2pEndPoint.External.ToString());
            string localendpoint = p2pEndPoint.Internal.ToString();
            string[] words = localendpoint.Split(':');
            // txtLocalHost.Text = Functions.Base64Encode(GetPhysicalIPAdress() + ":" + words[1]);
            txtLocalHost.Text = GetPhysicalIPAdress() + ":" + words[1];
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

        static P2pEndPoint GetExternalEndPoint(Socket socket)
        {
            // https://gist.github.com/zziuni/3741933

            List<Tuple<string, int>> stunServers = new List<Tuple<string, int>>();
            stunServers.Add(new Tuple<string, int>("stun.l.google.com", 19302));
            stunServers.Add(new Tuple<string, int>("iphone-stun.strato-iphone.de", 3478));
            stunServers.Add(new Tuple<string, int>("numb.viagenie.ca", 3478));
            stunServers.Add(new Tuple<string, int>("s1.taraba.net", 3478));
            stunServers.Add(new Tuple<string, int>("s2.taraba.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.12connect.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.12voip.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.1und1.de", 3478));
            stunServers.Add(new Tuple<string, int>("stun.2talk.co.nz", 3478));
            stunServers.Add(new Tuple<string, int>("stun.2talk.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.3clogic.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.3cx.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.a-mm.tv", 3478));
            stunServers.Add(new Tuple<string, int>("stun.aa.net.uk", 3478));
            stunServers.Add(new Tuple<string, int>("stun.acrobits.cz", 3478));
            stunServers.Add(new Tuple<string, int>("stun.actionvoip.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.advfn.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.aeta-audio.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.aeta.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.alltel.com.au", 3478));
            stunServers.Add(new Tuple<string, int>("stun.altar.com.pl", 3478));
            stunServers.Add(new Tuple<string, int>("stun.annatel.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.antisip.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.arbuz.ru", 3478));
            stunServers.Add(new Tuple<string, int>("stun.avigora.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.avigora.fr", 3478));
            stunServers.Add(new Tuple<string, int>("stun.awa-shima.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.awt.be", 3478));
            stunServers.Add(new Tuple<string, int>("stun.b2b2c.ca", 3478));
            stunServers.Add(new Tuple<string, int>("stun.bahnhof.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.barracuda.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.bluesip.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.bmwgs.cz", 3478));
            stunServers.Add(new Tuple<string, int>("stun.botonakis.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.budgetphone.nl", 3478));
            stunServers.Add(new Tuple<string, int>("stun.budgetsip.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.cablenet-as.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.callromania.ro", 3478));
            stunServers.Add(new Tuple<string, int>("stun.callwithus.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.cbsys.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.chathelp.ru", 3478));
            stunServers.Add(new Tuple<string, int>("stun.cheapvoip.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.ciktel.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.cloopen.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.colouredlines.com.au", 3478));
            stunServers.Add(new Tuple<string, int>("stun.comfi.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.commpeak.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.comtube.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.comtube.ru", 3478));
            stunServers.Add(new Tuple<string, int>("stun.cope.es", 3478));
            stunServers.Add(new Tuple<string, int>("stun.counterpath.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.counterpath.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.cryptonit.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.darioflaccovio.it", 3478));
            stunServers.Add(new Tuple<string, int>("stun.datamanagement.it", 3478));
            stunServers.Add(new Tuple<string, int>("stun.dcalling.de", 3478));
            stunServers.Add(new Tuple<string, int>("stun.decanet.fr", 3478));
            stunServers.Add(new Tuple<string, int>("stun.demos.ru", 3478));
            stunServers.Add(new Tuple<string, int>("stun.develz.org", 3478));
            stunServers.Add(new Tuple<string, int>("stun.dingaling.ca", 3478));
            stunServers.Add(new Tuple<string, int>("stun.doublerobotics.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.drogon.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.duocom.es", 3478));
            stunServers.Add(new Tuple<string, int>("stun.dus.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.e-fon.ch", 3478));
            stunServers.Add(new Tuple<string, int>("stun.easybell.de", 3478));
            stunServers.Add(new Tuple<string, int>("stun.easycall.pl", 3478));
            stunServers.Add(new Tuple<string, int>("stun.easyvoip.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.efficace-factory.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.einsundeins.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.einsundeins.de", 3478));
            stunServers.Add(new Tuple<string, int>("stun.ekiga.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.epygi.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.etoilediese.fr", 3478));
            stunServers.Add(new Tuple<string, int>("stun.eyeball.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.faktortel.com.au", 3478));
            stunServers.Add(new Tuple<string, int>("stun.freecall.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.freeswitch.org", 3478));
            stunServers.Add(new Tuple<string, int>("stun.freevoipdeal.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.fuzemeeting.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.gmx.de", 3478));
            stunServers.Add(new Tuple<string, int>("stun.gmx.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.gradwell.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.halonet.pl", 3478));
            stunServers.Add(new Tuple<string, int>("stun.hellonanu.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.hoiio.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.hosteurope.de", 3478));
            stunServers.Add(new Tuple<string, int>("stun.ideasip.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.imesh.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.infra.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.internetcalls.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.intervoip.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.ipcomms.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.ipfire.org", 3478));
            stunServers.Add(new Tuple<string, int>("stun.ippi.fr", 3478));
            stunServers.Add(new Tuple<string, int>("stun.ipshka.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.iptel.org", 3478));
            stunServers.Add(new Tuple<string, int>("stun.irian.at", 3478));
            stunServers.Add(new Tuple<string, int>("stun.it1.hr", 3478));
            stunServers.Add(new Tuple<string, int>("stun.ivao.aero", 3478));
            stunServers.Add(new Tuple<string, int>("stun.jappix.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.jumblo.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.justvoip.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.kanet.ru", 3478));
            stunServers.Add(new Tuple<string, int>("stun.kiwilink.co.nz", 3478));
            stunServers.Add(new Tuple<string, int>("stun.kundenserver.de", 3478));
            stunServers.Add(new Tuple<string, int>("stun.linea7.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.linphone.org", 3478));
            stunServers.Add(new Tuple<string, int>("stun.liveo.fr", 3478));
            stunServers.Add(new Tuple<string, int>("stun.lowratevoip.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.lugosoft.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.lundimatin.fr", 3478));
            stunServers.Add(new Tuple<string, int>("stun.magnet.ie", 3478));
            stunServers.Add(new Tuple<string, int>("stun.manle.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.mgn.ru", 3478));
            stunServers.Add(new Tuple<string, int>("stun.mit.de", 3478));
            stunServers.Add(new Tuple<string, int>("stun.mitake.com.tw", 3478));
            stunServers.Add(new Tuple<string, int>("stun.miwifi.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.modulus.gr", 3478));
            stunServers.Add(new Tuple<string, int>("stun.mozcom.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.myvoiptraffic.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.mywatson.it", 3478));
            stunServers.Add(new Tuple<string, int>("stun.nas.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.neotel.co.za", 3478));
            stunServers.Add(new Tuple<string, int>("stun.netappel.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.netappel.fr", 3478));
            stunServers.Add(new Tuple<string, int>("stun.netgsm.com.tr", 3478));
            stunServers.Add(new Tuple<string, int>("stun.nfon.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.noblogs.org", 3478));
            stunServers.Add(new Tuple<string, int>("stun.noc.ams-ix.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.node4.co.uk", 3478));
            stunServers.Add(new Tuple<string, int>("stun.nonoh.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.nottingham.ac.uk", 3478));
            stunServers.Add(new Tuple<string, int>("stun.nova.is", 3478));
            stunServers.Add(new Tuple<string, int>("stun.nventure.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.on.net.mk", 3478));
            stunServers.Add(new Tuple<string, int>("stun.ooma.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.ooonet.ru", 3478));
            stunServers.Add(new Tuple<string, int>("stun.oriontelekom.rs", 3478));
            stunServers.Add(new Tuple<string, int>("stun.outland-net.de", 3478));
            stunServers.Add(new Tuple<string, int>("stun.ozekiphone.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.patlive.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.personal-voip.de", 3478));
            stunServers.Add(new Tuple<string, int>("stun.petcube.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.phone.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.phoneserve.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.pjsip.org", 3478));
            stunServers.Add(new Tuple<string, int>("stun.poivy.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.powerpbx.org", 3478));
            stunServers.Add(new Tuple<string, int>("stun.powervoip.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.ppdi.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.prizee.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.qq.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.qvod.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.rackco.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.rapidnet.de", 3478));
            stunServers.Add(new Tuple<string, int>("stun.rb-net.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.refint.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.remote-learner.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.rixtelecom.se", 3478));
            stunServers.Add(new Tuple<string, int>("stun.rockenstein.de", 3478));
            stunServers.Add(new Tuple<string, int>("stun.rolmail.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.rounds.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.rynga.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.samsungsmartcam.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.schlund.de", 3478));
            stunServers.Add(new Tuple<string, int>("stun.services.mozilla.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.sigmavoip.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.sip.us", 3478));
            stunServers.Add(new Tuple<string, int>("stun.sipdiscount.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.sipgate.net", 10000));
            stunServers.Add(new Tuple<string, int>("stun.sipgate.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.siplogin.de", 3478));
            stunServers.Add(new Tuple<string, int>("stun.sipnet.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.sipnet.ru", 3478));
            stunServers.Add(new Tuple<string, int>("stun.siportal.it", 3478));
            stunServers.Add(new Tuple<string, int>("stun.sippeer.dk", 3478));
            stunServers.Add(new Tuple<string, int>("stun.siptraffic.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.skylink.ru", 3478));
            stunServers.Add(new Tuple<string, int>("stun.sma.de", 3478));
            stunServers.Add(new Tuple<string, int>("stun.smartvoip.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.smsdiscount.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.snafu.de", 3478));
            stunServers.Add(new Tuple<string, int>("stun.softjoys.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.solcon.nl", 3478));
            stunServers.Add(new Tuple<string, int>("stun.solnet.ch", 3478));
            stunServers.Add(new Tuple<string, int>("stun.sonetel.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.sonetel.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.sovtest.ru", 3478));
            stunServers.Add(new Tuple<string, int>("stun.speedy.com.ar", 3478));
            stunServers.Add(new Tuple<string, int>("stun.spokn.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.srce.hr", 3478));
            stunServers.Add(new Tuple<string, int>("stun.ssl7.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.stunprotocol.org", 3478));
            stunServers.Add(new Tuple<string, int>("stun.symform.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.symplicity.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.sysadminman.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.t-online.de", 3478));
            stunServers.Add(new Tuple<string, int>("stun.tagan.ru", 3478));
            stunServers.Add(new Tuple<string, int>("stun.tatneft.ru", 3478));
            stunServers.Add(new Tuple<string, int>("stun.teachercreated.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.tel.lu", 3478));
            stunServers.Add(new Tuple<string, int>("stun.telbo.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.telefacil.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.tis-dialog.ru", 3478));
            stunServers.Add(new Tuple<string, int>("stun.tng.de", 3478));
            stunServers.Add(new Tuple<string, int>("stun.twt.it", 3478));
            stunServers.Add(new Tuple<string, int>("stun.u-blox.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.ucallweconn.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.ucsb.edu", 3478));
            stunServers.Add(new Tuple<string, int>("stun.ucw.cz", 3478));
            stunServers.Add(new Tuple<string, int>("stun.uls.co.za", 3478));
            stunServers.Add(new Tuple<string, int>("stun.unseen.is", 3478));
            stunServers.Add(new Tuple<string, int>("stun.usfamily.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.veoh.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.vidyo.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.vipgroup.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.virtual-call.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.viva.gr", 3478));
            stunServers.Add(new Tuple<string, int>("stun.vivox.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.vline.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.vo.lu", 3478));
            stunServers.Add(new Tuple<string, int>("stun.vodafone.ro", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voicetrading.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voip.aebc.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voip.blackberry.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voip.eutelia.it", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voiparound.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voipblast.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voipbuster.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voipbusterpro.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voipcheap.co.uk", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voipcheap.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voipfibre.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voipgain.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voipgate.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voipinfocenter.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voipplanet.nl", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voippro.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voipraider.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voipstunt.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voipwise.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voipzoom.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.vopium.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voxgratia.org", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voxox.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voys.nl", 3478));
            stunServers.Add(new Tuple<string, int>("stun.voztele.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.vyke.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.webcalldirect.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.whoi.edu", 3478));
            stunServers.Add(new Tuple<string, int>("stun.wifirst.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.wwdl.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun.xs4all.nl", 3478));
            stunServers.Add(new Tuple<string, int>("stun.xtratelecom.es", 3478));
            stunServers.Add(new Tuple<string, int>("stun.yesss.at", 3478));
            stunServers.Add(new Tuple<string, int>("stun.zadarma.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.zadv.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun.zoiper.com", 3478));
            stunServers.Add(new Tuple<string, int>("stun1.faktortel.com.au", 3478));
            stunServers.Add(new Tuple<string, int>("stun1.l.google.com", 19302));
            stunServers.Add(new Tuple<string, int>("stun1.voiceeclipse.net", 3478));
            stunServers.Add(new Tuple<string, int>("stun2.l.google.com", 19302));
            stunServers.Add(new Tuple<string, int>("stun3.l.google.com", 19302));
            stunServers.Add(new Tuple<string, int>("stun4.l.google.com", 19302));
            stunServers.Add(new Tuple<string, int>("stunserver.org", 3478));

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
    }

    public class GlobalVariables
    {
        public static Form1 Root;
        public static pDesktop p2pDesktop;
    }
}

