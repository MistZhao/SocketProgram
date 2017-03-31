using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace TCPServer
{
    public class CClientObject
    {
        private Socket m_objClient;
        private Queue<string> m_objSendQueue = new Queue<string>();

        public void SetSocket(Socket objClient)
        {
            m_objClient = objClient;
        }

        public void AddSendMsg(string strMsg)
        {
            m_objSendQueue.Enqueue(strMsg);
        }

        public Socket GetSocket()
        {
            return m_objClient;
        }

        public Queue<string> GetQueue()
        {
            return m_objSendQueue;
        }
    }
}
