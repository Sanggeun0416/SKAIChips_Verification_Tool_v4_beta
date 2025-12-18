using System.Drawing;
using System.Windows.Forms;

namespace SKAIChips_Verification_Tool
{
    partial class FtdiSetupForm
    {
        
        private System.ComponentModel.IContainer components = null;
        private ListView lvDevices;
        private ColumnHeader colIdx;
        private ColumnHeader colDesc;
        private ColumnHeader colSerial;
        private ColumnHeader colLocation;
        private Button btnRefresh;
        private Button btnOk;
        private Button btnCancel;

        

        
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();

            base.Dispose(disposing);
        }

        

        
        private void InitializeComponent()
        {
            lvDevices = new ListView();
            colIdx = new ColumnHeader();
            colDesc = new ColumnHeader();
            colSerial = new ColumnHeader();
            colLocation = new ColumnHeader();
            btnRefresh = new Button();
            btnOk = new Button();
            btnCancel = new Button();
            SuspendLayout();
            
            
            
            lvDevices.Columns.AddRange(new ColumnHeader[] { colIdx, colDesc, colSerial, colLocation });
            lvDevices.FullRowSelect = true;
            lvDevices.GridLines = true;
            lvDevices.HideSelection = false;
            lvDevices.Location = new Point(12, 12);
            lvDevices.MultiSelect = false;
            lvDevices.Name = "lvDevices";
            lvDevices.Size = new Size(460, 200);
            lvDevices.TabIndex = 0;
            lvDevices.UseCompatibleStateImageBehavior = false;
            lvDevices.View = View.Details;
            
            
            
            colIdx.Text = "Index";
            colIdx.Width = 50;
            
            
            
            colDesc.Text = "Description";
            colDesc.Width = 160;
            
            
            
            colSerial.Text = "Serial";
            colSerial.Width = 120;
            
            
            
            colLocation.Text = "Location";
            colLocation.Width = 100;
            
            
            
            btnRefresh.Location = new Point(12, 218);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(80, 25);
            btnRefresh.TabIndex = 1;
            btnRefresh.Text = "Refresh";
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.Click += btnRefresh_Click;
            
            
            
            btnOk.Location = new Point(316, 218);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(75, 25);
            btnOk.TabIndex = 2;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOk_Click;
            
            
            
            btnCancel.Location = new Point(397, 218);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 25);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            
            
            
            AcceptButton = btnOk;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(484, 251);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            Controls.Add(btnRefresh);
            Controls.Add(lvDevices);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FtdiSetupForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "FTDI Device Setup";
            ResumeLayout(false);
        }

        
    }
}
