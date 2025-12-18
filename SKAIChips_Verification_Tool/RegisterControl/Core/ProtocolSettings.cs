namespace SKAIChips_Verification_Tool.RegisterControl.Core
{
    public enum ProtocolType
    {
        I2C,
        SPI
    }

    public enum DeviceKind
    {
        FT4222,
        UM232H
    }

    public sealed class ProtocolSettings
    {
        public ProtocolType ProtocolType
        {
            get; set;
        }
        public DeviceKind DeviceKind
        {
            get; set;
        }

        
        public int SpeedKbps { get; set; } = 400;
        public byte I2cSlaveAddress { get; set; } = 0x00;

        
        public int SpiClockKHz { get; set; } = 1000;
        public int SpiMode { get; set; } = 0;     
        public bool SpiLsbFirst { get; set; } = false;
    }
}
