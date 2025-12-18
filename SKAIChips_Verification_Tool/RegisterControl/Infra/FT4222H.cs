using System.Runtime.InteropServices;

namespace SKAIChips_Verification_Tool.RegisterControl.Infra
{
    public sealed class FT4222H : IDisposable
    {
        private IntPtr _handle = IntPtr.Zero;
        private bool _disposed;

        public bool IsOpen => _handle != IntPtr.Zero;

        public ushort CurrentI2cSpeedKbps
        {
            get; private set;
        }
        public int CurrentSpiClockKHz
        {
            get; private set;
        }

        public bool Open(uint deviceIndex)
        {
            ThrowIfDisposed();

            if (IsOpen)
                return true;

            uint deviceCount = 0;
            var status = FT_CreateDeviceInfoList(ref deviceCount);
            if (status != FT_STATUS.FT_OK || deviceCount == 0 || deviceIndex >= deviceCount)
                return false;

            status = FT_Open(deviceIndex, out _handle);
            if (status != FT_STATUS.FT_OK || _handle == IntPtr.Zero)
            {
                _handle = IntPtr.Zero;
                return false;
            }

            return true;
        }

        public void Close()
        {
            ThrowIfDisposed();

            if (_handle != IntPtr.Zero)
            {
                
                FT4222_I2CMaster_Reset(_handle);
                FT4222_SPI_Reset(_handle);
                FT4222_UnInitialize(_handle);
                FT_Close(_handle);

                _handle = IntPtr.Zero;
                CurrentI2cSpeedKbps = 0;
                CurrentSpiClockKHz = 0;
            }
        }

        public bool I2cInit(ushort kbps)
        {
            ThrowIfDisposed();
            EnsureOpen();

            var status = FT4222_I2CMaster_Init(_handle, kbps);
            if (status != FT4222_STATUS.FT4222_OK)
                return false;

            FT4222_I2CMaster_Reset(_handle);
            CurrentI2cSpeedKbps = kbps;
            return true;
        }

        public ushort I2cWrite(ushort slaveAddress, byte[] data)
        {
            ThrowIfDisposed();
            EnsureOpen();

            if (data == null || data.Length == 0)
                return 0;

            ushort transferred = 0;
            var status = FT4222_I2CMaster_Write(
                _handle,
                slaveAddress,
                data,
                (ushort)data.Length,
                ref transferred);

            if (status != FT4222_STATUS.FT4222_OK)
                throw new InvalidOperationException(
                    $"FT4222 I2C write failed. Status={status}, Written={transferred}");

            return transferred;
        }

        public ushort I2cRead(ushort slaveAddress, byte[] buffer)
        {
            ThrowIfDisposed();
            EnsureOpen();

            if (buffer == null || buffer.Length == 0)
                return 0;

            ushort transferred = 0;
            var status = FT4222_I2CMaster_Read(
                _handle,
                slaveAddress,
                buffer,
                (ushort)buffer.Length,
                ref transferred);

            if (status != FT4222_STATUS.FT4222_OK)
                throw new InvalidOperationException(
                    $"FT4222 I2C read failed. Status={status}, Read={transferred}");

            return transferred;
        }

        public bool SpiInitMaster(int sckRateKHz, int mode)
        {
            ThrowIfDisposed();
            EnsureOpen();

            GetSpiClockParameter(sckRateKHz, out SystemClock sysClk, out SPI_ClockDivider clkDiv);

            var cpol = (mode == 2 || mode == 3)
                ? SPI_ClkPolarity.ACTIVE_HIGH
                : SPI_ClkPolarity.ACTIVE_LOW;

            var cpha = (mode == 1 || mode == 3)
                ? SPI_ClkPhase.CLK_TRAILING
                : SPI_ClkPhase.CLK_LEADING;

            var st = FT4222_SetClock(_handle, sysClk);
            if (st != FT4222_STATUS.FT4222_OK)
                return false;

            st = FT4222_SPIMaster_Init(
                _handle,
                SPI_Mode.SPI_IO_SINGLE,
                clkDiv,
                cpol,
                cpha,
                0x01); 

            if (st != FT4222_STATUS.FT4222_OK)
                return false;

            FT4222_SPI_SetDrivingStrength(
                _handle,
                SPI_DrivingStrength.DS_4MA,
                SPI_DrivingStrength.DS_4MA,
                SPI_DrivingStrength.DS_4MA);

            FT4222_SetSuspendOut(_handle, false);
            FT4222_SetWakeUpInterrupt(_handle, false);

            switch (sysClk)
            {
                case SystemClock.SYS_CLK_80:
                    CurrentSpiClockKHz = 80000 / (1 << (int)clkDiv);
                    break;
                case SystemClock.SYS_CLK_60:
                    CurrentSpiClockKHz = 60000 / (1 << (int)clkDiv);
                    break;
                case SystemClock.SYS_CLK_48:
                    CurrentSpiClockKHz = 48000 / (1 << (int)clkDiv);
                    break;
                case SystemClock.SYS_CLK_24:
                    CurrentSpiClockKHz = 24000 / (1 << (int)clkDiv);
                    break;
            }

            return true;
        }

