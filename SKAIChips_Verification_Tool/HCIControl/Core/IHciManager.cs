using BlueTooth.HCI;

namespace SKAIChips_Verification_Tool.HCIControl.Core
{
    public interface IHciManager
    {
        bool IsOpen
        {
            get;
        }
        IList<CommandGroup> HCICommands
        {
            get;
        }

        string[] SearchSerialPort();
        string[] SearchUsbDevices();

        void OpenUART(string portName, int baudRate);
        void OpenUSB(int deviceIndex);
        void Close();

        void InitHciLogView();
        bool SetConnectionParameter(int rowIndex);
        Command GetCommand(ushort opcode);
    }
}
