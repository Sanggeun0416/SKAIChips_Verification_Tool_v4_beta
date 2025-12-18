using BlueTooth.HCI;
using FTD2XX_NET;
using SKAIChips_Verification_Tool.HCIControl.Core;
using SKAIChips_Verification_Tool.HCIControl.Core.Legacy;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Text;

namespace SKAIChips_Verification_Tool.HCIControl.UI
{
    public partial class HCIControlForm : Form
    {
        [Serializable]
        public class FormInfo
        {
            public Size FormSize = new Size(600, 600);

            public int SerialPortIndex = 0;
            public int SerialBaudRate = 115200;
            public int UsbDeviceIndex = 0;

            public int SplitDistance = 250;

            public string SelectedHciScriptFileName = "";
            public string HciScriptPath = "";
        }
        public static FormInfo formInfo = new FormInfo();

        private IHciManager HciMgr = null;
        private FTDI fTDI { get; set; } = new FTDI();

        BlueTooth.HCI.Command SelectedHciCommand = null;
        BlueTooth.HCI.Parameter SelectedHciParameter = null;

        public HCIControlForm()
        {
            InitializeComponent();

            this.SizeChanged -= HCIControlForm_SizeChanged;
            this.Size = formInfo.FormSize;

            if (System.IO.Directory.Exists(System.IO.Directory.GetCurrentDirectory() + "\\HCI_Script") == false)
                System.IO.Directory.CreateDirectory(System.IO.Directory.GetCurrentDirectory() + "\\HCI_Script");

            var legacyMgr = new BlueTooth.HCI.HCIManager()
            {
                ConnLogView = dataGridView_AdvReports,
                HciLogView = HciLogDataGridView,
            };
            HciMgr = new LegacyHciManagerAdapter(legacyMgr);
            HciMgr.InitHciLogView();
        }

        private void HCIControlForm_Load(object sender, EventArgs e)
        {
            
            SetPortOpenCloseButtonEnabled(true);
            FindSerialPorts(); 
            FindUsbDevices(); 

            
            ((System.ComponentModel.ISupportInitialize)(this.HCISplitContainer)).BeginInit();
            HCISplitContainer.SplitterDistance = (int)formInfo.SplitDistance;
            ((System.ComponentModel.ISupportInitialize)(this.HCISplitContainer)).EndInit();
            HCICommandTreeView.ContextMenuStrip = TreeContextMenuStrip;
            for (int i = 0; i < HciMgr.HCICommands.Count; i++)
                HCICommandTreeView.Nodes.Add(HciMgr.HCICommands[i].Node);
            HCICommandTreeView.ShowNodeToolTips = true;

            

            
            HCIScriptTreeView.ContextMenuStrip = HCIScriptContextMenuStrip;
            HCIScriptTreeView.ShowNodeToolTips = true;
            GetHCIScriptFiles();

            this.SizeChanged += HCIControlForm_SizeChanged;
        }

        private void HCIControlForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (HciMgr.IsOpen)
                HciMgr.Close();
        }

        private void HCIControlForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            
            
