namespace SKAIChips_Verification_Tool.HCIControl.Core
{
    public enum HciPacketType : byte
    {
        Command = 0x01,
        AclData = 0x02,
        SynchronousData = 0x03,
        Event = 0x04
    }

    public abstract class HciPacket
    {
        public HciPacketType PacketType
        {
            get;
        }

        protected HciPacket(HciPacketType packetType)
        {
            PacketType = packetType;
        }
    }

    public sealed class HciCommandPacket : HciPacket
    {
        public HciOpcode Opcode
        {
            get;
        }
        public byte[] Parameters
        {
            get;
        }

        public HciCommandPacket(HciOpcode opcode, byte[] parameters)
            : base(HciPacketType.Command)
        {
            Opcode = opcode;
            Parameters = parameters ?? Array.Empty<byte>();
        }
    }

    public sealed class HciEventPacket : HciPacket
    {
        public byte EventCode
        {
            get;
        }
        public byte[] Parameters
        {
            get;
        }

        public HciEventPacket(byte eventCode, byte[] parameters)
            : base(HciPacketType.Event)
        {
            EventCode = eventCode;
            Parameters = parameters ?? Array.Empty<byte>();
        }
    }
}
