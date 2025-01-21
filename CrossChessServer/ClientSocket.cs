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

        // 心跳消息时间
        private DateTime lastHeartbeatTime = DateTime.MinValue;
        // 心跳消息超时时间（秒）
        private static int TIME_OUT = 60;

        public ClientSocket(Socket socket)
        {
            this.socket = socket;
            this.clientID = CLIENT_INDEX;
            CLIENT_INDEX++;
        }

        #region "心跳消息"
        /// <summary>
        /// 开启心跳消息检测的线程
        /// </summary>
        public void StartCheckTimeOut()
        {
            ThreadPool.QueueUserWorkItem(CheckHeartMessage);
        }

        // 线程方法，每0.1s检测一次心跳消息超时
        private void CheckHeartMessage(object obj)
        {
            while(isConnected)
            {
                CheckHeartMessageTimeOut();
                Thread.Sleep(100);
            }
        }
        
        private void CheckHeartMessageTimeOut()
        {
            if (lastHeartbeatTime == DateTime.MinValue || !isConnected)
            {
                return;
            }
            TimeSpan timeSpan = DateTime.UtcNow - lastHeartbeatTime;
            if (timeSpan.TotalSeconds > TIME_OUT)
            {
                Console.WriteLine("客户端{0}心跳超时，即将断开连接", this.clientID);
                ServerSocket.Instance.RemoveClient(this.clientID);
                this.Close();
            }
        }
        #endregion

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
            if (isConnected)
            {
                try
                {
                    socket.Send(message.ConvertToByteArray());
                    //Console.WriteLine("发送消息给客户端，消息ID: " + message.GetMessageID());
                }
                catch (Exception e)
                {
                    Console.WriteLine("发送消息出错: " + e.Message);
                }
            }
        }

        public void Receive()
        {
            if (!isConnected)
            {
                return;
            }

            try
            {
                if(socket.Available > 0)
                {
                    byte[] buffer = new byte[1024 * 1024];
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

        public void SendHallClients()
        {
            this.Send(new HallClients(ServerSocket.Instance.hallClientDict));
        }

        private void HandleMessage(object obj)
        {
            byte[] buffer = (byte[])obj;
            int messageID = BitConverter.ToInt32(buffer, 0);
            //Console.WriteLine("处理客户端消息，消息ID: {0}", (MessageID)messageID);
            switch (messageID)
            {
                case (int)MessageID.RoundInfo:
                    Round round = new Round();
                    round.ReadFromBytes(buffer, sizeof(int)); // 跳过消息ID
                    Console.WriteLine("客户端{0}保存战局", this.clientID);
                    RoundManager.SaveRoundInfo(round); // 保存战局信息到txt
                    break;
                case (int)MessageID.RequestRoundList:
                    Round[] rounds = RoundManager.GetRoundList(); // 收到客户端请求后从txt中读取战局信息
                    Console.WriteLine("客户端{0}请求战局信息", this.clientID);
                    this.Send(new ProvideRoundList(rounds)); // 把战局信息发送给客户端
                    break;
                case (int)MessageID.EnterHall:
                    EnterHall enterHall = new EnterHall();
                    enterHall.ReadFromBytes(buffer, sizeof(int));
                    Console.WriteLine("客户端{0}进入大厅，用户名: {1}", this.clientID, enterHall.userName);
                    this.Send(new AllowEnterHall(this.clientID)); // 给客户端发送准许进入大厅的消息
                    ServerSocket.Instance.AddToHallClientDict(this.clientID, enterHall.userName); // 把进入大厅的客户端信息保存到大厅列表
                    break;
                case (int)MessageID.RequestHallClients:
                    this.Send(new HallClients(ServerSocket.Instance.hallClientDict)); // 向客户端发送大厅用户数据
                    break;
                case (int)MessageID.ClientQuit:
                    Console.WriteLine("客户端{0}发来断开连接", this.clientID);
                    ServerSocket.Instance.RemoveClient(this.clientID);
                    this.Close();
                    break;
                case (int)MessageID.QuitHall:
                    Console.WriteLine("客户端{0}退出大厅", this.clientID);
                    ServerSocket.Instance.RemoveFromHallClientDict(this.clientID);
                    break;
                case (int)MessageID.SendBattleRequest:
                    SendBattleRequest sendBattleRequest = new SendBattleRequest();
                    sendBattleRequest.ReadFromBytes(buffer, sizeof(int));
                    Console.WriteLine("客户端{0}向客户端{1}发送对战请求", this.clientID, sendBattleRequest.riverClientID);
                    // 给被发送请求的客户端发送对战请求
                    ServerSocket.Instance.clientDict[sendBattleRequest.riverClientID].Send(
                        new SendBattleRequest(sendBattleRequest.riverClientID, sendBattleRequest.senderClientName)); 
                    // 把被发送请求的客户端设置成繁忙
                    ServerSocket.Instance.SetHallClientIdle(sendBattleRequest.riverClientID, false);
                       break;
                case (int)MessageID.HeartMessage:
                    lastHeartbeatTime = DateTime.UtcNow;
                    Console.WriteLine($"Heartbeat received. lastHeartbeatTime updated to: {lastHeartbeatTime}");
                    break;
                default:
                    break;
            }
        }
    }
}
