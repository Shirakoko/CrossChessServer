using System.Net;
using System.Net.Sockets;
using CrossChessServer.MessageClasses;
namespace CrossChessServer
{
    /// <summary>
    /// 用户信息结构体
    /// </summary>
    public struct UserInfo
    {
        public string Name; // 用户名
        public bool IsIdle; // 用户是否空闲

        public UserInfo(string name, bool isIdle)
        {
            Name = name;
            IsIdle = isIdle;
        }
    }

    /// <summary>
    /// 在线战局状态结构体
    /// </summary>
    public struct OnlineRoundState
    {
        public int plater1ID; // 玩家1的ID
        public int plater2ID; // 玩家2的ID
        public int[] grids;

        public OnlineRoundState(int plater1ID, int plater2ID)
        {
            this.plater1ID = plater1ID;
            this.plater2ID = plater2ID;
            this.grids = new int[9];
        }
    }

    internal class ServerSocket
    {
        // 在线对战棋局状态
        public static int ONLINE_ROUND_INDEX = 1001;

        // clientDict锁定机制，避免遍历时被修改
        private readonly object _clientDictLock = new object();
        // hallClientDict锁定机制，避免遍历时被修改
        private readonly object _hallClientDictLock = new object();

        private static readonly ServerSocket instance = new ServerSocket();

        // 私有构造函数，防止外部实例化
        private ServerSocket() { }

        // 公共静态方法，提供全局访问点
        public static ServerSocket Instance
        {
            get
            {
                return instance;
            }
        }

        /// <summary>
        /// 服务端启动Socket
        /// </summary>
        public Socket socket;

        /// <summary>
        /// 用字典管理连入的所有客户端Socket
        /// </summary>
        public Dictionary<int, ClientSocket> clientDict = new Dictionary<int, ClientSocket>();

        /// <summary>
        /// 用字典管理大厅里的用户
        /// </summary>
        public Dictionary<int, UserInfo> hallClientDict = new Dictionary<int, UserInfo>();

        /// <summary>
        /// 用字典管理在线战局状态
        /// </summary>
        public Dictionary<int, OnlineRoundState> onlineRoundDict = new Dictionary<int, OnlineRoundState>();

        /// <summary>
        /// 用字典管理客户端发来的OnlineRoundResult，并转成Round
        /// </summary>
        public Dictionary<int, Round> onlineRoundResultDict = new Dictionary<int, Round>();

        /// <summary>
        /// 服务器是否开启
        /// </summary>
        private bool isStarted = false;

        /// <summary>
        /// 开启服务器
        /// </summary>
        /// <param name="ip">服务器IP（填本机）</param>
        /// <param name="port">服务器端口号</param>
        /// <param name="maxNum">最多连入客户端个数</param>
        public void StartServer(string ip = "127.0.0.1", int port = 8080, int maxNum = 10)
        {
            isStarted = true;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            socket.Bind(iPEndPoint);
            socket.Listen(maxNum);

            // 开启接受客户端连入的线程
            ThreadPool.QueueUserWorkItem(AcceptClient);
            // 开启消息接收线程
            ThreadPool.QueueUserWorkItem(ReceiveMsg);
        }

        /// <summary>
        /// 关闭服务器
        /// </summary>
        public void CloseServer()
        {
            isStarted=false;
            foreach(ClientSocket client in clientDict.Values)
            {
                client.Close();
            }
            lock (_clientDictLock)
            {
                clientDict.Clear();
            }
            if (socket != null)
            {
                socket.Close();
                socket = null;
            }
        }

        /// <summary>
        /// 在大厅用户字典中加入用户
        /// </summary>
        /// <param name="clientID">客户端ID</param>
        /// <param name="name">用户名</param>
        public void AddToHallClientDict(int clientID, string name)
        {
            lock (_hallClientDictLock)
            {
                if (hallClientDict.ContainsKey(clientID) == false)
                {
                    // 默认用户是空闲的
                    UserInfo userInfo = new UserInfo(name, true);
                    hallClientDict.Add(clientID, userInfo);
                }
            }

            // 通知所有客户端大厅用户数据变化
            foreach (int clinetID in hallClientDict.Keys)
            {
                clientDict[clinetID].SendHallClients();
            }
        }

        /// <summary>
        /// 从大厅用户字典中移除用户
        /// </summary>
        /// <param name="clientID">客户端ID</param>
        public void RemoveFromHallClientDict(int clientID)
        {
            if (hallClientDict.ContainsKey(clientID))
            {
                hallClientDict.Remove(clientID);
            }
            // 通知所有客户端大厅用户数据变化
            foreach (int clinetID in hallClientDict.Keys)
            {
                clientDict[clinetID].SendHallClients();
            }
        }

