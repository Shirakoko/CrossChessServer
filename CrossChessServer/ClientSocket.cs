using System;
using System.Net.Sockets;
using System.Text;
using CrossChessServer.MessageClasses;

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
        private void Send(BaseMessage message)
        {
            if (socket != null)
            {
                try
                {
                    socket.Send(message.ConvertToByteArray());
                    Console.WriteLine("发送消息给客户端，消息ID: " + message.GetMessageID());
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
                    if(receiveNum > 0)
                    {
                        // 传给消息处理线程去解析消息
                        ThreadPool.QueueUserWorkItem(HandleMessage, buffer);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("接收消息出错: " + e.Message);
                this.Close();
            }
        }

        private void HandleMessage(object obj)
        {
            byte[] buffer = (byte[])obj;
            int messageID = BitConverter.ToInt32(buffer, 0);
            Console.WriteLine("处理客户端消息，消息ID: {0}", (MessageID)messageID);
            switch (messageID)
            {
                case (int)MessageID.RoundInfo:
                    Round round = new Round();
                    round.ReadFromBytes(buffer, sizeof(int)); // 跳过消息ID
                    RoundManager.SaveRoundInfo(round); // 保存战局信息到txt
                    break;
                case (int)MessageID.RequestRoundList:
                    Round[] rounds = RoundManager.GetRoundList(); // 收到客户端请求后从txt中读取战局信息
                    this.Send(new ProvideRoundList(rounds)); // 把战局信息发送给客户端
                    break;
                case (int)MessageID.EnterHall:
                    EnterHall enterHall = new EnterHall();
                    enterHall.ReadFromBytes(buffer, sizeof(int));
                    ServerSocket.Instance.AddToHallClientDict(this.clientID, enterHall.userName); // 把进入大厅的客户端信息保存到大厅列表
                    this.Send(new AllowEnterHall()); // 给客户端发送准许进入大厅的消息
                    break;
                default:
                    break;
            }
        }
    }
}
