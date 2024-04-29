namespace ServerTcpUdp;
public class Channel
{
    public string? Name { get; set; }
    public List<Client> Clients { get; set; }

    public Channel(string name)
    {
        this.Name = name;
        this.Clients = new List<Client>();
    }
//Search for client in channel
    public Client? FindClient(Client client)
    {
        if (this.Clients == null)
        {
            return null;
        }
        return this.Clients.FirstOrDefault(x => x.Id == client.Id);
    }
//Add client to channel
    public void AddClient(Client client)
    {
        var find = this.Clients.FirstOrDefault(x => x.Id == client.Id);
        if (find == null)
        {
            this.Clients.Add(client);
        }
    }
//Remove client from channel
    public void RemoveClient(Client client)
    {
        if (this.Clients == null)
        {
            return;
        }
        var find = this.Clients.FirstOrDefault(x => x.Id == client.Id);
        if (find != null)
        {
            this.Clients.Remove(find);
        }
    }
//Broadcast connection message
    public void BroadcastConnection(Client client)//TODO
    {
        if (this.Clients.Count == 0)
        {
            return;
        }
        string TCPsend;
        var content = $"{client.DisplayName} joined {this.Name}.";
        UDPMessage message = new UDPMessage((byte)UDPMsgType.MSG, content);
        foreach (var usr in this.Clients)
        {
            if (usr.I_TCP_O_UDP)
            {
                TCPsend = Message.BuildMSG("Server", content);
                _ = usr.SendToClient(TCPsend);
            }
            else
            {
                message.DisplayName = "Server";
                _ = Task.Run(() => usr.SendFunc(message, message.BuildMSG()));
            }
        }
    }
//Broadcast disconnection message
    public void BroadcastDisconnection(Client client)
    {
        if (this.Clients.Count == 0)
        {
            return;
        }
        string TCPsend;
        var content = $"{client.DisplayName} has left {this.Name}.";
        UDPMessage message = new UDPMessage((byte)UDPMsgType.MSG, content);
        foreach (var usr in this.Clients)
        {
            if (usr.I_TCP_O_UDP)
            {
                TCPsend = Message.BuildMSG("Server", content);
                _ = usr.SendToClient(TCPsend);
            }
            else
            {
                message.DisplayName = "Server";
                _ = Task.Run(() => usr.SendFunc(message, message.BuildMSG()));
            }
        }
    }
//Broadcast message
    public void BroadcastMessage(Client client, string content)
    {
        if (this.Clients.Count == 0)
        {
            return;
        }
        string TCPsend;
        UDPMessage message = new UDPMessage((byte)UDPMsgType.MSG, content);
        foreach (var usr in this.Clients)
        {
            if (client != usr)
            {
                if (usr.I_TCP_O_UDP)
                {
                    TCPsend = Message.BuildMSG(client.DisplayName, content);
                    _ = usr.SendToClient(TCPsend);
                }
                else
                {
                    message.DisplayName = client.DisplayName;
                    _ = Task.Run(() => usr.SendFunc(message, message.BuildMSG()));
                }
            }
        }
    }
}