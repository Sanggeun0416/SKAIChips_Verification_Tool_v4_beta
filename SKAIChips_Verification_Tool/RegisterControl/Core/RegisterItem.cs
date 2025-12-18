namespace SKAIChips_Verification_Tool.RegisterControl.Core
{
    public class RegisterItem
    {

        public string Name
        {
            get;
        }
        public int UpperBit
        {
            get;
        }
        public int LowerBit
        {
            get;
        }
        public uint DefaultValue
        {
            get;
        }
        public string Description
        {
            get;
        }

        public RegisterItem(string name, int upperBit, int lowerBit, uint defaultValue, string description)
        {
            Name = name;
            UpperBit = upperBit;
            LowerBit = lowerBit;
            DefaultValue = defaultValue;
            Description = description;
        }

    }
}
