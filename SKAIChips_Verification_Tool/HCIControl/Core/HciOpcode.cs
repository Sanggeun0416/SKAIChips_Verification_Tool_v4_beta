namespace SKAIChips_Verification_Tool.HCIControl.Core
{
    public readonly struct HciOpcode : IEquatable<HciOpcode>
    {
        public byte Ogf
        {
            get;
        }
        public ushort Ocf
        {
            get;
        }

        public ushort Value => (ushort)((Ogf << 10) | (Ocf & 0x03FF));

        public HciOpcode(byte ogf, ushort ocf)
        {
            if (ogf > 0x3F)
                throw new ArgumentOutOfRangeException(nameof(ogf), "OGF must be 0..63.");
            if (ocf > 0x03FF)
                throw new ArgumentOutOfRangeException(nameof(ocf), "OCF must be 0..1023.");

            Ogf = ogf;
            Ocf = (ushort)(ocf & 0x03FF);
        }

        public HciOpcode(ushort value)
        {
            Ogf = (byte)((value >> 10) & 0x3F);
            Ocf = (ushort)(value & 0x03FF);
        }

        public static HciOpcode FromValue(ushort value) => new HciOpcode(value);

        public static HciOpcode FromOgfOcf(byte ogf, ushort ocf) => new HciOpcode(ogf, ocf);

        public override string ToString() => $"0x{Value:X4}";

        public bool Equals(HciOpcode other) => Value == other.Value;

        public override bool Equals(object obj) => obj is HciOpcode other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(HciOpcode left, HciOpcode right) => left.Equals(right);

        public static bool operator !=(HciOpcode left, HciOpcode right) => !left.Equals(right);
    }
}
