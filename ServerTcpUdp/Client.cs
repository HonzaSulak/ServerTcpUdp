using System.Net;
using System.Net.Sockets;

namespace ServerTcpUdp;
public abstract class Client
{
    public string Username { get; set; }
    public string Secret { get; set; }
    public string DisplayName { get; set; }
    public ushort Id { get; set; }
    public States CurrentState { get; set; }
    public Channel? CurrentChannel { get; set; }
    public bool I_TCP_O_UDP { get; set; }
    public List<UDPMessage>? _messages = null;
    public UdpClient? UdpCli { get; set; }


    public IPEndPoint? RemoteEP { get; set; }

    public static ushort cnt = 1;

    protected Client()
    {
        Username = "";
        Secret = "";
        DisplayName = "";
        Id = cnt++;
        CurrentState = States.Start;
    }

    public abstract Task SendToClient(string message);

    public abstract Task ReadMessage();
    public abstract Task SendFunc(UDPMessage message, byte[] function);

    public abstract Task DisconnectClient();

    public abstract Task EndServer();
//Display message
    public static void DisplayMessage(bool IO, IPAddress address, int port, string type)
    {
        if (IO)//input
        Console.WriteLine($"RECV {address}:{port} | {type}");
        else//output
        Console.WriteLine($"SENT {address}:{port} | {type}");
    }
}

public enum States
{
    Start,
    Auth,
    Open,
    Error,
    End
}