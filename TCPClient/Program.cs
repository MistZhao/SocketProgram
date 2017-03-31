using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ContentHeader;

namespace TCPClient
{
    class Program
    {
        static Socket objClient;
        static string strIp;
        static int iHeartBeat = 1;// 用于计时
        static int iTimerInterval = 100;
        static int iRetryTimes = 0;// 心跳包发送的次数
        static Int32 iStartIndex = 0;// 数组中的当前位置，动态计算得出
        static List<byte> liRecList = new List<byte>();// 存储接收到的报文数据
        static List<byte> liSizeList = new List<byte>();// 存储接收到的数据长度
        static object objLockHeartBeat = new object();// 用于心跳计数的同步
        static object objLockSendMsg = new object();// 用于发送信息的同步

        static BackgroundWorker bgwRecMsg = new BackgroundWorker();
        static System.Timers.Timer objSendTimer = new System.Timers.Timer();
        static System.Timers.Timer objHeartTimer = new System.Timers.Timer();

        static Queue<string> objSendQueue = new Queue<string>();// 用于发送

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

            bgwRecMsg.DoWork += bgwRecMsg_DoWork;
            bgwRecMsg.RunWorkerAsync();

            objSendTimer.Interval = 2000;
            objSendTimer.Elapsed += objSendTimer_Elapsed;
            objSendTimer.Start();

            objHeartTimer.Interval = iTimerInterval;
            objHeartTimer.Elapsed += objHeartTimer_Elapsed;
            objHeartTimer.Start();
            Console.WriteLine(DateTime.Now.ToLongTimeString());
            Console.ReadKey();
        }

        /// <summary>
        /// 恢复心跳计数，这个在一个最高级别的线程里
        /// </summary>
        static void ResetHeartBeat()
        {
            lock (objLockHeartBeat)
            {
                iHeartBeat = 1;
                iRetryTimes = 0;
            }
        }

        static void bgwRecMsg_DoWork(object sender, DoWorkEventArgs e)
        {
            while(true)
            {
                byte[] byMsg = new byte[1024];
                Int32 iLen = objClient.Receive(byMsg);// 使用阻塞式套接字，没有收到内容则一直阻塞在这里

                // 收到数据后重置心跳计数
                Thread objResetHeartBeat = new Thread(ResetHeartBeat);
                objResetHeartBeat.Priority = ThreadPriority.Highest;
                objResetHeartBeat.Start();

                // 解析收到的数据
                while (true)
                {
                    // 首先获取完整的数据长度
                    if ((iLen - iStartIndex) >= (sizeof(Int32) - liSizeList.Count))
                    {
                        byte[] bySize = new byte[sizeof(Int32) - liSizeList.Count];
                        Buffer.BlockCopy(byMsg, iStartIndex, bySize, 0, sizeof(Int32) - liSizeList.Count);
                        iStartIndex += sizeof(Int32) - liSizeList.Count;// 当前位置增加数据长度剩余字节的大小
                        liSizeList.AddRange(bySize);
                        if (liSizeList.Count != sizeof(Int32))// 检验存储的数据长度大小是否正确
                        {
                            Console.WriteLine("liSizeList中字节数错误：" + liSizeList.Count);
                            return;
                        }
                        Int32 iNetworkDataLen = BitConverter.ToInt32(liSizeList.ToArray(), 0);
                        Int32 iDataLen = IPAddress.NetworkToHostOrder(iNetworkDataLen);// 将数据从网络字节序转换为本地字节序（字符串不需要此操作）

                        // 接着获取完整的报文数据
                        if (iLen - iStartIndex >= iDataLen - liRecList.Count)
                        {
                            byte[] byRecMsg = new byte[iDataLen - liRecList.Count];
                            Buffer.BlockCopy(byMsg, iStartIndex, byRecMsg, 0, iDataLen - liRecList.Count);
                            iStartIndex += iDataLen - liRecList.Count;// 当前位置增加报文数据剩余字节的大小
                            liRecList.AddRange(byRecMsg);
                        }
                        else
                        {
                            byte[] byLastRecMsg = new byte[iLen - iStartIndex];
                            Buffer.BlockCopy(byMsg, iStartIndex, byLastRecMsg, 0, iLen - iStartIndex);
                            liRecList.AddRange(byLastRecMsg);
                            iStartIndex = 0;// 当前位置清0
                            break;// 解析完成socket接收到的数据（最后是不完整的数据），退出循环继续读取socket
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
                        iStartIndex = 0;// 当前位置清0
                        break;// 解析完成socket接收到的数据（最后是不完整的数据长度），退出循环继续读取socket
                    }
                }
            }
        }

        static void objHeartTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (objLockHeartBeat)
            {
                if (!objHeartTimer.Enabled)
                {
                    return;
                }
                if (!objClient.Connected)// 服务器关闭时客户端socket不会自动关闭，必须通过检测判断
                {
                    objHeartTimer.Stop();
                    objSendTimer.Stop();
                    Console.WriteLine("服务器已关闭连接！");
                    iHeartBeat = 1;
                    iRetryTimes = 0;
                    iStartIndex = 0;
                    liRecList.Clear();
                    liSizeList.Clear();
                    objSendQueue.Clear();

                    objClient.Close();
                    return;
                }
                iHeartBeat++;
                if (iHeartBeat >= 10000 / iTimerInterval)
                {
                    iRetryTimes++;
                    iHeartBeat = 1;
                    Console.WriteLine(DateTime.Now.ToLongTimeString());
                    if (iRetryTimes > 3)
                    {
                        Console.WriteLine("与服务器连接断开！");
                        iHeartBeat = 1;
                        iRetryTimes = 0;
                        iStartIndex = 0;
                        liRecList.Clear();
                        liSizeList.Clear();
                        objSendQueue.Clear();

                        objClient.Shutdown(SocketShutdown.Both);
                        objClient.Close();// 主动关闭后，会有time_wait的现象，对于客户端来说无所谓，可以通过修改内核减少这个时间
                        objSendTimer.Stop();
                        objHeartTimer.Stop();
                        return;
                    }
                    else if (iRetryTimes > 1)
                    {
                        Console.WriteLine("重发一次心跳包。。。");
                    }
                    string strHeart = "FF FF FF FF";
                    objSendQueue.Enqueue(strHeart);

                    Thread objSendMsg = new Thread(SendMsg);
                    objSendMsg.Start();
                }
            }
        }

        /// <summary>
        /// 定时向服务器发送内容，格式为：长度+数据（时间+"  "+机器名+" "+IP+"  "+"test"）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void objSendTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            string strSendMsg = DateTime.Now +" " + Dns.GetHostName() + " " + strIp + " " + "test";
            objSendQueue.Enqueue(strSendMsg);

            Thread objSendMsg = new Thread(SendMsg);
            objSendMsg.Start();
        }

        static void SendMsg()
        {
            lock (objLockSendMsg)
            {
                if (objSendQueue.Count > 0&&objClient.Connected)
                {
                    string strMsg = objSendQueue.Dequeue();
                    List<byte> liSendMsg = new List<byte>();
                    byte[] byMsg = Encoding.Default.GetBytes(strMsg);

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
    }
}
