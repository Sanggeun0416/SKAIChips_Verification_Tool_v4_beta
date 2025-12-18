using BlueTooth.HCI;

namespace SKAIChips_Verification_Tool.HCIControl.Core.Legacy
{
    public sealed class LegacyHciManagerAdapter : IHciManager
    {
        private readonly HCIManager _inner;

        public LegacyHciManagerAdapter(HCIManager inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public bool IsOpen => _inner.IsOpen;

        public IList<CommandGroup> HCICommands => _inner.HCICommands;

        public string[] SearchSerialPort() => _inner.SearchSerialPort();

        public string[] SearchUsbDevices() => _inner.SearchUsbDevices();

        public void OpenUART(string portName, int baudRate) => _inner.OpenUART(portName, baudRate);

        public void OpenUSB(int deviceIndex) => _inner.OpenUSB(deviceIndex);

        public void Close() => _inner.Close();

        public void InitHciLogView() => _inner.InitHciLogView();

        public bool SetConnectionParameter(int rowIndex) => _inner.SetConnectionParameter(rowIndex);

        public Command GetCommand(ushort opcode)
        {
            short code = unchecked((short)opcode);

            foreach (var group in _inner.HCICommands)
            {
                var cmd = group.Commands.GetCommand(code);
                if (cmd != null)
                    return cmd;
            }

            return null;
        }
    }
}
