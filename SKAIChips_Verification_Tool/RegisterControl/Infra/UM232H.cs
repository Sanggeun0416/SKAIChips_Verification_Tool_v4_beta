using FTD2XX_NET;

namespace SKAIChips_Verification_Tool.RegisterControl.Infra
{
    public sealed class UM232H : IDisposable
    {
        private readonly uint _deviceIndex;
        private readonly FTDI _ftdi = new FTDI();
        private readonly CommandQueue _queue = new CommandQueue(65536);
        private readonly ManualResetEvent _busyEvent = new ManualResetEvent(true);

        private bool _disposed;
        private bool _isOpen;
        private int _numBytesToRead;

        private ushort _clockDivisor;
        private byte _lowPinsDirections = 0xFB;
        private byte _lowPinsStates = 0xFB;
        private byte _highPinsDirections = 0xFF;
        private byte _highPinsStates = 0xFF;

        private int _spiMode;
        private Command.BitFirst _spiBitFirst;
        private Command.ClockEdge _spiEdge;

        public bool IsOpen => _isOpen;
        public double CurrentClockKHz => GetClockKHz();

        public UM232H(uint deviceIndex)
        {
            _deviceIndex = deviceIndex;
        }

        public bool Open()
        {
            ThrowIfDisposed();

            if (_isOpen)
                return true;

            var status = _ftdi.OpenByIndex(_deviceIndex);
            if (status != FTDI.FT_STATUS.FT_OK)
                return false;

            status |= _ftdi.ResetDevice();
            status |= _ftdi.SetCharacters(0, false, 0, false);
            status |= _ftdi.SetTimeouts(5000, 5000);
            status |= _ftdi.SetLatency(16);
            status |= _ftdi.SetBitMode(0x00, 0x00);
            status |= _ftdi.SetBitMode(0x00, 0x02); 

            if (status != FTDI.FT_STATUS.FT_OK)
            {
                _ftdi.Close();
                return false;
            }

            Thread.Sleep(50);

            
            for (byte b = 0xAA; b <= 0xAB; b++)
            {
                _queue.Clear();
                _queue.Set(b);
                if (!SendCommand())
                {
                    _ftdi.Close();
                    return false;
                }

                var recv = GetReceivedBytes(2);
                if (recv == null || recv.Length < 2)
                {
                    _ftdi.Close();
                    return false;
                }

                bool ok = false;
                for (int i = 0; i < recv.Length - 1; i++)
                {
                    if (recv[i] == Command.BadCommand && recv[i + 1] == b)
                    {
                        ok = true;
                        break;
                    }
                }

                if (!ok)
                {
                    _ftdi.Close();
                    return false;
                }
            }

            _queue.Clear();
            _queue.Set(Command.Clock.Disable5Divisor);
            _queue.Set(Command.Loopback.Disable);
            SendCommand();

            _queue.Set(Command.GPIO.SetGPIOL);
            _queue.Set(_lowPinsStates);
            _queue.Set(_lowPinsDirections);
            _queue.Set(Command.GPIO.SetGPIOH);
            _queue.Set(_highPinsStates);
            _queue.Set(_highPinsDirections);
            SendCommand();

            _isOpen = true;
            return true;
        }

        public void Close()
        {
            ThrowIfDisposed();

            if (!_isOpen)
                return;

            _ftdi.Close();
            _isOpen = false;
            _numBytesToRead = 0;
        }

        public bool I2cInit(int clockKHz)
        {
            ThrowIfDisposed();
            EnsureOpen();

            _queue.Clear();
            _queue.Set(Command.AdaptiveClocking.Disable);
            _queue.Set(Command.ThreePhase.Enable);

            _queue.Set(Command.DriveOnlyZero);
            _queue.Set(7);   
            _queue.Set(0);

            if (!SendCommand())
                return false;

            if (!SetClock(clockKHz))
                return false;

            Thread.Sleep(20);

            GPIOL_SetPins(0xF0, 0xF0, false);
            GPIOH_SetPins(0xFF, 0xFF);
            SendCommand();

            Thread.Sleep(30);
            return true;
        }

        public bool I2cWrite(byte slaveAddress7bit, byte[] data, bool sendStop)
        {
            ThrowIfDisposed();
            EnsureOpen();

            if (data == null || data.Length == 0)
                return true;

            _queue.Clear();
            _numBytesToRead = 0;

            I2C_SetStart();
            I2C_SetWriteByte((byte)((slaveAddress7bit << 1) | 0x00));
            for (int i = 0; i < data.Length; i++)
            {
                I2C_SetWriteByte(data[i]);
            }
            if (sendStop)
                I2C_SetStop();

            if (SendCommand(true))
            {
                var recv = GetReceivedBytes(_numBytesToRead);
                if (recv != null && recv.Length >= data.Length)
                    return true;
            }

            return false;
        }

