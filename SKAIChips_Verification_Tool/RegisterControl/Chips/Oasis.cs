using SKAIChips_Verification_Tool.RegisterControl.Core;

namespace SKAIChips_Verification_Tool.RegisterControl.Chips
{

    public class OasisProject : II2cChipProject, IChipProjectWithTests
    {
        public string Name => "Oasis";
        public IEnumerable<ProtocolType> SupportedProtocols { get; } = new[] { ProtocolType.I2C };

        public IRegisterChip CreateChip(II2cBus bus, ProtocolSettings settings)
        {
            if (bus == null)
                throw new ArgumentNullException(nameof(bus));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (settings.ProtocolType != ProtocolType.I2C)
                throw new InvalidOperationException("Oasis supports only I2C.");

            return new OasisRegisterChip("Oasis", bus, settings.I2cSlaveAddress);
        }

        public IChipTestSuite CreateTestSuite(IRegisterChip chip)
        {
            if (chip is not OasisRegisterChip oasisChip)
                throw new ArgumentException("Chip instance must be OasisRegisterChip.", nameof(chip));

            return new OasisTestSuite(oasisChip);
        }
    }

    internal sealed class OasisRegisterChip : IRegisterChip
    {
        private readonly II2cBus _bus;
        private readonly byte _slave;

        public string Name
        {
            get;
        }

        public OasisRegisterChip(string name, II2cBus bus, byte slaveAddr)
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
            _bus.Read(_slave, rcv, timeoutMs: 200);

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

        internal void HaltMcu()
        {
            Span<byte> cmd = stackalloc byte[] { 0xA1, 0x2C, 0x56, 0x78 };
            _bus.Write(_slave, cmd, stop: true);
            Thread.Sleep(50);
        }

        internal void ResetMcu()
        {
            Span<byte> cmd = stackalloc byte[] { 0xA1, 0x2C, 0xAB, 0xCD };
            _bus.Write(_slave, cmd, stop: true);
            Thread.Sleep(50);
        }
    }

    internal class OasisTestSuite : IChipTestSuite
    {

        private const uint RegI2cId = 0x5000_0000;
        private const uint RegFlashCmd = 0x5009_0008;
        private const uint RegFlashStatus = 0x5009_0020;
        private const uint RegFlashStatusAlt = 0x5009_000C;
        private const uint RegFlashTxBase = 0x5009_1000;

        private readonly OasisRegisterChip _chip;
        private string _firmwareFilePath = string.Empty;
        private uint _flashSizeBytes = 0;

        private enum TEST_ITEMS
        {
            GPIO_DISABLE,
            GPIO_04_ABGR,
            GPIO_04_RETLDO,
            GPIO_04_MBGR,
            GPIO_04_DALDO,
            GPIO_16_32KOSC,
            NUM_TEST_ITEMS,
        }

        private enum AUTO_TEST_ITEMS
        {
            NUM_TEST_ITEMS,
        }

        private enum FW_DN_ITEMS
        {
            FLASH_ERASE,
            FLASH_WRITE,
            FLASH_READ,
            FLASH_VERIFY,
            FIRM_ON_CLEAR,
            RAM_WRITE,
            RAM_READ,
            RESET,
            NUM_TEST_ITEMS,
        }

        private enum CAL_ITEMS
        {
            TEST_ITEM,
            NUM_TEST_ITEMS,
        }

        private enum FLASH_CMD : byte
        {
            WRSR = 0x01,    
            PP = 0x02,      
            RDCMD = 0x03,   
            WRDI = 0x04,    
            RDSR = 0x05,    
            WREN = 0x06,    
            F_RD = 0x0B,    
            SE = 0x20,      
            BE32 = 0x52,    
            RSTEN = 0x66,   
            REMS = 0x90,    
            RST = 0x99,     
            RDID = 0x9F,    
            RES = 0xAB,     
            ENSO = 0xB1,    
            DP = 0xB9,      
            EXSO = 0xC1,    
            CE = 0xC7,      
            BE64 = 0xD8,    
        }

        public IReadOnlyList<ChipTestInfo> Tests
        {
            get;
        }

