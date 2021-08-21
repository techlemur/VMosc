using System;
using System.Windows;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
//using WindowsInput.Native;
//using WindowsInput;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VoiceMeeterWrapper; //https://github.com/tocklime/VoiceMeeterWrapper
using SharpOSC;  //https://github.com/ValdemarOrn/SharpOSC

namespace VMosc
{
    public partial class Form1 : Form
    {
        //System.Threading.Thread t;
        private volatile bool _run;
        VoiceMeeterWrapper.VmClient vm;
        SharpOSC.UDPSender oscSender;
        //Timer timer1;
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

        Dictionary<string, float> voicemeeterVars = new Dictionary<string, float>();
        Dictionary<string, float> voicemeeterXYVars = new Dictionary<string, float>();

        Dictionary<string, string> voicemeeterOptionNames = new Dictionary<string, string>()
                    {
                        { "Bus[0].Gain" , "/Bus/0/Gain"},
                    };

        Dictionary<string, string> voicemeeterVarNames = new Dictionary<string, string>();
        Dictionary<string, string> voicemeeterXYVarNames = new Dictionary<string, string>();

        public static float ConvertRange(
        float originalStart, float originalEnd, // original range
        float newStart, float newEnd, // desired range
        float value) // value to convert
            {
                double scale = (double)(newEnd - newStart) / (originalEnd - originalStart);
                return (float)(newStart + ((value - originalStart) * scale));
            }

        public void Sync()
        {
            label_status.Invoke((MethodInvoker)(() => label_status.Text = "Syncing"));
            foreach (KeyValuePair<string, string> vVar in voicemeeterVarNames)
            {
                oscSender.Send(new SharpOSC.OscMessage(vVar.Value, (float)vm.GetParam(vVar.Key)));
                oscSender.Send(new SharpOSC.OscMessage(vVar.Value + "/Text", (float)vm.GetParam(vVar.Key)));
                System.Threading.Thread.Sleep(5);
                //Console.WriteLine(vVar.Key + " - " + vVar.Value.ToString());
            }

            for (int i = 0; i < 8; i++)
            {
                try
                {
                    oscSender.Send(new SharpOSC.OscMessage("/Slider/" + i.ToString() + "/device/Text",  vm.GetParam("Strip[" + i.ToString() + "].device")));
                    //Console.WriteLine((string)vm.GetParamString("Strip[" + i.ToString() + "].device"));
                }
                catch (System.Exception e)
                {
                    Console.WriteLine("oops");
                    Console.WriteLine(e);
                }
            }

            foreach (KeyValuePair<string, string> vVar in voicemeeterXYVarNames)
            {
                string value = vVar.Value;
                float tmpVarX = (float)vm.GetParam(vVar.Key + "_x");
                float tmpVarY = (float)vm.GetParam(vVar.Key + "_y");
                if (vVar.Key.Contains("fx"))
                {
                    value = vVar.Value + "y";
                }
                if (vVar.Key.Contains("Pan"))
                {
                    //Console.WriteLine("PANNNN!!!!!!!!");
                    if (vVar.Key.Contains("5") || vVar.Key.Contains("6") || vVar.Key.Contains("7"))
                    {
                        //Console.WriteLine("5, 6, OR 7!!!!!!!!");
                        tmpVarY = ConvertRange((float)-0.5, (float)0.5, (float)-1, (float)1, tmpVarY);
                        tmpVarX = ConvertRange((float)-0.5, (float)0.5, (float)-1, (float)1, tmpVarX);
                    }
                    else
                    {
                        //tmpVarX = ConvertRange((float)-0.5, (float)0.5, (float)0, (float)1, tmpVarX);
                        tmpVarY = ConvertRange((float)0, (float)1, (float)-0.5, (float)0.5, tmpVarY);
                    }
                }
                else
                {
                    //tmpVarY = ConvertRange((float)-0.5, (float)0.5, (float)0, (float)1,tmpVarY);
                    tmpVarY = ConvertRange((float)0, (float)1, (float)-0.5, (float)0.5, tmpVarY);
                }

                //if (voicemeeterXYVars[vVar.Key + "_x"] != tmpVarX || voicemeeterXYVars[vVar.Key + "_y"] != tmpVarY)
                //{
                    //voicemeeterXYVars[vVar.Key + "_x"] = tmpVarX;
                    //voicemeeterXYVars[vVar.Key + "_y"] = tmpVarY;
                    oscSender.Send(new SharpOSC.OscMessage(value, tmpVarY, tmpVarX));
                    oscSender.Send(new SharpOSC.OscMessage(value + "/Text", tmpVarY + " - " + tmpVarX));
                    System.Threading.Thread.Sleep(10);
                    //Console.WriteLine(vVar.Key + " - " + value.ToString());
                //}
            }

            label_status.Invoke((MethodInvoker)(() => label_status.Text = ""));
        }

