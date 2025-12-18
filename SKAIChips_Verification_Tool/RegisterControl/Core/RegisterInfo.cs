namespace SKAIChips_Verification_Tool.RegisterControl.Core
{
    public class RegisterInfo
    {

        public string Sheet
        {
            get; set;
        }
        public string Group
        {
            get; set;
        }
        public string Name
        {
            get; set;
        }

        public uint Address
        {
            get; set;
        }
        public string AddressText => $"0x{Address:X8}";

        public uint Reset
        {
            get; set;
        }
        public string ResetText => $"0x{Reset:X8}";

        public string Description
        {
            get; set;
        }

        public override string ToString() =>
            $"{Sheet}/{Group} - {Name} ({AddressText})";

    }
}
