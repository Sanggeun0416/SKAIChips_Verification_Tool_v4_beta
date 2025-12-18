namespace SKAIChips_Verification_Tool.RegisterControl.Core
{
    public class RegisterGroup
    {

        public string Name
        {
            get;
        }

        public List<Register> Registers { get; } = new List<Register>();

        public RegisterGroup(string name)
        {
            Name = name;
        }

        public Register AddRegister(string name, uint address)
        {
            var reg = new Register(name, address);
            Registers.Add(reg);
            return reg;
        }

    }
}
