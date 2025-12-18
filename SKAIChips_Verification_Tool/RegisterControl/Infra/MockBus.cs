using SKAIChips_Verification_Tool.RegisterControl.Core;

namespace SKAIChips_Verification_Tool.RegisterControl.Infra
{
    public sealed class MockBus : II2cBus
    {
        public bool IsConnected
        {
            get; private set;
        }

        public bool Connect()
        {
            IsConnected = true;
            return true;
        }

        public void Disconnect()
        {
            IsConnected = false;
        }

        public void Write(byte slaveAddr, ReadOnlySpan<byte> data, bool stop = true)
        {
        }

        public void Read(byte slaveAddr, Span<byte> buffer, int timeoutMs)
        {
            buffer.Clear();
        }

        public void WriteRead(byte slaveAddr, ReadOnlySpan<byte> w, Span<byte> r, int timeoutMs)
        {
            r.Clear();
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