    public void InitTimer()
        {
            string[] stripOptionNames = new string[] { "Gain", "Solo", "Mute","Comp","Gate","fx1","fx2","MC", "EQGain1", "EQGain2", "EQGain3" };
            string[] stripXYOptionNames = new string[] { "Color", "fx" ,"Pan"};

            string[] busOptionNames = new string[] { "Gain", "Solo", "Mute","Mono","SEL", "EQ.ON" };

            string[] recorderOptionNames = new string[] { "stop", "play", "A1","A2","A3","A4","A5","B1","B2","B3","record","recbus","PlayOnLoad","Channel","gain" };

            foreach (string rec in recorderOptionNames)
            {
                voicemeeterVarNames.Add("recorder." + rec.ToString() , "/recorder/" + rec);
            }

            for (int i = 0; i < 8; i++)
            {
                //Console.WriteLine(i);

                //voicemeeterXYVarNames.Add("Strip[" + i.ToString() + "].fx_x", "/Slider/" + i.ToString() + "/fxy");

                //voicemeeterVarNames.Add("Strip[" + i.ToString() + "].device", "/Slider/" + i.ToString() + "/device/Text");

                foreach (string strip in stripOptionNames)
                {
                    voicemeeterVarNames.Add("Strip[" + i.ToString() + "]."+ strip, "/Slider/" + i.ToString() + "/"+ strip);
                }

                foreach (string strip in stripXYOptionNames)
                {
                    voicemeeterXYVarNames.Add("Strip[" + i.ToString() + "]." + strip, "/Slider/" + i.ToString() + "/" + strip);
                }

                for (int a = 1; a < 6; a++)
                {
                    voicemeeterVarNames.Add("Strip[" + i.ToString() + "].A" + a.ToString(), "/Slider/" + i.ToString() + "/A"+a.ToString());
                }
                for (int b = 1; b < 4; b++)
                {
                    voicemeeterVarNames.Add("Strip[" + i.ToString() + "].B" + b.ToString(), "/Slider/" + i.ToString() + "/B"+b.ToString());
                }

                foreach (string bus in stripOptionNames)
                {
                    voicemeeterVarNames.Add("Bus[" + i.ToString() + "]."+bus, "/Bus/" + i.ToString() + "/"+ bus);
                }
            }

            foreach (KeyValuePair<string, string> vVar in voicemeeterVarNames)
            {
                voicemeeterVars.Add(vVar.Key, (float)vm.GetParam(vVar.Key));
                oscSender.Send(new SharpOSC.OscMessage(vVar.Value, (float)vm.GetParam(vVar.Key)));
                oscSender.Send(new SharpOSC.OscMessage(vVar.Value + "/Text", (float)vm.GetParam(vVar.Key)));
                //System.Threading.Thread.Sleep(10);
                //Console.WriteLine(vVar.Key + " - " + vVar.Value.ToString());
            }
 
            foreach (KeyValuePair<string, string> vVar in voicemeeterXYVarNames)
            {
                voicemeeterXYVars.Add(vVar.Key + "_x", (float)vm.GetParam(vVar.Key + "_x"));
                voicemeeterXYVars.Add(vVar.Key + "_y", (float)vm.GetParam(vVar.Key + "_y"));
                oscSender.Send(new SharpOSC.OscMessage(vVar.Value, (float)vm.GetParam(vVar.Key + "_y") , (float)vm.GetParam(vVar.Key+"_x")));
                oscSender.Send(new SharpOSC.OscMessage(vVar.Value + "/Text", (float)vm.GetParam(vVar.Key+"_y")+" - "+ (float)vm.GetParam(vVar.Key + "_x")));
                //System.Threading.Thread.Sleep(10);
                //Console.WriteLine(vVar.Key + " - " + vVar.Value.ToString());
            }

            var timer1 = new System.Windows.Forms.Timer();
            timer1.Tick += new EventHandler(timer1_Tick);
            timer1.Interval = 10; // in miliseconds
            timer1.Start();
        }

