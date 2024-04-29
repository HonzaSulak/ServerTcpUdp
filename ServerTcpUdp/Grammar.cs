namespace ServerTcpUdp;
public class Grammar
{
    // Core content components
    public const string ID = @"[A-Za-z0-9\-.]{1,20}";
    public const string SECRET = @"[A-Za-z0-9\-]{1,128}";
    public const string CONTENT = @"[\x20-\x7E]{1,1400}";
    public const string DNAME = @"[\x21-\x7E]{1,20}";

    // Additional content components
    public const string IS = @"\sIS\s";
    public const string AS = @"\sAS\s";
    public const string USING = @"\sUSING\s";

    // Message content variant parts
    public const string ContentJoin = @"JOIN\s" + ID + AS + DNAME;
    public const string ContentAuth = @"AUTH\s" + ID + AS + DNAME + USING + SECRET;
    public const string ContentMessage = @"MSG\sFROM\s" + DNAME + IS + CONTENT;
    public const string ContentError = @"ERR\sFROM\s" + DNAME + IS + CONTENT;
    public const string ContentReply = @"REPLY\s(OK|NOK)"+ IS + CONTENT;
    public const string ContentBye = @"BYE";

    // Message content variants
    public const string Content = ContentAuth + "|" + ContentJoin + "|" + ContentMessage + "|" + ContentError + "|" + ContentReply + "|" + ContentBye;

    // Each message is terminated with "\r\n"
    public const string Message = @"^(" + Content + @")" + "\r\n" + @"$";
}