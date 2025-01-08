using System.Net.Sockets;
using CrossChessServer.MessageClasses;

namespace CrossChessServer
{
    internal class Program
    {
        // 默认服务器IP地址
        const string DEFAULT_SERVER_IP = "192.168.1.5";
        // 默认服务器端口号
        const int DEFAULT_SERVER_PORT = 8080;
        // 默认最多连入客户端个数
        const int DEFAULT_MAX_NUM = 10;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            ServerSocket serverSocket = new ServerSocket();

            while (true)
            {
                string input = Console.ReadLine();
                string[] inputArgs = input.Split(' ');
                switch (inputArgs[0])
                {
                    case "Start":
                        string ip = DEFAULT_SERVER_IP;
                        int port = DEFAULT_SERVER_PORT;
                        int maxNum = DEFAULT_MAX_NUM;

                        for (int i = 1; i < inputArgs.Length; i++)
                        {
                            switch (inputArgs[i])
                            {
                                case "--ip":
                                    if (i + 1 < inputArgs.Length)
                                    {
                                        ip = inputArgs[i + 1];
                                        i++;
                                    }
                                    break;
                                case "--port":
                                    if (i + 1 < inputArgs.Length && int.TryParse(inputArgs[i + 1], out int parsedPort))
                                    {
                                        port = parsedPort;
                                        i++;
                                    }
                                    break;
                                case "--maxNum":
                                    if (i + 1 < inputArgs.Length && int.TryParse(inputArgs[i + 1], out int parsedMaxNum))
                                    {
                                        maxNum = parsedMaxNum;
                                        i++;
                                    }
                                    break;
                            }
                        }

                        serverSocket.StartServer(ip, port, maxNum);
                        Console.WriteLine("服务器开启成功，IP地址: {0}，端口：{1}", ip, port);
                        break;
                    case "Quit":
                        serverSocket.CloseServer();
                        Console.WriteLine("服务器关闭");
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
