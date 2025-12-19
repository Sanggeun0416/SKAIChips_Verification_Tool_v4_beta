using ClosedXML.Excel;
using SKAIChips_Verification_Tool.RegisterControl.Chips;
using SKAIChips_Verification_Tool.RegisterControl.Core;
using SKAIChips_Verification_Tool.RegisterControl.Infra;
using System.Diagnostics;
using System.Globalization;

namespace SKAIChips_Verification_Tool
{
    public partial class RegisterControlForm : Form
    {
        private II2cBus? _i2cBus;
        private ISpiBus? _spiBus;
        private IRegisterChip? _chip;

        private readonly List<IChipProject> _projects = new();
        private IChipProject? _selectedProject;

        private FtdiDeviceSettings? _ftdiSettings;
        private ProtocolSettings? _protocolSettings;

        private string? _regMapFilePath;
        private readonly List<RegisterGroup> _groups = new();
        private RegisterGroup? _selectedGroup;
        private Register? _selectedRegister;
        private RegisterItem? _selectedItem;
        private uint _currentRegValue;
        private bool _isUpdatingRegValue;
        private string? _scriptFilePath;
        private string? _firmwareFilePath;

        private const int I2cTimeoutMs = 200;

        private readonly Button[] _bitButtons = new Button[32];
        private bool _isUpdatingBits;

        private readonly Dictionary<Register, uint> _regValues = new();
        private readonly IniFile _iniFile = new(Path.Combine(AppContext.BaseDirectory, "settings.ini"));

        private IChipTestSuite? _testSuite;
        private CancellationTokenSource? _testCts;
        private bool _isRunningTest;

        public RegisterControlForm()
        {
            InitializeComponent();
            InitUi();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            try
            {
                _testCts?.Cancel();
            }
            catch { }
            DisconnectBus();
        }

        private void InitUi()
        {
            InitLogGrid();
            InitBitsGrid();
            InitTreeContextMenu();
            InitBitButtons();
            InitBitButtonLayoutHandlers();
            InitRegisterMapControls();
            InitRegisterValueControls();
            InitScriptControls();
            InitStatusControls();
            InitRunTestUi();

            LoadProjects();
            UpdateStatusText();
        }

        private void InitLogGrid()
        {
            dgvLog.Rows.Clear();
        }