        public void timer1_Tick(object sender, EventArgs e)
        {
            if (vm.Poll() is true)
            {
                //Console.WriteLine("poll - "+vm.Poll().ToString());
                foreach (KeyValuePair<string, string> vVar in voicemeeterVarNames)
                {
                    //Console.WriteLine(vm.GetParam(vVar.Key).ToString());
                    //var mymessage = new SharpOSC.OscMessage(vVar.Key, (float)vm.GetParam(vVar.Key));
                    //if (mymessage != null)
                    //{
                    float tmpVar = (float)vm.GetParam(vVar.Key);
                    if (voicemeeterVars[vVar.Key] != tmpVar)
                    {
                        voicemeeterVars[vVar.Key] = tmpVar;
                        oscSender.Send(new SharpOSC.OscMessage(vVar.Value, tmpVar));
                        oscSender.Send(new SharpOSC.OscMessage(vVar.Value + "/Text", tmpVar));
                    }
                    
                    //}
                }

                foreach (KeyValuePair<string, string> vVar in voicemeeterXYVarNames)
                {
                    string value = vVar.Value;
                    float tmpVarX = (float)vm.GetParam(vVar.Key + "_x");
                    float tmpVarY = (float)vm.GetParam(vVar.Key + "_y");

                    //Console.WriteLine(tmpVarX.ToString() + " - " + tmpVarY.ToString());
                    if (vVar.Key.Contains("fx"))
                    {
                        value = vVar.Value + "y";
                    }
                    if (vVar.Key.Contains("Pan"))
                    {
                        //Console.WriteLine("PANNNN!!!!!!!!");
                        if (vVar.Key.Contains("5") || vVar.Key.Contains("6") || vVar.Key.Contains("7"))
                        {
                           //Console.WriteLine("5, 6, OR 7!!!!!!!!");
                            tmpVarY = ConvertRange((float)-0.5, (float)0.5, (float)-1, (float)1, tmpVarY);
                            tmpVarX = ConvertRange((float)-0.5, (float)0.5, (float)-1, (float)1, tmpVarX);
                        }
                        else
                        {
                            //tmpVarX = ConvertRange((float)-0.5, (float)0.5, (float)0, (float)1, tmpVarX);
                            tmpVarY = ConvertRange((float)0, (float)1, (float)-0.5, (float)0.5, tmpVarY);
                        }
                    }
                    else
                    {
                        //tmpVarY = ConvertRange((float)-0.5, (float)0.5, (float)0, (float)1,tmpVarY);
                        tmpVarY = ConvertRange((float)0, (float)1, (float)-0.5, (float)0.5, tmpVarY);
                    }
                    
                    if (voicemeeterXYVars[vVar.Key+"_x"] != tmpVarX || voicemeeterXYVars[vVar.Key + "_y"] != tmpVarY)
                    {
                        voicemeeterXYVars[vVar.Key + "_x"] = tmpVarX;
                        voicemeeterXYVars[vVar.Key + "_y"] = tmpVarY;
                        oscSender.Send(new SharpOSC.OscMessage(value, tmpVarY, tmpVarX));
                        oscSender.Send(new SharpOSC.OscMessage(value + "/Text", tmpVarY + " - " + tmpVarX));
                        System.Threading.Thread.Sleep(10);
                        //Console.WriteLine(vVar.Key + " - " + value.ToString());
                    }
                }
            }
        }

