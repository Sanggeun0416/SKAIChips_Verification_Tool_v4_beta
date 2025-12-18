using System.Drawing;
using System.Windows.Forms;

namespace SKAIChips_Verification_Tool
{
    partial class InstrumentForm
    {
        
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel tableLayoutPanel_Instrument;
        private GroupBox groupBox_InsSetting;
        private GroupBox groupBox_InsCommand;
        private ComboBox comboBox_InsTypes;
        private Button button_AddInstrument;
        private Button button_RemoveInstrument;
        private Button button_InsUp;
        private Button button_InsDown;
        private DataGridView dataGridView_InsList;
        private DataGridViewTextBoxColumn Column_InsType;
        private DataGridViewCheckBoxColumn Column_InsEnabled;
        private DataGridViewTextBoxColumn Column_Address;
        private DataGridViewButtonColumn Column_InsTest;
        private DataGridViewTextBoxColumn Column_InsName;
        private TextBox textBox_InsType;
        private TextBox textBox_InsCommand;
        private Button button_SendInsCommand;
        private Button button_InsScreenCapture;
        private Button button_ClearInsLog;
        private RichTextBox richTextBox_InsCommandLog;

        

        
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();

            base.Dispose(disposing);
        }

        

        
        private void InitializeComponent()
        {
            tableLayoutPanel_Instrument = new TableLayoutPanel();
            groupBox_InsSetting = new GroupBox();
            button_InsDown = new Button();
            button_InsUp = new Button();
            dataGridView_InsList = new DataGridView();
            Column_InsType = new DataGridViewTextBoxColumn();
            Column_InsEnabled = new DataGridViewCheckBoxColumn();
            Column_Address = new DataGridViewTextBoxColumn();
            Column_InsTest = new DataGridViewButtonColumn();
            Column_InsName = new DataGridViewTextBoxColumn();
            button_RemoveInstrument = new Button();
            button_AddInstrument = new Button();
            comboBox_InsTypes = new ComboBox();
            groupBox_InsCommand = new GroupBox();
            button_ClearInsLog = new Button();
            button_InsScreenCapture = new Button();
            richTextBox_InsCommandLog = new RichTextBox();
            button_SendInsCommand = new Button();
            textBox_InsCommand = new TextBox();
            textBox_InsType = new TextBox();
            tableLayoutPanel_Instrument.SuspendLayout();
            groupBox_InsSetting.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_InsList).BeginInit();
            groupBox_InsCommand.SuspendLayout();
            SuspendLayout();
            
            
            
            tableLayoutPanel_Instrument.ColumnCount = 1;
            tableLayoutPanel_Instrument.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel_Instrument.Controls.Add(groupBox_InsSetting, 0, 0);
            tableLayoutPanel_Instrument.Controls.Add(groupBox_InsCommand, 0, 1);
            tableLayoutPanel_Instrument.Dock = DockStyle.Fill;
            tableLayoutPanel_Instrument.Location = new Point(0, 0);
            tableLayoutPanel_Instrument.Name = "tableLayoutPanel_Instrument";
            tableLayoutPanel_Instrument.RowCount = 2;
            tableLayoutPanel_Instrument.RowStyles.Add(new RowStyle(SizeType.Absolute, 240F));
            tableLayoutPanel_Instrument.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel_Instrument.Size = new Size(828, 499);
            tableLayoutPanel_Instrument.TabIndex = 0;
            
            
            
            groupBox_InsSetting.Controls.Add(button_InsDown);
            groupBox_InsSetting.Controls.Add(button_InsUp);
            groupBox_InsSetting.Controls.Add(dataGridView_InsList);
            groupBox_InsSetting.Controls.Add(button_RemoveInstrument);
            groupBox_InsSetting.Controls.Add(button_AddInstrument);
            groupBox_InsSetting.Controls.Add(comboBox_InsTypes);
            groupBox_InsSetting.Dock = DockStyle.Fill;
            groupBox_InsSetting.Location = new Point(3, 3);
            groupBox_InsSetting.Name = "groupBox_InsSetting";
            groupBox_InsSetting.Size = new Size(822, 234);
            groupBox_InsSetting.TabIndex = 0;
            groupBox_InsSetting.TabStop = false;
            groupBox_InsSetting.Text = "Instrument Setting";
            
            
            
