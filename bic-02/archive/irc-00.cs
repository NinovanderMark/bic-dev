using System;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;

public partial class IrcForm : Form {
    private TcpClient tcp;
    private NetworkStream stream;
    private string server = "irc.libera.chat", nick = "testnick", channel = "#libera";
    private bool registered = false;
    private TextBox chatDisplay, inputBox, serverBox, nickBox;
    private Button connectBtn, sendBtn;
    private bool uiThread = true;

    public IrcForm() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        this.Text = "Mono WinForms IRC Client";
        this.Size = new Size(700, 500);
        this.FormClosing += (s, e) => Application.Exit();

        // Server/Nick panel
        Panel topPanel = new Panel() { Dock = DockStyle.Top, Height = 40 };
        Label lblServer = new Label() { Text = "Server:", Location = new Point(10, 10), Size = new Size(50, 20) };
        serverBox = new TextBox() { Text = server, Location = new Point(65, 8), Size = new Size(150, 22) };
        Label lblNick = new Label() { Text = "Nick:", Location = new Point(225, 10), Size = new Size(40, 20) };
        nickBox = new TextBox() { Text = nick, Location = new Point(270, 8), Size = new Size(100, 22) };
        Label lblChan = new Label() { Text = "Chan:", Location = new Point(380, 10), Size = new Size(40, 20) };
        TextBox chanBox = new TextBox() { Text = channel, Location = new Point(425, 8), Size = new Size(100, 22) };
        chanBox.TextChanged += (s, e) => channel = chanBox.Text;
        connectBtn = new Button() { Text = "Connect", Location = new Point(535, 5), Size = new Size(80, 30) };
        connectBtn.Click += Connect_Click;
        topPanel.Controls.AddRange(new Control[] { lblServer, serverBox, lblNick, nickBox, lblChan, chanBox, connectBtn });
        this.Controls.Add(topPanel);

        // Chat display
        chatDisplay = new TextBox() {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.Black,
            ForeColor = Color.LimeGreen,
            Font = new Font("Courier New", 9)
        };
        this.Controls.Add(chatDisplay);

        // Input panel
        Panel bottomPanel = new Panel() { Dock = DockStyle.Bottom, Height = 40 };
        inputBox = new TextBox() { Dock = DockStyle.Left, Width = 500 };
        inputBox.KeyDown += Input_KeyDown;
        sendBtn = new Button() { Text = "Send", Dock = DockStyle.Right, Width = 80 };
        sendBtn.Click += Send_Click;
        sendBtn.Enabled = false;
        bottomPanel.Controls.AddRange(new Control[] { inputBox, sendBtn });
        this.Controls.Add(bottomPanel);
    }

    private void Connect_Click(object sender, EventArgs e) {
        try {
            server = serverBox.Text.Trim();
            nick = nickBox.Text.Trim();
            connectBtn.Enabled = false;
            
            tcp = new TcpClient();
            tcp.Connect(server, 6667);
            stream = tcp.GetStream();
            
            AppendChat("Connecting to " + server + "...");
            Send("NICK " + nick);
            Send("USER " + nick + " 0 * :Mono WinForms IRC");

            Thread readThread = new Thread(ReadLoop);
            readThread.IsBackground = true;
            readThread.Start();
            
            sendBtn.Enabled = true;
            inputBox.Enabled = true;
            inputBox.Focus();
        } catch (Exception ex) {
            AppendChat("Connect failed: " + ex.Message);
            connectBtn.Enabled = true;
        }
    }

    private void Send_Click(object sender, EventArgs e) {
        string text = inputBox.Text.Trim();
        if (text.Length > 0 && registered) {
            Send("PRIVMSG " + channel + " :" + text);
            inputBox.Clear();
        }
    }

    private void Input_KeyDown(object sender, KeyEventArgs e) {
        if (e.KeyCode == Keys.Enter) {
            Send_Click(sender, e);
            e.SuppressKeyPress = true;
        }
    }

    private void ReadLoop() {
        byte[] buf = new byte[512];
        StringBuilder line = new StringBuilder();
        while (tcp.Connected) {
            try {
                int n = stream.Read(buf, 0, buf.Length);
                if (n == 0) break;
                string chunk = Encoding.ASCII.GetString(buf, 0, n);
                line.Append(chunk);
                int idx;
                while ((idx = line.ToString().IndexOf('\n')) >= 0) {
                    string msg = line.ToString(0, idx).TrimEnd('\r');
                    line.Remove(0, idx + 1);
                    Invoke(new Action<string>(HandleMessage), msg);
                }
            } catch { break; }
        }
    }

    private void HandleMessage(string msg) {
        AppendChat("< " + msg);
        
        if (msg.StartsWith("PING ")) {
            Send(msg.Replace("PING", "PONG"));
        }
        if (!registered && (msg.Contains(" 001 ") || msg.Contains(":001 "))) {
            registered = true;
            Send("JOIN " + channel);
            AppendChat("> Auto-joined " + channel);
        }
        if (msg.Contains("PRIVMSG")) {
            ParsePrivmsg(msg);
        }
    }

    private void ParsePrivmsg(string msg) {
        int colonIdx = msg.IndexOf(" :");
        if (colonIdx > 0) {
            string text = msg.Substring(colonIdx + 2);
            int privIdx = msg.IndexOf("PRIVMSG ") + 8;
            if (privIdx > 8) {
                string target = msg.Substring(privIdx, colonIdx - privIdx).Trim();
                string sender = msg.Substring(1, msg.IndexOf('!') - 1);
                AppendChat($"[{target}] <{sender}> {text}");
            }
        }
    }

	private void Send(string cmd) {
		string full = cmd + "\r\n";
		byte[] data = System.Text.Encoding.ASCII.GetBytes(full);
		stream.Write(data, 0, data.Length);
		AppendChat("> " + cmd);
	}

    private void AppendChat(string text) {
        if (InvokeRequired) {
            Invoke(new Action<string>(AppendChat), text);
            return;
        }
        chatDisplay.AppendText(text + "\n");
        chatDisplay.SelectionStart = chatDisplay.Text.Length;
        chatDisplay.ScrollToCaret();
    }

    [STAThread]
    public static void Main() {
        Application.EnableVisualStyles();
        Application.Run(new IrcForm());
    }
}