        public ushort SpiWriteBytes(byte[] writeBuf, bool endTransaction)
        {
            ThrowIfDisposed();
            EnsureOpen();

            if (writeBuf == null || writeBuf.Length == 0)
                return 0;

            ushort sizeTransferred = 0;
            var st = FT4222_SPIMaster_SingleWrite(
                _handle,
                ref writeBuf[0],
                (ushort)writeBuf.Length,
                ref sizeTransferred,
                endTransaction);

            if (st != FT4222_STATUS.FT4222_OK)
                throw new InvalidOperationException(
                    $"FT4222 SPI write failed. Status={st}, Written={sizeTransferred}");

            return sizeTransferred;
        }

        public ushort SpiReadBytes(byte[] readBuf, bool endTransaction)
        {
            ThrowIfDisposed();
            EnsureOpen();

            if (readBuf == null || readBuf.Length == 0)
                return 0;

            ushort sizeRead = 0;
            var st = FT4222_SPIMaster_SingleRead(
                _handle,
                ref readBuf[0],
                (ushort)readBuf.Length,
                ref sizeRead,
                endTransaction);

            if (st != FT4222_STATUS.FT4222_OK)
                throw new InvalidOperationException(
                    $"FT4222 SPI read failed. Status={st}, Read={sizeRead}");

            return sizeRead;
        }

        public ushort SpiReadWriteBytes(byte[] readBuf, byte[] writeBuf, bool endTransaction)
        {
            ThrowIfDisposed();
            EnsureOpen();

            if (writeBuf == null || writeBuf.Length == 0)
                return 0;
            if (readBuf == null || readBuf.Length < writeBuf.Length)
                throw new ArgumentException("readBuf length must be >= writeBuf length");

            ushort sizeTransferred = 0;
            var st = FT4222_SPIMaster_SingleReadWrite(
                _handle,
                ref readBuf[0],
                ref writeBuf[0],
                (ushort)writeBuf.Length,
                ref sizeTransferred,
                endTransaction);

            if (st != FT4222_STATUS.FT4222_OK)
                throw new InvalidOperationException(
                    $"FT4222 SPI read/write failed. Status={st}, Transferred={sizeTransferred}");

            return sizeTransferred;
        }

        private void GetSpiClockParameter(int sckRateKHz, out SystemClock sysClk, out SPI_ClockDivider clkDiv)
        {
            sysClk = SystemClock.SYS_CLK_80;
            clkDiv = SPI_ClockDivider.CLK_DIV_8;

            for (int i = 1; i < 8; i++)
            {
                if (sckRateKHz >= 80000 / (1 << i))
                {
                    sysClk = SystemClock.SYS_CLK_80;
                    clkDiv = (SPI_ClockDivider)i;
                    break;
                }

                if (sckRateKHz >= 60000 / (1 << i))
                {
                    sysClk = SystemClock.SYS_CLK_60;
                    clkDiv = (SPI_ClockDivider)i;
                    break;
                }

                if (sckRateKHz >= 48000 / (1 << i))
                {
                    sysClk = SystemClock.SYS_CLK_48;
                    clkDiv = (SPI_ClockDivider)i;
                    break;
                }
            }
        }

