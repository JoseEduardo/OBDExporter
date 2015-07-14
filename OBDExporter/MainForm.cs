using OpenTibia.Client;
using OpenTibia.Client.Things;
using OpenTibia.Core;
using OpenTibia.Obd;
using OpenTibia.Utils;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace OBDExporter
{
    public partial class MainForm : Form
    {
        #region | Private Properties |

        private VersionStorage versions;
        private ClientImpl client;
        private BackgroundWorker worker;

        #endregion

        #region | Constructor |

        public MainForm()
        {
            this.versions = new VersionStorage();
            this.client = new ClientImpl();
            this.client.ClientLoaded += new EventHandler(this.ClientLoaded_Handler);
            this.client.ProgressChanged += new ProgressHandler(this.ClientProgressChanged_Handler);
            this.worker = new BackgroundWorker();
            this.worker.WorkerReportsProgress = true;
            this.worker.DoWork += new DoWorkEventHandler(this.DoWork_Handler);
            this.worker.ProgressChanged += new ProgressChangedEventHandler(this.ProgressChanged_Handler);
            this.worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.RunWorkerCompleted_Handler);

            this.InitializeComponent();
            this.LoadVersions();

            this.itemsListBox.Client = this.client;
            this.outfitsListBox.Client = this.client;
            this.effectsListBox.Client = this.client;
            this.missilesListBox.Client = this.client;
            this.thingTypeListBox.Client = this.client;
            this.saveButton.Enabled = false;
        }

        #endregion

        #region | Private Methods |

        private void LoadVersions()
        {
            this.versions.Load(@"versions.xml");
            this.versionsComboBox.Items.AddRange(this.versions.GetAllVersions());
            this.versionsComboBox.SelectedIndex = 0;
        }

        private void LoadClient()
        {
            if (this.client.Loaded)
            {
                return;
            }

            string directory = Path.Combine(PathUtils.ApplicationDirectory, "Client");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string datPath = Path.Combine(directory, "Tibia.dat");
            string sprPath = Path.Combine(directory, "Tibia.spr");

            try
            {


                OpenTibia.Core.Version version = this.versionsComboBox.SelectedItem as OpenTibia.Core.Version;
                client.Load(datPath, sprPath, version);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            this.itemsListBox.AddRange(this.client.GetAllItems());
            this.outfitsListBox.AddRange(this.client.GetAllOutfits());
            this.effectsListBox.AddRange(this.client.GetAllEffects());
            this.missilesListBox.AddRange(this.client.GetAllMissiles());
        }

        #endregion

        #region Private Methods

        private ListBox.SelectedObjectCollection GetSelectedThings()
        {
            if (this.client.Loaded)
            {
                switch (this.objectsTabControl.SelectedIndex)
                {
                    case 0:
                        return this.itemsListBox.SelectedItems;

                    case 1:
                        return this.outfitsListBox.SelectedItems;

                    case 2:
                        return this.effectsListBox.SelectedItems;

                    case 3:
                        return this.missilesListBox.SelectedItems;
                }
            }

            return null;
        }

        #endregion

        #region | Event Handlers |

        private void MainFrom_Load(object sender, EventArgs e)
        {
            this.Text = "OBDExporter 0.2.1";
        }

        private void ClientLoaded_Handler(object sender, EventArgs e)
        {
            this.loadButton.Enabled = false;
            this.progressBar.Value = 0;
        }

        private void ClientProgressChanged_Handler(object sender, int percentage)
        {
            this.progressBar.Value = percentage;
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            this.LoadClient();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (this.thingTypeListBox.Items.Count != 0)
            {
                this.saveButton.Enabled = false;
                this.addButton.Enabled = false;
                this.removeButton.Enabled = false;
                this.worker.RunWorkerAsync();
            }
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            ListBox.SelectedObjectCollection list = this.GetSelectedThings();
            if (list == null)
            {
                return;
            }

            foreach (ThingType thing in list)
            {
                if (!this.thingTypeListBox.Items.Contains(thing))
                {
                    this.thingTypeListBox.Items.Add(thing);
                }
            }

            this.saveButton.Enabled = this.thingTypeListBox.Items.Count > 0;
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            this.thingTypeListBox.RemoveSelectedThings();
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            this.thingTypeListBox.Clear();
        }

        private void DoWork_Handler(object sender, DoWorkEventArgs e)
        {
            string directory = Path.Combine(PathUtils.ApplicationDirectory, "Output");

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            int count = 0;

            foreach (ThingType thing in this.thingTypeListBox.Items)
            {
                ThingData data = client.GetThingData(thing.ID, thing.Category, true);

                string path = Path.Combine(directory, thing.Category + "_" + thing.ID + ".obd");
                using (FileStream writer = new FileStream(path, FileMode.Create))
                {
                    byte[] bytes = ObdCoder.Encode(data, ObdVersion.Version1);
                    writer.Write(bytes, 0, bytes.Length);
                }

                Thread.Sleep(10);
                worker.ReportProgress(((++count * 100) / this.thingTypeListBox.Items.Count));
            }
        }

        private void ProgressChanged_Handler(object sender, ProgressChangedEventArgs e)
        {
            this.progressBar.Value = e.ProgressPercentage;
        }

        private void RunWorkerCompleted_Handler(object sender, RunWorkerCompletedEventArgs e)
        {
            this.saveButton.Enabled = true;
            this.addButton.Enabled = true;
            this.removeButton.Enabled = true;
            this.progressBar.Value = 0;
        }

        #endregion
    }
}
