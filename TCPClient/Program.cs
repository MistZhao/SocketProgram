using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ContentHeader;

namespace TCPClient
{
    class Program
    {
        static Socket objClient;
        static string strIp;

        static void Main(string[] args)
        {
            objClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            objClient.Connect("9.5.3.156", 9000);
            foreach (IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    strIp = ip.ToString();
                    string[] strIpSplit = strIp.Split('.');
                    if (strIpSplit[0] == "9" && strIpSplit[1] == "5")
                    {
                        break;
                    }
                }
            }

            System.Timers.Timer objSendTimer = new System.Timers.Timer();
            objSendTimer.Interval = 2000;
            objSendTimer.Elapsed += objSendTimer_Elapsed;
            objSendTimer.Start();
            Console.ReadKey();
        }

        static void objSendTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            List<byte> liSendMsg = new List<byte>();
            string strSendMsg = DateTime.Now +" " + Dns.GetHostName() + " " + strIp + " " + "test";
            byte[] byMsg = Encoding.Default.GetBytes(strSendMsg);

            StuContentHeader stuHeaderSize = new StuContentHeader();
            stuHeaderSize.iHeaderSize = byMsg.Length;
            Int32 iSize = IPAddress.HostToNetworkOrder(stuHeaderSize.iHeaderSize);
            byte[] bySendSize = BitConverter.GetBytes(iSize);
            liSendMsg.AddRange(bySendSize);
            liSendMsg.AddRange(byMsg);

            objClient.Send(liSendMsg.ToArray());
        }

    }
}