            formInfo.SplitDistance = HCISplitContainer.SplitterDistance;
        }

        private void HCIControlForm_SizeChanged(object sender, EventArgs e)
        {
            
            if (((Size.Width == MdiParent.Size.Width - 4) && (Size.Height == MdiParent.Size.Height - 28))
                || ((Size.Width == 160) && (Size.Height == 27)))
                return;
            formInfo.FormSize = this.Size;
        }

        
        
        
        private void FindSerialPorts()
        {
            string[] SerialPorts = HciMgr.SearchSerialPort();
            SerialPortComboBox.Items.Clear();
            if (SerialPorts.Length > 0)
            {
                foreach (string Port in SerialPorts)
                    SerialPortComboBox.Items.Add(Port);
                if (formInfo.SerialPortIndex < SerialPorts.Length)
                    SerialPortComboBox.SelectedIndex = formInfo.SerialPortIndex;
                else
                    SerialPortComboBox.SelectedIndex = 0;
            }
            PortBaudRateComboBox.Items.Clear();
            PortBaudRateComboBox.Items.AddRange(new object[]
            {
                "9600", "19200", "38400", "57600",
                "115200", "230400", "460800", "921600"
            });
            this.PortBaudRateComboBox.SelectedIndex = 4;
            PortBaudRateComboBox.SelectedItem = formInfo.SerialBaudRate.ToString();
        }
        
        
        
        private void FindUsbDevices()
        {
            string[] ValidDevices = HciMgr.SearchUsbDevices();
            USBDevicesComboBox.Items.Clear();
            if (ValidDevices.Length > 0)
            {
                foreach (string Name in ValidDevices)
                    USBDevicesComboBox.Items.Add(Name);

                if (formInfo.UsbDeviceIndex < USBDevicesComboBox.Items.Count)
                    USBDevicesComboBox.SelectedIndex = formInfo.UsbDeviceIndex;
                else
                    USBDevicesComboBox.SelectedIndex = 0;
            }
        }

        private void PortRefreshButton_Click(object sender, EventArgs e)
        {
            FindSerialPorts();
            FindUsbDevices();
        }

        private void SetPortOpenCloseButtonEnabled(bool Enabled)
        {
            PortOpenButton.Enabled = Enabled;
            PortCloseButton.Enabled = !Enabled;
        }

        private void PortOpenButton_Click(object sender, EventArgs e)
        {
            if (!HciMgr.IsOpen)
            {
                if (OpenFtdiByDescription())
                {
                    fTDI.SetBitMode(0x00, FTDI.FT_BIT_MODES.FT_BIT_MODE_RESET);
                }
                fTDI.Close();

                if ((PortTabControl.SelectedIndex == 0) && (SerialPortComboBox.SelectedIndex >= 0)) 
                {
                    formInfo.SerialPortIndex = SerialPortComboBox.SelectedIndex;
                    formInfo.SerialBaudRate = int.Parse(PortBaudRateComboBox.SelectedItem.ToString());
                    HciMgr.OpenUART(SerialPortComboBox.SelectedItem.ToString(), formInfo.SerialBaudRate);
                }
                else if ((PortTabControl.SelectedIndex == 1) && (USBDevicesComboBox.SelectedIndex >= 0))
                {
                    formInfo.UsbDeviceIndex = USBDevicesComboBox.SelectedIndex;
                    HciMgr.OpenUSB(formInfo.UsbDeviceIndex);
                }

                if (HciMgr.IsOpen)
                {
                    SetPortOpenCloseButtonEnabled(false);
                    
                    
                }
            }
        }

        private void PortCloseButton_Click(object sender, EventArgs e)
        {
            if (HciMgr.IsOpen)
            {
                HciMgr.Close();
                if (!HciMgr.IsOpen)
                {
                    SetPortOpenCloseButtonEnabled(true);

                    if (OpenFtdiByDescription())
                    {
                        fTDI.SetBitMode(0xFF, FTDI.FT_BIT_MODES.FT_BIT_MODE_ASYNC_BITBANG);
                        byte[] lowData = new byte[] { 0x00 };
                        uint bytesWritten = 0;
                        fTDI.Write(lowData, lowData.Length, ref bytesWritten);
                    }
                    fTDI.Close();
                }

            }
        }

        private bool OpenFtdiByDescription(string targetDescription = "UM232H")
        {
            FTDI ftdi = new FTDI();

            uint deviceCount = 0;
            if (ftdi.GetNumberOfDevices(ref deviceCount) != FTDI.FT_STATUS.FT_OK || deviceCount == 0)
            {
                return false;
            }

            FTDI.FT_DEVICE_INFO_NODE[] deviceList = new FTDI.FT_DEVICE_INFO_NODE[deviceCount];
            if (ftdi.GetDeviceList(deviceList) != FTDI.FT_STATUS.FT_OK)
            {
                return false;
            }

            foreach (var device in deviceList)
            {
                if (device.Description.Contains(targetDescription))
                {
                    FTDI.FT_STATUS status = ftdi.OpenBySerialNumber(device.SerialNumber);
                    if (status == FTDI.FT_STATUS.FT_OK)
                    {
                        fTDI = ftdi;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        private BlueTooth.HCI.CommandGroup CurHciCmdGroup;
        private BlueTooth.HCI.Command CurHciCommand;
        private BlueTooth.HCI.Parameter CurHciParameter;

        private void HCICommandTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            switch (e.Node.Level)
            {
                case 0: 
                    CurHciCmdGroup = HciMgr.HCICommands[e.Node.Index];
                    CurHciCommand = null;
                    CurHciParameter = null;
                    break;
                case 1: 
                    CurHciCmdGroup = HciMgr.HCICommands[e.Node.Parent.Index];
                    CurHciCommand = CurHciCmdGroup.Commands[e.Node.Index];
                    CurHciParameter = null;
                    break;
                case 2: 
                    CurHciCmdGroup = HciMgr.HCICommands[e.Node.Parent.Parent.Index];
                    CurHciCommand = CurHciCmdGroup.Commands[e.Node.Parent.Index];
                    CurHciParameter = CurHciCommand.CommandParameters[e.Node.Index];
                    break;
                default:
                    CurHciCmdGroup = null;
                    CurHciCommand = null;
                    CurHciParameter = null;
                    break;
            }
            SetParameter(CurHciParameter);
            if (CurHciCommand != null)
                SelectedHciCommand = CurHciCommand;
        }

        private void sendCommandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurHciCommand != null)
                CurHciCommand.Send();
        }

        private void addScriptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddScriptFromCurHciCommand();
        }

        private void insertScriptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InsertScriptFromCurHciCommand();
        }

        DataTable dtHCIParameter = new DataTable();

        private void SetParameter(BlueTooth.HCI.Parameter Para)
        {
            HCIParaDataGridView.DataSource = null;
            dtHCIParameter.Rows.Clear();
            dtHCIParameter.Columns.Clear();
            if (Para != null)
            {
                
                for (int i = Para.Size - 1; i >= 0; i--)
                    dtHCIParameter.Columns.Add("B" + i.ToString(), typeof(string));
                dtHCIParameter.Rows.Add();
                
                for (int i = Para.Size - 1; i >= 0; i--)
                    dtHCIParameter.Rows[0]["B" + i.ToString()] = Para.Data[i].ToString("X2");

                HCIParaDataGridView.DataSource = dtHCIParameter;
                for (int i = 0; i < Para.Size; i++)
                {
                    HCIParaDataGridView.Columns[i].Width = 30;
                    HCIParaDataGridView.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                HCIParaNameLabel.Text = Para.Name;

                SelectedHciParameter = Para;
            }
            else
                HCIParaNameLabel.Text = "HCI Command Parameter";
        }

        private void HCIParaDataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (SelectedHciParameter != null)
            {
                byte Data = SelectedHciParameter.Data[SelectedHciParameter.Size - 1 - e.ColumnIndex];
                DataTable dt = (DataTable)HCIParaDataGridView.DataSource;

                try
                {
                    Data = (byte)int.Parse(dt.Rows[e.RowIndex][e.ColumnIndex].ToString(), System.Globalization.NumberStyles.HexNumber);
                }
                catch { }
                finally
                {
                    if (SelectedHciParameter.Data[SelectedHciParameter.Size - 1 - e.ColumnIndex] == Data)
                        dt.Rows[e.RowIndex][e.ColumnIndex] = Data.ToString("X2");
                    else
                    {
                        SelectedHciParameter.Data[SelectedHciParameter.Size - 1 - e.ColumnIndex] = Data;
                        
                        
                        
                    }
                }
            }
        }

        private void SendHciCommandButton_Click(object sender, EventArgs e)
        {
            if (SelectedHciCommand != null)
                SelectedHciCommand.Send();
        }

        private void ClearLogButton_Click(object sender, EventArgs e)
        {
            HciMgr.InitHciLogView();
        }

        private BackgroundWorker SaveLogWorker = null;
        private void SaveLogButton_Click(object sender, EventArgs e)
        {
            SaveLogWorker = new BackgroundWorker();
            SaveLogWorker.DoWork += SaveLogWorker_DoWork;
            SaveLogWorker.RunWorkerAsync();
        }

        private void SaveLogWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (HciLogDataGridView == null)
                    return;

                string dir = System.IO.Directory.GetCurrentDirectory();
                string fileName = Path.Combine(dir, "HCI_Log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");

                if (HciLogDataGridView.InvokeRequired)
                {
                    HciLogDataGridView.Invoke(new MethodInvoker(() =>
                    {
                        SaveGridToCsv(HciLogDataGridView, fileName);
                    }));
                }
                else
                {
                    SaveGridToCsv(HciLogDataGridView, fileName);
                }
            }
            finally
            {
                if (SaveLogWorker != null)
                {
                    SaveLogWorker.Dispose();
                    SaveLogWorker = null;
                }
            }
        }

        private static void SaveGridToCsv(DataGridView grid, string filePath)
        {
            if (grid == null)
                return;

            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                
                var headers = grid.Columns
                    .Cast<DataGridViewColumn>()
                    .Select(c => c.HeaderText);
                writer.WriteLine(string.Join(",", headers.Select(EscapeCsv)));

                
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow)
                        continue;

                    var cells = row.Cells
                        .Cast<DataGridViewCell>()
                        .Select(c => c.Value == null ? string.Empty : c.Value.ToString());

                    writer.WriteLine(string.Join(",", cells.Select(EscapeCsv)));
                }
            }
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Contains("\"") || value.Contains(",") || value.Contains("\n"))
            {
                value = value.Replace("\"", "\"\"");
                return "\"" + value + "\"";
            }

            return value;
        }

        private List<BlueTooth.HCI.Command> HCIScript = new List<BlueTooth.HCI.Command>();
        private int HciScrCommandIndex = -1;
        private int HciScrParameterIndex = -1;
        private BlueTooth.HCI.Command HciScrCommand;
        private BlueTooth.HCI.Parameter HciScrParameter;
        private string[] HCIScriptFiles = null;

        private void GetHCIScriptFiles()
        {
            int Index = -1;

            if ((formInfo.HciScriptPath == "") || !System.IO.Directory.Exists(formInfo.HciScriptPath))
                HCIScriptFiles = System.IO.Directory.GetFiles(System.IO.Directory.GetCurrentDirectory() + "\\HCI_Script");
            else
                HCIScriptFiles = System.IO.Directory.GetFiles(formInfo.HciScriptPath);

            HCIScriptComboBox.Items.Clear();
            for (int i = 0; i < HCIScriptFiles.Length; i++)
            {
                HCIScriptComboBox.Items.Add(System.IO.Path.GetFileName(HCIScriptFiles[i]));
                if (HCIScriptFiles[i] == formInfo.SelectedHciScriptFileName)
                    Index = i;
            }
            if (Index >= 0)
            {
                HCIScriptComboBox.SelectedIndex = Index;
                LoadHciScriptFile(HCIScriptFiles[Index]);
            }
        }

        private void ClearHCIScrpit()
        {
            HCIScriptTreeView.Nodes.Clear();
            HCIScript.Clear();
        }

        private void AddScriptFromCurHciCommand()
        {
            if (CurHciCommand != null)
            {
                BlueTooth.HCI.Command Cmd = CurHciCommand.Clone();
                HCIScript.Add(Cmd);
                HCIScriptTreeView.Nodes.Add(Cmd.CommandNode);
            }
        }

        private void InsertScriptFromCurHciCommand()
        {
            if (CurHciCommand != null)
            {
                BlueTooth.HCI.Command Cmd = CurHciCommand.Clone();
                if ((HciScrCommand != null) && (HciScrCommandIndex >= 0))
                {
                    HCIScript.Insert(HciScrCommandIndex, Cmd);
                    HCIScriptTreeView.Nodes.Insert(HciScrCommandIndex, Cmd.CommandNode);
                }
                else
                {
                    HCIScript.Add(Cmd);
                    HCIScriptTreeView.Nodes.Add(Cmd.CommandNode);
                }
            }
        }

        private void DeleteScriptFromScrHciCommand()
        {
            if ((HciScrCommand != null) && (HciScrCommandIndex >= 0))
            {
                HCIScript.RemoveAt(HciScrCommandIndex);
                HCIScriptTreeView.Nodes.RemoveAt(HciScrCommandIndex);
            }
        }

        private void ChangeScriptPath(string Path)
        {
            ClearHCIScrpit();
            formInfo.HciScriptPath = Path;
            GetHCIScriptFiles();
        }

        private void SaveHciScriptFile(string fileName)
        {
            if (HCIScript == null || HCIScript.Count == 0)
                return;

            using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                writer.WriteLine("HCI-SCRIPT-V1");

                foreach (var cmd in HCIScript)
                {
                    var parts = new List<string>
                    {
                        "CMD",
                        cmd.Name
                    };

                    foreach (Parameter para in cmd.CommandParameters)
                    {
                        string hex = BitConverter.ToString(para.Data).Replace("-", string.Empty);
                        parts.Add(hex);
                    }

                    writer.WriteLine(string.Join("|", parts));
                }
            }
        }

        private void SaveAsHciScriptFile()
        {
            using (SaveFileDialog fileDlg = new SaveFileDialog())
            {
                fileDlg.Filter = "Script File (*.hcs)|*.hcs|All files (*.*)|*.*";

                if (string.IsNullOrEmpty(formInfo.SelectedHciScriptFileName))
                    fileDlg.InitialDirectory = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "HCI_Script");
                else
                    fileDlg.InitialDirectory = System.IO.Path.GetDirectoryName(formInfo.SelectedHciScriptFileName);

                if (fileDlg.ShowDialog() == DialogResult.OK)
                {
                    formInfo.SelectedHciScriptFileName = fileDlg.FileName;
                    formInfo.HciScriptPath = System.IO.Path.GetDirectoryName(formInfo.SelectedHciScriptFileName);

                    SaveHciScriptFile(formInfo.SelectedHciScriptFileName);
                    ChangeScriptPath(formInfo.HciScriptPath);
                }
            }
        }

        private bool LoadHciScriptFile(string fileName)
        {
            if (!System.IO.File.Exists(fileName))
                return false;

            ClearHCIScrpit();

            string[] lines;
            try
            {
                lines = System.IO.File.ReadAllLines(fileName, Encoding.UTF8);
            }
            catch
            {
                MessageBox.Show("Failed to read HCI Script file.");
                return false;
            }

            int startIndex = 0;
            if (lines.Length > 0 && lines[0].StartsWith("HCI-SCRIPT-", StringComparison.OrdinalIgnoreCase))
                startIndex = 1;

            for (int i = startIndex; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                var tokens = line.Split('|');
                if (tokens.Length < 2)
                    continue;
                if (!string.Equals(tokens[0], "CMD", StringComparison.OrdinalIgnoreCase))
                    continue;

                string cmdName = tokens[1];

                BlueTooth.HCI.Command template = null;
                foreach (var group in HciMgr.HCICommands)
                {
                    var temp = group.Commands.GetCommand(cmdName);
                    if (temp != null)
                    {
                        template = temp;
                        break;
                    }
                }

                if (template == null)
                    continue;

                var cmd = template.Clone();

                for (int p = 0; p < cmd.CommandParameters.Count && (2 + p) < tokens.Length; p++)
                {
                    string hex = tokens[2 + p];
                    var para = cmd.CommandParameters[p];
                    var bytes = HexToBytes(hex);

                    int len = Math.Min(bytes.Length, para.Size);
                    for (int b = 0; b < len; b++)
                        para.Data[b] = bytes[b];
                    for (int b = len; b < para.Size; b++)
                        para.Data[b] = 0;
                }

                HCIScript.Add(cmd);
                HCIScriptTreeView.Nodes.Add(cmd.CommandNode);
            }

            return true;
        }

        private void sendCommandToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (HciScrCommand != null)
                HciScrCommand.Send();
        }

        private void runScriptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (BlueTooth.HCI.Command Cmd in HCIScript)
            {
                Cmd.Send();
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteScriptFromScrHciCommand();
        }

        private void changeScriptDirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog FolderDlg = new FolderBrowserDialog())
            {
                if (formInfo.SelectedHciScriptFileName == "")
                    FolderDlg.SelectedPath = System.IO.Directory.GetCurrentDirectory() + "\\HCI_Script";
                else
                    FolderDlg.SelectedPath = System.IO.Path.GetDirectoryName(formInfo.SelectedHciScriptFileName);

                if (FolderDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    ChangeScriptPath(FolderDlg.SelectedPath);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (HCIScript.Count > 0)
            {
                if ((formInfo.SelectedHciScriptFileName == null) || (formInfo.SelectedHciScriptFileName == ""))
                    SaveAsHciScriptFile();
                else
                    SaveHciScriptFile(formInfo.SelectedHciScriptFileName);
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAsHciScriptFile();
        }

        private void HCIScriptTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            switch (e.Node.Level)
            {
                case 0: 
                    HciScrCommand = HCIScript[e.Node.Index];
                    HciScrCommandIndex = e.Node.Index;
                    HciScrParameter = null;
                    HciScrParameterIndex = -1;
                    break;
                case 1: 
                    HciScrCommand = HCIScript[e.Node.Parent.Index];
                    HciScrCommandIndex = e.Node.Parent.Index;
                    HciScrParameter = HciScrCommand.CommandParameters[e.Node.Index];
                    HciScrParameterIndex = e.Node.Index;
                    break;
                default:
                    HciScrCommand = null;
                    HciScrParameter = null;
                    HciScrCommandIndex = -1;
                    HciScrParameterIndex = -1;
                    break;
            }
            SetParameter(HciScrParameter);
            if (HciScrCommand != null)
                SelectedHciCommand = HciScrCommand;
        }

        private void HCIScriptComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((HCIScriptComboBox.SelectedIndex >= 0)
                && (HCIScriptComboBox.SelectedIndex < HCIScriptFiles.Length))
            {
                if (LoadHciScriptFile(HCIScriptFiles[HCIScriptComboBox.SelectedIndex]))
                    formInfo.SelectedHciScriptFileName = HCIScriptFiles[HCIScriptComboBox.SelectedIndex];
            }
        }

        private void ClearHciScriptButton_Click(object sender, EventArgs e)
        {
            if (HciScrCommand != null)
            {
                HCIScript.Clear();
                HCIScriptTreeView.Nodes.Clear();
            }
        }

        BackgroundWorker HciTestWorker = new BackgroundWorker();

        private bool IsModTest = false;
        private void button_RunModTest_Click(object sender, EventArgs e)
        {
            if (IsModTest == false)
            {
                HciTestWorker = new BackgroundWorker()
                {
                    WorkerSupportsCancellation = true,
                };
                HciTestWorker.DoWork += RunModulationTest;
                HciTestWorker.RunWorkerAsync();
                IsModTest = true;
                button_RunModTest.Text = "Stop Modulation";
            }
            else
            {
                HciTestWorker.CancelAsync();
                IsModTest = false;
                button_RunModTest.Text = "Run Modulation";
            }
        }

        private void RunModulationTest(object sender, DoWorkEventArgs e)
        {
            BlueTooth.HCI.Command TxTest = null;
            BlueTooth.HCI.Command TestEnd = null;

            TxTest = HciMgr.GetCommand(0x0C03); 
            TxTest.Send();
            TestEnd = HciMgr.GetCommand(0x201F); 

            TxTest = HciMgr.GetCommand(0x201E); 
            TxTest.CommandParameters[0].Data[0] = 0x13; 
            TxTest.CommandParameters[1].Data[0] = 37; 

            while (!HciTestWorker.CancellationPending)
            {
                TxTest.CommandParameters[2].Data[0] = 0x01; 
                TxTest.Send();
                System.Threading.Thread.Sleep(30);
                TestEnd.Send();
                TxTest.CommandParameters[2].Data[0] = 0x02; 
                TxTest.Send();
                System.Threading.Thread.Sleep(30);
                TestEnd.Send();
            }
        }

        private void dataGridView_AdvReports_SizeChanged(object sender, EventArgs e)
        {
            int width = dataGridView_AdvReports.Size.Width;
            width = width - 21;

            int BDAddrWidth = 120;
            int RSSIWidth = 45;
            int TypeWidth = 30;
            int HandleWidth = 55;
            int DataWidth = BDAddrWidth + RSSIWidth + TypeWidth * 2 + HandleWidth;

            if ((DataWidth + 120) < width)
                DataWidth = width - DataWidth;
            else
                DataWidth = 120;

            Column_BDAddress.Width = BDAddrWidth;
            Column_RSSI.Width = RSSIWidth;
            Column_EventType.Width = TypeWidth;
            Column_AddrType.Width = TypeWidth;
            Column_ConnHandle.Width = HandleWidth;
            Column_Data.Width = DataWidth;
        }

        private void dataGridView_AdvReports_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView_AdvReports.RowCount > 0)
            {
                if (HciMgr.SetConnectionParameter(dataGridView_AdvReports.CurrentCell.RowIndex))
                    SetParameter(CurHciParameter);
            }
        }

        private static byte[] HexToBytes(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return Array.Empty<byte>();

            hex = hex.Trim();

            if ((hex.Length & 1) == 1)
                hex = "0" + hex;

            int len = hex.Length / 2;
            var bytes = new byte[len];

            for (int i = 0; i < len; i++)
            {
                bytes[i] = byte.Parse(
                    hex.Substring(i * 2, 2),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture);
            }

            return bytes;
        }
    }
}