        private void EnsureOpen()
        {
            if (!IsOpen)
                throw new InvalidOperationException("FT4222H device is not open.");
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FT4222H));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Close();
            _disposed = true;
        }

        private enum FT_STATUS : uint
        {
            FT_OK = 0,
            FT_INVALID_HANDLE,
            FT_DEVICE_NOT_FOUND,
            FT_DEVICE_NOT_OPENED,
            FT_IO_ERROR,
            FT_INSUFFICIENT_RESOURCES,
            FT_INVALID_PARAMETER,
            FT_INVALID_BAUD_RATE,
            FT_DEVICE_NOT_OPENED_FOR_ERASE,
            FT_DEVICE_NOT_OPENED_FOR_WRITE,
            FT_FAILED_TO_WRITE_DEVICE,
            FT_EEPROM_READ_FAILED,
            FT_EEPROM_WRITE_FAILED,
            FT_EEPROM_ERASE_FAILED,
            FT_EEPROM_NOT_PRESENT,
            FT_EEPROM_NOT_PROGRAMMED,
            FT_INVALID_ARGS,
            FT_NOT_SUPPORTED,
            FT_OTHER_ERROR,
            FT_DEVICE_LIST_NOT_READY
        }

        private enum FT4222_STATUS
        {
            FT4222_OK = 0,
            FT4222_INVALID_HANDLE = 1,
            FT4222_DEVICE_NOT_FOUND = 2,
            FT4222_DEVICE_NOT_OPENED = 3,
            FT4222_IO_ERROR = 4,
            FT4222_INSUFFICIENT_RESOURCES = 5,
            FT4222_INVALID_PARAMETER = 6,
            FT4222_INVALID_BAUD_RATE = 7,
            FT4222_DEVICE_NOT_OPENED_FOR_ERASE = 8,
            FT4222_DEVICE_NOT_OPENED_FOR_WRITE = 9,
            FT4222_FAILED_TO_WRITE_DEVICE = 10,
            FT4222_EEPROM_READ_FAILED = 11,
            FT4222_EEPROM_WRITE_FAILED = 12,
            FT4222_EEPROM_ERASE_FAILED = 13,
            FT4222_EEPROM_NOT_PRESENT = 14,
            FT4222_EEPROM_NOT_PROGRAMMED = 15,
            FT4222_INVALID_ARGS = 16,
            FT4222_NOT_SUPPORTED = 17,
            FT4222_OTHER_ERROR = 18,
            FT4222_DEVICE_LIST_NOT_READY = 19
        }

        private enum SystemClock
        {
            SYS_CLK_60,
            SYS_CLK_24,
            SYS_CLK_48,
            SYS_CLK_80
        }

        private enum SPI_Mode
        {
            SPI_IO_NONE = 0,
            SPI_IO_SINGLE = 1,
            SPI_IO_DUAL = 2,
            SPI_IO_QUAD = 4
        }

        private enum SPI_ClockDivider
        {
            CLK_NONE,
            CLK_DIV_2,
            CLK_DIV_4,
            CLK_DIV_8,
            CLK_DIV_16,
            CLK_DIV_32,
            CLK_DIV_64,
            CLK_DIV_128,
            CLK_DIV_256,
            CLK_DIV_512
        }

        private enum SPI_ClkPolarity
        {
            ACTIVE_LOW,
            ACTIVE_HIGH
        }

        private enum SPI_ClkPhase
        {
            CLK_LEADING,
            CLK_TRAILING
        }

        private enum SPI_DrivingStrength
        {
            DS_4MA,
            DS_8MA,
            DS_12MA,
            DS_16MA
        }

        [DllImport("ftd2xx.dll")]
        private static extern FT_STATUS FT_CreateDeviceInfoList(ref uint numDevices);

        [DllImport("ftd2xx.dll")]
        private static extern FT_STATUS FT_Open(uint deviceIndex, out IntPtr ftHandle);

        [DllImport("ftd2xx.dll")]
        private static extern FT_STATUS FT_Close(IntPtr ftHandle);

        [DllImport("LibFT4222-64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT4222_STATUS FT4222_UnInitialize(IntPtr ftHandle);

        [DllImport("LibFT4222-64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT4222_STATUS FT4222_SetClock(IntPtr ftHandle, SystemClock clk);

        [DllImport("LibFT4222-64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT4222_STATUS FT4222_SPI_SetDrivingStrength(
            IntPtr ftHandle,
            SPI_DrivingStrength clkStrength,
            SPI_DrivingStrength ioStrength,
            SPI_DrivingStrength ssoStrength);

        [DllImport("LibFT4222-64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT4222_STATUS FT4222_SetSuspendOut(IntPtr ftHandle, bool enable);

        [DllImport("LibFT4222-64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT4222_STATUS FT4222_SetWakeUpInterrupt(IntPtr ftHandle, bool enable);

        [DllImport("LibFT4222-64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT4222_STATUS FT4222_SPIMaster_Init(
            IntPtr ftHandle,
            SPI_Mode ioLine,
            SPI_ClockDivider clock,
            SPI_ClkPolarity cpol,
            SPI_ClkPhase cpha,
            byte ssoMap);

        [DllImport("LibFT4222-64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT4222_STATUS FT4222_SPIMaster_SingleRead(
            IntPtr ftHandle,
            ref byte buffer,
            ushort bytesToRead,
            ref ushort sizeOfRead,
            bool isEndTransaction);

        [DllImport("LibFT4222-64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT4222_STATUS FT4222_SPIMaster_SingleWrite(
            IntPtr ftHandle,
            ref byte buffer,
            ushort bytesToWrite,
            ref ushort sizeTransferred,
            bool isEndTransaction);

        [DllImport("LibFT4222-64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT4222_STATUS FT4222_SPIMaster_SingleReadWrite(
            IntPtr ftHandle,
            ref byte readBuffer,
            ref byte writeBuffer,
            ushort bufferSize,
            ref ushort sizeTransferred,
            bool isEndTransaction);

        [DllImport("LibFT4222-64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT4222_STATUS FT4222_SPI_Reset(IntPtr ftHandle);

        [DllImport("LibFT4222-64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT4222_STATUS FT4222_I2CMaster_Init(IntPtr ftHandle, ushort kbps);

        [DllImport("LibFT4222-64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT4222_STATUS FT4222_I2CMaster_Reset(IntPtr ftHandle);

        [DllImport("LibFT4222-64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT4222_STATUS FT4222_I2CMaster_Read(
            IntPtr ftHandle,
            ushort deviceAddress,
            [Out] byte[] buffer,
            ushort sizeToTransfer,
            ref ushort sizeTransferred);

        [DllImport("LibFT4222-64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT4222_STATUS FT4222_I2CMaster_Write(
            IntPtr ftHandle,
            ushort deviceAddress,
            byte[] buffer,
            ushort sizeToTransfer,
            ref ushort sizeTransferred);

    }
}
