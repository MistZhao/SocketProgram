﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using ContentHeader;

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
            Console.WriteLine(Marshal.SizeOf(typeof(StuTest)));
            Console.WriteLine(Marshal.SizeOf(typeof(StuTest1)));
            Console.WriteLine(Marshal.SizeOf(typeof(StuContentHeader)));
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
            int iStuSize = Marshal.SizeOf(typeof(StuContentHeader));// 获取协议头的大小
            Int32 iStartIndex = 0;// 数组中的当前位置，动态计算得出
            List<byte> liRecList = new List<byte>();// 存储接收到的报文数据
            List<byte> liSizeList = new List<byte>();// 存储接收到的数据长度

            while(true)
            {
                System.Threading.Thread.Sleep(30000);// 延时接收，触发“粘包”现象
                byte[] byMsg = new byte[1024];
                Int32 iLen = objClient.Receive(byMsg);

                // 解析收到的数据
                while (true)
                {
                    // 首先获取完整的数据长度
                    if ((iLen - iStartIndex) >= (sizeof(Int32)-liSizeList.Count))
                    {
                        byte[] bySize = new byte[sizeof(Int32)-liSizeList.Count];
                        Buffer.BlockCopy(byMsg, iStartIndex, bySize, 0, sizeof(Int32) - liSizeList.Count);
                        iStartIndex += sizeof(Int32) - liSizeList.Count;// 当前位置增加数据长度剩余字节的大小
                        liSizeList.AddRange(bySize);
                        if(liSizeList.Count!=sizeof(Int32))// 检验存储的数据长度大小是否正确
                        {
                            Console.WriteLine("liSizeList中字节数错误："+liSizeList.Count);
                            return;
                        }
                        Int32 iNetworkDataLen = BitConverter.ToInt32(liSizeList.ToArray(),0);
                        Int32 iDataLen = IPAddress.NetworkToHostOrder(iNetworkDataLen);// 将数据从网络字节序转换为本地字节序（字符串不需要此操作）

                        // 接着获取完整的报文数据
                        if (iLen - iStartIndex >= iDataLen-liRecList.Count)
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
    }
}
