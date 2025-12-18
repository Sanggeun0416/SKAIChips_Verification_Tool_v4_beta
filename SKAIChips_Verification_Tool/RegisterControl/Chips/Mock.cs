using SKAIChips_Verification_Tool.RegisterControl.Core;

namespace SKAIChips_Verification_Tool.RegisterControl.Chips
{
    public class MockProject : IChipProject, IChipProjectWithTests
    {
        public string Name => "Test";

        public IEnumerable<ProtocolType> SupportedProtocols { get; } = new[] { ProtocolType.I2C };

        public IRegisterChip CreateChip(II2cBus bus, ProtocolSettings settings)
        {
            return new MockRegisterChip();
        }

        public IChipTestSuite CreateTestSuite(IRegisterChip chip)
        {
            if (chip is not MockRegisterChip mockChip)
                throw new ArgumentException("Chip instance must be MockRegisterChip.", nameof(chip));

            return new MockTestSuite(mockChip);
        }
    }

    internal class MockRegisterChip : IRegisterChip
    {
        private readonly Dictionary<uint, uint> _registers = new();

        public string Name => "Mock Chip";

        public uint ReadRegister(uint address)
        {
            return _registers.TryGetValue(address, out var value) ? value : 0;
        }

        public void WriteRegister(uint address, uint data)
        {
            _registers[address] = data;
        }
    }

    internal class MockTestSuite : IChipTestSuite
    {
        private readonly MockRegisterChip _chip;

        public IReadOnlyList<ChipTestInfo> Tests
        {
            get;
        }

        public MockTestSuite(MockRegisterChip chip)
        {
            _chip = chip;

            Tests = new[]
            {
                new ChipTestInfo("TEST.DUMMY", "Dummy Test", "Mock 테스트 시퀀스 자리", "TEST")
            };
        }

        public async Task Run_TEST(string testId, Func<string, string, Task> log, CancellationToken cancellationToken)
        {
            await log("INFO", $"Mock Test '{testId}' 시작");

            switch (testId)
            {
                case "TEST.DUMMY":
                    await log("INFO", "아직 구현 안 됨");
                    await Task.Delay(200, cancellationToken);
                    break;

                default:
                    await log("ERROR", $"Unknown testId: {testId}");
                    break;
            }

            await log("INFO", $"Mock Test '{testId}' 종료");
        }
    }
}
