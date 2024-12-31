using System.Net.Sockets;

namespace CrossChessServer
{
    internal class Program
    {
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
                        string ip = "127.0.0.1";
                        int port = 8080;
                        int maxNum = 10;

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
