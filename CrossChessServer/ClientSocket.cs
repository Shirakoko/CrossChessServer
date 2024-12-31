using System;
using System.Net.Sockets;
using System.Text;

namespace CrossChessServer
{
    internal class ClientSocket
    {
        // 客户端索引
        private static int CLIENT_INDEX = 1;

        public int clientID;

        public Socket socket;

        public ClientSocket(Socket socket)
        {
            this.socket = socket;
            this.clientID = CLIENT_INDEX;
            CLIENT_INDEX++;
        }

        /// <summary>
        /// 客户端Socket是否连接
        /// </summary>
        public bool isConnected { get { return socket != null && socket.Connected; } }

        /// <summary>
        ///  关闭客户端Socket
        /// </summary>
        public void Close()
        {
            if (socket != null)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket = null;
            }
        }

        /// <summary>
        /// 发送字符串消息给客户端
        /// </summary>
        /// <param name="message">字符串消息</param>
        public void Send(string message)
        {
            if (socket != null)
            {
                try
                {
                    socket.Send(Encoding.UTF8.GetBytes(message));
                }
                catch (Exception e)
                {
                    Console.WriteLine("发送消息出错: " + e.Message);
                }
            }
        }

        public void Receive()
        {
            if (socket == null)
            {
                return;
            }

            try
            {
                if(socket.Available > 0)
                {
                    byte[] buffer = new byte[1024 * 10];
                    int receiveNum = socket.Receive(buffer, 0);

                    string message = Encoding.UTF8.GetString(buffer, 0, receiveNum);
                    ThreadPool.QueueUserWorkItem(HandleMessage, message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("接受消息出错: " + e.Message);
                this.Close();
            }
        }

        private void HandleMessage(object obj)
        {
            string msg = (string)obj;
            if (msg != null)
            {
                Console.WriteLine("客户端发来消息: " +  msg);
            }
        }
    }
}
