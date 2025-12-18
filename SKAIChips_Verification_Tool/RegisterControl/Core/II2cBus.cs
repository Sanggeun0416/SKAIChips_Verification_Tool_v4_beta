namespace SKAIChips_Verification_Tool.RegisterControl.Core
{
    public interface II2cBus : IDisposable
    {
        bool Connect();
        void Disconnect();
        bool IsConnected
        {
            get;
        }

        void Write(byte slaveAddr, ReadOnlySpan<byte> data, bool stop = true);
        void Read(byte slaveAddr, Span<byte> buffer, int timeoutMs);
        void WriteRead(byte slaveAddr, ReadOnlySpan<byte> w, Span<byte> r, int timeoutMs);
    }
}
