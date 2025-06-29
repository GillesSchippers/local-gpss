namespace GPSS_Client
{
    using GPSS_Client.Config;
    using GPSS_Client.Services;
    using GPSS_Client.Forms;
    using PKHeX.Core;
    using System;
    using System.Windows.Forms;

    /// <summary>
    /// Defines the <see cref="Plugin" />.
    /// </summary>
    public class Plugin : IPlugin
    {
#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously.
        /// <summary>
        /// Gets the Name.
        /// </summary>
        public string Name => nameof(GPSS_Client).Replace("_", " ");

        /// <summary>
        /// Gets the Priority.
        /// </summary>
        public int Priority => 17;// Loading order, lowest is first.

        // Initialized on plugin load

        /// <summary>
        /// Gets the SaveFileEditor.
        /// </summary>
        public ISaveFileProvider SaveFileEditor { get; private set; } = null!;

        /// <summary>
        /// Gets the PKMEditor.
        /// </summary>
        public IPKMView PKMEditor { get; private set; } = null!;

        /// <summary>
        /// Defines the Config.
        /// </summary>
        private static readonly ConfigHolder Config = new();

        /// <summary>
        /// Defines the API.
        /// </summary>
        private static readonly APIService API = new(Config);

        /// <summary>
        /// The Initialize.
        /// </summary>
        /// <param name="args">The args<see cref="object[]"/>.</param>
        public void Initialize(params object[] args)
        {
            Console.WriteLine($"Loading {Name}...");
            SaveFileEditor = (ISaveFileProvider)Array.Find(args, z => z is ISaveFileProvider)!;
            PKMEditor = (IPKMView)Array.Find(args, z => z is IPKMView)!;
            var menu = (ToolStrip)Array.Find(args, z => z is ToolStrip)!;
            LoadMenuStrip(menu);
        }

        /// <summary>
        /// The LoadMenuStrip.
        /// </summary>
        /// <param name="menuStrip">The menuStrip<see cref="ToolStrip"/>.</param>
        private void LoadMenuStrip(ToolStrip menuStrip)
        {
            var items = menuStrip.Items;
            if (items.Find("Menu_Tools", false)[0] is not ToolStripDropDownItem tools)
                throw new ArgumentException(null, nameof(menuStrip));
            AddPluginControl(tools);
        }

        /// <summary>
        /// The AddPluginControl.
        /// </summary>
        /// <param name="tools">The tools<see cref="ToolStripDropDownItem"/>.</param>
        private void AddPluginControl(ToolStripDropDownItem tools) // Boilerplate
        {
            var ctrl = new ToolStripMenuItem(Name);
            tools.DropDownItems.Add(ctrl);

            var upload = new ToolStripMenuItem($"{Name} upload active Pokémon");
            upload.Click += async (_, _) => {
                await UploadSelectedPKM();
            };
            var config = new ToolStripMenuItem($"{Name} config");
            config.Click += async (_, _) => {
                using var form = new ConfigForm(Config);
                form.ShowDialog();
            };

            ctrl.DropDownItems.Add(upload);
            ctrl.DropDownItems.Add(config);
            Console.WriteLine($"{Name} added menu items.");
        }

        /// <summary>
        /// The UploadSelectedPKM.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task UploadSelectedPKM()
        {
            try
            {
                var pkm = PKMEditor.PreparePKM();
                if (pkm == null)
                {
                    MessageBox.Show("No Pokémon selected.");
                    return;
                }
                var bytes = pkm.DecryptedBoxData;
                var gen = pkm.Context.Generation().ToString();
                var code = await API.UploadPokemonAsync(bytes, gen);
                MessageBox.Show(code != null ? "upload successful!" : "upload failed.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\n{ex}");
            }
        }

        /// <summary>
        /// The NotifySaveLoaded.
        /// </summary>
        public void NotifySaveLoaded()
        {
            return; // no action taken
        }

        /// <summary>
        /// The TryLoadFile.
        /// </summary>
        /// <param name="filePath">The filePath<see cref="string"/>.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        public bool TryLoadFile(string filePath)
        {
            return false; // no action taken
        }
#pragma warning restore CS1998 // This async method lacks 'await' operators and will run synchronously.
    }
}
