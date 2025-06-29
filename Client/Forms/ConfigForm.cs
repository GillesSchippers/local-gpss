namespace GPSS_Client.Forms
{
    using GPSS_Client.Config;

    /// <summary>
    /// Defines the <see cref="ConfigForm" />.
    /// </summary>
    public partial class ConfigForm : Form
    {
        /// <summary>
        /// Defines the Config.
        /// </summary>
        private readonly ConfigHolder Config;

        /// <summary>
        /// Defines the InputGpssUrl.
        /// </summary>
        private TextBox InputGpssUrl = null!;

        /// <summary>
        /// Defines the BtnOk.
        /// </summary>
        private Button BtnOk = null!;

        /// <summary>
        /// Defines the BtnCancel.
        /// </summary>
        private Button BtnCancel = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigForm"/> class.
        /// </summary>
        /// <param name="config">The config<see cref="ConfigHolder"/>.</param>
        public ConfigForm(ConfigHolder config)
        {
            Config = config;
            InitializeComponent();
        }

        /// <summary>
        /// The InitializeComponent.
        /// </summary>
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

        /// <summary>
        /// The OnBtnOk_Click.
        /// </summary>
        /// <param name="sender">The sender<see cref="object?"/>.</param>
        /// <param name="e">The e<see cref="EventArgs"/>.</param>
        private void OnBtnOk_Click(object? sender, EventArgs e)
        {
            Config.Set(config => config.GpssUrl, InputGpssUrl.Text);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
