using System.Drawing;
using System.Windows.Forms;

namespace SKAIChips_Verification_Tool
{
    partial class RegisterControlForm
    {
        private System.ComponentModel.IContainer components = null;

        private Button btnConnect;
        private DataGridView dgvLog;
        private DataGridViewTextBoxColumn colTime;
        private DataGridViewTextBoxColumn colType;
        private DataGridViewTextBoxColumn colAddr;
        private DataGridViewTextBoxColumn colData;
        private DataGridViewTextBoxColumn colResult;
        private Button btnRead;
        private Label lblStatus;
        private Button btnSelectMapFile;
        private CheckedListBox clbSheets;
        private Button btnLoadSelectedSheets;
        private TreeView tvRegs;
        private Button btnSaveScript;
        private Button btnLoadScript;
        private Label lblScriptFileName;
        private Button btnOpenScriptPath;
        private DataGridView dgvBits;
        private ComboBox comboProject;
        private Label labelProject;
        private Button btnProtocolSetup;
        private Button btnFtdiSetup;
        private GroupBox groupSetup;
        private GroupBox groupRegMap;
        private GroupBox groupRegCont;
        private GroupBox groupLog;
        private GroupBox groupRegDesc;
        private GroupBox groupRegTree;
        private Label lblMapFileName;
        private Button btnOpenMapPath;
        private NumericUpDown numRegIndex;
        private TextBox txtRegValueHex;
        private Label lblRegName;
        private FlowLayoutPanel flowBitsTop;
        private FlowLayoutPanel flowBitsBottom;
        private Label lblRegAddrSummary;
        private Label lblRegResetSummary;
        private Button btnWrite;
        private Button btnWriteAll;
        private Button btnReadAll;
        private Label labelProtocol;
        private Label labelFtdi;
        private Label labelConnection;
        private Label lblProtocolInfo;
        private Label lblFtdiInfo;
        private GroupBox grpRunTest;
        private ComboBox comboTestCategory;
        private ComboBox comboTests;
        private Button btnRunTest;
        private Button btnStopTest;
        private DataGridView dgvTestLog;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            btnConnect = new Button();
            dgvLog = new DataGridView();
            colTime = new DataGridViewTextBoxColumn();
            colType = new DataGridViewTextBoxColumn();
            colAddr = new DataGridViewTextBoxColumn();
            colData = new DataGridViewTextBoxColumn();
            colResult = new DataGridViewTextBoxColumn();
            btnRead = new Button();
            lblStatus = new Label();
            btnSelectMapFile = new Button();
            clbSheets = new CheckedListBox();
            btnLoadSelectedSheets = new Button();
            tvRegs = new TreeView();
            btnSaveScript = new Button();
            btnLoadScript = new Button();
            lblScriptFileName = new Label();
            btnOpenScriptPath = new Button();
            dgvBits = new DataGridView();
            comboProject = new ComboBox();
            labelProject = new Label();
            btnProtocolSetup = new Button();
            btnFtdiSetup = new Button();
            groupSetup = new GroupBox();
            labelProtocol = new Label();
            lblProtocolInfo = new Label();
            labelFtdi = new Label();
            lblFtdiInfo = new Label();
            labelConnection = new Label();
            groupRegMap = new GroupBox();
            lblMapFileName = new Label();
            btnOpenMapPath = new Button();
            groupRegCont = new GroupBox();
            numRegIndex = new NumericUpDown();
            txtRegValueHex = new TextBox();
            lblRegName = new Label();
            flowBitsTop = new FlowLayoutPanel();
            flowBitsBottom = new FlowLayoutPanel();
            lblRegAddrSummary = new Label();
            lblRegResetSummary = new Label();
            btnWrite = new Button();
            btnWriteAll = new Button();
            btnReadAll = new Button();
            groupLog = new GroupBox();
            groupRegDesc = new GroupBox();
            groupRegTree = new GroupBox();
            grpRunTest = new GroupBox();
            btnStopTest = new Button();
            btnRunTest = new Button();
            comboTests = new ComboBox();
            comboTestCategory = new ComboBox();
            dgvTestLog = new DataGridView();
            ((System.ComponentModel.ISupportInitialize)dgvLog).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvBits).BeginInit();
            groupSetup.SuspendLayout();
            groupRegMap.SuspendLayout();
            groupRegCont.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numRegIndex).BeginInit();
            groupLog.SuspendLayout();
            groupRegDesc.SuspendLayout();
            groupRegTree.SuspendLayout();
            grpRunTest.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvTestLog).BeginInit();
            SuspendLayout();
            
            
            
            btnConnect.Location = new Point(231, 96);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(98, 23);
            btnConnect.TabIndex = 10;
            btnConnect.Text = "Connect";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            
            
            
            dgvLog.AllowUserToAddRows = false;
            dgvLog.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvLog.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvLog.Columns.AddRange(new DataGridViewColumn[] { colTime, colType, colAddr, colData, colResult });
            dgvLog.Dock = DockStyle.Fill;
            dgvLog.Location = new Point(3, 19);
            dgvLog.Name = "dgvLog";
            dgvLog.ReadOnly = true;
            dgvLog.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvLog.Size = new Size(579, 254);
            dgvLog.TabIndex = 8;
            
            
            
            colTime.HeaderText = "Time";
            colTime.Name = "colTime";
            colTime.ReadOnly = true;
            
            
            
            colType.HeaderText = "Type";
            colType.Name = "colType";
            colType.ReadOnly = true;
            
            
            
            colAddr.HeaderText = "Addr";
            colAddr.Name = "colAddr";
            colAddr.ReadOnly = true;
            
            
            
            colData.HeaderText = "Data";
            colData.Name = "colData";
            colData.ReadOnly = true;
            
            
            
            colResult.HeaderText = "Result";
            colResult.Name = "colResult";
            colResult.ReadOnly = true;
            
            
            
            btnRead.Location = new Point(171, 165);
            btnRead.Name = "btnRead";
            btnRead.Size = new Size(77, 23);
            btnRead.TabIndex = 9;
            btnRead.Text = "Read";
            btnRead.UseVisualStyleBackColor = true;
            btnRead.Click += btnRead_Click;
            
            
            
            lblStatus.Location = new Point(71, 100);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(154, 15);
            lblStatus.TabIndex = 9;
            lblStatus.Text = "Disconnected";
            
            
            
            btnSelectMapFile.Location = new Point(8, 119);
            btnSelectMapFile.Name = "btnSelectMapFile";
            btnSelectMapFile.Size = new Size(100, 23);
            btnSelectMapFile.TabIndex = 18;
            btnSelectMapFile.Text = "Open RegMap";
            btnSelectMapFile.UseVisualStyleBackColor = true;
            btnSelectMapFile.Click += btnSelectMapFile_Click;
            
            
            
            clbSheets.FormattingEnabled = true;
            clbSheets.Location = new Point(8, 22);
            clbSheets.Name = "clbSheets";
            clbSheets.Size = new Size(320, 94);
            clbSheets.TabIndex = 20;
            
            
            
            btnLoadSelectedSheets.Location = new Point(114, 119);
            btnLoadSelectedSheets.Name = "btnLoadSelectedSheets";
            btnLoadSelectedSheets.Size = new Size(100, 23);
            btnLoadSelectedSheets.TabIndex = 21;
            btnLoadSelectedSheets.Text = "Add RegTree";
            btnLoadSelectedSheets.UseVisualStyleBackColor = true;
            btnLoadSelectedSheets.Click += btnLoadSelectedSheets_Click;
            
            
            
            tvRegs.Location = new Point(3, 19);
            tvRegs.Name = "tvRegs";
            tvRegs.Size = new Size(441, 442);
            tvRegs.TabIndex = 22;
            tvRegs.AfterSelect += tvRegs_AfterSelect;
            
            
            
            btnSaveScript.Location = new Point(3, 467);
            btnSaveScript.Name = "btnSaveScript";
            btnSaveScript.Size = new Size(90, 23);
            btnSaveScript.TabIndex = 23;
            btnSaveScript.Text = "Save Script";
            btnSaveScript.UseVisualStyleBackColor = true;
            btnSaveScript.Click += btnSaveScript_Click;
            
            
            
            btnLoadScript.Location = new Point(99, 467);
            btnLoadScript.Name = "btnLoadScript";
            btnLoadScript.Size = new Size(90, 23);
            btnLoadScript.TabIndex = 24;
            btnLoadScript.Text = "Load Script";
            btnLoadScript.UseVisualStyleBackColor = true;
            btnLoadScript.Click += btnLoadScript_Click;
            
            
            
            lblScriptFileName.Location = new Point(195, 471);
            lblScriptFileName.Name = "lblScriptFileName";
            lblScriptFileName.Size = new Size(180, 15);
            lblScriptFileName.TabIndex = 25;
            lblScriptFileName.Text = "(No script file)";
            
            
            
            btnOpenScriptPath.Location = new Point(381, 467);
            btnOpenScriptPath.Name = "btnOpenScriptPath";
            btnOpenScriptPath.Size = new Size(63, 23);
            btnOpenScriptPath.TabIndex = 26;
            btnOpenScriptPath.Text = "Path";
            btnOpenScriptPath.UseVisualStyleBackColor = true;
            btnOpenScriptPath.Click += btnOpenScriptPath_Click;
            
            
            
            dgvBits.AllowUserToResizeRows = false;
            dgvBits.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvBits.Dock = DockStyle.Fill;
            dgvBits.Location = new Point(3, 19);
            dgvBits.Name = "dgvBits";
            dgvBits.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            dgvBits.Size = new Size(1372, 311);
            dgvBits.TabIndex = 23;
            
            
            
            comboProject.DropDownStyle = ComboBoxStyle.DropDownList;
            comboProject.FormattingEnabled = true;
            comboProject.Location = new Point(70, 19);
            comboProject.Name = "comboProject";
            comboProject.Size = new Size(259, 23);
            comboProject.TabIndex = 1;
            comboProject.SelectedIndexChanged += comboProject_SelectedIndexChanged;
            
            
            
            labelProject.AutoSize = true;
            labelProject.Location = new Point(6, 22);
            labelProject.Name = "labelProject";
            labelProject.Size = new Size(44, 15);
            labelProject.TabIndex = 0;
            labelProject.Text = "Project";
            
            
            
            btnProtocolSetup.Location = new Point(231, 46);
            btnProtocolSetup.Name = "btnProtocolSetup";
            btnProtocolSetup.Size = new Size(98, 23);
            btnProtocolSetup.TabIndex = 4;
            btnProtocolSetup.Text = "Protocol Setup";
            btnProtocolSetup.UseVisualStyleBackColor = true;
            btnProtocolSetup.Click += btnProtocolSetup_Click;
            
            
            
            btnFtdiSetup.Location = new Point(231, 71);
            btnFtdiSetup.Name = "btnFtdiSetup";
            btnFtdiSetup.Size = new Size(98, 23);
            btnFtdiSetup.TabIndex = 7;
            btnFtdiSetup.Text = "Device Setup";
            btnFtdiSetup.UseVisualStyleBackColor = true;
            btnFtdiSetup.Click += btnFtdiSetup_Click;
            
            
            
            groupSetup.Controls.Add(labelProject);
            groupSetup.Controls.Add(comboProject);
            groupSetup.Controls.Add(labelProtocol);
            groupSetup.Controls.Add(lblProtocolInfo);
            groupSetup.Controls.Add(btnProtocolSetup);
            groupSetup.Controls.Add(labelFtdi);
            groupSetup.Controls.Add(lblFtdiInfo);
            groupSetup.Controls.Add(btnFtdiSetup);
            groupSetup.Controls.Add(labelConnection);
            groupSetup.Controls.Add(lblStatus);
            groupSetup.Controls.Add(btnConnect);
            groupSetup.Location = new Point(458, 4);
            groupSetup.Name = "groupSetup";
            groupSetup.Size = new Size(334, 125);
            groupSetup.TabIndex = 35;
            groupSetup.TabStop = false;
            groupSetup.Text = "Setup";
            
            
            
            labelProtocol.AutoSize = true;
            labelProtocol.Location = new Point(6, 50);
            labelProtocol.Name = "labelProtocol";
            labelProtocol.Size = new Size(52, 15);
            labelProtocol.TabIndex = 2;
            labelProtocol.Text = "Protocol";
            
            
            
            lblProtocolInfo.Location = new Point(70, 50);
            lblProtocolInfo.Name = "lblProtocolInfo";
            lblProtocolInfo.Size = new Size(155, 15);
            lblProtocolInfo.TabIndex = 3;
            lblProtocolInfo.Text = "(Not set)";
            
            
            
            labelFtdi.AutoSize = true;
            labelFtdi.Location = new Point(6, 75);
            labelFtdi.Name = "labelFtdi";
            labelFtdi.Size = new Size(43, 15);
            labelFtdi.TabIndex = 5;
            labelFtdi.Text = "Device";
            
            
            
            lblFtdiInfo.Location = new Point(70, 75);
            lblFtdiInfo.Name = "lblFtdiInfo";
            lblFtdiInfo.Size = new Size(155, 15);
            lblFtdiInfo.TabIndex = 6;
            lblFtdiInfo.Text = "(Not set)";
            
            
            
            labelConnection.AutoSize = true;
            labelConnection.Location = new Point(6, 100);
            labelConnection.Name = "labelConnection";
            labelConnection.Size = new Size(40, 15);
            labelConnection.TabIndex = 8;
            labelConnection.Text = "Status";
            
            
            
            groupRegMap.Controls.Add(clbSheets);
            groupRegMap.Controls.Add(btnSelectMapFile);
            groupRegMap.Controls.Add(btnLoadSelectedSheets);
            groupRegMap.Controls.Add(lblMapFileName);
            groupRegMap.Controls.Add(btnOpenMapPath);
            groupRegMap.Location = new Point(458, 135);
            groupRegMap.Name = "groupRegMap";
            groupRegMap.Size = new Size(334, 171);
            groupRegMap.TabIndex = 36;
            groupRegMap.TabStop = false;
            groupRegMap.Text = "Register Map";
            
            
            
            lblMapFileName.Location = new Point(8, 146);
            lblMapFileName.Name = "lblMapFileName";
            lblMapFileName.Size = new Size(270, 15);
            lblMapFileName.TabIndex = 22;
            lblMapFileName.Text = "(No file)";
            
            
            
            btnOpenMapPath.Location = new Point(278, 142);
            btnOpenMapPath.Name = "btnOpenMapPath";
            btnOpenMapPath.Size = new Size(50, 23);
            btnOpenMapPath.TabIndex = 23;
            btnOpenMapPath.Text = "Path";
            btnOpenMapPath.UseVisualStyleBackColor = true;
            btnOpenMapPath.Click += btnOpenMapPath_Click;
            
            
            
            groupRegCont.Controls.Add(numRegIndex);
            groupRegCont.Controls.Add(txtRegValueHex);
            groupRegCont.Controls.Add(lblRegName);
            groupRegCont.Controls.Add(flowBitsTop);
            groupRegCont.Controls.Add(flowBitsBottom);
            groupRegCont.Controls.Add(lblRegAddrSummary);
            groupRegCont.Controls.Add(lblRegResetSummary);
            groupRegCont.Controls.Add(btnWrite);
            groupRegCont.Controls.Add(btnWriteAll);
            groupRegCont.Controls.Add(btnReadAll);
            groupRegCont.Controls.Add(btnRead);
            groupRegCont.Location = new Point(458, 312);
            groupRegCont.Name = "groupRegCont";
            groupRegCont.Size = new Size(334, 194);
            groupRegCont.TabIndex = 37;
            groupRegCont.TabStop = false;
            groupRegCont.Text = "Register Control";
            
            
            
            numRegIndex.Location = new Point(6, 37);
            numRegIndex.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            numRegIndex.Name = "numRegIndex";
            numRegIndex.Size = new Size(60, 23);
            numRegIndex.TabIndex = 0;
            
            
            
            txtRegValueHex.Location = new Point(70, 37);
            txtRegValueHex.Name = "txtRegValueHex";
            txtRegValueHex.Size = new Size(166, 23);
            txtRegValueHex.TabIndex = 1;
            txtRegValueHex.Text = "0x00000000";
            
            
            
            lblRegName.Location = new Point(6, 19);
            lblRegName.Name = "lblRegName";
            lblRegName.Size = new Size(307, 15);
            lblRegName.TabIndex = 2;
            lblRegName.Text = "(No Register)";
            
            
            
            flowBitsTop.Location = new Point(6, 66);
            flowBitsTop.Name = "flowBitsTop";
            flowBitsTop.Size = new Size(323, 27);
            flowBitsTop.TabIndex = 3;
            flowBitsTop.WrapContents = false;
            
            
            
            flowBitsBottom.Location = new Point(6, 99);
            flowBitsBottom.Name = "flowBitsBottom";
            flowBitsBottom.Size = new Size(323, 27);
            flowBitsBottom.TabIndex = 4;
            flowBitsBottom.WrapContents = false;
            
            
            
            lblRegAddrSummary.AutoSize = true;
            lblRegAddrSummary.Location = new Point(6, 132);
            lblRegAddrSummary.Name = "lblRegAddrSummary";
            lblRegAddrSummary.Size = new Size(61, 15);
            lblRegAddrSummary.TabIndex = 3;
            lblRegAddrSummary.Text = "Address: -";
            
            
            
            lblRegResetSummary.AutoSize = true;
            lblRegResetSummary.Location = new Point(6, 147);
            lblRegResetSummary.Name = "lblRegResetSummary";
            lblRegResetSummary.Size = new Size(81, 15);
            lblRegResetSummary.TabIndex = 4;
            lblRegResetSummary.Text = "Reset Value: -";
            
            
            
            btnWrite.Location = new Point(5, 165);
            btnWrite.Name = "btnWrite";
            btnWrite.Size = new Size(77, 23);
            btnWrite.TabIndex = 5;
            btnWrite.Text = "Write";
            btnWrite.UseVisualStyleBackColor = true;
            btnWrite.Click += btnWrite_Click;
            
            
            
            btnWriteAll.Location = new Point(88, 165);
            btnWriteAll.Name = "btnWriteAll";
            btnWriteAll.Size = new Size(77, 23);
            btnWriteAll.TabIndex = 6;
            btnWriteAll.Text = "Write All";
            btnWriteAll.UseVisualStyleBackColor = true;
            
            
            
            btnReadAll.Location = new Point(254, 165);
            btnReadAll.Name = "btnReadAll";
            btnReadAll.Size = new Size(77, 23);
            btnReadAll.TabIndex = 8;
            btnReadAll.Text = "Read All";
            btnReadAll.UseVisualStyleBackColor = true;
            
            
            
            groupLog.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupLog.Controls.Add(dgvLog);
            groupLog.Location = new Point(798, 4);
            groupLog.Name = "groupLog";
            groupLog.Size = new Size(585, 276);
            groupLog.TabIndex = 38;
            groupLog.TabStop = false;
            groupLog.Text = "Register Control Log";
            
            
            
            groupRegDesc.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupRegDesc.Controls.Add(dgvBits);
            groupRegDesc.Location = new Point(5, 506);
            groupRegDesc.Name = "groupRegDesc";
            groupRegDesc.Size = new Size(1378, 333);
            groupRegDesc.TabIndex = 39;
            groupRegDesc.TabStop = false;
            groupRegDesc.Text = "Register Description";
            
            
            
            groupRegTree.Controls.Add(btnOpenScriptPath);
            groupRegTree.Controls.Add(lblScriptFileName);
            groupRegTree.Controls.Add(btnLoadScript);
            groupRegTree.Controls.Add(btnSaveScript);
            groupRegTree.Controls.Add(tvRegs);
            groupRegTree.Location = new Point(5, 4);
            groupRegTree.Name = "groupRegTree";
            groupRegTree.Size = new Size(447, 496);
            groupRegTree.TabIndex = 40;
            groupRegTree.TabStop = false;
            groupRegTree.Text = "Register Tree";
            
            
            
            grpRunTest.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpRunTest.Controls.Add(btnStopTest);
            grpRunTest.Controls.Add(btnRunTest);
            grpRunTest.Controls.Add(comboTests);
            grpRunTest.Controls.Add(comboTestCategory);
            grpRunTest.Controls.Add(dgvTestLog);
            grpRunTest.Location = new Point(798, 286);
            grpRunTest.Name = "grpRunTest";
            grpRunTest.Size = new Size(585, 220);
            grpRunTest.TabIndex = 41;
            grpRunTest.TabStop = false;
            grpRunTest.Text = "Run Test";
            
            
            
            btnStopTest.Location = new Point(484, 22);
            btnStopTest.Name = "btnStopTest";
            btnStopTest.Size = new Size(95, 23);
            btnStopTest.TabIndex = 3;
            btnStopTest.Text = "Stop";
            btnStopTest.UseVisualStyleBackColor = true;
            
            
            
            btnRunTest.Location = new Point(380, 22);
            btnRunTest.Name = "btnRunTest";
            btnRunTest.Size = new Size(98, 23);
            btnRunTest.TabIndex = 2;
            btnRunTest.Text = "Run Test";
            btnRunTest.UseVisualStyleBackColor = true;
            
            
            
            comboTests.DropDownStyle = ComboBoxStyle.DropDownList;
            comboTests.Location = new Point(154, 22);
            comboTests.Name = "comboTests";
            comboTests.Size = new Size(220, 23);
            comboTests.TabIndex = 1;
            
            
            
            comboTestCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            comboTestCategory.Location = new Point(8, 22);
            comboTestCategory.Name = "comboTestCategory";
            comboTestCategory.Size = new Size(140, 23);
            comboTestCategory.TabIndex = 0;
            
            
            
            dgvTestLog.AllowUserToAddRows = false;
            dgvTestLog.AllowUserToDeleteRows = false;
            dgvTestLog.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgvTestLog.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTestLog.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvTestLog.Location = new Point(3, 51);
            dgvTestLog.Name = "dgvTestLog";
            dgvTestLog.ReadOnly = true;
            dgvTestLog.RowHeadersVisible = false;
            dgvTestLog.Size = new Size(579, 163);
            dgvTestLog.TabIndex = 4;
            
            
            
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1384, 841);
            Controls.Add(grpRunTest);
            Controls.Add(groupRegTree);
            Controls.Add(groupRegDesc);
            Controls.Add(groupLog);
            Controls.Add(groupRegCont);
            Controls.Add(groupRegMap);
            Controls.Add(groupSetup);
            MinimumSize = new Size(1400, 880);
            Name = "RegisterControlForm";
            Text = "RegisterControl";
            ((System.ComponentModel.ISupportInitialize)dgvLog).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvBits).EndInit();
            groupSetup.ResumeLayout(false);
            groupSetup.PerformLayout();
            groupRegMap.ResumeLayout(false);
            groupRegCont.ResumeLayout(false);
            groupRegCont.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numRegIndex).EndInit();
            groupLog.ResumeLayout(false);
            groupRegDesc.ResumeLayout(false);
            groupRegTree.ResumeLayout(false);
            grpRunTest.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvTestLog).EndInit();
            ResumeLayout(false);
        }
    }
}