        public Form1()
        {
            InitializeComponent();
            //System.Threading.Thread t;
            //Run = true;
            //t = new System.Threading.Thread(DoThisAllTheTime);
            //t.Start();

            //int x = Convert.ToInt32(textBox1.Text);
            //textBox1.Text = Properties.Settings.Default.portIn.ToString();
            //textBox2.Text = Properties.Settings.Default.portOut.ToString();
            //textBox3.Text = Properties.Settings.Default.broadcastIP.ToString();

            vm = new VmClient();

            IPAddress broadcast;
            //Console.WriteLine(Properties.Settings.Default.portIn.ToString());
            //Console.WriteLine(Properties.Settings.Default.portOut.ToString());
            //Console.WriteLine(Properties.Settings.Default.broadcastIP.ToString());

            bool flag = IPAddress.TryParse(Properties.Settings.Default.broadcastIP, out broadcast);
            if (!flag)
            {
                Properties.Settings.Default.broadcastIP = "192.168.0.255";
                textBox3.Text = "192.168.0.255";
                Properties.Settings.Default.Save();
                broadcast = IPAddress.Parse(Properties.Settings.Default.broadcastIP);
            }
            oscSender = new SharpOSC.UDPSender(broadcast.ToString(), Properties.Settings.Default.portOut);
        }

        public bool Run
        {
            get { return _run; }
            set { _run = value; }
        }

        internal static class UnsafeNativeMethods
        {
            const string _dllLocation = "CoreDLL.dll";
            [DllImport(_dllLocation)]
            public static extern void SimulateGameDLL(int a, int b);
        }

        /*
         public void DoThisAllTheTime()
        {
            var listener = new UDPListener(55555);
            //MessageBox.Show("Starting OSC Listener");
            label1.Text = "Starting OSC Listener";
            OscMessage messageReceived = null;
            while (messageReceived == null)
            {
                messageReceived = (OscMessage)listener.Receive();
                Thread.Sleep(1);
            }
            label1.Text = "Received a message!";
            MessageBox.Show("Received a message!");

        }
        */