        private void InitBitsGrid()
        {
            if (dgvBits == null)
                return;

            dgvBits.AutoGenerateColumns = false;
            dgvBits.Columns.Clear();

            var colBit = new DataGridViewTextBoxColumn { Name = "colBit", HeaderText = "Bit", ReadOnly = true };
            var colName = new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Name", ReadOnly = true };
            var colDefault = new DataGridViewTextBoxColumn { Name = "colDefault", HeaderText = "Default", ReadOnly = true };
            var colDesc = new DataGridViewTextBoxColumn
            {
                Name = "colDesc",
                HeaderText = "Description",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };

            dgvBits.Columns.Add(colBit);
            dgvBits.Columns.Add(colName);
            dgvBits.Columns.Add(colDefault);
            dgvBits.Columns.Add(colDesc);

            dgvBits.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvBits.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            colDesc.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void InitTreeContextMenu()
        {
            var ctx = new ContextMenuStrip();
            var mExpand = new ToolStripMenuItem("모두 펼치기");
            var mCollapse = new ToolStripMenuItem("모두 접기");
            var mSearch = new ToolStripMenuItem("검색...");

            mExpand.Click += (s, e) => tvRegs.ExpandAll();
            mCollapse.Click += (s, e) => tvRegs.CollapseAll();
            mSearch.Click += (s, e) => ShowTreeSearchDialog();

            ctx.Items.Add(mExpand);
            ctx.Items.Add(mCollapse);
            ctx.Items.Add(new ToolStripSeparator());
            ctx.Items.Add(mSearch);
            tvRegs.ContextMenuStrip = ctx;
        }

        private void InitBitButtons()
        {
            flowBitsTop.Controls.Clear();
            flowBitsBottom.Controls.Clear();

            for (int i = 0; i < 32; i++)
            {
                var btn = new Button
                {
                    Margin = new Padding(1),
                    Padding = new Padding(0),
                    Width = 24,
                    Height = 25,
                    Text = "0",
                    Tag = i,
                    FlatStyle = FlatStyle.Flat
                };
                btn.FlatAppearance.BorderSize = 1;
                btn.Click += BitButton_Click;

                _bitButtons[i] = btn;

                if (i < 16)
                    flowBitsTop.Controls.Add(btn);
                else
                    flowBitsBottom.Controls.Add(btn);
            }

            UpdateBitButtonsFromValue(_currentRegValue);
            UpdateBitButtonLayout();
        }

        private void InitBitButtonLayoutHandlers()
        {
            flowBitsTop.SizeChanged += (s, e) => UpdateBitButtonLayout();
            flowBitsBottom.SizeChanged += (s, e) => UpdateBitButtonLayout();
            groupRegCont.Resize += (s, e) => UpdateBitButtonLayout();
        }

        private void UpdateBitButtonLayout()
        {
            int cols = 16;

            if (flowBitsTop.ClientSize.Width > 0)
            {
                int panelWidth = flowBitsTop.ClientSize.Width;
                int btnWidth = (panelWidth - (cols + 1) * 2) / cols;
                if (btnWidth < 16)
                    btnWidth = 16;
                if (btnWidth > 40)
                    btnWidth = 40;
                int btnHeight = 25;

                for (int i = 0; i < 16; i++)
                {
                    var btn = _bitButtons[i];
                    if (btn == null)
                        continue;
                    btn.Width = btnWidth;
                    btn.Height = btnHeight;
                }
            }

            if (flowBitsBottom.ClientSize.Width > 0)
            {
                int panelWidth = flowBitsBottom.ClientSize.Width;
                int btnWidth = (panelWidth - (cols + 1) * 2) / cols;
                if (btnWidth < 16)
                    btnWidth = 16;
                if (btnWidth > 40)
                    btnWidth = 40;
                int btnHeight = 25;

                for (int i = 16; i < 32; i++)
                {
                    var btn = _bitButtons[i];
                    if (btn == null)
                        continue;
                    btn.Width = btnWidth;
                    btn.Height = btnHeight;
                }
            }
        }

        private void InitRegisterMapControls()
        {
            lblMapFileName.Text = "(No file)";
            btnOpenMapPath.Enabled = false;
        }

        private void InitRegisterValueControls()
        {
            UpdateBitButtonsFromValue(_currentRegValue);
            SetBitButtonsEnabledForItem(null);

            txtRegValueHex.Leave += txtRegValueHex_Leave;

            btnWriteAll.Click += btnWriteAll_Click;
            btnReadAll.Click += btnReadAll_Click;

            lblRegName.Text = "(No Register)";
            lblRegAddrSummary.Text = "Address: -";
            lblRegResetSummary.Text = "Reset Value: -";
            txtRegValueHex.Text = "0x00000000";

            numRegIndex.Minimum = 0;
            numRegIndex.Maximum = 0;
            numRegIndex.Value = 0;
            numRegIndex.Enabled = false;
            numRegIndex.ValueChanged += numRegIndex_ValueChanged;
        }

        private void InitScriptControls()
        {
            lblScriptFileName.Text = "(No script)";
            btnOpenScriptPath.Enabled = false;
        }

        private void InitStatusControls()
        {
            btnConnect.Text = "Connect";
            UpdateStatusText();
        }

        private void InitRunTestUi()
        {
            dgvTestLog.AutoGenerateColumns = false;
            dgvTestLog.Columns.Clear();

            var colTime = new DataGridViewTextBoxColumn { Name = "colTime", HeaderText = "Time", ReadOnly = true };
            var colLevel = new DataGridViewTextBoxColumn { Name = "colLevel", HeaderText = "Level", ReadOnly = true };
            var colMessage = new DataGridViewTextBoxColumn
            {
                Name = "colMessage",
                HeaderText = "Message",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };

            dgvTestLog.Columns.Add(colTime);
            dgvTestLog.Columns.Add(colLevel);
            dgvTestLog.Columns.Add(colMessage);

            dgvTestLog.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvTestLog.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            colMessage.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            comboTestCategory.Items.Clear();
            comboTests.Items.Clear();
            btnRunTest.Enabled = false;
            btnStopTest.Enabled = false;
            dgvTestLog.Rows.Clear();

            comboTestCategory.SelectedIndexChanged += comboTestCategory_SelectedIndexChanged;
            btnRunTest.Click += btnRunTest_Click;
            btnStopTest.Click += btnStopTest_Click;
        }

        private string GetCurrentProjectName()
        {
            return _selectedProject?.Name ?? "UnknownProject";
        }

        private string? PromptText(string title, string label, string defaultValue)
        {
            using (var form = new Form())
            using (var lbl = new Label())
            using (var txt = new TextBox())
            using (var btnOk = new Button())
            using (var btnCancel = new Button())
            {
                form.Text = title;
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ClientSize = new Size(320, 120);

                lbl.AutoSize = true;
                lbl.Text = label;
                lbl.Location = new Point(9, 9);

                txt.Size = new Size(300, 23);
                txt.Location = new Point(9, 30);
                txt.Text = defaultValue;

                btnOk.Text = "OK";
                btnOk.DialogResult = DialogResult.OK;
                btnOk.Location = new Point(152, 70);

                btnCancel.Text = "Cancel";
                btnCancel.DialogResult = DialogResult.Cancel;
                btnCancel.Location = new Point(234, 70);

                form.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                var result = form.ShowDialog(this);
                if (result == DialogResult.OK)
                    return txt.Text;

                return null;
            }
        }

        private void ShowTreeSearchDialog()
        {
            string? text = PromptText("Register 검색", "검색할 텍스트를 입력하세요:", "");
            if (string.IsNullOrWhiteSpace(text))
                return;

            var matches = FindTreeNodesContains(tvRegs.Nodes, text);
            if (matches.Count == 0)
            {
                MessageBox.Show("일치하는 항목이 없습니다.");
                return;
            }

            if (matches.Count == 1)
            {
                var node = matches[0];
                tvRegs.SelectedNode = node;
                tvRegs.Focus();
                node.EnsureVisible();
                return;
            }

            using (var form = new Form())
            using (var lst = new ListBox())
            using (var btnOk = new Button())
            using (var btnCancel = new Button())
            {
                form.Text = "검색 결과";
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ClientSize = new Size(400, 320);

                lst.Location = new Point(10, 10);
                lst.Size = new Size(380, 250);
                lst.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

                foreach (var node in matches)
                    lst.Items.Add(node);

                lst.DisplayMember = "Text";

                btnOk.Text = "OK";
                btnOk.DialogResult = DialogResult.OK;
                btnOk.Location = new Point(224, 275);
                btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

                btnCancel.Text = "Cancel";
                btnCancel.DialogResult = DialogResult.Cancel;
                btnCancel.Location = new Point(315, 275);
                btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

                form.Controls.Add(lst);
                form.Controls.Add(btnOk);
                form.Controls.Add(btnCancel);
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    if (lst.SelectedItem is TreeNode selected)
                    {
                        tvRegs.SelectedNode = selected;
                        tvRegs.Focus();
                        selected.EnsureVisible();
                    }
                }
            }
        }