        public byte[] I2cRead(byte slaveAddress7bit, int length)
        {
            ThrowIfDisposed();
            EnsureOpen();

            if (length <= 0)
                return Array.Empty<byte>();

            _queue.Clear();
            _numBytesToRead = 0;

            I2C_SetStart();
            I2C_SetWriteByte((byte)((slaveAddress7bit << 1) | 0x01));
            for (int i = 0; i < length; i++)
            {
                I2C_SetReadByte(i == length - 1);
            }
            I2C_SetStop();

            if (SendCommand(true))
            {
                var recv = GetReceivedBytes(_numBytesToRead);
                if (recv != null && recv.Length >= length)
                {
                    var result = new byte[length];
                    for (int i = 0; i < length; i++)
                    {
                        result[length - 1 - i] = recv[recv.Length - 1 - i];
                    }
                    return result;
                }
            }

            return Array.Empty<byte>();
        }

        public byte[] I2cWriteAndRead(byte slaveAddress7bit, byte[] writeData, int numBytesToWrite, int numBytesToRead)
        {
            ThrowIfDisposed();
            EnsureOpen();

            if (numBytesToRead <= 0)
                return Array.Empty<byte>();

            if (writeData == null || numBytesToWrite <= 0)
                numBytesToWrite = 0;

            _queue.Clear();
            _numBytesToRead = 0;

            I2C_SetStart();
            I2C_SetWriteByte((byte)((slaveAddress7bit << 1) | 0x00));
            for (int i = 0; i < numBytesToWrite; i++)
            {
                I2C_SetWriteByte(writeData[i]);
            }
            I2C_SetStop();

            for (int j = 0; j < 10; j++)
            {
                GPIOL_SetPins(_lowPinsDirections, (byte)((_lowPinsStates & 0xF0u) | 0x00u), false);
            }

            I2C_SetStart();
            I2C_SetWriteByte((byte)((slaveAddress7bit << 1) | 0x01));
            for (int k = 0; k < numBytesToRead; k++)
            {
                I2C_SetReadByte(k == numBytesToRead - 1);
            }
            I2C_SetStop();

            if (SendCommand(true))
            {
                var recv = GetReceivedBytes(_numBytesToRead);
                if (recv != null && recv.Length >= numBytesToRead)
                {
                    var result = new byte[numBytesToRead];
                    for (int i = 0; i < numBytesToRead; i++)
                    {
                        result[numBytesToRead - 1 - i] = recv[recv.Length - 1 - i];
                    }
                    return result;
                }
            }

            return Array.Empty<byte>();
        }

        public bool SpiInit(int mode, int clockKHz, bool lsbFirst)
        {
            ThrowIfDisposed();
            EnsureOpen();

            _queue.Clear();
            _queue.Set(Command.AdaptiveClocking.Disable);
            _queue.Set(Command.ThreePhase.Disable);
            if (!SendCommand())
                return false;

            if (!SetClock(clockKHz))
                return false;

            Thread.Sleep(20);

            _spiMode = mode;
            _spiBitFirst = lsbFirst ? Command.BitFirst.LSB : Command.BitFirst.MSB;
            _spiEdge = (mode == 0 || mode == 3)
                ? Command.ClockEdge.OutRising
                : Command.ClockEdge.InFalling;

            if (_spiMode < 2)
            {
                GPIOL_SetPins(0xFB, 0xF8, false);
            }
            else
            {
                GPIOL_SetPins(0xFB, 0xF9, false);
            }

            GPIOH_SetPins(0xFF, 0xFF);
            SendCommand();
            Thread.Sleep(30);

            return true;
        }

        public void SpiWrite(byte[] data, bool endTransaction)
        {
            ThrowIfDisposed();
            EnsureOpen();

            if (data == null || data.Length == 0)
                return;

            _queue.Clear();
            _numBytesToRead = 0;

            SPI_SetStart();
            SPI_SetBytes(data, (ushort)data.Length, Command.PinConfig.Write);
            SPI_SetStop();

            SendCommand(endTransaction);
        }