            button_InsDown.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button_InsDown.Location = new Point(746, 20);
            button_InsDown.Name = "button_InsDown";
            button_InsDown.Size = new Size(70, 25);
            button_InsDown.TabIndex = 5;
            button_InsDown.Text = "Down";
            button_InsDown.UseVisualStyleBackColor = true;
            
            
            
            button_InsUp.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button_InsUp.Location = new Point(670, 20);
            button_InsUp.Name = "button_InsUp";
            button_InsUp.Size = new Size(70, 25);
            button_InsUp.TabIndex = 4;
            button_InsUp.Text = "Up";
            button_InsUp.UseVisualStyleBackColor = true;
            
            
            
            dataGridView_InsList.AllowUserToAddRows = false;
            dataGridView_InsList.AllowUserToDeleteRows = false;
            dataGridView_InsList.AllowUserToResizeColumns = false;
            dataGridView_InsList.AllowUserToResizeRows = false;
            dataGridView_InsList.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView_InsList.Columns.AddRange(new DataGridViewColumn[]
            {
                Column_InsType,
                Column_InsEnabled,
                Column_Address,
                Column_InsTest,
                Column_InsName
            });
            dataGridView_InsList.Location = new Point(6, 49);
            dataGridView_InsList.Name = "dataGridView_InsList";
            dataGridView_InsList.RowHeadersVisible = false;
            dataGridView_InsList.RowTemplate.Height = 20;
            dataGridView_InsList.Size = new Size(810, 180);
            dataGridView_InsList.TabIndex = 3;
            
            
            
            Column_InsType.DataPropertyName = "Type";
            Column_InsType.HeaderText = "Type";
            Column_InsType.MinimumWidth = 10;
            Column_InsType.Name = "Column_InsType";
            Column_InsType.SortMode = DataGridViewColumnSortMode.NotSortable;
            Column_InsType.Width = 125;
            
            
            
            Column_InsEnabled.DataPropertyName = "Enabled";
            Column_InsEnabled.HeaderText = "En";
            Column_InsEnabled.MinimumWidth = 10;
            Column_InsEnabled.Name = "Column_InsEnabled";
            Column_InsEnabled.Width = 30;
            
            
            
            Column_Address.DataPropertyName = "VisaAddress";
            Column_Address.HeaderText = "VISA Address";
            Column_Address.MinimumWidth = 125;
            Column_Address.Name = "Column_Address";
            Column_Address.SortMode = DataGridViewColumnSortMode.NotSortable;
            Column_Address.Width = 250;
            
            
            
            Column_InsTest.HeaderText = "Test";
            Column_InsTest.MinimumWidth = 10;
            Column_InsTest.Name = "Column_InsTest";
            Column_InsTest.Text = "Gets";
            Column_InsTest.UseColumnTextForButtonValue = true;
            Column_InsTest.Width = 50;
            
            
            
            Column_InsName.DataPropertyName = "Name";
            Column_InsName.HeaderText = "Name";
            Column_InsName.MinimumWidth = 10;
            Column_InsName.Name = "Column_InsName";
            Column_InsName.ReadOnly = true;
            Column_InsName.SortMode = DataGridViewColumnSortMode.NotSortable;
            Column_InsName.Width = 325;
            
            
            
            button_RemoveInstrument.Location = new Point(386, 20);
            button_RemoveInstrument.Name = "button_RemoveInstrument";
            button_RemoveInstrument.Size = new Size(100, 25);
            button_RemoveInstrument.TabIndex = 2;
            button_RemoveInstrument.Text = "Remove";
            button_RemoveInstrument.UseVisualStyleBackColor = true;
            
            
            