        private List<TreeNode> FindTreeNodesContains(TreeNodeCollection nodes, string text)
        {
            var result = new List<TreeNode>();

            foreach (TreeNode node in nodes)
            {
                if (node.Text.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                    result.Add(node);

                if (node.Nodes.Count > 0)
                    result.AddRange(FindTreeNodesContains(node.Nodes, text));
            }

            return result;
        }

        private void UpdateStatusText()
        {
            if (_protocolSettings == null)
            {
                lblProtocolInfo.Text = "(Not set)";
            }
            else
            {
                string t = _protocolSettings.ProtocolType.ToString();

                if (_protocolSettings.ProtocolType == ProtocolType.I2C)
                {
                    t += $" / {_protocolSettings.SpeedKbps} kHz";
                    t += $" / 0x{_protocolSettings.I2cSlaveAddress:X2}";
                }
                else if (_protocolSettings.ProtocolType == ProtocolType.SPI)
                {
                    t += $" / {_protocolSettings.SpiClockKHz} kHz";
                    t += $" / Mode {_protocolSettings.SpiMode}";
                    t += _protocolSettings.SpiLsbFirst ? " / LSB" : " / MSB";
                }

                t += $" / {_protocolSettings.DeviceKind}";
                lblProtocolInfo.Text = t;
            }

            if (_ftdiSettings == null)
            {
                lblFtdiInfo.Text = "(Not set)";
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(_ftdiSettings.Description))
                    lblFtdiInfo.Text = _ftdiSettings.Description;
                else
                    lblFtdiInfo.Text = $"DevIdx {_ftdiSettings.DeviceIndex}";
            }

            bool isConnected = (_i2cBus != null && _i2cBus.IsConnected) || (_spiBus != null && _spiBus.IsConnected);
            lblStatus.Text = isConnected ? "Connected" : "Disconnected";
            lblStatus.ForeColor = isConnected ? Color.LimeGreen : Color.DarkRed;
        }

        private async Task<(bool success, T result)> RunWithTimeout<T>(Func<T> action, int timeoutMs)
        {
            var task = Task.Run(action);
            var completed = await Task.WhenAny(task, Task.Delay(timeoutMs));
            if (completed == task)
                return (true, task.Result);
            return (false, default!);
        }

        private async Task<bool> RunWithTimeout(Action action, int timeoutMs)
        {
            var task = Task.Run(action);
            var completed = await Task.WhenAny(task, Task.Delay(timeoutMs));
            return completed == task;
        }

        private bool TryParseHexUInt(string text, out uint value)
        {
            text = text.Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(2);

            return uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }

        private void AddLog(string type, string addrText, string dataText, string result)
        {
            int rowIndex = dgvLog.Rows.Add();
            var row = dgvLog.Rows[rowIndex];

            row.Cells["colTime"].Value = DateTime.Now.ToString("HH:mm:ss");
            row.Cells["colType"].Value = type;
            row.Cells["colAddr"].Value = addrText;
            row.Cells["colData"].Value = dataText;
            row.Cells["colResult"].Value = result;

            dgvLog.FirstDisplayedScrollingRowIndex = rowIndex;
        }

        private void AddTestLogRow(string level, string message)
        {
            int rowIndex = dgvTestLog.Rows.Add();
            var row = dgvTestLog.Rows[rowIndex];

            row.Cells["colTime"].Value = DateTime.Now.ToString("HH:mm:ss");
            row.Cells["colLevel"].Value = level;
            row.Cells["colMessage"].Value = message;

            dgvTestLog.FirstDisplayedScrollingRowIndex = rowIndex;
            dgvTestLog.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
        }

        private static DeviceKind ResolveDeviceKind(FtdiDeviceSettings s)
        {
            var desc = s.Description ?? "";
            if (desc.IndexOf("UM232H", StringComparison.OrdinalIgnoreCase) >= 0)
                return DeviceKind.UM232H;
            if (desc.IndexOf("FT232H", StringComparison.OrdinalIgnoreCase) >= 0)
                return DeviceKind.UM232H;
            return DeviceKind.FT4222;
        }

        private void LoadProjects()
        {
            _projects.Clear();
            comboProject.Items.Clear();

            var projectType = typeof(IChipProject);
            var asm = typeof(OasisProject).Assembly;

            foreach (var t in asm.GetTypes())
            {
                if (t.IsAbstract || t.IsInterface)
                    continue;

                if (!projectType.IsAssignableFrom(t))
                    continue;

                if (Activator.CreateInstance(t) is IChipProject proj)
                {
                    _projects.Add(proj);
                    comboProject.Items.Add(proj.Name);
                }
            }

            if (comboProject.Items.Count > 0)
                comboProject.SelectedIndex = 0;
        }

        private void DisconnectBus()
        {
            try
            {
                _testCts?.Cancel();
            }
            catch { }
            _testCts = null;
            _isRunningTest = false;

            try
            {
                _i2cBus?.Disconnect();
            }
            catch { }
            try
            {
                _spiBus?.Disconnect();
            }
            catch { }

            _i2cBus = null;
            _spiBus = null;
            _chip = null;

            _testSuite = null;

            comboTestCategory.Items.Clear();
            comboTests.Items.Clear();
            btnRunTest.Enabled = false;
            btnStopTest.Enabled = false;
            dgvTestLog.Rows.Clear();

            btnConnect.Text = "Connect";
            UpdateStatusText();
        }