        public OasisTestSuite(OasisRegisterChip chip)
        {
            _chip = chip ?? throw new ArgumentNullException(nameof(chip));

            Tests = new[]
            {
                new ChipTestInfo("TEST.GPIO_DISABLE",   "GPIO Disable",      "GPIO Disable",        "TEST"),
                new ChipTestInfo("TEST.GPIO_04_ABGR",   "GPIO4 ABGR",        "GPIO4 to ABGR",       "TEST"),
                new ChipTestInfo("TEST.GPIO_04_RETLDO", "GPIO4 RETLDO",      "GPIO4 to RETLDO",     "TEST"),
                new ChipTestInfo("TEST.GPIO_04_MBGR",   "GPIO4 MBGR",        "GPIO4 to MBGR",       "TEST"),
                new ChipTestInfo("TEST.GPIO_04_DALDO",  "GPIO4 DALDO",       "GPIO4 to DALDO",      "TEST"),
                new ChipTestInfo("TEST.GPIO_16_32KOSC", "GPIO16 32K OSC",    "GPIO16 to 32KOSC",    "TEST"),

                new ChipTestInfo("FW.FLASH_ERASE",      "Flash Erase",       "NV Flash Erase",      "FW"),
                new ChipTestInfo("FW.FLASH_WRITE",      "Flash Write",       "NV Flash Download",   "FW"),
                new ChipTestInfo("FW.FLASH_READ",       "Flash Read",        "NV Flash Dump",       "FW"),
                new ChipTestInfo("FW.FLASH_VERIFY",     "Flash Verify",      "Flash Verify",        "FW"),
                new ChipTestInfo("FW.FIRM_ON_CLEAR",    "FirmOn Clear",      "0x0F Command",        "FW"),
                new ChipTestInfo("FW.RAM_WRITE",        "RAM Write",         "RAM Download",        "FW"),
                new ChipTestInfo("FW.RAM_READ",         "RAM Read",          "RAM Dump",            "FW"),
                new ChipTestInfo("FW.RESET",            "Reset Oasis",       "Chip Reset",          "FW"),
            };
        }

        public void SetFirmwareFilePath(string path) => _firmwareFilePath = path;

        public void SetFlashSize(uint size) => _flashSizeBytes = size;

        public async Task Run_TEST(string testId, Func<string, string, Task> log, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(testId))
            {
                await log("ERROR", "TestId is empty.");
                return;
            }

            await log("INFO", $"Oasis Test '{testId}' 시작");

            try
            {
                var parts = testId.Split('.', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    await log("ERROR", $"Invalid test id format: {testId}");
                    return;
                }

                string category = parts[0];
                string itemName = parts[1];

                switch (category)
                {
                    case "TEST":
                        await Run_TEST_CATEGORY(itemName, log, cancellationToken);
                        break;
                    case "AUTO":
                        await Run_AUTO_CATEGORY(itemName, log, cancellationToken);
                        break;
                    case "CAL":
                        await Run_CAL_CATEGORY(itemName, log, cancellationToken);
                        break;
                    case "FW":
                        await Run_FW_CATEGORY(itemName, log, cancellationToken);
                        break;
                    default:
                        await log("ERROR", $"Unknown category: {category}");
                        break;
                }
            }
            finally
            {
                await log("INFO", $"Oasis Test '{testId}' 종료");
            }
        }

        private async Task Run_TEST_CATEGORY(string itemName, Func<string, string, Task> log, CancellationToken ct)
        {
            if (!Enum.TryParse(itemName, out TEST_ITEMS item))
            {
                await log("ERROR", $"Unknown TEST item: {itemName}");
                return;
            }

            switch (item)
            {
                case TEST_ITEMS.GPIO_DISABLE:
                    await Set_GPIO_DISABLE(log, ct);
                    break;
                case TEST_ITEMS.GPIO_04_ABGR:
                    await Set_GPIO4_ABGR(true, log, ct);
                    break;
                case TEST_ITEMS.GPIO_04_RETLDO:
                    await Set_GPIO4_RETLDO(true, log, ct);
                    break;
                case TEST_ITEMS.GPIO_04_MBGR:
                    await Set_GPIO4_MBGR(true, log, ct);
                    break;
                case TEST_ITEMS.GPIO_04_DALDO:
                    await Set_GPIO4_DALDO(true, log, ct);
                    break;
                case TEST_ITEMS.GPIO_16_32KOSC:
                    await Set_GPIO16_32KOSC(true, log, ct);
                    break;
                default:
                    await log("ERROR", $"Unhandled TEST item: {itemName}");
                    break;
            }
        }

