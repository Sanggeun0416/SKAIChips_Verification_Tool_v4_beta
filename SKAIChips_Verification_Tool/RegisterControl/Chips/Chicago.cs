using SKAIChips_Verification_Tool.RegisterControl.Core;

namespace SKAIChips_Verification_Tool.RegisterControl.Chips
{
    public class ChicagoProject : II2cChipProject, IChipProjectWithTests
    {
        public string Name => "Chicago";
        public IEnumerable<ProtocolType> SupportedProtocols { get; } = new[] { ProtocolType.I2C };

        public IRegisterChip CreateChip(II2cBus bus, ProtocolSettings settings)
        {
            if (bus == null)
                throw new ArgumentNullException(nameof(bus));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (settings.ProtocolType != ProtocolType.I2C)
                throw new InvalidOperationException("Chicago supports only I2C.");

            return new ChicagoRegisterChip("Chicago", bus, settings.I2cSlaveAddress);
        }

        public IChipTestSuite CreateTestSuite(IRegisterChip chip)
        {
            if (chip is not ChicagoRegisterChip c)
                throw new ArgumentException("Chip instance must be ChicagoRegisterChip.", nameof(chip));

            return new ChicagoTestSuite(c);
        }
    }

    internal sealed class ChicagoRegisterChip : IRegisterChip
    {
        private readonly II2cBus _bus;
        private readonly byte _slave;
        public string Name
        {
            get;
        }

        public ChicagoRegisterChip(string name, II2cBus bus, byte slaveAddr)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _slave = slaveAddr;
        }

        public uint ReadRegister(uint address)
        {
            Span<byte> cmd = stackalloc byte[8];
            cmd[0] = 0xA1;
            cmd[1] = 0x2C;
            cmd[2] = 0x12;
            cmd[3] = 0x34;
            cmd[4] = (byte)((address >> 24) & 0xFF);
            cmd[5] = (byte)((address >> 16) & 0xFF);
            cmd[6] = (byte)((address >> 8) & 0xFF);
            cmd[7] = (byte)(address & 0xFF);

            _bus.Write(_slave, cmd, stop: true);

            Span<byte> rcv = stackalloc byte[4];
            _bus.Read(_slave, rcv, 200);

            return (uint)((rcv[3] << 24) | (rcv[2] << 16) | (rcv[1] << 8) | rcv[0]);
        }

        public void WriteRegister(uint address, uint data)
        {
            Span<byte> cmd = stackalloc byte[8];
            cmd[0] = 0xA1;
            cmd[1] = 0x2C;
            cmd[2] = 0x12;
            cmd[3] = 0x34;
            cmd[4] = (byte)((address >> 24) & 0xFF);
            cmd[5] = (byte)((address >> 16) & 0xFF);
            cmd[6] = (byte)((address >> 8) & 0xFF);
            cmd[7] = (byte)(address & 0xFF);

            _bus.Write(_slave, cmd, stop: true);

            Span<byte> dataBytes = stackalloc byte[4];
            dataBytes[0] = (byte)(data & 0xFF);
            dataBytes[1] = (byte)((data >> 8) & 0xFF);
            dataBytes[2] = (byte)((data >> 16) & 0xFF);
            dataBytes[3] = (byte)((data >> 24) & 0xFF);

            _bus.Write(_slave, dataBytes, stop: true);
        }
    }

    internal class ChicagoTestSuite : IChipTestSuite
    {
        private readonly ChicagoRegisterChip _chip;
        public IReadOnlyList<ChipTestInfo> Tests
        {
            get;
        }

        public ChicagoTestSuite(ChicagoRegisterChip chip)
        {
            _chip = chip;
            Tests = new[] { new ChipTestInfo("TEST.DUMMY", "Dummy", "TBD", "TEST") };
        }

        public async Task Run_TEST(string testId, Func<string, string, Task> log, CancellationToken cancellationToken)
        {
            await log("INFO", $"Chicago Test '{testId}' 시작");
            await Task.Delay(100, cancellationToken);
            await log("INFO", $"Chicago Test '{testId}' 종료");
        }
    }
}
