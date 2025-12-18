namespace SKAIChips_Verification_Tool.RegisterControl.Core
{
    public class FtdiDeviceSettings
    {

        public int DeviceIndex
        {
            get; set;
        }

        public string Description
        {
            get; set;
        }

        public string SerialNumber
        {
            get; set;
        }

        public string Location
        {
            get; set;
        }

        public override string ToString()
        {
            return $"{DeviceIndex} - {Description} ({SerialNumber})";
        }

    }
}
