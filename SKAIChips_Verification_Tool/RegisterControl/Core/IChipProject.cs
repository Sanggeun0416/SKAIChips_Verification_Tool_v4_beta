namespace SKAIChips_Verification_Tool.RegisterControl.Core
{
    public interface IChipProject
    {
        string Name
        {
            get;
        }
        IEnumerable<ProtocolType> SupportedProtocols
        {
            get;
        }
    }

    public interface II2cChipProject : IChipProject
    {
        IRegisterChip CreateChip(II2cBus bus, ProtocolSettings settings);
    }

    public interface ISpiChipProject : IChipProject
    {
        IRegisterChip CreateChip(ISpiBus bus, ProtocolSettings settings);
    }

    public interface IChipProjectWithTests : IChipProject
    {
        IChipTestSuite CreateTestSuite(IRegisterChip chip);
    }

    public interface IChipTestSuite
    {
        IReadOnlyList<ChipTestInfo> Tests
        {
            get;
        }
        Task Run_TEST(string testId, Func<string, string, Task> log, CancellationToken cancellationToken);
    }

    public sealed class ChipTestInfo
    {
        public string Id
        {
            get;
        }
        public string Name
        {
            get;
        }
        public string Description
        {
            get;
        }
        public string Category
        {
            get;
        }

        public ChipTestInfo(string id, string name, string description, string category)
        {
            Id = id;
            Name = name;
            Description = description;
            Category = category;
        }
    }
}