        /// <summary>
        /// 设置某个大厅用户的闲忙状态
        /// </summary>
        /// <param name="clientID">用户ID</param>
        /// <param name="isIdle">是否空闲</param>
        public void SetHallClientIdle(int clientID, bool isIdle)
        {
            if (hallClientDict.ContainsKey(clientID))
            {
                // 获取当前用户信息
                UserInfo userInfo = hallClientDict[clientID];
                // 更新用户状态
                userInfo.IsIdle = isIdle;
                // 将更新后的用户信息重新存入字典
                hallClientDict[clientID] = userInfo;
            }

            // 通知所有客户端大厅用户数据变化
            foreach (int clinetID in hallClientDict.Keys)
            {
                clientDict[clinetID].SendHallClients();
            }
        }

        /// <summary>
        /// 更新战局状态
        /// </summary>
        /// <param name="onlineRoundIndex">战局ID</param>
        /// <param name="gridIndex">格子ID</param>
        /// <param name="clientID">客户端ID</param>
        public void UpdateOnlineRoundState(int onlineRoundIndex, int gridIndex, int clientID)
        {
            if (onlineRoundDict.ContainsKey(onlineRoundIndex))
            {
                onlineRoundDict[onlineRoundIndex].grids[gridIndex] = clientID;
                Console.WriteLine("战局{0}更新，位置{1}被客户端{2}占据", onlineRoundIndex, gridIndex, clientID);
            }
            else
            {
                Console.WriteLine("战局{0}已不存在！", onlineRoundIndex);
            }
        }

        /// <summary>
        /// 获得某个客户端的对手客户端ID
        /// </summary>
        /// <param name="onlineRoundIndex">战局ID</param>
        /// <param name="clientID">客户端ID</param>
        /// <returns>对手客户端ID</returns>
        public int GetRiverClient(int onlineRoundIndex, int clientID)
        {
            if (onlineRoundDict.ContainsKey(onlineRoundIndex))
            {
                int player1ID = onlineRoundDict[onlineRoundIndex].plater1ID;
                int player2ID = onlineRoundDict[onlineRoundIndex].plater2ID;
                if (player1ID == clientID)
                {
                    return player2ID;
                }
                if(player2ID == clientID)
                {
                    return player1ID;
                }
                Console.WriteLine("战局{0}不包含客户端{1}，查询出错！", onlineRoundIndex, clientID);
                return 0;
            }
            else
            {
                Console.WriteLine("战局{0}已不存在！", onlineRoundIndex);
                return 0;
            }
        }

        /// <summary>
        /// 移除客户端
        /// </summary>
        /// <param name="clientID">客户端ID</param>
        public void RemoveClient(int clientID)
        {
            lock (_clientDictLock)
            {
                if (clientDict.ContainsKey(clientID))
                {
                    clientDict.Remove(clientID);
                    Console.WriteLine("客户端{0}从字典中移除", clientID);
                }
            }

            lock (_hallClientDictLock)
            {
                if (hallClientDict.ContainsKey(clientID))
                {
                    hallClientDict.Remove(clientID);
                    Console.WriteLine("客户端{0}从大厅用户字典中移除", clientID);

                    // 通知所有客户端大厅用户数据变化
                    foreach (int clinetID in hallClientDict.Keys)
                    {
                        clientDict[clinetID].SendHallClients();
                    }
                }
            }
        }

        #region "线程方法"
        private void AcceptClient(object obj)
        {
            while(isStarted)
            {
                try
                {
                    Socket clientSocket = socket.Accept();
                    // 获取客户端的IP地址和端口号
                    IPEndPoint clientEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;
                    ClientSocket client = new ClientSocket(clientSocket);
                    // 客户端Socket开启检测心跳消息是否超时的线程
                    client.StartCheckTimeOut();
                    lock (_clientDictLock)
                    {
                        clientDict.Add(client.clientID, client);
                    }
                    Console.WriteLine("客户端连入，ID: {0}，IP地址: {1}，端口: {2}", client.clientID, 
                        clientEndPoint.Address.ToString(), clientEndPoint.Port);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("客户端连入错误: " + ex.Message);
                }
            }
        }

        private void ReceiveMsg(object obj)
        {
            while(isStarted)
            {
                if (clientDict.Count > 0)
                {
                    lock (_clientDictLock)
                    {
                        foreach (ClientSocket client in clientDict.Values)
                        {
                            client.Receive();
                        }
                    }
                }
            }
        }
        #endregion
    }
}