        public byte[] SpiRead(int length)
        {
            ThrowIfDisposed();
            EnsureOpen();

            if (length <= 0)
                return Array.Empty<byte>();

            _queue.Clear();
            _numBytesToRead = 0;

            SPI_SetStart();
            SPI_SetBytes(null, (ushort)length, Command.PinConfig.Read);
            SPI_SetStop();

            if (SendCommand(true))
            {
                var recv = GetReceivedBytes(length);
                return recv ?? Array.Empty<byte>();
            }

            return Array.Empty<byte>();
        }

        public byte[] SpiReadWrite(byte[] writeData, int length)
        {
            ThrowIfDisposed();
            EnsureOpen();

            if (length <= 0)
                return Array.Empty<byte>();
            if (writeData == null || writeData.Length < length)
                throw new ArgumentException("writeData length must be >= length", nameof(writeData));

            _queue.Clear();
            _numBytesToRead = 0;

            SPI_SetStart();
            SPI_SetBytes(writeData, (ushort)length, Command.PinConfig.ReadWrite);
            SPI_SetStop();

            if (SendCommand(true))
            {
                var recv = GetReceivedBytes(length);
                return recv ?? Array.Empty<byte>();
            }

            return Array.Empty<byte>();
        }

        private bool SetClock(int clockKHz)
        {
            double baseKHz = 60000.0; 
            ushort divisor = (ushort)(baseKHz / (2.0 * clockKHz) - 1.0);

            _queue.Set(Command.Clock.SetDivisor);
            _queue.Set((byte)(divisor & 0xFF));
            _queue.Set((byte)((divisor >> 8) & 0xFF));

            if (SendCommand())
            {
                _clockDivisor = divisor;
                return true;
            }
            return false;
        }

        private double GetClockKHz()
        {
            if (!_isOpen)
                return 0.0;

            double baseKHz = 60000.0;
            return baseKHz / ((1.0 + _clockDivisor) * 2.0);
        }

        private void GPIOL_SetPins(byte directions, byte states, bool protect)
        {
            if (protect)
            {
                var current = GPIOL_GetPins();
                _lowPinsStates = (byte)((states & 0xF0u) | (current & 0x0Fu));
                _lowPinsDirections = (byte)((directions & 0xF0u) | (_lowPinsDirections & 0x0Fu));
            }
            else
            {
                _lowPinsStates = states;
                _lowPinsDirections = directions;
            }

            _queue.Set(Command.GPIO.SetGPIOL);
            _queue.Set(_lowPinsStates);
            _queue.Set(_lowPinsDirections);
        }

        private byte GPIOL_GetPins()
        {
            byte bitMode = 0;
            WaitBusyEvent(200, "GPIOL_GetPins");
            _ftdi.GetPinStates(ref bitMode);
            _lowPinsStates = bitMode;
            _busyEvent.Set();
            return _lowPinsStates;
        }

        private void GPIOH_SetPins(byte directions, byte states)
        {
            _highPinsStates = states;
            _highPinsDirections = directions;

            _queue.Set(Command.GPIO.SetGPIOH);
            _queue.Set(_highPinsStates);
            _queue.Set(_highPinsDirections);
        }

        private void I2C_SetStart()
        {
            byte directions = (byte)((_lowPinsDirections & 0xF0u) | 0x03u);
            for (int i = 0; i < 20; i++)
            {
                GPIOL_SetPins(directions, (byte)((_lowPinsStates & 0xF0u) | 0x03u), false);
            }
            for (int j = 0; j < 20; j++)
            {
                GPIOL_SetPins(directions, (byte)((_lowPinsStates & 0xF0u) | 0x01u), false);
            }
            GPIOL_SetPins(directions, (byte)((_lowPinsStates & 0xF0u) | 0x00u), false);
        }

        private void I2C_SetStop()
        {
            byte directions0 = (byte)((_lowPinsDirections & 0xF0u) | 0x00u);
            byte directions3 = (byte)((_lowPinsDirections & 0xF0u) | 0x03u);

            for (int i = 0; i < 20; i++)
            {
                GPIOL_SetPins(directions3, (byte)((_lowPinsStates & 0xF0u) | 0x01u), false);
            }
            for (int j = 0; j < 20; j++)
            {
                GPIOL_SetPins(directions3, (byte)((_lowPinsStates & 0xF0u) | 0x03u), false);
            }

            GPIOL_SetPins(directions0, (byte)((_lowPinsStates & 0xF0u) | 0x00u), false);
        }

