using System.Net;
using System.Net.Sockets;

namespace ServerTcpUdp;

public class UDP : Client
{
    List<UDPMessage> _received;
    int _delay;
    int _retransmissions;
    private readonly object _lock = new object(); // Lock object for synchronization

    public UDP(UdpClient dynamicClient, Channel channel, IPEndPoint remoteEP, int delay, int retransmissions)
    {
        RemoteEP = remoteEP;
        UdpCli = dynamicClient;
        I_TCP_O_UDP = false;
        _messages = new List<UDPMessage>();
        _received = new List<UDPMessage>();

        _delay = delay;
        _retransmissions = retransmissions + 1;//+1 for the first message

        CurrentState = States.Start;
        CurrentChannel = channel;
    }
    //only for tcp
    public override Task SendToClient(string message)
    {
        throw new NotImplementedException();
    }
//Send message to client based on function
    public override async Task SendFunc(UDPMessage message, byte[] function)
    {
        if (_messages == null)
        {
            Console.Error.WriteLine("ERR: _messages is null");
            await DisconnectClient();
            return;
        }
        lock (_lock)
        {
            _messages.Add(message);
        }
        var send = function;
        UDPMessage? find;
        int count = 0;
        do
        {
            if (UdpCli == null || RemoteEP == null)
            {
                Console.Error.WriteLine("ERR: UdpCli or RemoteEP is null");
                CurrentState = States.End;
                return;
            }
            await UdpCli.SendAsync(send, send.Length, RemoteEP);
            DisplayMessage(false, RemoteEP.Address, RemoteEP.Port, message.TypeToString());
            await Task.Delay(_delay);
            find = _messages.FirstOrDefault(x => x.MessageID == message.MessageID);
            // Console.WriteLine($"SendFunc confirmed = {find!._confirmed}, {find.TypeToString()} (ID = {find.MessageID})");
            if (find == null)
            {
                Console.Error.WriteLine("ERR: Message not found");
                UdpCli = null;
                await DisconnectClient();
                return;
            }
            count++;
            if (count == _retransmissions && !find._confirmed)//error and exit
            {
                Console.Error.WriteLine("ERR: No confirmation received");
                UdpCli = null;
                await DisconnectClient();
                return;
            }
        } while (!find._confirmed && count < _retransmissions);
    }
//Read message from client
    public async Task<UDPMessage?> ReadFromClient()//After AUTH
    {
        if (UdpCli == null)
        {
            Console.Error.WriteLine("ERR: UdpCli is null");
            CurrentState = States.End;
            return null;
        }
        var result = await UdpCli.ReceiveAsync();
        var data = result.Buffer;
        var message = new UDPMessage();//no MessageID
        if (message.ParseMessage(data))
        {
            DisplayMessage(true, result.RemoteEndPoint.Address, result.RemoteEndPoint.Port, message.TypeToString());
            return message;
        }
        return null;
    }
    public void ErrorToClient(string messageContents)
    {
        UDPMessage message = new UDPMessage((byte)UDPMsgType.ERR, messageContents);
        message.DisplayName = DisplayName;
        _ = Task.Run(() => SendFunc(message, message.BuildERR()));
    }
    public async Task SendAuthReply(UdpReceiveResult udpResult)//auth
    {
        while (CurrentState == States.Start)//start
        {
            byte[] data = udpResult.Buffer;
            UDPMessage parse = new UDPMessage();//no MessageID
            if (parse.ParseMessage(data))//auth
            {
                UDPMessage message;
                if (parse.DisplayName == null || parse.Username == null || parse.Secret == null)
                {
                    message = new UDPMessage((byte)UDPMsgType.REPLY, "Auth failed.");
                    message.Ref_MessageID = parse.MessageID;
                    message.Result = 0;//nok
                    _ = Task.Run(() => SendFunc(message, message.BuildREPLY()));
                }
                else
                {
                    DisplayName = parse.DisplayName;
                    Username = parse.Username;
                    Secret = parse.Secret;
                    message = new UDPMessage((byte)UDPMsgType.REPLY, "Auth success.");
                    message.Ref_MessageID = parse.MessageID;
                    message.Result = 1;//ok
                    _ = Task.Run(() => SendFunc(message, message.BuildREPLY()));
                    if (CurrentState != States.End)
                    {
                        CurrentState = States.Auth;
                    }
                }
            }
            else
            {
                UDPMessage message = new UDPMessage((byte)UDPMsgType.REPLY, "Auth failed.");
                message.Ref_MessageID = parse.MessageID;
                message.Result = 0;//nok
                _ = Task.Run(() => SendFunc(message, message.BuildREPLY()));
            }

            if (CurrentState == States.Start && UdpCli != null)
            {
                udpResult = await UdpCli.ReceiveAsync();
                Program.SendAUTHConfirm(udpResult);
            }
        }
    }