        private void TryConnect()
        {
            if (_selectedProject == null)
            {
                MessageBox.Show("프로젝트를 선택하세요.");
                return;
            }

            bool isMockProject = _selectedProject is MockProject;

            DisconnectBus();

            if (isMockProject)
            {
                _protocolSettings ??= new ProtocolSettings { ProtocolType = ProtocolType.I2C, DeviceKind = DeviceKind.FT4222 };

                _i2cBus = new MockBus();
                if (!_i2cBus.Connect())
                {
                    _i2cBus = null;
                    MessageBox.Show("Mock 연결 실패");
                    UpdateStatusText();
                    return;
                }

                if (_selectedProject is not II2cChipProject i2cProj)
                {
                    MessageBox.Show("MockProject가 II2cChipProject가 아닙니다.");
                    DisconnectBus();
                    return;
                }

                _chip = i2cProj.CreateChip(_i2cBus, _protocolSettings);

                LoadTestSuiteIfAny();
                btnConnect.Text = "Disconnect";
                UpdateStatusText();
                return;
            }

            if (_ftdiSettings == null)
            {
                MessageBox.Show("FTDI 장비 셋업이 필요합니다.");
                return;
            }

            if (_protocolSettings == null)
            {
                MessageBox.Show("프로토콜 셋업이 필요합니다.");
                return;
            }

            _protocolSettings.DeviceKind = ResolveDeviceKind(_ftdiSettings);
            uint devIndex = (uint)_ftdiSettings.DeviceIndex;

            try
            {
                if (_protocolSettings.ProtocolType == ProtocolType.I2C)
                {
                    if (_selectedProject is not II2cChipProject i2cProj)
                    {
                        MessageBox.Show("선택한 프로젝트가 I2C 칩 프로젝트가 아닙니다.");
                        return;
                    }

                    _i2cBus = new I2cBus(devIndex, _protocolSettings);
                    if (!_i2cBus.Connect())
                    {
                        _i2cBus = null;
                        MessageBox.Show("I2C 연결 실패");
                        UpdateStatusText();
                        return;
                    }

                    _chip = i2cProj.CreateChip(_i2cBus, _protocolSettings);
                }
                else if (_protocolSettings.ProtocolType == ProtocolType.SPI)
                {
                    if (_selectedProject is not ISpiChipProject spiProj)
                    {
                        MessageBox.Show("선택한 프로젝트가 SPI 칩 프로젝트가 아닙니다.");
                        return;
                    }

                    _spiBus = new SpiBus(devIndex, _protocolSettings);
                    if (!_spiBus.Connect())
                    {
                        _spiBus = null;
                        MessageBox.Show("SPI 연결 실패");
                        UpdateStatusText();
                        return;
                    }

                    _chip = spiProj.CreateChip(_spiBus, _protocolSettings);
                }
                else
                {
                    MessageBox.Show("지원하지 않는 Protocol입니다.");
                    return;
                }

                LoadTestSuiteIfAny();

                btnConnect.Text = "Disconnect";
                UpdateStatusText();
            }
            catch (Exception ex)
            {
                DisconnectBus();
                MessageBox.Show("연결 중 오류: " + ex.Message);
            }
        }

        private void LoadTestSuiteIfAny()
        {
            _testSuite = null;
            comboTestCategory.Items.Clear();
            comboTests.Items.Clear();
            btnRunTest.Enabled = false;
            btnStopTest.Enabled = false;
            dgvTestLog.Rows.Clear();

            if (_chip == null || _selectedProject == null)
                return;

            if (_selectedProject is not IChipProjectWithTests projWithTests)
                return;

            _testSuite = projWithTests.CreateTestSuite(_chip);
            if (_testSuite == null || _testSuite.Tests == null || _testSuite.Tests.Count == 0)
                return;

            var categories = _testSuite.Tests
                .Select(t => t.Category)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            foreach (var c in categories)
                comboTestCategory.Items.Add(c);

            if (comboTestCategory.Items.Count > 0)
                comboTestCategory.SelectedIndex = 0;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            bool isConnected = (_i2cBus != null && _i2cBus.IsConnected) || (_spiBus != null && _spiBus.IsConnected);

            if (!isConnected)
                TryConnect();
            else
                DisconnectBus();
        }

        private void comboProject_SelectedIndexChanged(object sender, EventArgs e)
        {
            var name = comboProject.SelectedItem as string;
            _selectedProject = null;

            foreach (var p in _projects)
            {
                if (p.Name == name)
                {
                    _selectedProject = p;
                    break;
                }
            }

            _protocolSettings = null;
            DisconnectBus();
            UpdateStatusText();
        }