        private void I2C_SetWriteByte(byte data)
        {
            byte directions = (byte)((_lowPinsDirections & 0xF0u) | 0x03u);

            SSC_SetBits(data, 8, Command.PinConfig.Write, Command.BitFirst.MSB, Command.ClockEdge.OutRising);

            for (int i = 0; i < 20; i++)
            {
                GPIOL_SetPins(directions, (byte)((_lowPinsStates & 0xF0u) | 0x02u), false);
            }

            SSC_SetBits(0, 1, Command.PinConfig.Read, Command.BitFirst.MSB, Command.ClockEdge.OutFalling);

            for (int j = 0; j < 100; j++)
            {
                GPIOL_SetPins(directions, (byte)((_lowPinsStates & 0xF0u) | 0x02u), false);
            }
        }

        private void I2C_SetReadByte(bool nak)
        {
            byte directions = (byte)((_lowPinsDirections & 0xF0u) | 0x03u);

            for (int i = 0; i < 50; i++)
            {
                GPIOL_SetPins(directions, (byte)((_lowPinsStates & 0xF0u) | 0x02u), false);
            }

            SSC_SetBits(0, 8, Command.PinConfig.Read, Command.BitFirst.MSB, Command.ClockEdge.OutFalling);
            SSC_SetBits((byte)(nak ? 0xFF : 0x00), 1, Command.PinConfig.Write, Command.BitFirst.MSB, Command.ClockEdge.OutRising);
        }

        private void SSC_SetBits(byte sendBits, byte length, Command.PinConfig config, Command.BitFirst first, Command.ClockEdge edge)
        {
            if (length > 8 || length < 1)
                return;

            _queue.Set(Command.Get(config, edge, Command.DataUnit.Bit, first));
            _queue.Set((byte)(length - 1));

            if (config == Command.PinConfig.Write || config == Command.PinConfig.ReadWrite)
            {
                _queue.Set(sendBits);
            }

            if (config == Command.PinConfig.Read || config == Command.PinConfig.ReadWrite)
            {
                _numBytesToRead++;
            }
        }

        private void SSC_SetBytes(byte[] bytes, ushort length, Command.PinConfig config, Command.BitFirst first, Command.ClockEdge edge)
        {
            if (config == Command.PinConfig.TMS_Write ||
                config == Command.PinConfig.TMS_ReadWrite ||
                ((config == Command.PinConfig.Write || config == Command.PinConfig.ReadWrite) &&
                 (bytes == null || bytes.Length < length)))
            {
                return;
            }

            _queue.Set(Command.Get(config, edge, Command.DataUnit.Byte, first));
            _queue.Set((byte)((length - 1) & 0xFF));
            _queue.Set((byte)(((length - 1) >> 8) & 0xFF));

            if (config == Command.PinConfig.Write || config == Command.PinConfig.ReadWrite)
            {
                for (int i = 0; i < length; i++)
                {
                    _queue.Set(bytes[i]);
                }
            }

            if (config == Command.PinConfig.Read || config == Command.PinConfig.ReadWrite)
            {
                _numBytesToRead += length;
            }
        }

        private void SPI_SetBytes(byte[] bytes, ushort length, Command.PinConfig config)
        {
            SSC_SetBytes(bytes, length, config, _spiBitFirst, _spiEdge);
        }

        private void SPI_SetStart()
        {
            if (_spiMode < 2)
            {
                for (int i = 0; i < 10; i++)
                {
                    GPIOL_SetPins(_lowPinsDirections, (byte)((_lowPinsStates & 0xF0u) | 0x08u), false);
                }
                GPIOL_SetPins(_lowPinsDirections, (byte)((_lowPinsStates & 0xF0u) | 0x00u), false);
            }
            else
            {
                for (int i = 0; i < 10; i++)
                {
                    GPIOL_SetPins(_lowPinsDirections, (byte)((_lowPinsStates & 0xF0u) | 0x09u), false);
                }
                GPIOL_SetPins(_lowPinsDirections, (byte)((_lowPinsStates & 0xF0u) | 0x01u), false);
            }
        }

        private void SPI_SetStop()
        {
            if (_spiMode < 2)
            {
                for (int i = 0; i < 10; i++)
                {
                    GPIOL_SetPins(_lowPinsDirections, (byte)((_lowPinsStates & 0xF0u) | 0x00u), false);
                }
                GPIOL_SetPins(_lowPinsDirections, (byte)((_lowPinsStates & 0xF0u) | 0x08u), false);
            }
            else
            {
                for (int i = 0; i < 10; i++)
                {
                    GPIOL_SetPins(_lowPinsDirections, (byte)((_lowPinsStates & 0xF0u) | 0x01u), false);
                }
                GPIOL_SetPins(_lowPinsDirections, (byte)((_lowPinsStates & 0xF0u) | 0x09u), false);
            }
        }