        public void Form1_Load(object sender, EventArgs e)
        {
            Run = true;
            //t = new System.Threading.Thread(DoThisAllTheTime);
            //t.Start();

            //Clear labels.
            label1.Invoke((MethodInvoker)(() => label1.Text = ""));
            label_single.Invoke((MethodInvoker)(() => label_single.Text = ""));
            label_x.Invoke((MethodInvoker)(() => label_x.Text = ""));
            label_y.Invoke((MethodInvoker)(() => label_y.Text = ""));
            label_status.Invoke((MethodInvoker)(() => label_status.Text = ""));

            InitTimer();

            HandleOscPacket callback = delegate (OscPacket packet)
                {
                    //Console.WriteLine("oscPacket");
                    var messageReceived = (OscMessage)packet;
                if (messageReceived == null)
                {
                    return;
                }

                if (messageReceived.Address != null)
                {
                    Console.WriteLine(messageReceived.Address);
                    label1.Invoke((MethodInvoker)(() => label1.Text = messageReceived.Address));

                    string[] addressParts = messageReceived.Address.Split('/');

                    Dictionary<string, float> msgVars = new Dictionary<string, float>();

                    //Console.WriteLine(addressParts[1]);

                    if (messageReceived.Address.Contains("/z") || messageReceived.Address.Contains("nvm") )
                    {
                        //label_status.Invoke((MethodInvoker)(() => label_status.Text = "end string, no action"));
                        return;
                    }
                    else if (messageReceived.Address.Contains("keyboard"))//remote program calls
                        {
                            //Console.WriteLine("keyboard");
                            //Console.WriteLine(addressParts[2]);
                            SendKeys.SendWait(addressParts[2]);
                            return;
                        }
                    else if (messageReceived.Address.Contains("osc"))//remote program calls
                        {
                            //label_status.Invoke((MethodInvoker)(() => label_status.Text = "end string, no action"));
                            //Console.WriteLine("osc command");
                            switch (addressParts[2])
                            {
                                case "sync":
                                    //Console.WriteLine("osc sync");
                                    //Form1.invoke((MethodInvoker)(Sync()));
                                    this.Invoke((MethodInvoker)delegate { Sync(); });
                                break;
                            }

                            return;
                    } else if (addressParts.Count() < 4)
                        {
                            return;
                        }
                    else
                    {
                        //Console.WriteLine(addressParts[1]);
                        switch (addressParts[1])
                        {
                            case "Slider":
                                addressParts[1] = "Strip";
                                break;
                            case "keyboard":
                                    Console.WriteLine("keyboard");
                                    Console.WriteLine(addressParts[2]);
                                    SendKeys.SendWait(addressParts[2]);
                                break;
                        }
                        //Console.WriteLine(messageReceived.Arguments.Count.ToString());
                        //label_status.Invoke((MethodInvoker)(() => label_status.Text = messageReceived.Arguments.Count.ToString()));

                        switch (addressParts[3])
                        {
                            
                            case "fxy":
                                if (messageReceived.Arguments.Count == 2)
                                {
                                    label_single.Invoke((MethodInvoker)(() => label_single.Text = ""));

                                    msgVars.Add("fx_x", (float)messageReceived.Arguments[1]);
                                    label_x.Invoke((MethodInvoker)(() => label_x.Text = ((float)messageReceived.Arguments[1] - (float)0.5).ToString()));

                                    msgVars.Add("fx_y", ConvertRange((float)-0.5, (float)0.5, (float)0, (float)1, (float)messageReceived.Arguments[0]));
                                    label_y.Invoke((MethodInvoker)(() => label_y.Text = messageReceived.Arguments[0].ToString()));
                                }
                                else
                                {
                                    msgVars.Add("fx_x", (float)0);
                                    msgVars.Add("fx_y", (float)0);
                                }
                                break;
                            case "Color": label_single.Invoke((MethodInvoker)(() => label_single.Text = ""));
                                if (messageReceived.Arguments.Count == 2)
                                {
                                    label_single.Invoke((MethodInvoker)(() => label_single.Text = ""));

                                    msgVars.Add(addressParts[3] + "_x", (float)messageReceived.Arguments[1]);
                                    label_x.Invoke((MethodInvoker)(() => label_x.Text = ((float)messageReceived.Arguments[1] ).ToString()));

                                    if (Convert.ToInt32(addressParts[2]) > 4)
                                    {
                                        msgVars.Add(addressParts[3] + "_y", (float)messageReceived.Arguments[0] - (float)1);
                                        label_y.Invoke((MethodInvoker)(() => label_y.Text = messageReceived.Arguments[0].ToString()));
                                    }
                                    else
                                    {
                                        msgVars.Add(addressParts[3] + "_y", ConvertRange((float)-0.5, (float)0.5, (float)0, (float)1, (float)messageReceived.Arguments[0]));
                                        label_y.Invoke((MethodInvoker)(() => label_y.Text = ((float)messageReceived.Arguments[0]).ToString()));
                                    }
                                }
                                else
                                {
                                    msgVars.Add(addressParts[3] + "_x", (float)0);
                                    msgVars.Add(addressParts[3] + "_y", (float)0);
                                }
                                break;
                            case "Pan":
                                if (messageReceived.Arguments.Count == 2)
                                {
                                    label_single.Invoke((MethodInvoker)(() => label_single.Text = ""));

                                            if (addressParts[2] == "5" || addressParts[2] == "6" || addressParts[2] == "7")
                                            {
                                                //Console.WriteLine("5, 6, OR 7!!!!!!!! "+ ConvertRange((float)-1, (float)1, (float)-0.5, (float)0.5, (float)messageReceived.Arguments[1]).ToString() +" -- " + ConvertRange((float)-1, (float)1, (float)-0.5, (float)0.5, (float)messageReceived.Arguments[0]).ToString());
                                                msgVars.Add(addressParts[3] + "_x", ConvertRange((float)-1, (float)1, (float)-0.5, (float)0.5,  (float)messageReceived.Arguments[1]));
                                                label_x.Invoke((MethodInvoker)(() => label_x.Text = ((float)messageReceived.Arguments[1]).ToString()));
                                                msgVars.Add(addressParts[3] + "_y", ConvertRange((float)-1, (float)1, (float)-0.5, (float)0.5, (float)messageReceived.Arguments[0]));
                                                label_y.Invoke((MethodInvoker)(() => label_y.Text = messageReceived.Arguments[0].ToString()));
                                            }
                                            else
                                            {
                                                Console.WriteLine("pan");
                                                msgVars.Add(addressParts[3] + "_x", (float)messageReceived.Arguments[1] );
                                                //msgVars.Add(addressParts[3] + "_x", ConvertRange((float)-0.5, (float)0.5, (float)0, (float)1, (float)messageReceived.Arguments[1]));
                                                label_x.Invoke((MethodInvoker)(() => label_x.Text = ((float)messageReceived.Arguments[1] - (float)0.5).ToString()));
                                                //msgVars.Add(addressParts[3] + "_y", (float)messageReceived.Arguments[0] );
                                                msgVars.Add(addressParts[3] + "_y", ConvertRange((float)-0.5, (float)0.5, (float)0, (float)1, (float)messageReceived.Arguments[0]));
                                            label_y.Invoke((MethodInvoker)(() => label_y.Text = messageReceived.Arguments[0].ToString()));
                                            }
                                }
                                else
                                {
                                    msgVars.Add(addressParts[3] + "_x", (float)0);
                                    msgVars.Add(addressParts[3] + "_y", (float)0);
                                }
                                break;
                            default:
                                msgVars.Add(addressParts[3], (float)messageReceived.Arguments[0]);
                                label_single.Invoke((MethodInvoker)(() => label_single.Text = messageReceived.Arguments[0].ToString()));

                                label_x.Invoke((MethodInvoker)(() => label_x.Text = ""));
                                label_y.Invoke((MethodInvoker)(() => label_y.Text = ""));

                                break;
                        }

                        if (messageReceived.Address.Contains("reset") || messageReceived.Address.Contains("Reset"))
                        {
                            //Console.WriteLine("RESET!");
                            foreach (KeyValuePair<string, float> entry in msgVars)
                            {
                                //Console.WriteLine(entry.Key.ToString() + " - " + entry.Value.ToString());
                                vm.SetParam(addressParts[1] + "[" + addressParts[2] + "]." + entry.Key, (float)0);
                            }

                            var message = new SharpOSC.OscMessage("/" + addressParts[1] + "/" + addressParts[2] + "/" + addressParts[3], (float)0, (float)0.5);

                                if (addressParts[2] == "5" || addressParts[2] == "6" || addressParts[2] == "7")
                            {
                                message = new SharpOSC.OscMessage("/" + addressParts[1] + "/" + addressParts[2] + "/" + addressParts[3], (float)0, (float)0);
                            }
                                
                            oscSender.Send(message);

                            if (addressParts[1] == "Strip")
                            {
                                if (addressParts[2] == "5" || addressParts[2] == "6" || addressParts[2] == "7")
                                {
                                    message = new SharpOSC.OscMessage("/Slider/" + addressParts[2] + "/" + addressParts[3], (float)0, (float)0);
                                }
                                else
                                {
                                    message = new SharpOSC.OscMessage("/Slider/" + addressParts[2] + "/" + addressParts[3], (float)-0.5, (float)0);
                                }
                                oscSender.Send(message);
                            }
                        }
                        else
                        {
                            // float tmpFloat = 0;
                            foreach (KeyValuePair<string, float> entry in msgVars)
                            {
                                //Console.WriteLine(entry.Key.ToString() + " - " + entry.Value.ToString());
                                //   tmpFloat = (float)entry.Value;
                                vm.SetParam(addressParts[1] + "[" + addressParts[2] + "]." + entry.Key, (float)entry.Value);
                                //var message = new SharpOSC.OscMessage(messageReceived.Address, (float)entry.Value);
                                //oscSender.Send(new SharpOSC.OscMessage(messageReceived.Address, (float)entry.Value));
                                //message = new SharpOSC.OscMessage(messageReceived.Address + "/Text", (float)entry.Value);
                                //oscSender.Send(new SharpOSC.OscMessage(messageReceived.Address + "/Text", (float)entry.Value));
                             }
                        }
                    }

                        /*
                         * if (messageReceived.Address.Contains("nvm"))
                         {
                             //don't do anything
                         }
                         else if (messageReceived.Address.Contains("Color"))
                         {
                            vm.SetParam(addressParts[1] + "[" + addressParts[2] + "]." + addressParts[3] + "_y", (float)messageReceived.Arguments[0]);
                            vm.SetParam(addressParts[1] + "[" + addressParts[2] + "]." + addressParts[3] + "_x", (float)messageReceived.Arguments[1] - (float)0.5);
                            label_status.Invoke((MethodInvoker)(() => label_status.Text = messageReceived.Arguments[0].ToString() + " - " + messageReceived.Arguments[0].ToString()));
                         }
                         else
                         {
                             if (messageReceived.Address.Contains("reset"))
                             {
                                 vm.SetParam(addressParts[1]+"[" + addressParts[2] + "]." + addressParts[3] , 0);
                             }
                             else
                             {
                                 foreach (float test in messageReceived.Arguments)
                                 {
                                     //label_status.Invoke((MethodInvoker)(() => label_status.Text = test.ToString()));
                                     label_status.Invoke((MethodInvoker)(() => label_status.Text = addressParts[1] + "[" + addressParts[2] + "]." + addressParts[3]));
                                     vm.SetParam(addressParts[1] + "[" + addressParts[2] + "]." + addressParts[3], test);
                                 }
                             }

                         }
                         */

                    }
                    return;
             };

            var listener = new UDPListener(Properties.Settings.Default.portIn, callback);
            //MessageBox.Show("Starting OSC Listener");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //MessageBox.Show("this is a test, please work", "hello world");
            label1.Text = "this a a test of the click thing";
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }

