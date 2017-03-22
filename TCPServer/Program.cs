using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TCPServer
{
    class Program
    {
        static Socket objServer;

        static void Main(string[] args)
        {
            objServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint objIpep = new IPEndPoint(IPAddress.Parse("9.5.3.156"),9000);
            objServer.Bind(objIpep);
            objServer.Listen(2);
            BackgroundWorker bgwListen = new BackgroundWorker();
            bgwListen.DoWork += bgwListen_DoWork;
            bgwListen.RunWorkerAsync();
            Console.WriteLine("Waiting for connections...");
            Console.ReadKey();
        }

        static void bgwListen_DoWork(object sender, DoWorkEventArgs e)
        {
            Socket objConnection = null;
            while(true)
            {
                objConnection = objServer.Accept();
                IPEndPoint objIpep = (IPEndPoint)objConnection.RemoteEndPoint;
                Console.WriteLine(string.Format("客户端{0}:{1}连接成功！",objIpep.Address.ToString(),objIpep.Port));
                BackgroundWorker bgwRec = new BackgroundWorker();
                bgwRec.DoWork += bgwRec_DoWork;
                bgwRec.RunWorkerAsync(objConnection);
            }
        }

        static void bgwRec_DoWork(object sender, DoWorkEventArgs e)
        {
            StringBuilder strSb = new StringBuilder();
            Socket objClient = (Socket)e.Argument;
            Int32 iStartIndex = 0;// 数组中的当前位置，动态计算得出
            List<byte> liRecList = new List<byte>();// 存储接收到的报文数据
            List<byte> liSizeList = new List<byte>();// 存储接收到的报文长度数据

            while(true)
            {
                System.Threading.Thread.Sleep(30000);
                byte[] byMsg = new byte[1024];
                Int32 iLen = objClient.Receive(byMsg);

                while (true)
                {
                    if ((iLen - iStartIndex) >= (sizeof(Int32)-liSizeList.Count))
                    {
                        byte[] bySize = new byte[sizeof(Int32)-liSizeList.Count];
                        Buffer.BlockCopy(byMsg, iStartIndex, bySize, 0, sizeof(Int32) - liSizeList.Count);
                        iStartIndex += sizeof(Int32) - liSizeList.Count;
                        liSizeList.AddRange(bySize);
                        if(liSizeList.Count!=sizeof(Int32))
                        {
                            Console.WriteLine("liSizeList中字节数错误："+liSizeList.Count);
                            return;
                        }
                        Int32 iNetworkDataLen = BitConverter.ToInt32(liSizeList.ToArray(),0);
                        Int32 iDataLen = IPAddress.NetworkToHostOrder(iNetworkDataLen);

                        if (iLen - iStartIndex >= iDataLen-liRecList.Count)
                        {
                            byte[] byRecMsg = new byte[iDataLen - liRecList.Count];
                            Buffer.BlockCopy(byMsg, iStartIndex, byRecMsg, 0, iDataLen - liRecList.Count);
                            iStartIndex += iDataLen - liRecList.Count;
                            liRecList.AddRange(byRecMsg); 
                        }
                        else
                        {
                            byte[] byLastRecMsg = new byte[iLen - iStartIndex];
                            Buffer.BlockCopy(byMsg, iStartIndex, byLastRecMsg, 0, iLen - iStartIndex);
                            liRecList.AddRange(byLastRecMsg);
                            iStartIndex = 0;
                            break;
                        }

                        string strMsg = Encoding.Default.GetString(liRecList.ToArray());
                        liRecList.Clear();
                        liSizeList.Clear();
                        Console.WriteLine(strMsg);
                    }
                    else
                    {
                        byte[] byLast = new byte[iLen - iStartIndex];
                        Buffer.BlockCopy(byMsg, iStartIndex, byLast, 0, iLen - iStartIndex);
                        liSizeList.AddRange(byLast);
                        iStartIndex = 0;
                        break;
                    }
                }
            }
        }

        private static IPAddress[] GetHostIp()
        {
            return Dns.GetHostEntry(Dns.GetHostName()).AddressList;
        }
    }
}
