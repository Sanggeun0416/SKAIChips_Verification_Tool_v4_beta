using SKAIChips_Verification_Tool.RegisterControl.Core;

namespace SKAIChips_Verification_Tool.RegisterControl.Infra
{
    public sealed class I2cBus : II2cBus
    {
        private readonly uint _deviceIndex;
        private readonly ProtocolSettings _settings;

        private FT4222H? _ft4222;
        private UM232H? _um232h;

        public bool IsConnected
        {
            get; private set;
        }

        public I2cBus(uint deviceIndex, ProtocolSettings settings)
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

                    if (!_ft4222.I2cInit((ushort)_settings.SpeedKbps))
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

                    if (!_um232h.I2cInit(_settings.SpeedKbps))
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

        public void Write(byte slaveAddr, ReadOnlySpan<byte> data, bool stop = true)
        {
            EnsureConnected();

            if (_settings.DeviceKind == DeviceKind.FT4222)
            {
                var buf = data.ToArray();
                _ft4222!.I2cWrite(slaveAddr, buf);
                return;
            }

            _um232h!.I2cWrite(slaveAddr, data.ToArray(), stop);
        }

        public void Read(byte slaveAddr, Span<byte> buffer, int timeoutMs)
        {
            EnsureConnected();

            if (_settings.DeviceKind == DeviceKind.FT4222)
            {
                var tmp = new byte[buffer.Length];
                _ft4222!.I2cRead(slaveAddr, tmp);
                tmp.AsSpan().CopyTo(buffer);
                return;
            }

            var r = _um232h!.I2cRead(slaveAddr, buffer.Length);
            r.AsSpan(0, Math.Min(r.Length, buffer.Length)).CopyTo(buffer);
        }

        public void WriteRead(byte slaveAddr, ReadOnlySpan<byte> w, Span<byte> r, int timeoutMs)
        {
            EnsureConnected();

            if (_settings.DeviceKind == DeviceKind.FT4222)
            {

                Write(slaveAddr, w, stop: true);
                Read(slaveAddr, r, timeoutMs);
                return;
            }

            var rr = _um232h!.I2cWriteAndRead(slaveAddr, w.ToArray(), w.Length, r.Length);
            rr.AsSpan(0, Math.Min(rr.Length, r.Length)).CopyTo(r);
        }

        public void Dispose() => Disconnect();

        private void EnsureConnected()
        {
            if (!IsConnected)
                throw new InvalidOperationException("I2C bus is not connected.");
        }
    }
}
