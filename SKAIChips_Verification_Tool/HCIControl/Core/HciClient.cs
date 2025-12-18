namespace SKAIChips_Verification_Tool.HCIControl.Core
{
    public sealed class HciClient : IDisposable
    {
        private readonly IHciTransport _transport;
        private readonly CancellationTokenSource _cts = new();
        private Task _rxTask;
        private readonly byte[] _rxBuffer = new byte[4096];
        private int _rxCount;

        public bool IsRunning => _rxTask != null && !_rxTask.IsCompleted;

        public event Action<HciPacket> PacketReceived;

        public HciClient(IHciTransport transport)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public void Start()
        {
            if (IsRunning)
                return;

            if (!_transport.IsOpen)
                _transport.Open();

            _rxTask = Task.Run(ReceiveLoopAsync);
        }

        public void Stop()
        {
            _cts.Cancel();
            try
            {
                _rxTask?.Wait(500);
            }
            catch
            {
                
            }
        }

        public void Dispose()
        {
            Stop();
            _transport.Dispose();
            _cts.Dispose();
        }

        public void SendPacket(HciCommandPacket packet)
        {
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            var bytes = HciPacketCodec.EncodeCommand(packet);
            _transport.Send(bytes, 0, bytes.Length);
        }

        private async Task ReceiveLoopAsync()
        {
            var token = _cts.Token;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    int read = _transport.Receive(_rxBuffer, _rxCount, _rxBuffer.Length - _rxCount, 100);
                    if (read <= 0)
                    {
                        await Task.Delay(10, token);
                        continue;
                    }

                    _rxCount += read;

                    int offset = 0;
                    while (offset < _rxCount)
                    {
                        if (!HciPacketCodec.TryDecodeEvent(new ReadOnlySpan<byte>(_rxBuffer, offset, _rxCount - offset),
                                                      out var packet,
                                                      out int consumed))
                        {
                            break;
                        }

                        offset += consumed;
                        PacketReceived?.Invoke(packet);
                    }

                    if (offset > 0 && offset < _rxCount)
                    {
                        Buffer.BlockCopy(_rxBuffer, offset, _rxBuffer, 0, _rxCount - offset);
                        _rxCount -= offset;
                    }
                    else if (offset >= _rxCount)
                    {
                        _rxCount = 0;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    await Task.Delay(50, token);
                }
            }
        }
    }
}