        public void Form1_FormClosing(object sender, EventArgs e)
        {
            //t.Suspend();
            Run = false;
            Environment.Exit(Environment.ExitCode);
            Application.ExitThread();
            Application.Exit();
        }

        public void setNeedsSave(bool needsSaving)
        {
            if (needsSaving)
            {
                label_status.Text = "Restart needed before the new setting will take effect.";
                restartButton.Text = "Save And\nRestart";
            }
            else
            {
                label_status.Text = "";
                restartButton.Text = "Restart";
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            //Run = false;
            //t.Suspend();
            Environment.Exit(Environment.ExitCode);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Sync();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            int x = Convert.ToInt32(textBox1.Text);
            Properties.Settings.Default.portIn = x;
            setNeedsSave(true);
        }

        private void restartButton_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
            System.Windows.Forms.Application.Restart();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            int x = Convert.ToInt32(textBox2.Text);
            Properties.Settings.Default.portOut = x;
            setNeedsSave(true);
        }

        private void label4_Click(object sender, EventArgs e)
        {
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            IPAddress broadcast;
            bool testflag = IPAddress.TryParse(textBox3.Text, out broadcast);
            Console.WriteLine(testflag);
            if (!testflag)
            {
                label_status.Text = "IP Address not valid";
            }
            else
            {
                Properties.Settings.Default.broadcastIP = textBox3.Text;
                setNeedsSave(true);
            }
        }

        private void label4_Click_1(object sender, EventArgs e)
        {
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.portIn = 8001;
            textBox1.Text = "8001";
            Properties.Settings.Default.portOut = 9000;
            textBox2.Text = "9000";
            Properties.Settings.Default.broadcastIP = "192.168.0.255";
            textBox3.Text = "192.168.0.255";
            setNeedsSave(true);
        }

        private void toolTip1_Popup(object sender, PopupEventArgs e)
        {
        }

        private void toolTip1_Popup_1(object sender, PopupEventArgs e)
        {
        }
    }
}