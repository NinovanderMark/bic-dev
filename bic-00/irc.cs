using System;
using System.Net.Sockets;
using System.Text;

class MinimalIrcClient {
    private TcpClient tcp;
    private NetworkStream stream;
    private readonly string server, nick, channel;
    private bool registered = false;

    public MinimalIrcClient(string server, int port, string nick, string channel) {
        this.server = server;
        this.nick = nick;
        this.channel = channel;
    }

    public void Run() {
        tcp = new TcpClient();
        tcp.Connect(server, 6667);
        stream = tcp.GetStream();

        // Send NICK/USER only (no JOIN yet)
        Send("NICK " + nick);
        Send("USER " + nick + " 0 * :Mono IRC Minimal");

        // Background read loop
        System.Threading.Thread readThread = new System.Threading.Thread(ReadLoop);
        readThread.IsBackground = true;
        readThread.Start();

        // Interactive input loop
        Console.WriteLine("Connecting... Type messages (or /quit):");
        string input;
        while ((input = Console.ReadLine()) != null) {
            if (input == "/quit") break;
            if (registered) {
                Send("PRIVMSG " + channel + " :" + input);
            } else {
                Console.WriteLine("Waiting for registration...");
            }
        }
    }

    private void ReadLoop() {
        byte[] buf = new byte[512];
        StringBuilder line = new StringBuilder();
        
        while (tcp.Connected) {
            try {
                int n = stream.Read(buf, 0, buf.Length);
                if (n == 0) break;

                string chunk = System.Text.Encoding.ASCII.GetString(buf, 0, n);
                line.Append(chunk);
                
                int idx;
                while ((idx = line.ToString().IndexOf('\n')) >= 0) {
                    string msg = line.ToString(0, idx).TrimEnd('\r');
                    line.Remove(0, idx + 1);
                    HandleMessage(msg);
                }
            } catch {
                break;
            }
        }
    }

    private void HandleMessage(string msg) {
        Console.WriteLine("< {0}", msg);
        
        if (msg.StartsWith("PING ")) {
            Send(msg.Replace("PING", "PONG"));
        }
        
        // Wait for 001 RPL_WELCOME before JOIN
        if (!registered && (msg.Contains(" 001 ") || msg.Contains(":001 "))) {
            registered = true;
            Send("JOIN " + channel);
            Console.WriteLine("> Auto-joined " + channel);
            return;
        }
        
        if (msg.Contains("PRIVMSG")) {
            int colonIdx = msg.IndexOf(" :");
            if (colonIdx > 0) {
                string beforeMsg = msg.Substring(0, colonIdx);
                string text = msg.Substring(colonIdx + 2);
                
                int privIdx = beforeMsg.IndexOf("PRIVMSG ") + 8;
                if (privIdx > 8) {
                    string target = beforeMsg.Substring(privIdx).Trim();
                    string sender = msg.Substring(1, msg.IndexOf('!') - 1);
                    Console.WriteLine("[{0}] <{1}> {2}", target, sender, text);
                }
            }
        }
    }

    private void Send(string cmd) {
        string full = cmd + "\r\n";
        byte[] data = System.Text.Encoding.ASCII.GetBytes(full);
        stream.Write(data, 0, data.Length);
        Console.WriteLine("> {0}", cmd);
    }

    static void Main(string[] args) {
        if (args.Length < 4) {
            Console.WriteLine("Usage: mono irc.exe <server> <port> <nick> <\"#channel\">");
            Console.WriteLine("Example: mono irc.exe irc.libera.chat 6667 testnick \"#libera\"");
            return;
        }
        string channel = args[3];
        if (channel.StartsWith("\"") && channel.EndsWith("\"")) {
            channel = channel.Substring(1, channel.Length - 2);
        }
        MinimalIrcClient client = new MinimalIrcClient(args[0], int.Parse(args[1]), args[2], channel);
        client.Run();
    }
}
