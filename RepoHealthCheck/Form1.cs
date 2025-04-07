using Timer = System.Windows.Forms.Timer;

namespace RepoHealthCheck
{
    public partial class Form1 : Form
    {
        private System.Windows.Forms.TextBox logTextBox;
        private Timer timer;

        public Form1()
        {
            InitializeComponent();
            InitializeLogTextBox();
            InitializeTimer();
        }

        private void InitializeLogTextBox()
        {
            logTextBox = new System.Windows.Forms.TextBox
            {
                Location = new System.Drawing.Point(12, 12),
                Multiline = true,
                Name = "logTextBox",
                ScrollBars = System.Windows.Forms.ScrollBars.Vertical,
                Size = new System.Drawing.Size(776, 426),
                TabIndex = 0
            };
            Controls.Add(logTextBox);
        }

        private void InitializeTimer()
        {
            timer = new Timer();
            timer.Interval = 30000; // 30 seconds in milliseconds
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            await Program.CheckCommitsAsync();
        }

        public void LogMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(LogMessage), message);
                return;
            }
            // Assuming there is a TextBox or ListBox named logTextBox in the Form to display logs
            logTextBox.AppendText($"{DateTime.Now}: {message}{Environment.NewLine}");
        }

        public void LogError(string message)
        {
            LogMessage("ERROR ERROR ERROR ERROR\nERROR ERROR ERROR ERROR\nERROR ERROR ERROR ERROR\nERROR ERROR ERROR ERROR\n");
            LogMessage(message);
            LogMessage("ERROR ERROR ERROR ERROR\nERROR ERROR ERROR ERROR\nERROR ERROR ERROR ERROR\nERROR ERROR ERROR ERROR\n");
        }
    }
}