            button_AddInstrument.Location = new Point(280, 20);
            button_AddInstrument.Name = "button_AddInstrument";
            button_AddInstrument.Size = new Size(100, 25);
            button_AddInstrument.TabIndex = 1;
            button_AddInstrument.Text = "Add";
            button_AddInstrument.UseVisualStyleBackColor = true;
            
            
            
            comboBox_InsTypes.FormattingEnabled = true;
            comboBox_InsTypes.Location = new Point(6, 20);
            comboBox_InsTypes.Name = "comboBox_InsTypes";
            comboBox_InsTypes.Size = new Size(250, 23);
            comboBox_InsTypes.TabIndex = 0;
            
            
            
            groupBox_InsCommand.Controls.Add(button_ClearInsLog);
            groupBox_InsCommand.Controls.Add(button_InsScreenCapture);
            groupBox_InsCommand.Controls.Add(richTextBox_InsCommandLog);
            groupBox_InsCommand.Controls.Add(button_SendInsCommand);
            groupBox_InsCommand.Controls.Add(textBox_InsCommand);
            groupBox_InsCommand.Controls.Add(textBox_InsType);
            groupBox_InsCommand.Dock = DockStyle.Fill;
            groupBox_InsCommand.Location = new Point(3, 243);
            groupBox_InsCommand.Name = "groupBox_InsCommand";
            groupBox_InsCommand.Size = new Size(822, 253);
            groupBox_InsCommand.TabIndex = 1;
            groupBox_InsCommand.TabStop = false;
            groupBox_InsCommand.Text = "Instrument Command";
            
            
            
            button_ClearInsLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            button_ClearInsLog.Location = new Point(696, 217);
            button_ClearInsLog.Name = "button_ClearInsLog";
            button_ClearInsLog.Size = new Size(120, 30);
            button_ClearInsLog.TabIndex = 5;
            button_ClearInsLog.Text = "Clear Log";
            button_ClearInsLog.UseVisualStyleBackColor = true;
            
            
            
            button_InsScreenCapture.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button_InsScreenCapture.Location = new Point(696, 51);
            button_InsScreenCapture.Name = "button_InsScreenCapture";
            button_InsScreenCapture.Size = new Size(120, 30);
            button_InsScreenCapture.TabIndex = 4;
            button_InsScreenCapture.Text = "Capture";
            button_InsScreenCapture.UseVisualStyleBackColor = true;
            
            
            
            richTextBox_InsCommandLog.Location = new Point(6, 51);
            richTextBox_InsCommandLog.Name = "richTextBox_InsCommandLog";
            richTextBox_InsCommandLog.Size = new Size(684, 196);
            richTextBox_InsCommandLog.TabIndex = 3;
            richTextBox_InsCommandLog.Text = "";
            
            
            
            button_SendInsCommand.Location = new Point(165, 20);
            button_SendInsCommand.Name = "button_SendInsCommand";
            button_SendInsCommand.Size = new Size(140, 25);
            button_SendInsCommand.TabIndex = 2;
            button_SendInsCommand.Text = "Send Command";
            button_SendInsCommand.UseVisualStyleBackColor = true;
            
            
            
            textBox_InsCommand.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBox_InsCommand.Location = new Point(311, 20);
            textBox_InsCommand.Name = "textBox_InsCommand";
            textBox_InsCommand.Size = new Size(505, 23);
            textBox_InsCommand.TabIndex = 1;
            
            
            
            textBox_InsType.Location = new Point(9, 20);
            textBox_InsType.Name = "textBox_InsType";
            textBox_InsType.ReadOnly = true;
            textBox_InsType.Size = new Size(150, 23);
            textBox_InsType.TabIndex = 0;
            
            
            
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(828, 499);
            Controls.Add(tableLayoutPanel_Instrument);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            Name = "InstrumentForm";
            Text = "Instrument Setup";
            tableLayoutPanel_Instrument.ResumeLayout(false);
            groupBox_InsSetting.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView_InsList).EndInit();
            groupBox_InsCommand.ResumeLayout(false);
            groupBox_InsCommand.PerformLayout();
            ResumeLayout(false);
        }

        
    }
}
