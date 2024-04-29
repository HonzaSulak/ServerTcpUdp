using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerTcpUdp;

public class TCP : Client
{
    public TcpClient TcpCli { get; set; }
    StreamReader _reader;
    StreamWriter _writer;
    IPAddress _address;
    int _port;

    public TCP(TcpClient tcpClient, Channel channel)
    {
        Username = "";
        Secret = "";
        DisplayName = "";
        I_TCP_O_UDP = true;
        TcpCli = tcpClient;
        CurrentChannel = channel;
        CurrentState = States.Start;
        var stream = TcpCli.GetStream();
        _reader = new StreamReader(stream);
        _writer = new StreamWriter(stream);
        if (tcpClient.Client.RemoteEndPoint is IPEndPoint ipEndPoint)
        {
            _address = ipEndPoint.Address;
            _port = ipEndPoint.Port;
        }
        else
        {
            Console.Error.WriteLine("ERR: Cannot get remote endpoint.");
            _address = IPAddress.Any;
            _port = 0;
            CleanResourcesAndExit();
            return;
        }
    }
//Send message to client
    public override async Task SendToClient(string message)
    {
        if (message == "")
        {
            CurrentState = States.Error;
            Console.Error.WriteLine("ERR: Message did not pass the check.");
            return;
        }
        if (TcpCli == null || !TcpCli.Connected || _writer == null)
        {
            Console.Error.WriteLine("ERR: TcpClient or writer is null.");
            CurrentState = States.End;
            return;
        }
        await _writer.WriteAsync(message);
        await _writer.FlushAsync();
    }
    //only for UDP
    public override Task SendFunc(UDPMessage message, byte[] function)
    {
        throw new NotImplementedException();
    }
//Read message from client
    public async Task<Message?> ReadFromClient()
    {
        var reply = new StringBuilder();
        char[] buffer = new char[1];
        while (true)
        {
            if (TcpCli == null || !TcpCli.Connected || _reader == null)
            {
                Console.Error.WriteLine("ERR: TcpClient or reader is null.");
                CurrentState = States.End;
                return null;
            }
            await _reader.ReadAsync(buffer, 0, 1);
            if (buffer[0] == '\0')
            {   
                TcpCli.Close();
                CurrentState = States.End;
                return null;
            }
            reply.Append(buffer[0]);
            if (buffer[0] == '\n' && reply.Length >= 2 && reply[reply.Length - 2] == '\r')
            {
                break;
            }
        }

        var message = new Message();
        // Console.WriteLine(reply.ToString());
        if (message.ParseMessage(reply.ToString()))
        {
            DisplayMessage(true, _address, _port, message.TypeToString());
            return message;
        }
        return null;
    }
//Send auth reply
    public async Task SendAuthReply()
    {
        var parsed = await ReadFromClient();
        if (parsed == null)
        {
            if (CurrentState != States.Error || CurrentState != States.End)
            {//message was invalid
                CurrentState = States.Error;
            }
            return;
        }
        string? send;

        if (parsed.MessageType == MsgTypes.AUTH)
        {
            if (parsed.Username == null || parsed.Secret == null || parsed.DisplayName == null)
            {
                send = Message.BuildREPLYnok("Auth failed.");
                DisplayMessage(false, _address, _port, "REPLY");
                await SendToClient(send);
                return;
            }
            Username = parsed.Username;
            Secret = parsed.Secret;
            DisplayName = parsed.DisplayName;

            send = Message.BuildREPLYok("Auth success.");
            DisplayMessage(false, _address, _port, "REPLY");
            await SendToClient(send);
            if (CurrentState != States.Error)
            {
                CurrentState = States.Auth;
            }
        }
    }
//Send join reply
    public async Task SendJoinReply(Message parsed)
    {
        string? send;
        var channel = CurrentChannel;
        if (parsed.ChannelID != null && parsed.ChannelID.Length > 0)
        {
            var find = Program._channels.Find(x => x.Name == parsed.ChannelID);
            if (find == null)
            {
                find = new Channel(parsed.ChannelID);
                Program._channels.Add(find);
            }
            if (CurrentChannel != null)
            {
                CurrentChannel.RemoveClient(this);
                CurrentChannel.BroadcastDisconnection(this);
            }
            CurrentChannel = find;
            CurrentChannel.AddClient(this);
            send = Message.BuildREPLYok("Join success.");
            DisplayMessage(false, _address, _port, "REPLY");
            await SendToClient(send);
            if (CurrentState != States.Error)
            {
                CurrentChannel.BroadcastConnection(this);
                CurrentState = States.Open;
            }
        }
        else
        {
            if (channel != null)
            {
                CurrentChannel = channel;
                if (CurrentChannel.FindClient(this) == null)
                {
                    CurrentChannel.AddClient(this);
                }
            }
            else
            {
                Console.Error.WriteLine("ERR: Channel is null");
                CleanResourcesAndExit();
                return;
            }
            send = Message.BuildREPLYnok($"Join failed");
            DisplayMessage(false, _address, _port, "REPLY");
            await SendToClient(send);
        }
    }
//Read message
    public override async Task ReadMessage()
    {
        var parsed = await ReadFromClient();
        if (parsed == null)
        {
            if (CurrentState != States.Error || CurrentState != States.End)
            {//message was invalid
                CurrentState = States.Error;
            }
            return;
        }
        switch (parsed.MessageType)
        {
            case MsgTypes.MSG:
                if (DisplayName != parsed.DisplayName && !string.IsNullOrEmpty(parsed.DisplayName))
                {
                    DisplayName = parsed.DisplayName;
                }
                if (string.IsNullOrEmpty(parsed.MessageContent))
                {
                    CurrentState = States.Error;
                    break;
                }
                if (CurrentChannel == null)
                {
                    CurrentState = States.Error;
                    break;
                }
                CurrentChannel.BroadcastMessage(this, parsed.MessageContent);
                break;
            case MsgTypes.JOIN:
                if (string.IsNullOrEmpty(parsed.ChannelID))
                {
                    CurrentState = States.Error;
                    break;
                }
                await SendJoinReply(parsed);
                break;
            case MsgTypes.ERR:
                CurrentState = States.End;
                break;
            case MsgTypes.BYE:
                CleanResourcesAndExit();
                break;
            default:
                CurrentState = States.Error;
                break;
        }
    }
//Function to process clients requests based on state
    public async Task Process()
    {
        while (CurrentState != States.End)
        {
            switch (CurrentState)
            {
                case States.Start:
                    await SendAuthReply();
                    break;
                case States.Auth:
                    if (CurrentChannel == null)
                    {
                        CurrentState = States.Error;
                        break;
                    }
                    CurrentChannel.AddClient(this);
                    CurrentState = States.Open;
                    CurrentChannel.BroadcastConnection(this);
                    break;
                case States.Open:
                    await ReadMessage();
                    break;
                case States.Error:
                    DisplayMessage(false, _address, _port, "ERR");
                    await SendToClient(Message.BuildERR(DisplayName, "Invalid message"));
                    CurrentState = States.End;
                    break;
                default:
                    CurrentState = States.Error;
                    break;
            }
        }
        await DisconnectClient();
    }
//Clean resources and exit
    public void CleanResourcesAndExit()
    {
        CurrentState = States.End;
        if (CurrentChannel != null)
        {
            CurrentChannel.RemoveClient(this);
            CurrentChannel.BroadcastDisconnection(this);
            CurrentChannel = null;
        }
    }
//Disconnect client
    public override async Task DisconnectClient()
    {
        CurrentState = States.End;
        if (TcpCli == null || !TcpCli.Connected || _writer == null || _reader == null)
        {
            CleanResourcesAndExit();
            return;
        }
        await SendToClient(Message.BuildBYE());
        DisplayMessage(false, _address, _port, "BYE");

        CleanResourcesAndExit();
    }
    //function does not send message about disconnection from channel
    public override async Task EndServer()
    {
        CurrentState = States.End;
        if (TcpCli == null || !TcpCli.Connected || _writer == null || _reader == null)
        {
            return;
        }
        await SendToClient(Message.BuildBYE());
        DisplayMessage(false, _address, _port, "BYE");
        if (TcpCli != null && TcpCli.Connected)
        {
            TcpCli.Close();
        }
    }
}