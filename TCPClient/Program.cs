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

        /// <summary>
        /// 定时向服务器发送内容，格式为：长度+数据（时间+"  "+机器名+" "+IP+"  "+"test"）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void objSendTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            List<byte> liSendMsg = new List<byte>();
            string strSendMsg = DateTime.Now +" " + Dns.GetHostName() + " " + strIp + " " + "test";
            byte[] byMsg = Encoding.Default.GetBytes(strSendMsg);

            StuContentHeader stuHeaderSize = new StuContentHeader();// 使用结构体作为协议头
            stuHeaderSize.iHeaderSize = byMsg.Length;// 获取数据的长度
            Int32 iSize = IPAddress.HostToNetworkOrder(stuHeaderSize.iHeaderSize);// 将数据从本地字节序转换为网络字节序（字符串不需要此操作）

            byte[] bySendSize = BitConverter.GetBytes(iSize);
            liSendMsg.AddRange(bySendSize);// 添加数据的长度
            liSendMsg.AddRange(byMsg);// 添加数据

            objClient.Send(liSendMsg.ToArray());
        }

    }
}
