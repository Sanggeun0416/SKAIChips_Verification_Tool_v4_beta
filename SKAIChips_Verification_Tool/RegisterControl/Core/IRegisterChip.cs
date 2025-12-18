namespace SKAIChips_Verification_Tool.RegisterControl.Core
{
    public interface IRegisterChip
    {

        string Name
        {
            get;
        }

        uint ReadRegister(uint address);
        void WriteRegister(uint address, uint data);

    }
}
