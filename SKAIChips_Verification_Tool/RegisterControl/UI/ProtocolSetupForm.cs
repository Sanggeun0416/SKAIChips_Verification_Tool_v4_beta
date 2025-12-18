using SKAIChips_Verification_Tool.RegisterControl.Core;
using System.Globalization;

namespace SKAIChips_Verification_Tool
{
    public partial class ProtocolSetupForm : Form
    {
        private readonly IChipProject _project;

        public ProtocolSettings? Result { get; private set; }

        public ProtocolSetupForm(IChipProject project, ProtocolSettings current)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));

            InitializeComponent();
            InitProtocolCombo();
            InitSpiModeCombo();

            if (current != null)
                ApplyCurrent(current);
            else
                ApplyDefault();
        }

        private void InitProtocolCombo()
        {
            comboProtocol.Items.Clear();

            foreach (var p in _project.SupportedProtocols)
                comboProtocol.Items.Add(p);

            if (comboProtocol.Items.Count > 0)
                comboProtocol.SelectedIndex = 0;
        }

        private void InitSpiModeCombo()
        {
            comboSpiMode.Items.Clear();
            comboSpiMode.Items.Add(0);
            comboSpiMode.Items.Add(1);
            comboSpiMode.Items.Add(2);
            comboSpiMode.Items.Add(3);
            comboSpiMode.SelectedIndex = 0;
        }

        private void ApplyDefault()
        {
            SetNumericWithinRange(numSpeed, 400);
            txtSlaveAddr.Text = "0x52";
            comboSpiMode.SelectedIndex = 0;
            UpdateControlsEnabled();
        }

        private void ApplyCurrent(ProtocolSettings current)
        {
            
            for (var i = 0; i < comboProtocol.Items.Count; i++)
            {
                if (comboProtocol.Items[i] is ProtocolType pt && pt == current.ProtocolType)
                {
                    comboProtocol.SelectedIndex = i;
                    break;
                }
            }

            
            if (current.ProtocolType == ProtocolType.I2C)
            {
                if (current.SpeedKbps > 0 && current.SpeedKbps >= (int)numSpeed.Minimum && current.SpeedKbps <= (int)numSpeed.Maximum)
                    numSpeed.Value = current.SpeedKbps;

                txtSlaveAddr.Text = $"0x{current.I2cSlaveAddress:X2}";
            }
            
            else if (current.ProtocolType == ProtocolType.SPI)
            {
                if (current.SpiClockKHz > 0 && current.SpiClockKHz >= (int)numSpeed.Minimum && current.SpiClockKHz <= (int)numSpeed.Maximum)
                    numSpeed.Value = current.SpiClockKHz;

                int mode = current.SpiMode;
                int index = comboSpiMode.Items.IndexOf(mode);
                comboSpiMode.SelectedIndex = index >= 0 ? index : 0;
            }

            UpdateControlsEnabled();
        }

        private void comboProtocol_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateControlsEnabled();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (comboProtocol.SelectedItem is not ProtocolType protocol)
            {
                MessageBox.Show("Protocol을 선택하세요.");
                return;
            }

            var settings = new ProtocolSettings
            {
                ProtocolType = protocol
            };

            if (protocol == ProtocolType.I2C)
            {
                settings.SpeedKbps = (int)numSpeed.Value;

                if (!TryParseHexByte(txtSlaveAddr.Text, out var slave))
                {
                    MessageBox.Show("I2C Slave Address 형식이 잘못되었습니다. 예: 0x52");
                    return;
                }

                settings.I2cSlaveAddress = slave;
            }
            else if (protocol == ProtocolType.SPI)
            {
                settings.SpiClockKHz = (int)numSpeed.Value;

                if (comboSpiMode.SelectedItem is int mode)
                {
                    settings.SpiMode = mode;
                }
                else
                {
                    MessageBox.Show("SPI Mode를 선택하세요.");
                    return;
                }

                
                settings.SpiLsbFirst = false;
            }

            Result = settings;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void UpdateControlsEnabled()
        {
            var selected = comboProtocol.SelectedItem;
            var isI2c = selected is ProtocolType pt1 && pt1 == ProtocolType.I2C;
            var isSpi = selected is ProtocolType pt2 && pt2 == ProtocolType.SPI;

            lblSpeed.Visible = isI2c || isSpi;
            numSpeed.Visible = isI2c || isSpi;

            lblSlaveAddr.Visible = isI2c;
            txtSlaveAddr.Visible = isI2c;

            lblSpiMode.Visible = isSpi;
            comboSpiMode.Visible = isSpi;
        }

        private static bool TryParseHexByte(string text, out byte value)
        {
            value = 0;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();

            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                text = text[2..];

            return byte.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }

        private static void SetNumericWithinRange(NumericUpDown n, decimal value)
        {
            if (value < n.Minimum) value = n.Minimum;
            if (value > n.Maximum) value = n.Maximum;
            n.Value = value;
        }

    }
}
