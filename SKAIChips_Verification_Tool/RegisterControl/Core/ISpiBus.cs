namespace SKAIChips_Verification_Tool.RegisterControl.Core
{
    public interface ISpiBus : IDisposable
    {
        bool Connect();
        void Disconnect();
        bool IsConnected
        {
            get;
        }

        void Transfer(ReadOnlySpan<byte> tx, Span<byte> rx, bool endTransaction = true);
        void Write(ReadOnlySpan<byte> tx, bool endTransaction = true);
        void Read(Span<byte> rx, bool endTransaction = true);
    }
}