        private bool SendCommand(bool sendAnswerBackImmediately = false)
        {
            uint written = 0;
            if (sendAnswerBackImmediately)
            {
                _queue.Set(Command.SendAnswerBackImmediately);
            }

            var bytes = _queue.GetBytes(_queue.Count);
            if (bytes.Length == 0)
                return true;

            WaitBusyEvent(200, "SendCommand");
            var status = _ftdi.Write(bytes, bytes.Length, ref written);
            _busyEvent.Set();

            return status == FTDI.FT_STATUS.FT_OK;
        }

        private byte[] GetReceivedBytes(int length)
        {
            uint rxQueue = 0;
            uint read = 0;
            int tries = 0;
            byte[] buf = null;

            WaitBusyEvent(200, "GetReceivedBytes");
            FTDI.FT_STATUS status;
            do
            {
                Thread.Sleep(1);
                status = _ftdi.GetRxBytesAvailable(ref rxQueue);
                tries++;
            }
            while (rxQueue < length && tries < 500);

            if (status == FTDI.FT_STATUS.FT_OK && length <= rxQueue)
            {
                buf = new byte[rxQueue];
                _ftdi.Read(buf, rxQueue, ref read);
                _numBytesToRead = 0;
            }

            _busyEvent.Set();
            return buf;
        }

        private bool WaitBusyEvent(int timeoutMs, string debug)
        {
            if (_busyEvent.WaitOne(timeoutMs))
            {
                _busyEvent.Reset();
                return true;
            }

            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff}> UM232H:BDF:{debug}");
            return false;
        }

        private void EnsureOpen()
        {
            if (!_isOpen)
                throw new InvalidOperationException("UM232H device is not open.");
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UM232H));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Close();
            _disposed = true;
        }

        private sealed class CommandQueue
        {
            private readonly List<byte> _buffer;

            public int Count => _buffer.Count;

            public CommandQueue(int capacity)
            {
                _buffer = new List<byte>(capacity);
            }

            public void Clear() => _buffer.Clear();

            public void Set(byte value) => _buffer.Add(value);

            public byte[] GetBytes(int count) => _buffer.ToArray();
        }

        private static class Command
        {
            public static class Clock
            {
                public const byte SetDivisor = 0x86;
                public const byte Disable5Divisor = 0x8A;
                public const byte Enable5Divisor = 0x8B;
            }

            public static class GPIO
            {
                public const byte SetGPIOL = 0x80;
                public const byte GetGPIOL = 0x81;
                public const byte SetGPIOH = 0x82;
                public const byte GetGPIOH = 0x83;
            }

            public static class Loopback
            {
                public const byte Enable = 0x84;
                public const byte Disable = 0x85;
            }

            public static class ThreePhase
            {
                public const byte Enable = 0x8C;
                public const byte Disable = 0x8D;
            }

            public static class AdaptiveClocking
            {
                public const byte Enable = 0x96;
                public const byte Disable = 0x97;
            }

            public enum PinConfig
            {
                Write = 0x10,
                Read = 0x20,
                ReadWrite = 0x30,
                TMS_Write = 0x40,
                TMS_ReadWrite = 0x60
            }

            public enum DataUnit
            {
                Byte = 0x00,
                Bit = 0x02
            }

            public enum BitFirst
            {
                MSB = 0x00,
                LSB = 0x08
            }

            public enum ClockEdge
            {
                OutRising = 0x01,
                OutFalling = 0x00,
                InRising = 0x00,
                InFalling = 0x04
            }

            public const byte BadCommand = 0xFA;
            public const byte SendAnswerBackImmediately = 0x87;
            public const byte DriveOnlyZero = 0x9E;

            public static byte Get(PinConfig config, ClockEdge edge, DataUnit unit, BitFirst first)
            {
                if ((config == PinConfig.TMS_Write || config == PinConfig.TMS_ReadWrite) &&
                    unit == DataUnit.Byte)
                {
                    unit = DataUnit.Bit;
                }

                return (byte)((byte)config | (byte)edge | (byte)unit | (byte)first);
            }
        }

        private enum GPIO_Direction
        {
            Input,
            Output
        }

    }
}
