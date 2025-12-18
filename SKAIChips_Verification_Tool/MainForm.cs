using SKAIChips_Verification_Tool.HCIControl.UI;

namespace SKAIChips_Verification_Tool
{
    public partial class MainForm : Form
    {
        private RegisterControlForm _regForm;
        private InstrumentForm _instrumentForm;
        private HCIControlForm _hciForm;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Text = "SKAIChips_Verification_V4_beta [Confidential]";
        }

        private void menuRegisterControl_Click(object sender, EventArgs e)
        {
            if (_regForm == null || _regForm.IsDisposed)
            {
                _regForm = new RegisterControlForm
                {
                    MdiParent = this,
                    WindowState = FormWindowState.Maximized
                };
                _regForm.Show();
            }
            else
            {
                _regForm.Activate();
            }
        }

        private void menuHCIControl_Click(object sender, EventArgs e)
        {
            if (_hciForm == null || _hciForm.IsDisposed)
            {
                _hciForm = new HCIControlForm
                {
                    MdiParent = this,
                    WindowState = FormWindowState.Maximized
                };
                _hciForm.Show();
            }
            else
            {
                _hciForm.Activate();
            }
        }

        private void menuExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void menuSetupInstrument_Click(object sender, EventArgs e)
        {
            if (_instrumentForm == null || _instrumentForm.IsDisposed)
            {
                _instrumentForm = new InstrumentForm
                {
                    StartPosition = FormStartPosition.CenterParent
                };
                _instrumentForm.Show(this);
            }
            else
            {
                if (!_instrumentForm.Visible)
                    _instrumentForm.Show(this);

                _instrumentForm.Activate();
            }
        }
    }
}
