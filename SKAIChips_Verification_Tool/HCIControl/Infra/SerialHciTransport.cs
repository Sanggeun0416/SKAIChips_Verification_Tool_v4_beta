using SKAIChips_Verification_Tool.HCIControl.Core;
using System.IO.Ports;

namespace SKAIChips_Verification_Tool.HCIControl.Infra
{
    public sealed class SerialHciTransport : IHciTransport
    {
        private readonly string _portName;
        private readonly int _baudRate;
        private SerialPort _port;

        public string Name => _portName;
        public bool IsOpen => _port != null && _port.IsOpen;

        public SerialHciTransport(string portName, int baudRate)
        {
            _portName = portName ?? throw new ArgumentNullException(nameof(portName));
            _baudRate = baudRate;
        }

        public void Open()
        {
            if (IsOpen)
                return;

            _port = new SerialPort(_portName, _baudRate, Parity.None, 8, StopBits.One)
            {
                Handshake = Handshake.None,
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };
            _port.Open();
        }

        public void Close()
        {
            if (_port == null)
                return;

            try
            {
                _port.Close();
            }
            finally
            {
                _port.Dispose();
                _port = null;
            }
        }

        public void Send(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (!IsOpen)
                throw new InvalidOperationException("Serial port is not open.");

            _port.Write(buffer, offset, count);
        }

        public int Receive(byte[] buffer, int offset, int count, int timeoutMs)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (!IsOpen)
                throw new InvalidOperationException("Serial port is not open.");

            int originalTimeout = _port.ReadTimeout;
            try
            {
                _port.ReadTimeout = timeoutMs;
                int read = 0;
                while (read < count)
                {
                    int r = _port.Read(buffer, offset + read, count - read);
                    if (r <= 0)
                        break;
                    read += r;
                }
                return read;
            }
            catch (TimeoutException)
            {
                return 0;
            }
            finally
            {
                _port.ReadTimeout = originalTimeout;
            }
        }

        public void Dispose()
        {
            Close();
        }

        public static string[] GetAvailablePortNames()
        {
            return SerialPort.GetPortNames();
        }
    }
}
