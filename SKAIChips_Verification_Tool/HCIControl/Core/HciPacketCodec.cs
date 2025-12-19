namespace SKAIChips_Verification_Tool.HCIControl.Core
{
    public static class HciPacketCodec
    {

        public static byte[] EncodeCommand(HciCommandPacket packet)
        {
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            var parameters = packet.Parameters ?? Array.Empty<byte>();
            var buffer = new byte[1 + 2 + 1 + parameters.Length];

            buffer[0] = (byte)HciPacketType.Command;

            ushort opcode = packet.Opcode.Value;
            buffer[1] = (byte)(opcode & 0xFF);
            buffer[2] = (byte)((opcode >> 8) & 0xFF);

            buffer[3] = (byte)parameters.Length;

            if (parameters.Length > 0)
                Buffer.BlockCopy(parameters, 0, buffer, 4, parameters.Length);

            return buffer;
        }

        public static bool TryDecodeEvent(ReadOnlySpan<byte> buffer, out HciEventPacket packet, out int bytesConsumed)
        {
            packet = null;
            bytesConsumed = 0;

            if (buffer.Length < 3)
                return false;

            if (buffer[0] != (byte)HciPacketType.Event)
                return false;

            byte eventCode = buffer[1];
            byte paramLength = buffer[2];

            if (buffer.Length < 3 + paramLength)
                return false;

            var parameters = new byte[paramLength];
            buffer.Slice(3, paramLength).CopyTo(parameters);

            packet = new HciEventPacket(eventCode, parameters);
            bytesConsumed = 3 + paramLength;

            return true;
        }
    }
}
