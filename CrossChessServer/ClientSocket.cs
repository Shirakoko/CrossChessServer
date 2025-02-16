using System;
using System.Net.Sockets;
using System.Text;
using CrossChessServer.MessageClasses;
using static CrossChessServer.ServerSocket;

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
                        new SendBattleRequest(this.clientID, sendBattleRequest.senderClientName)); 
                    // 把被发送请求的客户端设置成繁忙
                    ServerSocket.Instance.SetHallClientIdle(sendBattleRequest.riverClientID, false);
                       break;
                case (int)MessageID.ReplyBattleRequest:
                    ReplyBattleRequest replyBattleRequest = new ReplyBattleRequest();
                    replyBattleRequest.ReadFromBytes(buffer, sizeof(int));
                    int riverClientID = replyBattleRequest.riverClientID;
                    bool accept = replyBattleRequest.accept;
                    Console.WriteLine("客户端{0}回复客户端{1}的对战请求，是否接受: {2}", this.clientID, riverClientID, accept);
                    if(!accept) {
                        // TODO 如果是false，把客户端{0}设置成空闲，给客户端{1}发送"你被拒绝了"
                        ServerSocket.Instance.SetHallClientIdle(this.clientID, true);
                    } else {
                        // 如果是true，把双方设置成繁忙
                        ServerSocket.Instance.SetHallClientIdle(this.clientID, false);
                        ServerSocket.Instance.SetHallClientIdle(riverClientID, false);

                        // 通知双方进入对战
                        this.Send(new EnterRound(true, ONLINE_ROUND_INDEX)); // 被请求方是先手
                        ServerSocket.Instance.clientDict[riverClientID].Send(new EnterRound(false, ONLINE_ROUND_INDEX)); // 请求方是后手

                        // 创建战局
                        ServerSocket.Instance.onlineRoundDict.Add(ONLINE_ROUND_INDEX, new OnlineRoundState(this.clientID, riverClientID));
                        ONLINE_ROUND_INDEX++;
                    }
                    break;
                case (int)MessageID.MoveInfo:
                    MoveInfo moveInfo = new MoveInfo();
                    moveInfo.ReadFromBytes(buffer, sizeof(int));
                    // 更新战局信息
                    ServerSocket.Instance.UpdateOnlineRoundState(moveInfo.onlineRoundIndex, moveInfo.pos, this.clientID);
                    // 从战局信息中获取对手的clientID
                    int riverID = ServerSocket.Instance.GetRiverClient(moveInfo.onlineRoundIndex, this.clientID);
                    // 给它的对手发送同样的落子信息
                    ServerSocket.Instance.clientDict[riverID].Send(moveInfo);
                    break;
                case (int)MessageID.OnlineRoundResult:
                    OnlineRoundResult onlineRoundResult = new OnlineRoundResult();
                    onlineRoundResult.ReadFromBytes(buffer, sizeof(int));
                    // 战局ID
                    int roundIndex = onlineRoundResult.roundID;
                    bool isPrevPlayer = onlineRoundResult.isPrevPlayer;
                    string playerName = onlineRoundResult.playerName;
                    int result = onlineRoundResult.result;
                    int[] steps = onlineRoundResult.steps;
                    Console.WriteLine("收到客户端{0}发来的联机对战结果，roundID为{1}", clientID, roundIndex);
                    if(!ServerSocket.Instance.onlineRoundResultDict.ContainsKey(roundIndex))
                    {
                        Round newRound = new Round();
                        newRound.roundID = roundIndex;
                        if(isPrevPlayer) {
                            newRound.player1 = playerName;
                        } else
                        {
                            newRound.player2 = playerName;
                        }
                        newRound.result = result;

                        // 深拷贝 steps 数组
                        newRound.steps = new int[steps.Length];
                        Array.Copy(steps, newRound.steps, steps.Length);

                        // 第一个客户端发来的OnlineRoundResult存入字典
                        ServerSocket.Instance.onlineRoundResultDict.Add(roundIndex, newRound);
                    }
                    else
                    {
                        // 第二个客户端发来的OnlineRoundResult用于校验
                        // 从字典中取出第一个客户端发来的Round
                        Round existRound = ServerSocket.Instance.onlineRoundResultDict[roundIndex];
                        // 校验结果
                        bool pass = true;
                        // 校验 result
                        if (existRound.result != result) {
                            Console.WriteLine("result校验失败,existRound {0}，result {1}", existRound.result, result);
                            pass = false;
                        }

                        // 校验 steps，是否【完全相同】
                        if (!existRound.steps.SequenceEqual(steps)) {
                            Console.WriteLine("steps校验失败");
                            pass = false;
                        }

                        // 若校验通过
                        if (pass) {
                            // 补全剩下那个没有赋值过的 player（1或2）
                            if (string.IsNullOrEmpty(existRound.player1))
                            {
                                existRound.player1 = playerName;
                            }
                            else if (string.IsNullOrEmpty(existRound.player2))
                            {
                                existRound.player2 = playerName;
                            }

                            // 保存 Round 到 txt 文件
                            RoundManager.SaveRoundInfo(existRound);
                            Console.WriteLine("战局ID {0} 校验通过并保存成功", roundIndex);

                            // 从字典中删除该键值对
                            ServerSocket.Instance.onlineRoundResultDict.Remove(roundIndex);
                            Console.WriteLine("战局ID {0} 已从字典中移除", roundIndex);
                        } else
                        {
                            Console.WriteLine("校验失败, 战局 {0} 没保存");
                        }
                    }
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
