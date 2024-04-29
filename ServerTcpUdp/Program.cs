using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ServerTcpUdp;
public class Program
{
    static TcpListener? _tcpListener;
    static UdpClient? _udpServer;
    public static List<Channel> _channels = new List<Channel>();
    public static List<int> usedPorts = new List<int>();
    private static readonly object _lock = new object(); // Lock object for synchronization
    public static IPAddress _address = IPAddress.Parse("0.0.0.0");
    public static int _retransmissions = 3;
    public static int _port = 4567;
    public static int _delay = 250;

    static async Task Main(string[] args)
    {
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            DisconnectAndExit();
        };
        try
        {
            //Parsing arguments
            if (args.Length == 1 && Array.IndexOf(args, "-h") != -1)
            {
                PrintHelp();
                return;
            }
            if (Array.IndexOf(args, "-l") != -1)
                _address = IPAddress.Parse(args[Array.IndexOf(args, "-l") + 1]);
            if (Array.IndexOf(args, "-p") != -1)
                _port = int.Parse(args[Array.IndexOf(args, "-p") + 1]);
            if (Array.IndexOf(args, "-d") != -1)
                _delay = int.Parse(args[Array.IndexOf(args, "-d") + 1]);
            if (Array.IndexOf(args, "-r") != -1)
                _retransmissions = int.Parse(args[Array.IndexOf(args, "-r") + 1]);


            Channel default_channel = new Channel("default");
            _channels.Add(default_channel);

            _ = Task.Run(() => WritePackets());
            Task tcpTask = TCPHandleConnections();
            Task udpTask = UDPHandleConnections();

            await Task.WhenAll(tcpTask, udpTask);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            DisconnectAndExit();
        }
    }

    private static async Task TCPHandleConnections()
    {
        _tcpListener = new TcpListener(_address, _port);
        _tcpListener.Start();
        // Console.WriteLine("TCP server started.");
        while (true)
        {
            // Console.WriteLine("Waiting for a TCP connection...");
            if (_tcpListener == null)
            {
                throw new Exception("TCP listener is null.");
            }
            var tcpClient = await _tcpListener.AcceptTcpClientAsync();
            var tcpHandler = new TCP(tcpClient, _channels[0]); // Assuming the default channel for now
            _ = Task.Run(() => tcpHandler.Process());
        }
    }

    private static async Task UDPHandleConnections()
    {
        var endpoint = new IPEndPoint(_address, _port);
        _udpServer = new UdpClient(endpoint);
        // Console.WriteLine(value: "UDP server started.");
        while (true)
        {
            if (_udpServer == null)
            {
                throw new Exception("UDP server is null.");
            }
            // Console.WriteLine("Waiting for a UDP connection...");
            UdpReceiveResult result = await _udpServer.ReceiveAsync();//AUTH
            SendAUTHConfirm(result);

            if (!usedPorts.Contains(result.RemoteEndPoint.Port))
            {
                lock (_lock)
                {
                    usedPorts.Add(result.RemoteEndPoint.Port);
                }
                var dynEndPoint = new IPEndPoint(_address, 0);
                var dynClient = new UdpClient(dynEndPoint);

                var udpHandler = new UDP(dynClient, _channels[0], result.RemoteEndPoint, _delay, _retransmissions);
                _ = Task.Run(() => udpHandler.Process(result));
            }
        }
    }

    public static void SendAUTHConfirm(UdpReceiveResult result)
    {
        UDPMessage parse = new UDPMessage();

        byte[] data = result.Buffer;
        if (parse.ParseMessage(data) && _udpServer != null)
        {
            Client.DisplayMessage(true, result.RemoteEndPoint.Address, result.RemoteEndPoint.Port, "AUTH");
            parse.Ref_MessageID = parse.MessageID;
            var send = parse.BuildCONFIRM();
            _ = Task.Run(() => _udpServer.SendAsync(send, send.Length, result.RemoteEndPoint));
            // await _udpServer.SendAsync(send, send.Length, result.RemoteEndPoint);
            Client.DisplayMessage(false, result.RemoteEndPoint.Address, result.RemoteEndPoint.Port, "CONFIRM");
        }
    }
    private static async void DisconnectAndExit()
    {

        var channelsCopy = new List<Channel>(_channels);
        foreach (var channel in channelsCopy)
        {
            var clientsCopy = new List<Client>(channel.Clients);
            foreach (var client in clientsCopy)
            {
                if (client != null)
                {
                    await client.EndServer();
                }
            }
        }
        Environment.Exit(0);

    }

    public static void WritePackets()
    {
        while (true)
        {
            var userInput = Console.ReadLine();
            if (string.IsNullOrEmpty(userInput))
            {
                // Ctrl+D, Ctrl+C, EOF, empty line
                DisconnectAndExit();
                return;
            }
        }
    }
    static void PrintHelp()
    {
        Console.WriteLine("CLI arguments:");
        Console.WriteLine("-l\t0.0.0.0\t\tIP address\tServer listening IP address for welcome sockets");
        Console.WriteLine("-p\t4567\t\tuint16\t\tServer listening port for welcome sockets");
        Console.WriteLine("-d\t250\t\tuint16\t\tUDP CONFIRM timeout");
        Console.WriteLine("-r\t3\t\tuint8\t\tMax UDP retransmissions");
        Console.WriteLine("-h\t\t\t\t\tPrints program help output and exits");
        Console.WriteLine();
        Console.WriteLine("ENTER\tTo terminate the server");
    }
}
