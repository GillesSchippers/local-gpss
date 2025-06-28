namespace GPSS_Client.Forms
{
    using GPSS_Client.Config;

    public partial class ConfigForm : Form
    {
        private readonly ConfigHolder Config;

        private TextBox InputGpssUrl = null!;
        private Button BtnOk = null!;
        private Button BtnCancel = null!;

        public ConfigForm(ConfigHolder config)
        {
            Config = config;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "GPSS Client Configuration";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Width = 400;
            this.Height = 150;

            var lblGpssUrl = new Label { Text = "GPSS URL:", Left = 10, Top = 20, Width = 80 };
            InputGpssUrl = new TextBox { Left = 100, Top = 18, Width = 260 };
            InputGpssUrl.Text = Config.Get(config => config.GpssUrl);

            BtnOk = new Button { Text = "OK", Left = 200, Width = 75, Top = 60, DialogResult = DialogResult.OK };
            BtnCancel = new Button { Text = "Cancel", Left = 285, Width = 75, Top = 60, DialogResult = DialogResult.Cancel };

            BtnOk.Click += OnBtnOk_Click;

            this.Controls.Add(lblGpssUrl);
            this.Controls.Add(InputGpssUrl);
            this.Controls.Add(BtnOk);
            this.Controls.Add(BtnCancel);

            this.AcceptButton = BtnOk;
            this.CancelButton = BtnCancel;
        }

        private void OnBtnOk_Click(object? sender, EventArgs e)
        {
            Config.Set(config => config.GpssUrl, InputGpssUrl.Text);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
