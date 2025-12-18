using Ivi.Visa;
using Keysight.Visa;
using System.ComponentModel;
using System.Text.Json;

namespace SKAIChips_Verification_Tool
{
    public partial class InstrumentForm : Form
    {

        private readonly BindingList<InstrumentInfo> _instruments = new BindingList<InstrumentInfo>();

        private InstrumentInfo SelectedIns
        {
            get
            {
                if (dataGridView_InsList.CurrentRow == null)
                    return null;

                return dataGridView_InsList.CurrentRow.DataBoundItem as InstrumentInfo;
            }
        }

        public InstrumentForm()
        {
            InitializeComponent();
            InitializeLogic();
        }

        private void InitializeLogic()
        {
            dataGridView_InsList.AutoGenerateColumns = false;
            dataGridView_InsList.DataSource = _instruments;

            dataGridView_InsList.SelectionChanged += dataGridView_InsList_SelectionChanged;
            dataGridView_InsList.CellContentClick += dataGridView_InsList_CellContentClick;

            button_AddInstrument.Click += button_AddInstrument_Click;
            button_RemoveInstrument.Click += button_RemoveInstrument_Click;
            button_InsUp.Click += button_InsUp_Click;
            button_InsDown.Click += button_InsDown_Click;

            button_SendInsCommand.Click += button_SendInsCommand_Click;
            button_InsScreenCapture.Click += button_InsScreenCapture_Click;
            button_ClearInsLog.Click += button_ClearInsLog_Click;

            textBox_InsCommand.KeyDown += textBox_InsCommand_KeyDown;

            LoadDefaultTypes();
            UpdateSelectedInstrument();

            Load += InstrumentForm_Load;
            FormClosing += InstrumentForm_FormClosing;
        }

        private void InstrumentForm_Load(object sender, EventArgs e)
        {
            LoadInstrumentSettings();
        }

        private void InstrumentForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveInstrumentSettings();
        }

        private void LoadDefaultTypes()
        {
            string[] defaults =
            {
                "SpectrumAnalyzer",
                "OscilloScope",
                "PowerSupply",
                "DigitalMultimeter",
                "TempChamber"
            };

            comboBox_InsTypes.Items.Clear();
            comboBox_InsTypes.Items.AddRange(defaults);
            if (comboBox_InsTypes.Items.Count > 0)
                comboBox_InsTypes.SelectedIndex = 0;
        }

