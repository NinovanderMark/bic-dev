using System;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;

public class IrcForm: Form {

	public TextBox chatBox, inputBox;
	public SplitContainer split; 

	public IrcForm() {
		InitializeComponent();
	}

	private void InitializeComponent() {
		Text = "bic";
		Size = new Size(320,200);

		split = new SplitContainer() {
			Dock = DockStyle.Fill,
			Orientation = Orientation.Horizontal,
		};
		Controls.Add(split);

		chatBox = new TextBox() {
			Dock = DockStyle.Fill,
			Multiline = true,
			ReadOnly = true,
			ScrollBars = ScrollBars.Vertical,
		};
		split.Panel1.Controls.Add(chatBox);

		inputBox = new TextBox() {
			Multiline = true,
			Dock = DockStyle.Fill,
		};
		split.Panel2.Controls.Add(inputBox);

		CenterToScreen();
	}

	[STAThread]
	public static void Main() {
		Application.EnableVisualStyles();
		Application.Run(new IrcForm());
	}

}
