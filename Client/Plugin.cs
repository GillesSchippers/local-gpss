namespace GPSS_Client
{
    using GPSS_Client.Config;
    using PKHeX.Core;
    using System;
    using System.Windows.Forms;

    /// <summary>
    /// Defines the <see cref="Plugin" />.
    /// </summary>
    public class Plugin : IPlugin
    {
        /// <summary>
        /// Gets the Name.
        /// </summary>
        public string Name => nameof(GPSS_Client);

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
                throw new ArgumentException(nameof(menuStrip));
            AddPluginControl(tools);
        }

        /// <summary>
        /// The AddPluginControl.
        /// </summary>
        /// <param name="tools">The tools<see cref="ToolStripDropDownItem"/>.</param>
        private void AddPluginControl(ToolStripDropDownItem tools)
        {
            var ctrl = new ToolStripMenuItem(Name);
            tools.DropDownItems.Add(ctrl);

            var c2 = new ToolStripMenuItem($"{Name} sub form");
            c2.Click += (_, _) => new Form().ShowDialog();
            var c3 = new ToolStripMenuItem($"{Name} show message");
            c3.Click += (_, _) => MessageBox.Show("Hello!");
            var c4 = new ToolStripMenuItem($"{Name} modify current SaveFile");
            c4.Click += (_, _) => ModifySaveFile();
            ctrl.DropDownItems.Add(c2);
            ctrl.DropDownItems.Add(c3);
            ctrl.DropDownItems.Add(c4);
            Console.WriteLine($"{Name} added menu items.");
        }

        /// <summary>
        /// The ModifySaveFile.
        /// </summary>
        private void ModifySaveFile()
        {
            var sav = SaveFileEditor.SAV;
            sav.ModifyBoxes(ModifyPKM);
            SaveFileEditor.ReloadSlots();
        }

        /// <summary>
        /// The ModifyPKM.
        /// </summary>
        /// <param name="pk">The pk<see cref="PKM"/>.</param>
        public static void ModifyPKM(PKM pk)
        {
            // Make everything Bulbasaur!
            pk.Species = (ushort)Species.Bulbasaur;
            pk.Move1 = (ushort)Move.Pound; // pound
            pk.Move1_PP = 40;
            CommonEdits.SetShiny(pk);
        }

        /// <summary>
        /// The NotifySaveLoaded.
        /// </summary>
        public void NotifySaveLoaded()
        {
            Console.WriteLine($"{Name} was notified that a Save File was just loaded.");
        }

        /// <summary>
        /// The TryLoadFile.
        /// </summary>
        /// <param name="filePath">The filePath<see cref="string"/>.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        public bool TryLoadFile(string filePath)
        {
            Console.WriteLine($"{Name} was provided with the file path, but chose to do nothing with it.");
            return false; // no action taken
        }
    }
}
