using SKAIChips_Verification_Tool.RegisterControl.Core;

namespace SKAIChips_Verification_Tool.RegisterControl.Infra
{
    public sealed class SpiBus : ISpiBus
    {
        private readonly uint _deviceIndex;
        private readonly ProtocolSettings _settings;

        private FT4222H? _ft4222;
        private UM232H? _um232h;

        public bool IsConnected
        {
            get; private set;
        }

        public SpiBus(uint deviceIndex, ProtocolSettings settings)
        {
            _deviceIndex = deviceIndex;
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public bool Connect()
        {
            Disconnect();

            switch (_settings.DeviceKind)
            {
                case DeviceKind.FT4222:
                    _ft4222 = new FT4222H();
                    if (!_ft4222.Open(_deviceIndex))
                        return false;

                    if (!_ft4222.SpiInitMaster(_settings.SpiClockKHz, _settings.SpiMode))
                    {
                        _ft4222.Close();
                        _ft4222 = null;
                        return false;
                    }

                    IsConnected = true;
                    return true;

                case DeviceKind.UM232H:
                    _um232h = new UM232H(_deviceIndex);
                    if (!_um232h.Open())
                    {
                        _um232h = null;
                        return false;
                    }

                    if (!_um232h.SpiInit(_settings.SpiMode, _settings.SpiClockKHz, _settings.SpiLsbFirst))
                    {
                        _um232h.Close();
                        _um232h = null;
                        return false;
                    }

                    IsConnected = true;
                    return true;

                default:
                    throw new NotSupportedException($"DeviceKind not supported: {_settings.DeviceKind}");
            }
        }

        public void Disconnect()
        {
            IsConnected = false;

            try
            {
                _ft4222?.Close();
            }
            catch { }
            _ft4222 = null;

            try
            {
                _um232h?.Close();
            }
            catch { }
            _um232h = null;
        }

        public void Transfer(ReadOnlySpan<byte> tx, Span<byte> rx, bool endTransaction = true)
        {
            EnsureConnected();

            if (_settings.DeviceKind == DeviceKind.FT4222)
            {
                var w = tx.ToArray();
                var rbuf = new byte[Math.Max(rx.Length, w.Length)];
                _ft4222!.SpiReadWriteBytes(rbuf, w, endTransaction);
                rbuf.AsSpan(0, rx.Length).CopyTo(rx);
                return;
            }

            
            if (rx.Length > 0)
            {
                var w = tx.ToArray();
                if (w.Length < rx.Length)
                {
                    var w2 = new byte[rx.Length];
                    w.CopyTo(w2, 0);
                    w = w2;
                }

                var r = _um232h!.SpiReadWrite(w, rx.Length);
                r.AsSpan(0, Math.Min(r.Length, rx.Length)).CopyTo(rx);
            }
        }

        public void Write(ReadOnlySpan<byte> tx, bool endTransaction = true)
        {
            EnsureConnected();

            if (_settings.DeviceKind == DeviceKind.FT4222)
            {
                _ft4222!.SpiWriteBytes(tx.ToArray(), endTransaction);
                return;
            }

            _um232h!.SpiWrite(tx.ToArray(), endTransaction);
        }

        public void Read(Span<byte> rx, bool endTransaction = true)
        {
            EnsureConnected();

            if (_settings.DeviceKind == DeviceKind.FT4222)
            {
                var tmp = new byte[rx.Length];
                _ft4222!.SpiReadBytes(tmp, endTransaction);
                tmp.AsSpan().CopyTo(rx);
                return;
            }

            var r = _um232h!.SpiRead(rx.Length);
            r.AsSpan(0, Math.Min(r.Length, rx.Length)).CopyTo(rx);
        }

        public void Dispose() => Disconnect();

        private void EnsureConnected()
        {
            if (!IsConnected)
                throw new InvalidOperationException("SPI bus is not connected.");
        }
    }
}