        private string GetSettingsPath()
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(exeDir, "InstrumentSettings.json");
        }

        private void LoadInstrumentSettings()
        {
            try
            {
                string path = GetSettingsPath();
                if (!File.Exists(path))
                    return;

                string json = File.ReadAllText(path);
                var list = JsonSerializer.Deserialize<List<InstrumentInfo>>(json);
                if (list == null)
                    return;

                _instruments.Clear();
                foreach (var ins in list)
                    _instruments.Add(ins);
            }
            catch
            {
            }
        }

        private void SaveInstrumentSettings()
        {
            try
            {
                string path = GetSettingsPath();
                var list = _instruments.ToList();
                var json = JsonSerializer.Serialize(list, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(path, json);
            }
            catch
            {
            }
        }

        private void dataGridView_InsList_SelectionChanged(object sender, EventArgs e)
        {
            UpdateSelectedInstrument();
        }

        private void UpdateSelectedInstrument()
        {
            var ins = SelectedIns;
            textBox_InsType.Text = ins?.Type ?? string.Empty;
        }

        private int GetNextIndexForType(string baseType)
        {
            var max = -1;

            foreach (var ins in _instruments)
            {
                if (string.IsNullOrEmpty(ins.Type))
                    continue;

                if (!ins.Type.StartsWith(baseType, StringComparison.OrdinalIgnoreCase))
                    continue;

                var suffix = ins.Type.Substring(baseType.Length);
                if (int.TryParse(suffix, out var n))
                {
                    if (n > max)
                        max = n;
                }
            }

            return max + 1;
        }

        private void button_AddInstrument_Click(object sender, EventArgs e)
        {
            var baseType = comboBox_InsTypes.Text?.Trim();
            if (string.IsNullOrEmpty(baseType))
                return;

            var exists = comboBox_InsTypes.Items.Cast<object>()
                .Any(x => string.Equals(x.ToString(), baseType, StringComparison.OrdinalIgnoreCase));
            if (!exists)
                comboBox_InsTypes.Items.Add(baseType);

            var idx = GetNextIndexForType(baseType);
            var typeName = baseType + idx;

            var info = new InstrumentInfo
            {
                Type = typeName,
                Enabled = false,
                VisaAddress = string.Empty,
                Name = string.Empty
            };

            _instruments.Add(info);

            dataGridView_InsList.ClearSelection();
            var rowIndex = _instruments.Count - 1;
            if (rowIndex >= 0)
            {
                dataGridView_InsList.Rows[rowIndex].Selected = true;
                dataGridView_InsList.CurrentCell = dataGridView_InsList.Rows[rowIndex].Cells[0];
            }
        }

        private void button_RemoveInstrument_Click(object sender, EventArgs e)
        {
            if (dataGridView_InsList.CurrentRow == null)
                return;

            var index = dataGridView_InsList.CurrentRow.Index;
            if (index < 0 || index >= _instruments.Count)
                return;

            _instruments.RemoveAt(index);

            if (_instruments.Count == 0)
            {
                textBox_InsType.Text = string.Empty;
                return;
            }

            var newIndex = index;
            if (newIndex >= _instruments.Count)
                newIndex = _instruments.Count - 1;

            dataGridView_InsList.ClearSelection();
            dataGridView_InsList.Rows[newIndex].Selected = true;
            dataGridView_InsList.CurrentCell = dataGridView_InsList.Rows[newIndex].Cells[0];
        }

        private void button_InsUp_Click(object sender, EventArgs e)
        {
            if (dataGridView_InsList.CurrentRow == null)
                return;

            var index = dataGridView_InsList.CurrentRow.Index;
            if (index <= 0 || index >= _instruments.Count)
                return;

            var item = _instruments[index];
            _instruments.RemoveAt(index);
            _instruments.Insert(index - 1, item);

            dataGridView_InsList.ClearSelection();
            dataGridView_InsList.Rows[index - 1].Selected = true;
            dataGridView_InsList.CurrentCell = dataGridView_InsList.Rows[index - 1].Cells[0];
        }

        private void button_InsDown_Click(object sender, EventArgs e)
        {
            if (dataGridView_InsList.CurrentRow == null)
                return;

            var index = dataGridView_InsList.CurrentRow.Index;
            if (index < 0 || index >= _instruments.Count - 1)
                return;

            var item = _instruments[index];
            _instruments.RemoveAt(index);
            _instruments.Insert(index + 1, item);

            dataGridView_InsList.ClearSelection();
            dataGridView_InsList.Rows[index + 1].Selected = true;
            dataGridView_InsList.CurrentCell = dataGridView_InsList.Rows[index + 1].Cells[0];
        }

        private void dataGridView_InsList_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (!ReferenceEquals(dataGridView_InsList.Columns[e.ColumnIndex], Column_InsTest))
                return;

            var info = dataGridView_InsList.Rows[e.RowIndex].DataBoundItem as InstrumentInfo;
            if (info == null)
                return;

            TestInstrument(info);
            dataGridView_InsList.Refresh();
        }

        private bool IsInstrumentReady(InstrumentInfo instrument)
        {
            if (instrument == null)
                return false;

            if (!instrument.Enabled)
            {
                MessageBox.Show(this, "Enable 체크 후 사용하세요.", "Instrument", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(instrument.VisaAddress))
            {
                MessageBox.Show(this, "VISA Address가 비어 있습니다.", "Instrument", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void TestInstrument(InstrumentInfo info)
        {
            if (!IsInstrumentReady(info))
                return;

            var rowIndex = _instruments.IndexOf(info);
            DataGridViewCell nameCell = null;

            if (rowIndex >= 0 && rowIndex < dataGridView_InsList.Rows.Count)
                nameCell = dataGridView_InsList.Rows[rowIndex].Cells[Column_InsName.Index];

            try
            {
                using (var scpi = CreateScpiClient(info.VisaAddress))
                {
                    if (!scpi.Open())
                    {
                        info.Name = "Open failed";
                        if (nameCell != null)
                            nameCell.Style.ForeColor = System.Drawing.Color.Coral;

                        AppendLog($"[{DateTime.Now:HH:mm:ss}] {info.Type} open failed");
                        return;
                    }

                    var idn = scpi.Query("*IDN?", 2000);
                    info.Name = idn;

                    if (nameCell != null)
                        nameCell.Style.ForeColor = System.Drawing.Color.ForestGreen;

                    AppendLog($"[{DateTime.Now:HH:mm:ss}] {info.Type} *IDN? => {idn}");
                    scpi.Close();
                }
            }
            catch (Exception ex)
            {
                info.Name = "Error";
                if (nameCell != null)
                    nameCell.Style.ForeColor = System.Drawing.Color.Coral;

                AppendLog($"[{DateTime.Now:HH:mm:ss}] {info.Type} error: {ex.Message}");
            }
        }

        private void button_SendInsCommand_Click(object sender, EventArgs e)
        {
            var ins = SelectedIns;
            if (ins == null)
                return;

            if (!IsInstrumentReady(ins))
                return;

            var cmd = textBox_InsCommand.Text;
            if (string.IsNullOrWhiteSpace(cmd))
                return;

            AppendLog($"[{DateTime.Now:HH:mm:ss}] {ins.Type} > {cmd}");

            try
            {
                using (var scpi = CreateScpiClient(ins.VisaAddress))
                {
                    if (!scpi.Open())
                    {
                        AppendLog($"[{DateTime.Now:HH:mm:ss}] {ins.Type} open failed");
                        return;
                    }

                    if (cmd.Contains("?"))
                    {
                        var reply = scpi.Query(cmd, 2000);
                        AppendLog($"[{DateTime.Now:HH:mm:ss}] {ins.Type} < {reply}");
                    }
                    else
                    {
                        scpi.Write(cmd);
                    }

                    scpi.Close();
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[{DateTime.Now:HH:mm:ss}] {ins.Type} error: {ex.Message}");
            }

            textBox_InsCommand.SelectAll();
        }

        private void textBox_InsCommand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            e.SuppressKeyPress = true;
            button_SendInsCommand_Click(sender, EventArgs.Empty);
        }

        private void button_InsScreenCapture_Click(object sender, EventArgs e)
        {
            var ins = SelectedIns;
            if (ins == null)
                return;

            if (!IsInstrumentReady(ins))
                return;

            try
            {
                using (var scpi = CreateScpiClient(ins.VisaAddress))
                {
                    if (!scpi.Open())
                    {
                        AppendLog($"[{DateTime.Now:HH:mm:ss}] {ins.Type} open failed");
                        return;
                    }

                    byte[] data = null;

                    if (ins.Type.StartsWith("SpectrumAnalyzer", StringComparison.OrdinalIgnoreCase))
                    {
                        var dir = scpi.Query(":MMEMory:CDIRectory?", 2000);
                        var tempName = "\\JL_TempScreenCapture.png";
                        var cleaned = string.Empty;

                        foreach (var c in dir)
                        {
                            if (c != '"' && c != '\n' && c != '\r')
                                cleaned += c;
                        }

                        var fullPath = cleaned + tempName;

                        scpi.Write(":MMEMory:STORe:SCReen \"" + fullPath + "\"");
                        scpi.Write("*WAI");
                        data = scpi.QueryBytes(":MMEM:DATA? \"" + fullPath + "\"", 30000);
                        scpi.Write("*WAI");
                        scpi.Write(":MMEM:DEL \"" + fullPath + "\"");
                        scpi.Write("*CLS");
                    }
                    else if (ins.Type.StartsWith("OscilloScope", StringComparison.OrdinalIgnoreCase))
                    {
                        data = scpi.QueryBytes(":HCOPY:SDUMp:DATA?", 30000);
                    }

                    if (data != null && data.Length > 0)
                    {
                        using (var ms = new MemoryStream(data))
                        {
                            var image = System.Drawing.Image.FromStream(ms);
                            Clipboard.Clear();
                            Clipboard.SetImage(image);
                            richTextBox_InsCommandLog.Paste();
                        }

                        AppendLog($"[{DateTime.Now:HH:mm:ss}] {ins.Type} capture done");
                    }
                    else
                    {
                        AppendLog($"[{DateTime.Now:HH:mm:ss}] {ins.Type} capture failed or no data");
                    }

                    scpi.Close();
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[{DateTime.Now:HH:mm:ss}] {ins.Type} capture error: {ex.Message}");
            }
        }

        private void button_ClearInsLog_Click(object sender, EventArgs e)
        {
            richTextBox_InsCommandLog.Clear();
        }

        private void AppendLog(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (richTextBox_InsCommandLog.TextLength > 0)
                richTextBox_InsCommandLog.AppendText(Environment.NewLine);

            richTextBox_InsCommandLog.AppendText(text);
        }

        private IScpiClient CreateScpiClient(string visaAddress)
        {
            return new VisaScpiClient(visaAddress);
        }

    }

    public class InstrumentInfo : INotifyPropertyChanged
    {
        private string type;
        private bool enabled;
        private string visaAddress;
        private string name;

        public string Type
        {
            get => type;
            set
            {
                if (type == value)
                    return;
                type = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        public bool Enabled
        {
            get => enabled;
            set
            {
                if (enabled == value)
                    return;
                enabled = value;
                OnPropertyChanged(nameof(Enabled));
            }
        }

        public string VisaAddress
        {
            get => visaAddress;
            set
            {
                if (visaAddress == value)
                    return;
                visaAddress = value;
                OnPropertyChanged(nameof(VisaAddress));
            }
        }

        public string Name
        {
            get => name;
            set
            {
                if (name == value)
                    return;
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            var h = PropertyChanged;
            if (h != null)
                h(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public interface IScpiClient : IDisposable
    {
        bool Open();
        void Close();
        void Write(string command);
        string Query(string command, int timeoutMs = 1000);
        byte[] QueryBytes(string command, int timeoutMs = 30000);
    }

    public class VisaScpiClient : IScpiClient
    {
        private readonly string _visaAddress;
        private IMessageBasedSession _session;

        public VisaScpiClient(string visaAddress)
        {
            _visaAddress = visaAddress ?? throw new ArgumentNullException(nameof(visaAddress));
        }

        public bool Open()
        {
            if (_session != null)
                return true;

            var rm = new ResourceManager();
            var addr = _visaAddress.Trim();

            _session = rm.Open(addr) as IMessageBasedSession;
            if (_session == null)
                return false;

            _session.TimeoutMilliseconds = 5000;
            _session.TerminationCharacter = (byte)'\n';
            _session.TerminationCharacterEnabled = true;

            return true;
        }

        public void Close()
        {
            _session?.Dispose();
            _session = null;
        }

        public void Write(string command)
        {
            if (_session == null)
                throw new InvalidOperationException("VISA session is not open.");
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            _session.FormattedIO.WriteLine(command);
        }

        public string Query(string command, int timeoutMs = 1000)
        {
            if (_session == null)
                throw new InvalidOperationException("VISA session is not open.");
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var oldTimeout = _session.TimeoutMilliseconds;
            try
            {
                _session.TimeoutMilliseconds = timeoutMs;
                _session.FormattedIO.WriteLine(command);
                return _session.FormattedIO.ReadLine();
            }
            finally
            {
                _session.TimeoutMilliseconds = oldTimeout;
            }
        }

        public byte[] QueryBytes(string command, int timeoutMs = 30000)
        {
            if (_session == null)
                throw new InvalidOperationException("VISA session is not open.");
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var oldTimeout = _session.TimeoutMilliseconds;
            try
            {
                _session.TimeoutMilliseconds = timeoutMs;
                _session.FormattedIO.WriteLine(command);
                var data = _session.FormattedIO.ReadBinaryBlockOfByte();
                return data;
            }
            finally
            {
                _session.TimeoutMilliseconds = oldTimeout;
            }
        }

        public void Dispose()
        {
            Close();
        }
    }

}
