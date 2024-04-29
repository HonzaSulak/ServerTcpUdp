using System.Text.RegularExpressions;

namespace ServerTcpUdp;
public class Message
{
    public ushort MessageID { get; set; }//uint16
    public string? Username { get; set; }//20
    public string? ChannelID { get; set; }//20
    public string? Secret { get; set; }//128
    public string? DisplayName { get; set; }//20
    public string? MessageContent { get; set; }//1400
    public static ushort cnt = 1;
    public MsgTypes MessageType { get; set; }

    public Message()
    {
        this.MessageID = cnt++;
        this.MessageType = MsgTypes.ERR;
    }

    public string TypeToString()
    {
        switch (this.MessageType)
        {
            case MsgTypes.AUTH:
                return "AUTH";
            case MsgTypes.JOIN:
                return "JOIN";
            case MsgTypes.MSG:
                return "MSG";
            case MsgTypes.ERR:
                return "ERR";
            case MsgTypes.BYE:
                return "BYE";
            default:
                return "ERR";
        }
    }
    public static bool CheckMessage(string message)
    {
        Match match = Regex.Match(message, Grammar.Message);
        if (match.Success)
        {
            return true;
        }
        return false;
    }
    public bool ParseMessage(string? message)
    {
        bool retVal = false;
        if (message != null) 
        {
            if(!CheckMessage(message)){
                return retVal;
            }
            retVal = true;
            string[] parts = message.Split('\r');
            parts = parts[0].Split(' ');
            var opcode = parts[0].ToUpper();
            switch (opcode)
            {
                case "AUTH":
                    this.MessageType = MsgTypes.AUTH;
                    this.Username = parts[1];
                    this.DisplayName = parts[3];
                    this.Secret = parts[5];
                    break;
                case "JOIN":
                    this.MessageType = MsgTypes.JOIN;
                    this.ChannelID = parts[1];
                    this.DisplayName = parts[3];
                    break;
                case "MSG":
                    this.MessageType = MsgTypes.MSG;
                    this.DisplayName = parts[2];
                    this.MessageContent  = string.Join(" ", parts[4..]);
                    break;
                case "ERR":
                    this.MessageType = MsgTypes.ERR;
                    this.DisplayName = parts[2];
                    this.MessageContent  = string.Join(" ", parts[4..]);
                    break;
                case "BYE":
                    this.MessageType = MsgTypes.BYE;
                    break;
                default:
                    break;
            }
        }
        return retVal;
    }

    public static string BuildAUTH(string username, string displayName, string secret)
    {
        string message = $"AUTH {username} AS {displayName} USING {secret}\r\n";
        if (CheckMessage(message))
        {
            return message;
        }
        return "";
    }
    public static string BuildJOIN(string ChannelID, string displayName)
    {
        string message = $"JOIN {ChannelID} AS {displayName}\r\n";
        if (CheckMessage(message))
        {
            return message;
        }
        return "";
    }
    public static string BuildMSG(string displayName, string messageContent)
    {
        string message = $"MSG FROM {displayName} IS {messageContent}\r\n";
        if (CheckMessage(message))
        {
            return message;
        }
        return "";
    }
    public static string BuildERR(string displayName, string messageContent)
    {
        string message = $"ERR FROM {displayName} IS {messageContent}\r\n";
        if (CheckMessage(message))
        {
            return message;
        }
        return "";
    }
    public static string BuildREPLYok(string messageContent)
    {
        string message = $"REPLY OK IS {messageContent}\r\n";
        if (CheckMessage(message))
        {
            return message;
        }
        return "";
    }
    public static string BuildREPLYnok(string messageContent)
    {
        string message = $"REPLY NOK IS {messageContent}\r\n";
        if (CheckMessage(message))
        {
            return message;
        }
        return "";
    }
    public static string BuildBYE()
    {
        string message = "BYE\r\n";
        if (CheckMessage(message))
        {
            return message;
        }
        return "";
    }
}
public enum MsgTypes
{
    ERR,
    REPLY,
    AUTH,
    JOIN,
    MSG,
    BYE
}
