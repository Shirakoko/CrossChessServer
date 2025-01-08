using System.Net;
using System.Net.Sockets;
namespace CrossChessServer
{
    internal class ServerSocket
    {
        // clientDict锁定机制，避免遍历时被修改
        private readonly object _lock = new object();

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
        public Dictionary<int, string> hallClientDict = new Dictionary<int, string>();

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

            clientDict.Clear();
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
            lock (_lock)
            {
                hallClientDict.Add(clientID, name);
            }
        }

        /// <summary>
        /// 从大厅用户字典中移除用户
        /// </summary>
        /// <param name="clientID">客户端ID</param>
        public void RemoveFromHallClientDict(int clientID)
        {
            lock (_lock)
            {
                hallClientDict.Remove(clientID);
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
                    lock (_lock)
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
                lock (_lock)
                {
                    if (clientDict.Count > 0)
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