        private async Task Run_AUTO_CATEGORY(string itemName, Func<string, string, Task> log, CancellationToken ct)
        {
            if (!Enum.TryParse(itemName, out AUTO_TEST_ITEMS item))
            {
                await log("ERROR", $"Unknown AUTO item: {itemName}");
                return;
            }

            await log("INFO", $"AUTO test '{item}' is not implemented yet.");
            await Task.Delay(100, ct);
        }

        private async Task Run_CAL_CATEGORY(string itemName, Func<string, string, Task> log, CancellationToken ct)
        {
            if (!Enum.TryParse(itemName, out CAL_ITEMS item))
            {
                await log("ERROR", $"Unknown CAL item: {itemName}");
                return;
            }

            await log("INFO", $"CAL test '{item}' is not implemented yet.");
            await Task.Delay(100, ct);
        }

        private async Task Run_FW_CATEGORY(string itemName, Func<string, string, Task> log, CancellationToken ct)
        {
            if (!Enum.TryParse(itemName, out FW_DN_ITEMS item))
            {
                await log("ERROR", $"Unknown FW item: {itemName}");
                return;
            }

            switch (item)
            {
                case FW_DN_ITEMS.FLASH_ERASE:
                    await Run_FLASH_ERASE(log, ct);
                    break;
                case FW_DN_ITEMS.FLASH_WRITE:
                    await Run_FLASH_WRITE(log, ct);
                    break;
                case FW_DN_ITEMS.FLASH_READ:
                    await Run_FLASH_READ(log, ct);
                    break;
                case FW_DN_ITEMS.FLASH_VERIFY:
                    await Run_FLASH_VERIFY(log, ct);
                    break;
                case FW_DN_ITEMS.FIRM_ON_CLEAR:
                    await Run_FIRM_ON_CLEAR(log, ct);
                    break;
                case FW_DN_ITEMS.RAM_WRITE:
                    await Run_RAM_WRITE(log, ct);
                    break;
                case FW_DN_ITEMS.RAM_READ:
                    await Run_RAM_READ(log, ct);
                    break;
                case FW_DN_ITEMS.RESET:
                    await Run_RESET(log, ct);
                    break;
                default:
                    await log("INFO", $"FW item '{item}' is not implemented yet.");
                    break;
            }
        }

        private async Task Set_GPIO_DISABLE(Func<string, string, Task> log, CancellationToken ct)
        {
            try
            {
                await Set_GPIO4_ABGR(false, log, ct);
                await Set_GPIO16_32KOSC(false, log, ct);
                await Set_GPIO4_RETLDO(false, log, ct);
                await Set_GPIO4_MBGR(false, log, ct);
                await Set_GPIO4_DALDO(false, log, ct);
            }
            catch (Exception ex)
            {
                await log("ERROR", $"Error in Set_GPIO_DISABLE: {ex.Message}");
                throw;
            }
        }

