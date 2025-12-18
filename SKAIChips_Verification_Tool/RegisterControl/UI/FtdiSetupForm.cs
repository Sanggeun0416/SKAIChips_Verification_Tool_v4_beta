using SKAIChips_Verification_Tool.RegisterControl.Core;
using System.Runtime.InteropServices;
using System.Text;

namespace SKAIChips_Verification_Tool
{
    public partial class FtdiSetupForm : Form
    {

        public FtdiDeviceSettings Result
        {
            get; private set;
        }

        public FtdiSetupForm(FtdiDeviceSettings current = null)
        {
            InitializeComponent();
            LoadDeviceList();

            if (current != null)
                ApplyCurrent(current);
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadDeviceList();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            var idx = GetSelectedDeviceIndex();
            if (idx < 0)
            {
                MessageBox.Show("장치를 선택하세요.");
                return;
            }

            var item = lvDevices.Items[idx];

            Result = new FtdiDeviceSettings
            {
                DeviceIndex = int.Parse(item.SubItems[0].Text),
                Description = item.SubItems[1].Text,
                SerialNumber = item.SubItems[2].Text,
                Location = item.SubItems[3].Text
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private int GetSelectedDeviceIndex()
        {
            if (lvDevices.SelectedItems.Count == 0)
                return -1;

            return lvDevices.SelectedItems[0].Index;
        }

        private void ApplyCurrent(FtdiDeviceSettings current)
        {
            for (var i = 0; i < lvDevices.Items.Count; i++)
            {
                var item = lvDevices.Items[i];
                if (!int.TryParse(item.SubItems[0].Text, out var devIdx))
                    continue;

                if (devIdx != current.DeviceIndex)
                    continue;

                item.Selected = true;
                item.Focused = true;
                lvDevices.EnsureVisible(i);
                break;
            }
        }

        private void LoadDeviceList()
        {
            lvDevices.Items.Clear();

            uint numDevs = 0;
            var status = FT_CreateDeviceInfoList(ref numDevs);
            if (status != FT_STATUS.FT_OK)
            {
                MessageBox.Show($"FT_CreateDeviceInfoList 실패: {status}");
                return;
            }

            for (uint i = 0; i < numDevs; i++)
            {
                uint flags = 0;
                uint type = 0;
                uint id = 0;
                uint locId = 0;
                var serial = new byte[16];
                var desc = new byte[64];
                var handle = IntPtr.Zero;

                status = FT_GetDeviceInfoDetail(
                    i,
                    ref flags,
                    ref type,
                    ref id,
                    ref locId,
                    serial,
                    desc,
                    ref handle);

                if (status != FT_STATUS.FT_OK)
                    continue;

                var serialStr = BytesToString(serial);
                var descStr = BytesToString(desc);
                var locStr = $"0x{locId:X8}";

                var lvi = new ListViewItem(i.ToString());
                lvi.SubItems.Add(descStr);
                lvi.SubItems.Add(serialStr);
                lvi.SubItems.Add(locStr);

                lvDevices.Items.Add(lvi);
            }

            if (lvDevices.Items.Count > 0)
                lvDevices.Items[0].Selected = true;
        }

        private static string BytesToString(byte[] buf)
        {
            if (buf == null || buf.Length == 0)
                return string.Empty;

            var s = Encoding.ASCII.GetString(buf);
            var idx = s.IndexOf('\0');
            if (idx >= 0)
                s = s.Substring(0, idx);

            return s.Trim();
        }

        private enum FT_STATUS : uint
        {
            FT_OK = 0,
            FT_INVALID_HANDLE,
            FT_DEVICE_NOT_FOUND,
            FT_DEVICE_NOT_OPENED,
            FT_IO_ERROR,
            FT_INSUFFICIENT_RESOURCES,
            FT_INVALID_PARAMETER,
            FT_INVALID_BAUD_RATE,

            FT_DEVICE_NOT_OPENED_FOR_ERASE = 0x10,
            FT_DEVICE_NOT_OPENED_FOR_WRITE,
            FT_FAILED_TO_WRITE_DEVICE,
            FT_EEPROM_READ_FAILED,
            FT_EEPROM_WRITE_FAILED,
            FT_EEPROM_ERASE_FAILED,
            FT_EEPROM_NOT_PRESENT,
            FT_EEPROM_NOT_PROGRAMMED,
            FT_INVALID_ARGS,
            FT_NOT_SUPPORTED,
            FT_OTHER_ERROR
        }

        [DllImport("ftd2xx.dll")]
        private static extern FT_STATUS FT_CreateDeviceInfoList(ref uint numDevs);

        [DllImport("ftd2xx.dll")]
        private static extern FT_STATUS FT_GetDeviceInfoDetail(
            uint index,
            ref uint flags,
            ref uint type,
            ref uint id,
            ref uint locId,
            [Out] byte[] serialNumber,
            [Out] byte[] description,
            ref IntPtr ftHandle);

    }
}
