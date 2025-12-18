namespace SKAIChips_Verification_Tool.HCIControl.Core
{
    public interface IHciTransport : IDisposable
    {
        bool IsOpen
        {
            get;
        }
        string Name
        {
            get;
        }

        void Open();
        void Close();

        void Send(byte[] buffer, int offset, int count);

        int Receive(byte[] buffer, int offset, int count, int timeoutMs);
    }
}