        private void btnFtdiSetup_Click(object sender, EventArgs e)
        {
            using (var dlg = new FtdiSetupForm(_ftdiSettings))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _ftdiSettings = dlg.Result;
                    UpdateStatusText();
                }
            }
        }

        private void btnProtocolSetup_Click(object sender, EventArgs e)
        {
            if (_selectedProject == null)
            {
                MessageBox.Show("먼저 프로젝트를 선택하세요.");
                return;
            }

            using (var dlg = new ProtocolSetupForm(_selectedProject, _protocolSettings))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _protocolSettings = dlg.Result;
                    UpdateStatusText();
                }
            }
        }

        private void BitButton_Click(object sender, EventArgs e)
        {
            if (_isUpdatingBits)
                return;

            if (sender is not Button btn)
                return;

            btn.Text = (btn.Text == "0") ? "1" : "0";

            _currentRegValue = GetValueFromBitButtons();
            UpdateBitCurrentValues();
        }

        private void UpdateBitButtonsFromValue(uint value)
        {
            _isUpdatingBits = true;

            for (int bit = 0; bit < 32; bit++)
            {
                int btnIndex = 31 - bit;
                uint mask = 1u << bit;
                bool isOne = (value & mask) != 0;

                var btn = _bitButtons[btnIndex];
                if (btn != null)
                    btn.Text = isOne ? "1" : "0";
            }

            _isUpdatingBits = false;
        }

        private uint GetValueFromBitButtons()
        {
            uint value = 0;

            for (int btnIndex = 0; btnIndex < 32; btnIndex++)
            {
                var btn = _bitButtons[btnIndex];
                if (btn == null)
                    continue;

                int bit = 31 - btnIndex;
                if (btn.Text == "1")
                    value |= (1u << bit);
            }

            return value;
        }

        private void txtRegValueHex_Leave(object sender, EventArgs e)
        {
            if (TryParseHexUInt(txtRegValueHex.Text, out uint v))
            {
                _currentRegValue = v;
                UpdateBitCurrentValues();
            }
            else
            {
                MessageBox.Show("레지스터 값 형식이 잘못되었습니다. 예: 0x00000000");
                txtRegValueHex.Text = $"0x{_currentRegValue:X8}";
            }
        }

        private void btnSelectMapFile_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Excel Files|*.xlsx;*.xlsm;*.xls";
                ofd.Title = "Select RegisterMap Excel";

                if (ofd.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    _regMapFilePath = ofd.FileName;
                    lblMapFileName.Text = Path.GetFileName(_regMapFilePath);
                    btnOpenMapPath.Enabled = true;
                    clbSheets.Items.Clear();

                    using (var wb = new XLWorkbook(_regMapFilePath))
                    {
                        foreach (var ws in wb.Worksheets)
                            clbSheets.Items.Add(ws.Name);
                    }

                    _groups.Clear();
                    _regValues.Clear();
                    tvRegs.Nodes.Clear();
                    dgvBits.Rows.Clear();
                }
                catch (Exception ex)
                {
                    _regMapFilePath = null;
                    lblMapFileName.Text = "(No file)";
                    btnOpenMapPath.Enabled = false;
                    MessageBox.Show("엑셀 파일 열기 실패: " + ex.Message);
                }
            }
        }

        private void btnLoadSelectedSheets_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_regMapFilePath))
            {
                MessageBox.Show("먼저 엑셀 파일을 선택하세요.");
                return;
            }

            if (clbSheets.CheckedItems.Count == 0)
            {
                MessageBox.Show("로드할 시트를 선택하세요.");
                return;
            }

            try
            {
                _groups.Clear();
                _regValues.Clear();

                using (var wb = new XLWorkbook(_regMapFilePath))
                {
                    foreach (var item in clbSheets.CheckedItems)
                    {
                        string sheetName = item.ToString()!;
                        var ws = wb.Worksheet(sheetName);
                        string[,] data = ExcelHelper.WorksheetToArray(ws);

                        RegisterGroup group = RegisterMapParser.MakeRegisterGroup(sheetName, data);
                        _groups.Add(group);
                    }
                }

                BuildRegisterTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show("RegisterMap 로딩 실패: " + ex.Message);
            }
        }

        private void btnOpenMapPath_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_regMapFilePath) || !File.Exists(_regMapFilePath))
            {
                MessageBox.Show("열려 있는 레지스터맵 파일이 없습니다.");
                return;
            }

            try
            {
                var arg = $"/select,\"{_regMapFilePath}\"";
                Process.Start("explorer.exe", arg);
            }
            catch (Exception ex)
            {
                MessageBox.Show("경로 오픈 실패: " + ex.Message);
            }
        }

        private void BuildRegisterTree()
        {
            tvRegs.Nodes.Clear();

            foreach (var g in _groups)
            {
                var groupNode = new TreeNode(g.Name)
                {
                    Tag = g
                };

                foreach (var reg in g.Registers)
                {
                    var regNode = new TreeNode($"{reg.Name} (0x{reg.Address:X8})")
                    {
                        Tag = reg
                    };

                    uint regVal = GetRegisterValue(reg);

                    foreach (var item in reg.Items)
                    {
                        var itemNode = new TreeNode(FormatItemNodeText(item, regVal))
                        {
                            Tag = item
                        };

                        regNode.Nodes.Add(itemNode);
                    }

                    groupNode.Nodes.Add(regNode);
                }

                tvRegs.Nodes.Add(groupNode);
            }

            tvRegs.BeginUpdate();

            foreach (TreeNode sheetNode in tvRegs.Nodes)
            {
                sheetNode.Expand();

                foreach (TreeNode child in sheetNode.Nodes)
                    child.Collapse();
            }

            tvRegs.EndUpdate();
        }

        private static string FormatItemNodeText(RegisterItem item, uint regValue)
        {
            string bitText = item.UpperBit == item.LowerBit
                ? item.UpperBit.ToString()
                : $"{item.UpperBit}:{item.LowerBit}";

            int width = item.UpperBit - item.LowerBit + 1;
            uint mask = width >= 32 ? 0xFFFFFFFFu : ((1u << width) - 1u);
            uint fieldVal = (regValue >> item.LowerBit) & mask;

            return $"[{bitText}] {item.Name} = {fieldVal} (0x{fieldVal:X})";
        }

        private uint GetRegisterValue(Register reg)
        {
            if (_regValues.TryGetValue(reg, out var v))
                return v;

            v = reg.ResetValue;
            _regValues[reg] = v;
            return v;
        }

        private void tvRegs_AfterSelect(object sender, TreeViewEventArgs e)
        {
            ResetSelectionState();

            if (e.Node?.Tag is RegisterGroup group)
            {
                HandleGroupSelection(group);
                return;
            }

            if (e.Node?.Tag is Register register)
            {
                HandleRegisterSelection(register, e.Node.Parent?.Tag as RegisterGroup);
                return;
            }

            if (e.Node?.Tag is RegisterItem item)
            {
                HandleItemSelection(item, e.Node?.Parent?.Tag as Register, e.Node?.Parent?.Parent?.Tag as RegisterGroup);
                return;
            }

            ShowNoSelectionState();
        }

        private void ResetSelectionState()
        {
            _selectedGroup = null;
            _selectedRegister = null;
            _selectedItem = null;
        }

        private void HandleGroupSelection(RegisterGroup group)
        {
            _selectedGroup = group;

            dgvBits.Rows.Clear();
            lblRegName.Text = "(Group Selected)";
            lblRegAddrSummary.Text = "Address: -";
            lblRegResetSummary.Text = "Reset Value: -";

            _currentRegValue = 0;
            UpdateBitCurrentValues();

            SetBitButtonsEnabledForItem(null);
            UpdateNumRegIndexForSelectedItem();
        }

        private void HandleRegisterSelection(Register register, RegisterGroup? parentGroup)
        {
            _selectedGroup = parentGroup;
            LoadRegisterToUi(register, null);
        }

        private void HandleItemSelection(RegisterItem item, Register? parentRegister, RegisterGroup? parentGroup)
        {
            _selectedGroup = parentGroup;
            if (parentRegister != null)
                LoadRegisterToUi(parentRegister, item);
            else
                ShowNoSelectionState();
        }

        private void ShowNoSelectionState()
        {
            dgvBits.Rows.Clear();
            lblRegName.Text = "(No Register)";
            lblRegAddrSummary.Text = "Address: -";
            lblRegResetSummary.Text = "Reset Value: -";

            _currentRegValue = 0;
            UpdateBitCurrentValues();

            SetBitButtonsEnabledForItem(null);
            UpdateNumRegIndexForSelectedItem();
        }

        private void LoadRegisterToUi(Register register, RegisterItem? selectedItem)
        {
            _selectedRegister = register;
            _selectedItem = selectedItem;

            lblRegName.Text = register.Name;
            lblRegAddrSummary.Text = $"Address: 0x{register.Address:X8}";
            lblRegResetSummary.Text = $"Reset Value: 0x{register.ResetValue:X8}";

            dgvBits.Rows.Clear();

            foreach (var item in register.Items)
            {
                int rowIndex = dgvBits.Rows.Add();
                var row = dgvBits.Rows[rowIndex];

                string bitText = item.UpperBit == item.LowerBit
                    ? item.UpperBit.ToString()
                    : $"{item.UpperBit}:{item.LowerBit}";

                row.Cells["colBit"].Value = bitText;
                row.Cells["colName"].Value = item.Name;
                row.Cells["colDefault"].Value = $"0x{item.DefaultValue:X}";
                row.Cells["colDesc"].Value = item.Description;

                row.Tag = item;

                if (ReferenceEquals(item, selectedItem))
                    row.Selected = true;
            }

            _currentRegValue = GetRegisterValue(register);
            UpdateBitCurrentValues();

            SetBitButtonsEnabledForItem(selectedItem);
            UpdateNumRegIndexForSelectedItem();
        }

        private void SetBitButtonsEnabledForItem(RegisterItem? item)
        {
            if (item == null)
            {
                for (int i = 0; i < _bitButtons.Length; i++)
                {
                    var btn = _bitButtons[i];
                    if (btn != null)
                        btn.Enabled = true;
                }
                return;
            }

            for (int bit = 0; bit < 32; bit++)
            {
                int btnIndex = 31 - bit;
                var btn = _bitButtons[btnIndex];
                if (btn == null)
                    continue;

                bool inRange = bit >= item.LowerBit && bit <= item.UpperBit;
                btn.Enabled = inRange;
            }
        }

        private void UpdateBitCurrentValues()
        {
            txtRegValueHex.Text = $"0x{_currentRegValue:X8}";

            if (_selectedRegister != null)
                _regValues[_selectedRegister] = _currentRegValue;

            UpdateBitButtonsFromValue(_currentRegValue);
            UpdateNumRegIndexForSelectedItem();

            if (_selectedRegister != null)
                UpdateTreeNodesForRegister(_selectedRegister, _currentRegValue);
        }

        private void UpdateTreeNodesForRegister(Register reg, uint regValue)
        {
            if (tvRegs == null)
                return;

            var stack = new Stack<TreeNode>();
            foreach (TreeNode root in tvRegs.Nodes)
                stack.Push(root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();

                if (node.Tag is RegisterItem item && node.Parent?.Tag is Register parentReg && ReferenceEquals(parentReg, reg))
                    node.Text = FormatItemNodeText(item, regValue);

                foreach (TreeNode child in node.Nodes)
                    stack.Push(child);
            }
        }

        private void RefreshRegisterTreeValues()
        {
            if (tvRegs.Nodes.Count == 0)
                return;

            tvRegs.BeginUpdate();
            try
            {
                foreach (TreeNode groupNode in tvRegs.Nodes)
                {
                    if (groupNode.Tag is not RegisterGroup g)
                        continue;

                    foreach (TreeNode regNode in groupNode.Nodes)
                    {
                        if (regNode.Tag is not Register reg)
                            continue;

                        uint regVal = GetRegisterValue(reg);

                        foreach (TreeNode itemNode in regNode.Nodes)
                        {
                            if (itemNode.Tag is not RegisterItem item)
                                continue;

                            itemNode.Text = FormatItemNodeText(item, regVal);
                        }
                    }
                }
            }
            finally
            {
                tvRegs.EndUpdate();
            }
        }

        private void UpdateNumRegIndexForSelectedItem()
        {
            _isUpdatingRegValue = true;
            try
            {
                if (_selectedItem == null)
                {
                    numRegIndex.Enabled = false;
                    numRegIndex.Minimum = 0;
                    numRegIndex.Maximum = 0;
                    numRegIndex.Value = 0;
                    return;
                }

                int width = _selectedItem.UpperBit - _selectedItem.LowerBit + 1;
                uint mask = width >= 32 ? 0xFFFFFFFFu : ((1u << width) - 1u);
                uint fieldVal = (_currentRegValue >> _selectedItem.LowerBit) & mask;

                numRegIndex.Minimum = 0;
                numRegIndex.Maximum = mask;
                numRegIndex.Enabled = true;

                numRegIndex.Value = fieldVal <= mask ? fieldVal : mask;
            }
            finally
            {
                _isUpdatingRegValue = false;
            }
        }

        private void numRegIndex_ValueChanged(object sender, EventArgs e)
        {
            if (_isUpdatingRegValue)
                return;

            if (_selectedItem == null)
                return;

            uint fieldVal = (uint)numRegIndex.Value;

            int width = _selectedItem.UpperBit - _selectedItem.LowerBit + 1;
            uint mask = width >= 32 ? 0xFFFFFFFFu : ((1u << width) - 1u);

            if (fieldVal > mask)
                fieldVal = mask;

            uint regVal = _currentRegValue;
            uint fieldMask = mask << _selectedItem.LowerBit;

            regVal &= ~fieldMask;
            regVal |= (fieldVal << _selectedItem.LowerBit);

            _currentRegValue = regVal;
            UpdateBitCurrentValues();
        }

        private void SaveRegisterScriptLegacy(string path)
        {
            using (var sw = new StreamWriter(path))
            {
                foreach (var group in _groups)
                {
                    sw.WriteLine(group.Name);

                    foreach (var reg in group.Registers)
                    {
                        uint value = GetRegisterValue(reg);
                        sw.WriteLine($"\t{reg.Address:X8}\t{value:X8}\t{reg.Name}");

                        foreach (var item in reg.Items)
                        {
                            int width = item.UpperBit - item.LowerBit + 1;
                            uint mask = width >= 32 ? 0xFFFFFFFFu : ((1u << width) - 1u);
                            uint fieldVal = (value >> item.LowerBit) & mask;

                            string bitText = $"[{item.UpperBit}:{item.LowerBit}]";
                            sw.WriteLine($"\t\t{bitText}{item.Name}\t{fieldVal}");
                        }
                    }
                }
            }
        }

        private void LoadRegisterScriptLegacy(string path)
        {
            var addrToReg = new Dictionary<uint, Register>();
            foreach (var g in _groups)
            {
                foreach (var reg in g.Registers)
                    addrToReg[reg.Address] = reg;
            }

            foreach (var raw in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                string line = raw.Trim();
                if (line.StartsWith("["))
                    continue;

                var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 2 &&
                    TryParseHexUInt(parts[0], out uint addr) &&
                    TryParseHexUInt(parts[1], out uint value))
                {
                    if (addrToReg.TryGetValue(addr, out var reg))
                        _regValues[reg] = value;
                }
            }

            if (_selectedRegister != null)
            {
                _currentRegValue = GetRegisterValue(_selectedRegister);
                UpdateBitCurrentValues();
            }

            RefreshRegisterTreeValues();
        }

        private void btnSaveScript_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "Register Script|*.txt|All Files|*.*";

                if (!string.IsNullOrEmpty(_scriptFilePath))
                {
                    sfd.InitialDirectory = Path.GetDirectoryName(_scriptFilePath);
                    sfd.FileName = Path.GetFileName(_scriptFilePath);
                }

                if (sfd.ShowDialog(this) != DialogResult.OK)
                    return;

                SaveRegisterScriptLegacy(sfd.FileName);
                SetScriptFilePath(sfd.FileName);
            }
        }

        private void btnLoadScript_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Register Script|*.txt|All Files|*.*";

                if (!string.IsNullOrEmpty(_scriptFilePath))
                {
                    ofd.InitialDirectory = Path.GetDirectoryName(_scriptFilePath);
                    ofd.FileName = Path.GetFileName(_scriptFilePath);
                }

                if (ofd.ShowDialog(this) != DialogResult.OK)
                    return;

                LoadRegisterScriptLegacy(ofd.FileName);
                SetScriptFilePath(ofd.FileName);

                if (_selectedRegister != null)
                {
                    _currentRegValue = GetRegisterValue(_selectedRegister);
                    UpdateBitCurrentValues();
                }
            }
        }

        private void SetScriptFilePath(string path)
        {
            _scriptFilePath = path;

            if (string.IsNullOrEmpty(path))
            {
                lblScriptFileName.Text = "(No script)";
                btnOpenScriptPath.Enabled = false;
            }
            else
            {
                lblScriptFileName.Text = Path.GetFileName(path);
                btnOpenScriptPath.Enabled = true;
            }
        }

        private void btnOpenScriptPath_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_scriptFilePath) || !File.Exists(_scriptFilePath))
            {
                MessageBox.Show("열려 있는 스크립트 파일이 없습니다.");
                return;
            }

            var arg = $"/select,\"{_scriptFilePath}\"";
            Process.Start("explorer.exe", arg);
        }

        private async void btnRead_Click(object sender, EventArgs e)
        {
            if (_chip == null)
            {
                MessageBox.Show("먼저 Connect 하세요.");
                return;
            }

            if (_selectedRegister == null)
            {
                MessageBox.Show("먼저 레지스터를 선택하세요.");
                return;
            }

            uint addr = _selectedRegister.Address;

            try
            {
                var result = await RunWithTimeout(() => _chip.ReadRegister(addr), I2cTimeoutMs);

                if (!result.success)
                {
                    AddLog("READ", $"0x{addr:X8}", "", "TIMEOUT");
                    return;
                }

                uint data = result.result;
                _currentRegValue = data;

                AddLog("READ", $"0x{addr:X8}", $"0x{data:X8}", "OK");
                UpdateBitCurrentValues();
            }
            catch (Exception ex)
            {
                AddLog("READ", $"0x{addr:X8}", "", "ERR");
                MessageBox.Show(ex.Message, "Read Error");
            }
        }

        private async void btnWrite_Click(object sender, EventArgs e)
        {
            if (_chip == null)
            {
                MessageBox.Show("먼저 Connect 하세요.");
                return;
            }

            if (_selectedRegister == null)
            {
                MessageBox.Show("먼저 레지스터를 선택하세요.");
                return;
            }

            uint addr = _selectedRegister.Address;
            uint newValue;

            if (!TryParseHexUInt(txtRegValueHex.Text, out newValue))
                newValue = _currentRegValue;

            try
            {
                bool success = await RunWithTimeout(() =>
                {
                    _chip.WriteRegister(addr, newValue);
                }, I2cTimeoutMs);

                if (!success)
                {
                    AddLog("WRITE", $"0x{addr:X8}", $"0x{newValue:X8}", "TIMEOUT");
                    return;
                }

                _currentRegValue = newValue;
                AddLog("WRITE", $"0x{addr:X8}", $"0x{newValue:X8}", "OK");
                UpdateBitCurrentValues();
            }
            catch (Exception ex)
            {
                AddLog("WRITE", $"0x{addr:X8}", $"0x{newValue:X8}", "ERR");
                MessageBox.Show(ex.Message, "Write Error");
            }
        }

        private async void btnWriteAll_Click(object sender, EventArgs e)
        {
            if (_chip == null)
            {
                MessageBox.Show("먼저 Connect 하세요.");
                return;
            }

            if (_groups.Count == 0)
            {
                MessageBox.Show("먼저 Register Tree를 로드하세요.");
                return;
            }

            foreach (var group in _groups)
            {
                foreach (var reg in group.Registers)
                {
                    uint addr = reg.Address;
                    uint data = GetRegisterValue(reg);

                    try
                    {
                        bool success = await RunWithTimeout(() => _chip.WriteRegister(addr, data), I2cTimeoutMs);
                        if (!success)
                        {
                            AddLog("WRITE_ALL", $"0x{addr:X8}", $"0x{data:X8}", "TIMEOUT");
                            continue;
                        }

                        _regValues[reg] = data;
                        AddLog("WRITE_ALL", $"0x{addr:X8}", $"0x{data:X8}", "OK");
                    }
                    catch (Exception ex)
                    {
                        AddLog("WRITE_ALL", $"0x{addr:X8}", $"0x{data:X8}", "ERR");
                        Debug.WriteLine(ex);
                    }
                }
            }

            if (_selectedRegister != null)
            {
                _currentRegValue = GetRegisterValue(_selectedRegister);
                UpdateBitCurrentValues();
            }

            RefreshRegisterTreeValues();
        }

        private async void btnReadAll_Click(object sender, EventArgs e)
        {
            if (_chip == null)
            {
                MessageBox.Show("먼저 Connect 하세요.");
                return;
            }

            if (_groups.Count == 0)
            {
                MessageBox.Show("먼저 Register Tree를 로드하세요.");
                return;
            }

            foreach (var group in _groups)
            {
                foreach (var reg in group.Registers)
                {
                    uint addr = reg.Address;

                    try
                    {
                        var result = await RunWithTimeout(() => _chip.ReadRegister(addr), I2cTimeoutMs);
                        if (!result.success)
                        {
                            AddLog("READ_ALL", $"0x{addr:X8}", "", "TIMEOUT");
                            continue;
                        }

                        uint data = result.result;
                        _regValues[reg] = data;
                        AddLog("READ_ALL", $"0x{addr:X8}", $"0x{data:X8}", "OK");
                    }
                    catch (Exception ex)
                    {
                        AddLog("READ_ALL", $"0x{addr:X8}", "", "ERR");
                        Debug.WriteLine(ex);
                    }
                }
            }

            if (_selectedRegister != null)
            {
                _currentRegValue = GetRegisterValue(_selectedRegister);
                UpdateBitCurrentValues();
            }

            RefreshRegisterTreeValues();
        }

        private void comboTestCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboTests.Items.Clear();

            if (_testSuite == null)
                return;

            if (comboTestCategory.SelectedItem is not string category)
                return;

            var testsInCategory = _testSuite.Tests
                .Where(t => string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var t in testsInCategory)
                comboTests.Items.Add(t);

            comboTests.DisplayMember = "Name";

            if (comboTests.Items.Count > 0)
                comboTests.SelectedIndex = 0;

            btnRunTest.Enabled = comboTests.Items.Count > 0;
            btnStopTest.Enabled = false;
        }

        private async void btnRunTest_Click(object sender, EventArgs e)
        {
            if (_testSuite == null)
            {
                MessageBox.Show("현재 프로젝트는 Run Test를 지원하지 않습니다.");
                return;
            }

            if (_isRunningTest)
            {
                MessageBox.Show("이미 테스트가 실행 중입니다.");
                return;
            }

            if (comboTests.SelectedItem is not ChipTestInfo info)
            {
                MessageBox.Show("실행할 테스트를 선택하세요.");
                return;
            }

            if (info.Id == "FW.FLASH_WRITE")
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Filter = "FW File (*.bin;*.hex)|*.bin;*.hex|All files (*.*)|*.*";
                    ofd.Title = "Select Firmware File";

                    if (ofd.ShowDialog(this) != DialogResult.OK)
                    {
                        MessageBox.Show("펌웨어 파일 선택이 취소되었습니다.");
                        return;
                    }

                    if (_testSuite is OasisTestSuite oasisSuite)
                        oasisSuite.SetFirmwareFilePath(ofd.FileName);
                }
            }

            if (info.Id == "FW.FLASH_VERIFY" || info.Id == "FW.FLASH_READ")
            {
                string? s = PromptText("FLASH FUNCTION", "Flash 크기(Byte)를 입력하세요:", "524288");
                if (string.IsNullOrWhiteSpace(s))
                {
                    MessageBox.Show("Flash 크기 입력이 취소되었습니다.");
                    return;
                }

                if (!uint.TryParse(s, out uint flashSize) || flashSize == 0)
                {
                    MessageBox.Show("Flash 크기(Byte)는 0보다 큰 숫자여야 합니다.");
                    return;
                }

                if (_testSuite is OasisTestSuite oasisSuite)
                    oasisSuite.SetFlashSize(flashSize);
            }

            _testCts = new CancellationTokenSource();
            _isRunningTest = true;

            btnRunTest.Enabled = false;
            btnStopTest.Enabled = true;
            dgvTestLog.Rows.Clear();

            try
            {
                await Task.Run(async () =>
                {
                    await _testSuite.Run_TEST(
                        info.Id,
                        (level, message) =>
                        {
                            if (IsDisposed)
                                return Task.CompletedTask;

                            if (InvokeRequired)
                                BeginInvoke(new Action(() => AddTestLogRow(level, message)));
                            else
                                AddTestLogRow(level, message);

                            return Task.CompletedTask;
                        },
                        _testCts.Token);
                }, _testCts.Token);
            }
            catch (OperationCanceledException)
            {
                AddTestLogRow("INFO", "테스트가 취소되었습니다.");
            }
            catch (Exception ex)
            {
                AddTestLogRow("ERROR", ex.Message);
            }
            finally
            {
                _isRunningTest = false;
                _testCts?.Dispose();
                _testCts = null;

                btnRunTest.Enabled = _testSuite != null && comboTests.Items.Count > 0;
                btnStopTest.Enabled = false;
            }
        }

        private void btnStopTest_Click(object sender, EventArgs e)
        {
            if (!_isRunningTest)
                return;

            _testCts?.Cancel();
            btnStopTest.Enabled = false;
        }
    }
}