        private async Task Set_GPIO4_ABGR(bool enable, Func<string, string, Task> log, CancellationToken ct)
        {
            try
            {
                uint regDC340050 = _chip.ReadRegister(0xDC34_0050);
                uint regDC340054 = _chip.ReadRegister(0xDC34_0054);
                uint regDC34006C = _chip.ReadRegister(0xDC34_006C);

                if (enable)
                {
                    _chip.WriteRegister(0xDC34_0050, regDC340050 | (1u << 15));
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_0054, regDC340054 | 15u);
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_006C, regDC34006C | (1u << 15));
                    await Task.Delay(10, ct);
                }
                else
                {
                    _chip.WriteRegister(0xDC34_0050, regDC340050 & 0x7FFFu);
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_0054, regDC340054 & 0xFFF0u);
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_006C, regDC34006C & 0x7FFFu);
                    await Task.Delay(10, ct);
                }
            }
            catch (Exception ex)
            {
                await log("ERROR", $"Error in Set_GPIO4_ABGR: {ex.Message}");
                throw;
            }
        }

        private async Task Set_GPIO16_32KOSC(bool enable, Func<string, string, Task> log, CancellationToken ct)
        {
            try
            {
                uint regDC340050 = _chip.ReadRegister(0xDC34_0050);
                uint regDC340054 = _chip.ReadRegister(0xDC34_0054);
                uint regDC34006C = _chip.ReadRegister(0xDC34_006C);

                if (enable)
                {
                    _chip.WriteRegister(0xDC34_0050, regDC340050 | (1u << 14));
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_0054, regDC340054 | (1u << 8));
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_006C, regDC34006C | (1u << 14));
                    await Task.Delay(10, ct);
                }
                else
                {
                    _chip.WriteRegister(0xDC34_0050, regDC340050 & 0xBFFFu);
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_0054, regDC340054 & 0xFEFFu);
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_006C, regDC34006C & 0xBFFFu);
                    await Task.Delay(10, ct);
                }
            }
            catch (Exception ex)
            {
                await log("ERROR", $"Error in Set_GPIO16_32KOSC: {ex.Message}");
                throw;
            }
        }

        private async Task Set_GPIO4_RETLDO(bool enable, Func<string, string, Task> log, CancellationToken ct)
        {
            try
            {
                uint regDC340050 = _chip.ReadRegister(0xDC34_0050);
                uint regDC340054 = _chip.ReadRegister(0xDC34_0054);
                uint regDC34006C = _chip.ReadRegister(0xDC34_006C);

                if (enable)
                {
                    _chip.WriteRegister(0xDC34_0050, regDC340050 | (1u << 13));
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_0054, regDC340054 | (0xFu << 4));
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_006C, regDC34006C | (1u << 13));
                    await Task.Delay(10, ct);
                }
                else
                {
                    _chip.WriteRegister(0xDC34_0050, regDC340050 & 0xDFFFu);
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_0054, regDC340054 & 0xFF0Fu);
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_006C, regDC34006C & 0xDFFFu);
                    await Task.Delay(10, ct);
                }
            }
            catch (Exception ex)
            {
                await log("ERROR", $"Error in Set_GPIO4_RETLDO: {ex.Message}");
                throw;
            }
        }

        private async Task Set_GPIO4_MBGR(bool enable, Func<string, string, Task> log, CancellationToken ct)
        {
            try
            {
                uint regDC340050 = _chip.ReadRegister(0xDC34_0050);
                uint regDC340054 = _chip.ReadRegister(0xDC34_0054);
                uint regDC34006C = _chip.ReadRegister(0xDC34_006C);

                if (enable)
                {
                    _chip.WriteRegister(0xDC34_0050, regDC340050 | (1u << 12));
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_0054, regDC340054 | (0xFu << 16));
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_006C, regDC34006C | (1u << 12));
                    await Task.Delay(10, ct);
                }
                else
                {
                    _chip.WriteRegister(0xDC34_0050, regDC340050 & 0xEFFFu);
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_0054, regDC340054 & 0xF0FFu);
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_006C, regDC34006C & 0xEFFFu);
                    await Task.Delay(10, ct);
                }
            }
            catch (Exception ex)
            {
                await log("ERROR", $"Error in Set_GPIO4_MBGR: {ex.Message}");
                throw;
            }
        }

        private async Task Set_GPIO4_DALDO(bool enable, Func<string, string, Task> log, CancellationToken ct)
        {
            try
            {
                uint regDC340050 = _chip.ReadRegister(0xDC34_0050);
                uint regDC340054 = _chip.ReadRegister(0xDC34_0054);
                uint regDC34006C = _chip.ReadRegister(0xDC34_006C);

                if (enable)
                {
                    _chip.WriteRegister(0xDC34_0050, regDC340050 | (1u << 11));
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_0054, regDC340054 | (0xFu << 20));
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_006C, regDC34006C | (1u << 11));
                    await Task.Delay(10, ct);
                }
                else
                {
                    _chip.WriteRegister(0xDC34_0050, regDC340050 & 0xF7FFu);
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_0054, regDC340054 & 0x0FFFFu);
                    await Task.Delay(10, ct);

                    _chip.WriteRegister(0xDC34_006C, regDC34006C & 0xF7FFu);
                    await Task.Delay(10, ct);
                }
            }
            catch (Exception ex)
            {
                await log("ERROR", $"Error in Set_GPIO4_DALDO: {ex.Message}");
                throw;
            }
        }

        private async Task Run_FLASH_ERASE(Func<string, string, Task> log, CancellationToken ct)
        {
            try
            {
                if (!await Check_I2C_ID(log, ct))
                    return;

                _chip.HaltMcu();

                await log("INFO", "Start FLASH_ERASE (8 sectors of 64KB).");

                for (uint num = 0; num < 8; num++)
                {
                    ct.ThrowIfCancellationRequested();

                    uint secAddr = num * 0x10000u;
                    await log("INFO", $"Erase sector #{num} @ 0x{secAddr:X8}");

                    uint cmd = ((uint)FLASH_CMD.BE64 << 24) | (secAddr & 0x00FFFFFFu);
                    _chip.WriteRegister(RegFlashCmd, cmd);

                    bool ok = await Wait_FLASH_READY(log, ct);
                    if (!ok)
                    {
                        await log("ERROR", $"Sector erase timeout @ 0x{secAddr:X8}");
                        return;
                    }
                }

                await log("INFO", "FLASH_ERASE completed successfully.");
            }
            finally
            {
                _chip.ResetMcu();
            }
        }

        private async Task Run_FLASH_WRITE(Func<string, string, Task> log, CancellationToken ct)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_firmwareFilePath))
                {
                    await log("ERROR", "Firmware file path is not set.");
                    return;
                }

                byte[] fwData;
                try
                {
                    fwData = File.ReadAllBytes(_firmwareFilePath);
                }
                catch (Exception ex)
                {
                    await log("ERROR", $"Failed to read firmware file: {ex.Message}");
                    return;
                }

                if (fwData.Length == 0)
                {
                    await log("ERROR", "Firmware file is empty.");
                    return;
                }

                if (!await Check_I2C_ID(log, ct))
                    return;

                await log("INFO", "FLASH_WRITE: Start FLASH_ERASE before FLASH_WRITE.");
                await Run_FLASH_ERASE(log, ct);

                if (!await Check_I2C_ID(log, ct))
                {
                    await log("ERROR", "FLASH_WRITE: After erase, I2C ID check failed. Abort write.");
                    return;
                }

                _chip.HaltMcu();

                const int PageSize = 256;
                byte[] pageBuffer = new byte[PageSize];
                byte[] readBuffer = new byte[PageSize];

                for (uint flashAddress = 0; flashAddress < fwData.Length; flashAddress += (uint)PageSize)
                {
                    ct.ThrowIfCancellationRequested();

                    for (int i = 0; i < PageSize; i++)
                    {
                        int srcIndex = (int)flashAddress + i;
                        pageBuffer[i] = srcIndex < fwData.Length ? fwData[srcIndex] : (byte)0xFF;
                    }

                    if (flashAddress % 0x1000 == 0)
                        await log("INFO", $"Write page @ 0x{flashAddress:X8}");

                    if (!await Write_MEMORY_NVM(flashAddress, pageBuffer, log, ct))
                        return;

                    Thread.Sleep(1);

                    for (uint i = 0; i < PageSize; i += 4)
                    {
                        uint data = _chip.ReadRegister(flashAddress + i);
                        for (int j = 0; j < 4; j++)
                        {
                            int idx = (int)i + j;
                            if (idx < PageSize)
                                readBuffer[idx] = (byte)((data >> (8 * j)) & 0xFF);
                        }
                    }

                    for (int i = 0; i < PageSize; i++)
                    {
                        byte expected = ((int)flashAddress + i < fwData.Length) ? fwData[(int)flashAddress + i] : (byte)0xFF;
                        if (readBuffer[i] != expected)
                        {
                            await log("ERROR", $"Verify failed @ 0x{flashAddress + (uint)i:X8}: W=0x{expected:X2}, R=0x{readBuffer[i]:X2}");
                            return;
                        }
                    }
                }

                await log("INFO", "FLASH_WRITE completed successfully.");
            }
            finally
            {
                _chip.ResetMcu();
            }
        }

        private async Task Run_FLASH_READ(Func<string, string, Task> log, CancellationToken ct)
        {
            try
            {
                await log("INFO", "Start FLASH_READ (Dump NV memory).");

                if (!await Check_I2C_ID(log, ct))
                    return;

                _chip.HaltMcu();

                uint dumpSize = _flashSizeBytes;
                if (dumpSize <= 0)
                {
                    if (!string.IsNullOrWhiteSpace(_firmwareFilePath) && File.Exists(_firmwareFilePath))
                    {
                        dumpSize = (uint)new FileInfo(_firmwareFilePath).Length;
                    }
                    else
                    {
                        dumpSize = 256 * 1024;
                        await log("INFO", $"Dump size is not set. Use default {dumpSize} bytes (256KB).");
                    }
                }

                const int PageSize = 4;
                var firmwareData = new List<byte>((int)dumpSize);

                for (uint addr = 0; addr < dumpSize; addr += PageSize)
                {
                    ct.ThrowIfCancellationRequested();

                    if ((addr % 0x1000) == 0)
                        await log("INFO", $"Read Flash @ 0x{addr:X8}");

                    uint rcv = _chip.ReadRegister(addr);

                    firmwareData.Add((byte)(rcv & 0xFF));
                    firmwareData.Add((byte)((rcv >> 8) & 0xFF));
                    firmwareData.Add((byte)((rcv >> 16) & 0xFF));
                    firmwareData.Add((byte)((rcv >> 24) & 0xFF));
                }

                string time = DateTime.Now.ToString("HHmmss");
                string fileName = $"ReadFlash_{time}.bin";
                try
                {
                    File.WriteAllBytes(fileName, firmwareData.ToArray());
                }
                catch (Exception ex)
                {
                    await log("ERROR", $"Failed to write dump file '{fileName}': {ex.Message}");
                    return;
                }

                await log("INFO", $"FLASH_READ completed. File = {fileName}, Size = {firmwareData.Count} bytes.");
            }
            finally
            {
                _chip.ResetMcu();
            }
        }

        private async Task Run_FLASH_VERIFY(Func<string, string, Task> log, CancellationToken ct)
        {
            uint flashSize = _flashSizeBytes;
            if (flashSize == 0)
            {
                await log("ERROR", "Flash verify size is not set. (SetFlashSize required)");
                return;
            }

            if (!await Check_I2C_ID(log, ct))
                return;

            try
            {
                const int PageSize = 256;
                byte[] pageBuffer = new byte[PageSize];
                byte[] readBuffer = new byte[PageSize];
                byte[] patterns = new byte[] { 0xAA, 0x55 };

                await log("INFO", $"Start FW.FLASH_VERIFY. Size={flashSize} bytes");

                for (int p = 0; p < patterns.Length; p++)
                {
                    ct.ThrowIfCancellationRequested();

                    byte pattern = patterns[p];
                    await log("INFO", $"Pattern {p + 1}/{patterns.Length}: 0x{pattern:X2}");

                    await log("INFO", "Erase NV memory...");
                    await Run_FLASH_ERASE(log, ct);
                    _chip.HaltMcu();

                    await log("INFO", "Erase verify...");
                    for (uint addr = 0; addr < flashSize; addr += PageSize)
                    {
                        ct.ThrowIfCancellationRequested();

                        if ((addr % 0x1000) == 0)
                            await log("INFO", $"Erase verify @ 0x{addr:X8}");

                        int len = (int)Math.Min((uint)PageSize, flashSize - addr);
                        readBuffer = new byte[len];
                        readBuffer = Read_FLASH_BUFFER(addr, len);

                        for (int i = 0; i < len; i++)
                        {
                            if (readBuffer[i] != 0xFF)
                            {
                                await log("ERROR", $"Fail to Erase: Addr=0x{(addr + (uint)i):X8}, Read=0x{readBuffer[i]:X2}");
                                return;
                            }
                        }
                    }
                    await log("INFO", "Erase verify OK.");

                    await log("INFO", "Write pattern...");
                    for (uint addr = 0; addr < flashSize; addr += PageSize)
                    {
                        ct.ThrowIfCancellationRequested();

                        if ((addr % 0x1000) == 0)
                            await log("INFO", $"Write pattern @ 0x{addr:X8}");

                        int len = (int)Math.Min((uint)PageSize, flashSize - addr);

                        for (int i = 0; i < len; i++)
                            pageBuffer[i] = pattern;

                        if (!await Write_MEMORY_NVM(addr, pageBuffer, log, ct))
                        {
                            await log("ERROR", $"Fail to Write: Addr=0x{addr:X8}");
                            return;
                        }

                        Thread.Sleep(1);

                        readBuffer = new byte[len];
                        readBuffer = Read_FLASH_BUFFER(addr, len);

                        for (int i = 0; i < len; i++)
                        {
                            if (readBuffer[i] != pattern)
                            {
                                await log("ERROR", $"Fail to Verify: Addr=0x{(addr + (uint)i):X8}, W=0x{pattern:X2}, R=0x{readBuffer[i]:X2}");
                                return;
                            }
                        }
                    }
                    await log("INFO", "Write OK.");
                }

                await log("INFO", "FW.FLASH_VERIFY completed successfully.");
            }
            finally
            {
                _chip.ResetMcu();
            }
        }

        private byte[] Read_FLASH_BUFFER(uint flashAddress, int bufferLen)
        {
            byte[] buffer = new byte[bufferLen];

            for (int i = 0; i < bufferLen; i += 4)
            {
                uint data = _chip.ReadRegister(flashAddress + (uint)i);
                buffer[i + 0] = (byte)(data & 0xFF);
                buffer[i + 1] = (byte)((data >> 8) & 0xFF);
                buffer[i + 2] = (byte)((data >> 16) & 0xFF);
                buffer[i + 3] = (byte)((data >> 24) & 0xFF);
            }

            return buffer;
        }

        private async Task Run_FIRM_ON_CLEAR(Func<string, string, Task> log, CancellationToken ct)
        {
            await log("INFO", "FIRM_ON_CLEAR is not implemented in V4 yet.");
            await Task.Delay(100, ct);
        }

        private async Task Run_RAM_WRITE(Func<string, string, Task> log, CancellationToken ct)
        {
            await log("INFO", "RAM_WRITE is not implemented in V4 yet.");
            await Task.Delay(100, ct);
        }

        private async Task Run_RAM_READ(Func<string, string, Task> log, CancellationToken ct)
        {
            await log("INFO", "RAM_READ is not implemented in V4 yet.");
            await Task.Delay(100, ct);
        }

        private async Task Run_RESET(Func<string, string, Task> log, CancellationToken ct)
        {
            await log("INFO", "Reset Oasis (MCU reset).");

            if (!await Check_I2C_ID(log, ct))
                return;

            _chip.ResetMcu();

            await Task.Delay(100, ct);

            await log("INFO", "Reset command has been sent.");
        }

        private async Task<bool> Check_I2C_ID(Func<string, string, Task> log, CancellationToken ct)
        {
            uint id = _chip.ReadRegister(RegI2cId);
            uint ipId = id >> 12;

            if (ipId != 0x02021)
            {
                await log("ERROR", $"Fail to Check I2C IP ID. R = 0x{ipId:X5}");
                return false;
            }

            await log("INFO", "CheckI2C_ID OK.");
            return true;
        }

        private async Task<bool> Wait_FLASH_READY(Func<string, string, Task> log, CancellationToken ct, int maxLoopCount = 20, int delayMs = 200)
        {
            for (int cnt = 0; cnt < maxLoopCount; cnt++)
            {
                ct.ThrowIfCancellationRequested();

                uint status;
                try
                {
                    status = _chip.ReadRegister(RegFlashStatus);
                }
                catch (Exception ex)
                {
                    await log("ERROR", $"WaitFlashReady: ReadRegister(0x50090020) failed: {ex.Message}");
                    return false;
                }
                uint busy = status & 0x01u;

                if (busy == 0)
                    return true;

                await Task.Delay(delayMs, ct);
            }

            await log("ERROR", "Flash controller did not become ready within timeout.");
            return false;
        }

        private async Task<bool> Write_MEMORY_NVM(uint flashAddress, byte[] pageBuffer, Func<string, string, Task> log, CancellationToken ct)
        {
            const byte FlashCmdPageProgram = (byte)FLASH_CMD.PP;

            for (int i = 0; i < pageBuffer.Length; i += 4)
            {
                ct.ThrowIfCancellationRequested();

                uint word = (uint)(pageBuffer[i]
                    | (pageBuffer[Math.Min(i + 1, pageBuffer.Length - 1)] << 8)
                    | (pageBuffer[Math.Min(i + 2, pageBuffer.Length - 1)] << 16)
                    | (pageBuffer[Math.Min(i + 3, pageBuffer.Length - 1)] << 24));

                _chip.WriteRegister(RegFlashTxBase + (uint)i, word);
            }

            _chip.WriteRegister(RegFlashCmd, (uint)((FlashCmdPageProgram << 24) | (flashAddress & 0xFFFFFFu)));

            for (int retry = 0; retry < 2000; retry++)
            {
                ct.ThrowIfCancellationRequested();

                uint status = _chip.ReadRegister(RegFlashStatusAlt);
                if ((status & 0x1u) == 0)
                    return true;

                await Task.Delay(1, ct);
            }

            await log("ERROR", "Timeout waiting for flash page program to complete.");
            return false;
        }

    }

}