    public void SendJoinReply(UDPMessage parse)
    {
        var channel = CurrentChannel;
        if (parse.ChannelID != null && parse.ChannelID.Length != 0 && parse.MessageID != null)
        {
            var find = Program._channels.Find(x => x.Name == parse.ChannelID);
            if (find == null)
            {
                find = new Channel(parse.ChannelID);
                Program._channels.Add(find);
            }
            if (CurrentChannel != null)
            {
                CurrentChannel.RemoveClient(this);
                CurrentChannel.BroadcastDisconnection(this);
            }
            CurrentChannel = find;
            CurrentChannel.AddClient(this);
            UDPMessage message = new UDPMessage((byte)UDPMsgType.REPLY, "Join success.");
            message.Ref_MessageID = parse.MessageID;
            message.Result = 1;//ok
            _ = Task.Run(() => SendFunc(message, message.BuildREPLY()));
            if (CurrentState != States.End)
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
                CurrentState = States.End;
                return;
            }
            UDPMessage message = new UDPMessage((byte)UDPMsgType.REPLY, "Join failed.");
            message.Ref_MessageID = parse.MessageID;
            message.Result = 0;//nok
            _ = Task.Run(() => SendFunc(message, message.BuildREPLY()));
        }
    }
    public void HandleConfirm(UDPMessage message)
    {
        lock (_lock) // Acquire lock before accessing _messages
        {
            if (_messages == null)
            {
                Console.Error.WriteLine("ERR: _messages is null");
                CurrentState = States.End;
                return;
            }
            var find = _messages.FirstOrDefault(x => x.MessageID == message.Ref_MessageID);
            if (find != null)
            {
                find._confirmed = true;
            }
        }
    }
    public async void SendConfirm(UDPMessage message)
    {
        UDPMessage confirm = new UDPMessage();//no MessageID
        confirm.Ref_MessageID = message.MessageID;
        var send = confirm.BuildCONFIRM();
        if (UdpCli == null || RemoteEP == null)
        {
            Console.Error.WriteLine("ERR: UdpCli or RemoteEP is null");
            CurrentState = States.End;
            return;
        }
        await UdpCli.SendAsync(send, send.Length, RemoteEP);
        DisplayMessage(false, RemoteEP.Address, RemoteEP.Port, "CONFIRM");
    }
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
        UDPMessage? find;
        switch (parsed.Type)
        {
            case (byte)UDPMsgType.CONFIRM:
                HandleConfirm(parsed);
                break;
            case (byte)UDPMsgType.MSG:
                if (CurrentState == States.End || CurrentState == States.Error)
                {
                    break;
                }
                find = _received.FirstOrDefault(x => x.MessageID == parsed.MessageID);
                if (find == null)
                {
                    lock (_lock)
                    {
                        _received.Add(parsed);
                    }
                    if (DisplayName != parsed.DisplayName && !string.IsNullOrEmpty(parsed.DisplayName))//rename
                    {
                        DisplayName = parsed.DisplayName;
                    }
                    if (string.IsNullOrEmpty(parsed.MessageContents))
                    {
                        CurrentState = States.Error;
                        SendConfirm(parsed);
                        break;
                    }
                    if (CurrentChannel == null)
                    {
                        CurrentState = States.Error;
                        SendConfirm(parsed);
                        break;
                    }
                    CurrentChannel.BroadcastMessage(this, parsed.MessageContents);
                }
                SendConfirm(parsed);
                break;
            case (byte)UDPMsgType.JOIN:
                if (CurrentState == States.End || CurrentState == States.Error)
                {
                    break;
                }
                find = _received.FirstOrDefault(x => x.MessageID == parsed.MessageID);
                if (find == null)
                {
                    lock (_lock)
                    {
                        _received.Add(parsed);
                    }

                    if (string.IsNullOrEmpty(parsed.ChannelID))
                    {
                        CurrentState = States.Error;
                        SendConfirm(parsed);
                        break;
                    }
                    SendJoinReply(parsed);
                }
                SendConfirm(parsed);
                break;
            case (byte)UDPMsgType.ERR:
                if (CurrentState == States.End || CurrentState == States.Error)
                {
                    break;
                }
                //if message was already processed, skips
                find = _received.FirstOrDefault(x => x.MessageID == parsed.MessageID);
                if (find == null)
                {
                    lock (_lock)
                    {
                        _received.Add(parsed);
                    }
                    SendConfirm(parsed);
                    await DisconnectClient();
                    break;
                }
                SendConfirm(parsed);
                break;
            case (byte)UDPMsgType.BYE:
                if (CurrentState == States.End || CurrentState == States.Error)
                {
                    break;
                }
                find = _received.FirstOrDefault(x => x.MessageID == parsed.MessageID);
                if (find == null)
                {
                    lock (_lock)
                    {
                        _received.Add(parsed);
                    }
                    SendConfirm(parsed);
                    CleanResourcesAndExit();
                    break;
                }
                SendConfirm(parsed);
                break;
            default:
                if (CurrentState == States.End || CurrentState == States.Error)
                {
                    break;
                }
                CurrentState = States.Error;
                break;
        }
    }
    //Function to process clients requests based on state
    public async Task Process(UdpReceiveResult result)
    {
        while (CurrentState != States.End)
        {
            switch (CurrentState)
            {
                case States.Start:
                    await SendAuthReply(result);
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
                    ErrorToClient("Invalid message");
                    await DisconnectClient();
                    break;
                default:
                    CurrentState = States.Error;
                    break;
            }
        }
    }
    //Function to wait for BYE confirmation
    public async Task WaitBYEconfirm(UDPMessage bye)
    {
        if (bye.MessageID == null)
        {
            Console.Error.WriteLine("ERR: MessageID is null");
            if (UdpCli != null)
            {
                UdpCli.Close();
            }
            return;
        }
        while (true)
        {
            if (UdpCli == null)
            {
                Console.Error.WriteLine("ERR: UdpCli is null");
                return;
            }
            var parsed = await ReadFromClient();
            if (parsed == null)
            {
                Console.Error.WriteLine("ERR: ReadFromClient returned null");
                if (UdpCli != null)
                {
                    UdpCli.Close();
                }
                return;
            }
            if (parsed.Type == (byte)UDPMsgType.CONFIRM && parsed.Ref_MessageID == bye.MessageID)
            {
                HandleConfirm(parsed);
                break; // Exit the function once confirmation is received
            }
        }
    }
//Disconnect client
    public override async Task DisconnectClient()
    {
        CurrentState = States.End;
        if (UdpCli == null || RemoteEP == null || _messages == null || CurrentChannel == null)
        {
            CleanResourcesAndExit();
            return;
        }
        UDPMessage bye = new UDPMessage((byte)UDPMsgType.BYE);
        _ = Task.Run(() => WaitBYEconfirm(bye));
        await SendFunc(bye, bye.BuildBYE());

        CleanResourcesAndExit();
    }
    public void CleanResourcesAndExit()
    {
        CurrentState = States.End;
        if (UdpCli != null)
        {
            UdpCli.Close();
        }

        if (CurrentChannel != null)
        {
            CurrentChannel.RemoveClient(this);
            CurrentChannel.BroadcastDisconnection(this);
        }
    }
//Disconnect client without sending message about channel disconnection
    public override async Task EndServer()
    {
        CurrentState = States.End;
        if (UdpCli == null || RemoteEP == null || _messages == null || CurrentChannel == null)
        {
            return;
        }
        UDPMessage bye = new UDPMessage((byte)UDPMsgType.BYE);
        _ = Task.Run(() => WaitBYEconfirm(bye));
        await SendFunc(bye, bye.BuildBYE());
    }
}
