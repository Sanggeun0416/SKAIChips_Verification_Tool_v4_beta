using LibUsbDotNet;
using LibUsbDotNet.Main;
using System.Data;
using System.IO.Ports;

namespace BlueTooth
{
    namespace HCI
    {
        public class HCIManager
        {
            public enum ComnType
            {
                None,
                Serial,
                USB,
            }
            public enum PacketType
            {
                HCI_No_Packet = 0x00,
                HCI_Command_Packet = 0x01,
                HCI_ACL_Data_Packet = 0x02,
                HCI_Synchronous_Packet = 0x03,
                HCI_Event_Packet = 0x04,
            }

            public static string[] ErrorCodes = new string[] {
                "Success",
                "Unknown HCI Command",
                "Unknown Connection Identifier",
                "Hardware Failure",
                "Page Timeout",
                "Authentication Failure",
                "PIN or Key Missing",
                "Memory Capacity Exceeded",
                "Connection Timeout",
                "Connection Limit Exceeded",
                "Synchronous Connection Limit To A Device Exceeded",
                "ACL Connection Already Exists",
                "Command Disallowed",
                "Connection Rejected due to Limited Resources",
                "Connection Rejected Due To Security Reasons",
                "Connection Rejected due to Unacceptable BD_ADDR",
                "Connection Accept Timeout Exceeded",
                "Unsupported Feature or Parameter Value",
                "Invalid HCI Command Parameters",
                "Remote User Terminated Connection",
                "Remote Device Terminated Connection due to Low Resources",
                "Remote Device Terminated Connection due to Power Off",
                "Connection Terminated By Local Host",
                "Repeated Attempts",
                "Pairing Not Allowed",
                "Unknown LMP PDU",
                "Unsupported Remote Feature / Unsupported LMP Feature",
                "SCO Offset Rejected",
                "SCO Interval Rejected",
                "SCO Air Mode Rejected",
                "Invalid LMP Parameters / Invalid LL Parameters",
                "Unspecified Error",
                "Unsupported LMP Parameter Value / Unsupported LL Parameter Value",
                "Role Change Not Allowed",
                "LMP Response Timeout / LL Response Timeout",
                "LMP Error Transaction Collision",
                "LMP PDU Not Allowed",
                "Encryption Mode Not Acceptable",
                "Link Key cannot be Changed",
                "Requested QoS Not Supported",
                "Instant Passed",
                "Pairing With Unit Key Not Supported",
                "Different Transaction Collision",
                "Reserved",
                "QoS Unacceptable Parameter",
                "QoS Rejected",
                "Channel Classification Not Supported",
                "Insufficient Security",
                "Parameter Out Of Mandatory Range",
                "Reserved",
                "Role Switch Pending",
                "Reserved",
                "Reserved Slot Violation",
                "Role Switch Failed",
                "Extended Inquiry Response Too Large",
                "Secure Simple Pairing Not Supported By Host",
                "Host Busy - Pairing",
                "Connection Rejected due to No Suitable Channel Found",
                "Controller Busy",
                "Unacceptable Connection Parameters",
                "Directed Advertising Timeout",
                "Connection Terminated due to MIC Failure",
                "Connection Failed to be Established",
                "MAC Connection Failed",
                "Coarse Clock Adjustment Rejected but Will Try to Adjust Using Clock Dragging"};

            public static string[] CoreVersion = new string[] {
                "Bluetooth Core Specification 1.0b",
                "Bluetooth Core Specification 1.1",
                "Bluetooth Core Specification 1.2",
                "Bluetooth Core Specification 2.0+EDR",
                "Bluetooth Core Specification 2.1+EDR",
                "Bluetooth Core Specification 3.0+HS",
                "Bluetooth Core Specification 4.0",
                "Bluetooth Core Specification 4.1",
                "Bluetooth Core Specification 4.2"
            };

            private System.IO.Ports.SerialPort UART = null;
            private LibUsbDotNet.UsbDevice USB;
            private List<LibUsbDotNet.Main.UsbRegistry> UsbDevices = new List<LibUsbDotNet.Main.UsbRegistry>();
            private LibUsbDotNet.Main.UsbSetupPacket UsbSetupPck = new LibUsbDotNet.Main.UsbSetupPacket(0x20, 0x00, 0x00, 0x00, 4);
            private LibUsbDotNet.UsbEndpointReader UsbEventReader;
            private LibUsbDotNet.UsbEndpointWriter UsbDataWriter;
            private LibUsbDotNet.UsbEndpointReader UsbDataReader;

            private List<UsbEndpointWriter> UsbIsochWriter = new List<UsbEndpointWriter>();
            private List<UsbEndpointReader> UsbIsochReader = new List<UsbEndpointReader>();
            private bool Opened = false;
            private string ConnectionMessage = "Disconnect";
            private ComnType CurComnType = ComnType.None;
            private Queue<byte> RcvQueue = new Queue<byte>(8192);
            private LinkLayerState CurLLState = LinkLayerState.Unknown;

            private List<CommandGroup> CmdGroupList = new List<CommandGroup>();
            private EventCollection EvtCollection = new EventCollection();

            public DataGridView HciLogView
            {
                get; set;
            }
            public DataGridView ConnLogView
            {
                get; set;
            }
            private DataTable dtHciLog = new DataTable("HCI Log");

            private System.Threading.ManualResetEvent HCIResetEvent = new ManualResetEvent(true);

            public List<ConnectionInfo> AdvertisingReports
            {
                get; set;
            }

            public HCIManager()
            {
                InitCommand();
                InitEvents();

                AdvertisingReports = new List<ConnectionInfo>();
            }
            ~HCIManager()
            {
                if (Opened)
                    Close();
            }

            public bool IsOpen
            {
                get
                {
                    return Opened;
                }
            }

            public string ConnMessage
            {
                get
                {
                    return ConnectionMessage;
                }
            }

            public List<CommandGroup> HCICommands
            {
                get
                {
                    return CmdGroupList;
                }
                set
                {
                    CmdGroupList = value;
                }
            }

            public EventCollection HCIEvents
            {
                get
                {
                    return EvtCollection;
                }
                set
                {
                    EvtCollection = value;
                }
            }

            public DataGridView HCILog_DataGridView
            {
                get
                {
                    return HciLogView;
                }
                set
                {
                    HciLogView = value;
                    if (HciLogView != null)
                        InitHciLogView();
                }
            }

            public DataTable HCILog
            {
                get
                {
                    return dtHciLog;
                }
            }

            public string[] SearchSerialPort()
            {
                string[] PortNameAry = SerialPort.GetPortNames();
                List<string> ValidPorts = new List<string>();

                using (SerialPort sp = new SerialPort())
                {
                    foreach (string PortName in PortNameAry)
                    {
                        try
                        {
                            sp.PortName = PortName;
                            sp.Open();
                            if (sp.IsOpen)
                                ValidPorts.Add(PortName);
                            sp.Close();
                        }
                        catch { }
                    }
                }
                return ValidPorts.ToArray();
            }

            public string[] SearchUsbDevices()
            {
                List<string> ValidDevices = new List<string>();

                UsbDevices.Clear();
                for (int i = 0; i < LibUsbDotNet.UsbDevice.AllDevices.Count; i++)
                {
                    if (LibUsbDotNet.UsbDevice.AllDevices[i].GetType() == typeof(LibUsbDotNet.LibUsb.LibUsbRegistry))
                    {
                        Console.WriteLine(LibUsbDotNet.UsbDevice.AllDevices[i].ToString());
                        ValidDevices.Add(LibUsbDotNet.UsbDevice.AllDevices[i].Name + "(" + i.ToString() + ")");
                        UsbDevices.Add(LibUsbDotNet.UsbDevice.AllDevices[i]);
                    }
                }

                LibUsbDotNet.UsbDevice.Exit();

                return ValidDevices.ToArray();
            }

            public bool OpenUART(string PortName, int BaudRate)
            {
                if (!Opened)
                {
                    if (UART == null)
                        UART = new SerialPort();

                    UART.PortName = PortName;
                    UART.BaudRate = BaudRate;
                    UART.DataBits = 8;
                    UART.StopBits = StopBits.One;
                    UART.Parity = Parity.None;
                    UART.Handshake = Handshake.None;

                    try
                    {
                        UART.Open();
                        UART.DataReceived += UART_DataReceived;
                    }
                    catch
                    {
                        System.Windows.Forms.MessageBox.Show(UART.PortName + " was denied!!");
                    }
                    if (UART.IsOpen)
                    {
                        Opened = true;
                        CurComnType = ComnType.Serial;
                        ConnectionMessage = UART.PortName + "-" + UART.BaudRate.ToString();
                        RunThread();
                    }
                }
                else
                {
                    ConnectionMessage = "Aleady opend!!";
                }
                return Opened;
            }

            public bool OpenUSB(int DeviceIndex)
            {
                if (!Opened)
                {
                    UsbDevices[DeviceIndex].Open(out USB);
                    if (USB != null)
                    {
                        foreach (LibUsbDotNet.Info.UsbConfigInfo Config in USB.Configs)
                        {
                            Console.WriteLine(Config.ToString());
                            foreach (LibUsbDotNet.Info.UsbInterfaceInfo Interface in Config.InterfaceInfoList)
                            {
                                Console.WriteLine(Interface.ToString());
                                foreach (LibUsbDotNet.Info.UsbEndpointInfo Endpoint in Interface.EndpointInfoList)
                                {
                                    Console.WriteLine(Endpoint.ToString());
                                    if ((Interface.Descriptor.InterfaceID == 0) && (Interface.Descriptor.AlternateID == 0))
                                    {

                                        if (Endpoint.Descriptor.EndpointID == 0x81)
                                        {
                                            UsbEventReader = USB.OpenEndpointReader(ReadEndpointID.Ep01, 1024, EndpointType.Interrupt);
                                            UsbEventReader.DataReceivedEnabled = true;
                                            UsbEventReader.DataReceived += UsbEventReader_DataReceived;
                                            UsbEventReader.ReadThreadPriority = ThreadPriority.Highest;
                                        }
                                        else if (Endpoint.Descriptor.EndpointID == 0x82)
                                            UsbDataReader = USB.OpenEndpointReader(ReadEndpointID.Ep02, 1024, EndpointType.Bulk);
                                        else if (Endpoint.Descriptor.EndpointID == 0x02)
                                            UsbDataWriter = USB.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk);
                                    }
                                    else if (Interface.Descriptor.InterfaceID == 1)
                                    {
                                        if (Endpoint.Descriptor.EndpointID == 0x83)
                                        {
                                            UsbEndpointReader reader = USB.OpenEndpointReader(ReadEndpointID.Ep02, 1024, EndpointType.Isochronous);
                                            UsbIsochReader.Add(reader);
                                        }
                                        else if (Endpoint.Descriptor.EndpointID == 0x03)
                                        {
                                            UsbEndpointWriter writer = USB.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Isochronous);
                                            UsbIsochWriter.Add(writer);
                                        }
                                    }
                                }
                            }
                        }
                        Opened = true;
                        CurComnType = ComnType.USB;
                        ConnectionMessage = UsbDevices[DeviceIndex].Name;
                        RunThread();
                    }
                    else
                        LibUsbDotNet.UsbDevice.Exit();
                }
                else
                {
                    ConnectionMessage = "Aleady opend!!";
                }
                return Opened;
            }

            public bool Close()
            {
                if (Opened)
                {
                    if (CurComnType == ComnType.USB)
                    {
                        USB.Close();
                        LibUsbDotNet.UsbDevice.Exit();
                        USB = null;
                        Opened = false;
                    }
                    else if (CurComnType == ComnType.Serial)
                    {
                        UART.Close();
                        UART.Dispose();
                        UART = null;
                        Opened = false;
                    }
                    if (!Opened)
                    {
                        CurComnType = ComnType.None;
                        ConnectionMessage = "Disconnect";
                        StopThread();
                    }
                }
                return Opened;
            }

            private void SetIndividualCommandControl(Command Cmd)
            {
                switch (Cmd.Name)
                {
                    case "HCI_LE_Set_Scan_Enable":
                        if (Cmd.CommandParameters[0].Data[0] == 1)
                        {
                            AdvertisingReports.Clear();
                            CurLLState = LinkLayerState.Scanning;
                        }
                        else
                            CurLLState = LinkLayerState.Standby;
                        break;

                    case "HCI_LE_Set_Advertising_Enable":
                        if (Cmd.CommandParameters[0].Data[0] == 1)
                        {
                            AdvertisingReports.Clear();
                            CurLLState = LinkLayerState.Advertising;
                        }
                        else
                            CurLLState = LinkLayerState.Standby;
                        break;

                    case "HCI_LE_Create_Connection":

                        break;
                }
            }

            private bool SendCommand(Command Cmd)
            {
                bool Status = false;
                byte[] BytesToSend = Cmd.GetCommandPacket();

                HCIResetEvent.WaitOne(1000);
                HCIResetEvent.Reset();
                if (CurComnType == ComnType.USB)
                {
                    Status = USB.ControlTransfer(ref UsbSetupPck, BytesToSend, BytesToSend.Length, out int Length);
                }
                else if (CurComnType == ComnType.Serial)
                {
                    byte[] WriteBuffer = new byte[BytesToSend.Length + 1];
                    WriteBuffer[0] = 0x01;
                    for (int i = 0; i < BytesToSend.Length; i++)
                        WriteBuffer[i + 1] = BytesToSend[i];
                    UART.Write(WriteBuffer, 0, WriteBuffer.Length);
                    Status = true;
                }

                if (Status)
                {
                    SetIndividualCommandControl(Cmd);
                    SetHciCommandLog(Cmd);
                }

                return Status;
            }

            private void UART_DataReceived(object sender, SerialDataReceivedEventArgs e)
            {
                if (UART != null)
                {

                    int NumBytesToRead = UART.BytesToRead;
                    byte[] RcvBytes = new byte[NumBytesToRead];
                    UART.Read(RcvBytes, 0, NumBytesToRead);

                    for (int i = 0; i < RcvBytes.Length; i++)
                        RcvQueue.Enqueue(RcvBytes[i]);

                    HCIResetEvent.Set();
                }
            }

            private int NumUsbEventRemainderBits = 0;
            private void UsbEventReader_DataReceived(object sender, EndpointDataEventArgs e)
            {
                if (USB != null)
                {
                    if (NumUsbEventRemainderBits == 0)
                    {
                        RcvQueue.Enqueue(0x04);
                        if (e.Buffer[1] > (e.Count - 2))
                            NumUsbEventRemainderBits = e.Buffer[1] - (e.Count - 2);
                    }
                    else
                    {
                        if (NumUsbEventRemainderBits <= e.Count)
                            NumUsbEventRemainderBits = 0;
                        else
                            NumUsbEventRemainderBits -= e.Count;
                    }

                    for (int i = 0; i < e.Count; i++)
                        RcvQueue.Enqueue(e.Buffer[i]);

                    HCIResetEvent.Set();
                }
            }

            private System.Threading.Thread ProcRcvMessage;
            private System.Threading.ManualResetEvent ProcMsgReset = new System.Threading.ManualResetEvent(false);
            private volatile bool IsRunning = false;

            PacketType PacketIndicator = PacketType.HCI_No_Packet;
            uint PayloadLength = uint.MaxValue;
            byte EventCode = 0;

            short ACLHandle = 0;
            byte PacketBoudaryFlag = 0;
            byte BrodcastFlag = 0;
            short ConnectionHandle = 0;
            byte PacketStatusFlag = 0;

            private void RunThread()
            {
                ProcMsgReset.Reset();

                IsRunning = true;
                ProcRcvMessage = new System.Threading.Thread(ProcRcvMessage_DoWork);
                ProcRcvMessage.IsBackground = true;
                ProcRcvMessage.Start();

                while (!ProcRcvMessage.IsAlive)
                    ;
            }

            private void StopThread()
            {
                IsRunning = false;
                ProcRcvMessage.Join();
                ProcRcvMessage = null;
            }

            private void ResetPacketInfo()
            {
                PacketIndicator = PacketType.HCI_No_Packet;
                PayloadLength = uint.MaxValue;
                EventCode = 0;
                ACLHandle = 0;
                PacketBoudaryFlag = 0;
                BrodcastFlag = 0;
                ConnectionHandle = 0;
                PacketStatusFlag = 0;
            }

            private const int MaxRcvFailCount = 5;
            private void ProcRcvMessage_DoWork()
            {
                int RcvFailCount = 0;

                while (IsRunning)
                {
                    if (RcvQueue.Count > 0)
                    {
                        if (PacketIndicator == PacketType.HCI_No_Packet)
                            PacketIndicator = (PacketType)RcvQueue.Dequeue();

                        switch (PacketIndicator)
                        {
                            case PacketType.HCI_ACL_Data_Packet:
                                RcvFailCount = ProcACLDataPacket() ? 0 : RcvFailCount + 1;
                                break;
                            case PacketType.HCI_Synchronous_Packet:
                                RcvFailCount = ProcSynchronousPacket() ? 0 : RcvFailCount + 1;
                                break;
                            case PacketType.HCI_Event_Packet:
                                RcvFailCount = ProcEventPacket() ? 0 : RcvFailCount + 1;
                                break;

                            case PacketType.HCI_Command_Packet:
                            default:
                                RcvFailCount = MaxRcvFailCount;
                                break;
                        }
                        if (RcvFailCount == MaxRcvFailCount)
                        {
                            RcvQueue.Clear();
                            ResetPacketInfo();
                        }
                        else
                            System.Threading.Thread.Sleep(20);
                    }
                    else
                        System.Threading.Thread.Sleep(20);
                }
            }

            private bool ProcEventPacket()
            {

                if ((RcvQueue.Count >= 2) && (PayloadLength == uint.MaxValue))
                {
                    EventCode = RcvQueue.Dequeue();
                    PayloadLength = RcvQueue.Dequeue();
                }

                if ((PayloadLength <= RcvQueue.Count) && (PayloadLength < uint.MaxValue))
                {
                    List<byte> Packet = new List<byte>();
                    for (int i = 0; i < PayloadLength; i++)
                        Packet.Add(RcvQueue.Dequeue());

                    Event Evt = EvtCollection.GetEvent(EventCode);
                    Command Cmd = null;
                    if (Evt != null)
                    {

                        Evt.SetParameters(Packet.ToArray(), 0);

                        switch (EventCode)
                        {
                            case (byte)EventCodes.Command_Complete_Event:
                                short OpCode = (short)((Evt.Parameters[1].Data[1] << 8) | Evt.Parameters[1].Data[0]);
                                for (int i = 0; i < CmdGroupList.Count; i++)
                                {
                                    Cmd = CmdGroupList[i].Commands.GetCommand(OpCode);
                                    if (Cmd != null)
                                    {
                                        Cmd.SetReturnParameters(Packet.ToArray(), Evt.Parameters.TotalLength);
                                        break;
                                    }
                                }
                                break;
                            case (byte)EventCodes.LE_Meta_Event:
                                byte SubEventCode = Evt.Parameters.GetParameter("Subevent_Code").Data[0];
                                Event SubEvent = Evt.SubEvents.GetEvent(SubEventCode);
                                if (SubEvent != null)
                                    SubEvent.SetParameters(Packet.ToArray(), Evt.Parameters.TotalLength);
                                break;
                            default:
                                break;
                        }
                    }
                    SetHciEventLog(Evt, Cmd, Packet.ToArray());
                    ResetPacketInfo();
                    return true;
                }
                return false;
            }

            private bool ProcACLDataPacket()
            {

                if ((RcvQueue.Count >= 4) && (PayloadLength == uint.MaxValue))
                {
                    ACLHandle = RcvQueue.Dequeue();
                    ACLHandle |= (short)(RcvQueue.Dequeue() << 8);
                    PayloadLength = RcvQueue.Dequeue();
                    PayloadLength |= (uint)(RcvQueue.Dequeue() << 8);
                    PacketBoudaryFlag = (byte)((ACLHandle >> 12) & 0x3);
                    BrodcastFlag = (byte)((ACLHandle >> 14) & 0x3);
                    ACLHandle &= 0xFFF;
                }

                if ((PayloadLength <= RcvQueue.Count) && (PayloadLength < uint.MaxValue))
                {
                    List<byte> Packet = new List<byte>();
                    for (int i = 0; i < PayloadLength; i++)
                        Packet.Add(RcvQueue.Dequeue());

                    string Payload = "";
                    for (int i = 0; i < Packet.Count; i++)
                        Payload += Packet[i].ToString("X2") + " ";
                    dtHciLog.Rows.Add(DateTime.Now.ToString("HH:mm:ss"), "HCI ACL Data Packet", Payload);

                    ResetPacketInfo();

                    return true;
                }
                return false;
            }

            private bool ProcSynchronousPacket()
            {

                if ((RcvQueue.Count >= 3) && (PayloadLength == uint.MaxValue))
                {
                    ConnectionHandle = RcvQueue.Dequeue();
                    ConnectionHandle |= (short)(RcvQueue.Dequeue() << 8);
                    PayloadLength = RcvQueue.Dequeue();
                    PacketStatusFlag = (byte)((ACLHandle >> 12) & 0x3);
                    ConnectionHandle &= 0xFFF;
                }

                if ((PayloadLength <= RcvQueue.Count) && (PayloadLength < uint.MaxValue))
                {
                    List<byte> Packet = new List<byte>();
                    for (int i = 0; i < PayloadLength; i++)
                        Packet.Add(RcvQueue.Dequeue());

                    string Payload = "";
                    for (int i = 0; i < Packet.Count; i++)
                        Payload += Packet[i].ToString("X2") + " ";
                    dtHciLog.Rows.Add(DateTime.Now.ToString("HH:mm:ss"), "HCI Synchronous Data Packet", Payload);

                    ResetPacketInfo();

                    return true;
                }
                return false;
            }

            private void RefreshHciLogView()
            {
                HciLogView.Invoke(new MethodInvoker(delegate ()
                {
                    HciLogView.Update();
                    HciLogView.Refresh();
                }));
            }

            private void SetHciParameterToHciLog(ParameterCollection Parameters)
            {
                foreach (Parameter Para in Parameters)
                {
                    dtHciLog.Rows.Add("", Para.Name, Para.Information);
                    HciLogView[1, HciLogView.RowCount - 1].Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                    HciLogView[1, HciLogView.RowCount - 1].ToolTipText = Para.Name + "\n" + Para.Description;
                    if (Para.InfoColor != Color.Black)
                        HciLogView[2, HciLogView.RowCount - 1].Style.ForeColor = Para.InfoColor;
                }
            }

            private void SetHciCommandLog(Command Cmd)
            {
                HciLogView.Invoke(new MethodInvoker(delegate ()
                {
                    dtHciLog.Rows.Add(DateTime.Now.ToString("HH:mm:ss"), Cmd.Name, "OGF=0x" +
                        Cmd.OGF.ToString("X2") + ", OCF=0x" + Cmd.OCF.ToString("X2"));
                    HciLogView[1, HciLogView.RowCount - 1].ToolTipText = Cmd.Name + "\n" + Cmd.Description;
                    for (int c = 0; c < HciLogView.ColumnCount; c++)
                    {
                        HciLogView[c, HciLogView.RowCount - 1].Style.ForeColor = Color.RoyalBlue;
                        HciLogView[c, HciLogView.RowCount - 1].Style.Font = new Font("Consolas", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
                    }
                    dtHciLog.Rows.Add("", "OpCode", "0x" + Cmd.OpCode.ToString("X4"));
                    HciLogView[1, HciLogView.RowCount - 1].Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                    dtHciLog.Rows.Add("", "Payload Length", Cmd.CommandParameters.TotalLength.ToString() + " Octets");
                    HciLogView[1, HciLogView.RowCount - 1].Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                    SetHciParameterToHciLog(Cmd.CommandParameters);
                    HciLogView.FirstDisplayedCell = HciLogView[1, HciLogView.RowCount - 1];
                }));
            }

            private void SetHciEventLog(Event Evt, Command Cmd, byte[] Packet)
            {
                HciLogView.Invoke(new MethodInvoker(delegate ()
                {

                    if (Evt != null)
                    {
                        byte SubEventCode = 0;
                        Event SubEvent = null;

                        if (Evt.SubEvents != null)
                        {
                            SubEventCode = Evt.Parameters.GetParameter("Subevent_Code").Data[0];
                            SubEvent = Evt.SubEvents.GetEvent(SubEventCode);

                            switch (SubEventCode)
                            {
                                case (byte)LEMetaEvents.LE_Advertising_Report_Event:
                                    Parameter DesPara = SubEvent.Parameters.GetParameter("Address[i]");
                                    for (int i = 0; i < AdvertisingReports.Count; i++)
                                    {
                                        if (AdvertisingReports[i].BDAddress == DesPara.ToString())
                                        {
                                            AdvertisingReports[i].RSSI = SubEvent.Parameters.GetParameter("RSSI[i]").ToString();
                                            goto default;
                                        }
                                    }
                                    AdvertisingReports.Add(new ConnectionInfo());
                                    AdvertisingReports[AdvertisingReports.Count - 1].BDAddress = SubEvent.Parameters.GetParameter("Address[i]").ToString();
                                    AdvertisingReports[AdvertisingReports.Count - 1].RSSI = SubEvent.Parameters.GetParameter("RSSI[i]").ToString();
                                    AdvertisingReports[AdvertisingReports.Count - 1].EventType = SubEvent.Parameters.GetParameter("Event_Type[i]").ToString();
                                    AdvertisingReports[AdvertisingReports.Count - 1].AddrType = SubEvent.Parameters.GetParameter("Address_Type[i]").ToString();
                                    AdvertisingReports[AdvertisingReports.Count - 1].ConnHandle = "";
                                    AdvertisingReports[AdvertisingReports.Count - 1].AdvData = SubEvent.Parameters.GetParameter("Data[i]").ToString();
                                    goto default;

                                default:
                                    if (ConnLogView != null)
                                    {
                                        ConnLogView.Invoke(new MethodInvoker(delegate ()
                                        {
                                            ConnLogView.Rows.Clear();
                                            foreach (ConnectionInfo ci in AdvertisingReports)
                                                ConnLogView.Rows.Add(ci.BDAddress, ci.RSSI, ci.EventType, ci.AddrType, ci.ConnHandle, ci.AdvData);
                                        }));
                                    }
                                    return;
                            }
                        }

                        dtHciLog.Rows.Add(DateTime.Now.ToString("HH:mm:ss"), Evt.Name, "Event Code=0x" + Evt.Code.ToString("X2"));
                        HciLogView[1, HciLogView.RowCount - 1].ToolTipText = Evt.Name + "\n" + Evt.Description;
                        for (int c = 0; c < HciLogView.ColumnCount; c++)
                        {
                            HciLogView[c, HciLogView.RowCount - 1].Style.ForeColor = Color.BlueViolet;
                            HciLogView[c, HciLogView.RowCount - 1].Style.Font = new Font("Consolas", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
                        }
                        dtHciLog.Rows.Add("", "Payload Length", PayloadLength.ToString() + " Octets");
                        HciLogView[1, HciLogView.RowCount - 1].Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                        SetHciParameterToHciLog(Evt.Parameters);

                        if (SubEvent != null)
                        {
                            dtHciLog.Rows.Add("", "HCI Sub-event", SubEvent.Name);
                            HciLogView[1, HciLogView.RowCount - 1].Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                            HciLogView[2, HciLogView.RowCount - 1].ToolTipText = SubEvent.Name + "\n" + SubEvent.Description;
                            for (int c = 0; c < HciLogView.ColumnCount; c++)
                                HciLogView[c, HciLogView.RowCount - 1].Style.ForeColor = Color.OliveDrab;
                            SetHciParameterToHciLog(SubEvent.Parameters);
                        }
                    }
                    else
                    {
                        string Payload = EventCode.ToString("X2") + " " + PayloadLength.ToString("X2");
                        if (Packet != null)
                            for (int i = 0; i < Packet.Length; i++)
                                Payload += " " + Packet[i].ToString("X2");
                        dtHciLog.Rows.Add(DateTime.Now.ToString("HH:mm:ss"), "Unknown HCI event", Payload);
                        for (int c = 0; c < HciLogView.ColumnCount; c++)
                        {
                            HciLogView[c, HciLogView.RowCount - 1].Style.ForeColor = Color.Tomato;
                            HciLogView[c, HciLogView.RowCount - 1].Style.Font = new Font("Consolas", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
                        }
                    }

                    if (Cmd != null)
                    {
                        dtHciLog.Rows.Add("", "HCI Command Response", Cmd.Name);
                        HciLogView[1, HciLogView.RowCount - 1].Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                        HciLogView[2, HciLogView.RowCount - 1].ToolTipText = Cmd.Name + "\n" + Cmd.Description;
                        for (int c = 0; c < HciLogView.ColumnCount; c++)
                            HciLogView[c, HciLogView.RowCount - 1].Style.ForeColor = Color.OliveDrab;
                        SetHciParameterToHciLog(Cmd.ReturnParameters);
                    }
                    HciLogView.FirstDisplayedCell = HciLogView[1, HciLogView.RowCount - 1];
                }));
            }

            public void InitHciLogView()
            {
                HciLogView.DataSource = null;
                dtHciLog.Rows.Clear();
                dtHciLog.Columns.Clear();

                dtHciLog.Columns.Add("Time", typeof(string));
                dtHciLog.Columns.Add("Name", typeof(string));
                dtHciLog.Columns.Add("Info.", typeof(string));

                HciLogView.DataSource = dtHciLog;
                for (int i = 0; i < HciLogView.Columns.Count; i++)
                {
                    HciLogView.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    HciLogView.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                }
                HciLogView.Columns[0].Width = 56;
                HciLogView.Columns[1].Width = 170;
                HciLogView.Columns[2].Width = HciLogView.Width - 5 - HciLogView.Columns[0].Width - HciLogView.Columns[1].Width - 20;

                HciLogView.ShowCellToolTips = true;
                HciLogView.Resize += HciLogView_Resize;
                HciLogView.ColumnWidthChanged += HciLogView_ColumnWidthChanged;
            }

            private void HciLogView_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
            {
                if (HciLogView.Columns.Count >= 3)
                    HciLogView.Columns[2].Width = HciLogView.Width - 5 - HciLogView.Columns[0].Width - HciLogView.Columns[1].Width - 20;
            }

            private void HciLogView_Resize(object sender, EventArgs e)
            {
                if (HciLogView.Columns.Count >= 3)
                    HciLogView.Columns[2].Width = HciLogView.Width - 5 - HciLogView.Columns[0].Width - HciLogView.Columns[1].Width - 20;
            }

            public Command GetCommand(string Name)
            {
                Command cmd = null;
                foreach (CommandGroup cg in CmdGroupList)
                {
                    cmd = cg.Commands.GetCommand(Name);
                    if (cmd != null)
                        break;
                }
                return cmd;
            }

            public Command GetCommand(short OpCode)
            {
                Command cmd = null;
                foreach (CommandGroup cg in CmdGroupList)
                {
                    cmd = cg.Commands.GetCommand(OpCode);
                    if (cmd != null)
                        break;
                }
                return cmd;
            }

            private void InitLinkControlCommands(CommandGroup cg)
            {
                Command c;
                Parameter p;

                c = cg.Commands.Add("HCI_Disconnect", 0x0006,
                    "The Disconnection command is used to terminate an existing connection.\n" +
                    "The Connection_Handle command parameter indicates which connection is to\n" +
                    "be disconnected.The Reason command parameter indicates the reason for\n" +
                    "ending the connection.The remote Controller will receive the Reason\n" +
                    "command parameter in the Disconnection Complete event.");
                c.SendCommand = SendCommand;

                p = c.CommandParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle for the connection being disconnected.\n" +
                    "Range: 0x0000 - 0x0EFF(0x0F00 - 0x0FFF Reserved for future use)");
                p = c.CommandParameters.Add("Reason", 1, ParameterDataType.Bytes,
                    "0x05      Authentication Failure error code\n" +
                    "0x13-0x15 Other End Terminated Connection error codes\n" +
                    "0x1A      Unsupported Remote Feature error code\n" +
                    "0x29      Pairing with Unit Key Not Supported error code\n" +
                    "0x3B      Unacceptable Connection Parameters error code");
                p.Data = new byte[] { 0x13 };

                c = cg.Commands.Add("HCI_Read_Remote_Version_Information", 0x001D,
                    "This command will obtain the values for the version information for the remote\n" +
                    "device identified by the Connection_Handle parameter.\n" +
                    "The Connection_Handle must be a Connection_Handle for an ACL or LE connection.");
                c.SendCommand = SendCommand;

                p = c.CommandParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle for the connection being disconnected.\n" +
                    "Range: 0x0000 - 0x0EFF(0x0F00 - 0x0FFF Reserved for future use)");

                c = cg.Commands.Add("HCI_Set_Connection_Encryption", 0x0013, "암호화 On/Off 설정");

                c.SendCommand = SendCommand;

                p = c.CommandParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes, "연결 핸들");
                p = c.CommandParameters.Add("Encryption_Enable", 1, ParameterDataType.Bytes, "0x00=OFF, 0x01=ON");

                p.Data = new byte[] { 0x01 };
            }

            private void InitControlAndBasebandCommands(CommandGroup cg)
            {
                Command c;
                Parameter p;

                c = cg.Commands.Add("HCI_Set_Event_Mask", 0x001,
                "The Set_Event_Mask command is used to control which events are generated by the\n" +
                "HCI for the Host. If the bit in the Event_Mask is set to a one, then the event\n" +
                "associated with that bit will be enabled.For an LE Controller, the LE Meta Event\n" +
                "bit in the Event_Mask shall enable or disable all LE events in the LE Meta Event"
                );
                c.SendCommand = SendCommand;

                p = c.CommandParameters.Add("Event_Mask", 8, ParameterDataType.Bytes,
                    "0x0000000000000000 No events specified\n" +
                    "0x0000000000000001 Inquiry Complete Event\n" +
                    "0x0000000000000002 Inquiry Result Event\n" +
                    "0x0000000000000004 Connection Complete Event\n" +
                    "0x0000000000000008 Connection Request Event\n" +
                    "0x0000000000000010 Disconnection Complete Event\n" +
                    "0x0000000000000020 Authentication Complete Event\n" +
                    "0x0000000000000040 Remote Name Request Complete Event\n" +
                    "0x0000000000000080 Encryption Change Event\n" +
                    "0x0000000000000100 Change Connection Link Key Complete Event\n" +
                    "0x0000000000000200 Master Link Key Complete Event\n" +
                    "0x0000000000000400 Read Remote Supported Features Complete Event\n" +
                    "0x0000000000000800 Read Remote Version Information Complete Event\n" +
                    "0x0000000000001000 QoS Setup Complete Event\n" +
                    "0x0000000000008000 Hardware Error Event\n" +
                    "0x0000000000010000 Flush Occurred Event\n" +
                    "0x0000000000020000 Role Change Event\n" +
                    "0x0000000000080000 Mode Change Event\n" +
                    "0x0000000000100000 Return Link Keys Event\n" +
                    "0x0000000000200000 PIN Code Request Event\n" +
                    "0x0000000000400000 Link Key Request Event\n" +
                    "0x0000000000800000 Link Key Notification Event\n" +
                    "0x0000000001000000 Loopback Command Event\n" +
                    "0x0000000002000000 Data Buffer Overflow Event\n" +
                    "0x0000000004000000 Max Slots Change Event\n" +
                    "0x0000000008000000 Read Clock Offset Complete Event\n" +
                    "0x0000000010000000 Connection Packet Type Changed Event\n" +
                    "0x0000000020000000 QoS Violation Event\n" +
                    "0x0000000040000000 Page Scan Mode Change Event[deprecated]\n" +
                    "0x0000000080000000 Page Scan Repetition Mode Change Event\n" +
                    "0x0000000100000000 Flow Specification Complete Event\n" +
                    "0x0000000200000000 Inquiry Result with RSSI Event\n" +
                    "0x0000000400000000 Read Remote Extended Features Complete Event\n" +
                    "0x0000080000000000 Synchronous Connection Complete Event\n" +
                    "0x0000100000000000 Synchronous Connection Changed Event\n" +
                    "0x0000200000000000 Sniff Subrating Event\n" +
                    "0x0000400000000000 Extended Inquiry Result Event\n" +
                    "0x0000800000000000 Encryption Key Refresh Complete Event\n" +
                    "0x0001000000000000 IO Capability Request Event\n" +
                    "0x0002000000000000 IO Capability Request Reply Event\n" +
                    "0x0004000000000000 User Confirmation Request Event\n" +
                    "0x0008000000000000 User Passkey Request Event\n" +
                    "0x0010000000000000 Remote OOB Data Request Event\n" +
                    "0x0020000000000000 Simple Pairing Complete Event\n" +
                    "0x0080000000000000 Link Supervision Timeout Changed Event\n" +
                    "0x0100000000000000 Enhanced Flush Complete Event\n" +
                    "0x0400000000000000 User Passkey Notification Event\n" +
                    "0x0800000000000000 Keypress Notification Event\n" +
                    "0x1000000000000000 Remote Host Supported Features Notification Event\n" +
                    "0x2000000000000000 LE Meta - Event\n" +
                    "0x00001FFFFFFFFFFF Default\n");
                p.Data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x3F };

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 Set_Event_Mask command succeeded.\n" +
                    "0x01-0xFF Set_Event_Mask command failed.");

                c = cg.Commands.Add("HCI_Reset", 0x003,
                    "The Reset command will reset the Controller and the Link Manager on the BR / EDR Controller,\n" +
                    "the PAL on an AMP Controller, or the Link Layer on an LE Controller.");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 Reset command succeeded, was received and will be executed.\n" +
                    "0x01-0xFF Reset command failed.");

                c = cg.Commands.Add("HCI_Read_Local_Name", 0x014,
                    "The Read_Local_Name command provides the ability to read the stored userfriendly\n" +
                    "name for the BR/ EDR Controller.");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 Reset command succeeded, was received and will be executed.\n" +
                    "0x01-0xFF Reset command failed.");
                c.ReturnParameters.Add("Local_Name", 248, ParameterDataType.String,
                    "A UTF-8 encoded User Friendly Descriptive Name for the device.");

                c = cg.Commands.Add("HCI_Read_Transmit_Power_Level", 0x02D,
                    "The Read_Local_Name command provides the ability to read the stored userfriendly\n" +
                    "name for the BR/ EDR Controller.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection Handle\n" +
                    "Range: 0x0000 - 0x0EFF(0x0F00 - 0x0FFF Reserved for future use)");
                c.CommandParameters.Add("Type", 1, ParameterDataType.Bytes,
                    "0x00 Read Current Transmit Power Level.\n" +
                    "0x01 Read Maximum Transmit Power Level.\n" +
                    "0x02 - 0xFF Reserved");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_Read_Transmit_Power_Level command succeeded.\n" +
                    "0x01-0xFF HCI_Read_Transmit_Power_Level command failed.");
                c.ReturnParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Specifies which Connection_Handle’s Transmit Power Level setting is returned.\n" +
                    "Range: 0x0000-0x0EFF(0x0F00 - 0x0FFF Reserved for future use) ");
                c.ReturnParameters.Add("Transmit_Power_Level", 1, ParameterDataType.S8,
                    "Size: 1 Octet (signed integer)\n" +
                    "Range: -30 ≤ N ≤ 20, Units: dBm");

                c = cg.Commands.Add("HCI_Set_Controller_To_Host_Flow_Control", 0x031,
                    "This command is used by the Host to turn flow control on or off for data and/or\n" +
                    "voice sent in the direction from the Controller to the Host.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Flow_Control_Enable", 1, ParameterDataType.Bytes,
                    "0x00 Flow control off in direction from Controller to Host. Default.\n" +
                    "0x01 Flow control on for HCI ACL Data Packets and off for HCI synchronous\n" +
                    "     Data Packets in direction from Controller to Host.\n" +
                    "0x02 Flow control off for HCI ACL Data Packets and on for HCI synchronous\n" +
                    "     Data Packets in direction from Controller to Host.\n" +
                    "0x03 Flow control on both for HCI ACL Data Packets and HCI synchronous\n" +
                    "     Data Packets in direction from Controller to Host.\n" +
                    "0x04-0xFF Reserved");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 Flow_Control_Enable command succeeded.\n" +
                    "0x01-0xFF Flow_Control_Enable command failed.");

                c = cg.Commands.Add("HCI_Host_Buffer_Size", 0x033,
                    "The Host_Buffer_Size command is used by the Host to notify the Controller\n" +
                    "about the maximum size of the data portion of HCI ACL and synchronous Data\n" +
                    "Packets sent from the Controller to the Host.\n" +
                    "The Controller shall segment the data to be transmitted from the Controller\n" +
                    "to the Host according to these sizes, so that the HCI Data Packets will contain\n" +
                    "data with up to these sizes.\n" +
                    "The Host_Buffer_Size command also notifies the Controller about the total number\n" +
                    "of HCI ACL and synchronous Data Packets that can be stored in the data buffers\n" +
                    "of the Host.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Host_ACL_Data_Packet_Length", 2, ParameterDataType.Number,
                    "Maximum length (in octets) of the data portion of each HCI ACL Data\n" +
                    "Packet that the Host is able to accept.");
                c.CommandParameters.Add("Host_Synchronous_Data_Packet_Length", 1, ParameterDataType.Number,
                    "Maximum length (in octets) of the data portion of each HCI synchronous\n" +
                    "Data Packet that the Host is able to accept.");
                c.CommandParameters.Add("Host_Total_Num_ACL_Data_Packets", 2, ParameterDataType.Number,
                    "Total number of HCI ACL Data Packets that can be stored in the data\n" +
                    "buffers of the Host.");
                c.CommandParameters.Add("Host_Total_Num_Synchronous_Data_Packets", 2, ParameterDataType.Number,
                    "Total number of HCI synchronous Data Packets that can be stored in the\n" +
                    "data buffers of the Host.");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_Host_Buffer_Size command succeeded.\n" +
                    "0x01-0xFF HCI_Host_Buffer_Size command failed.");

                c = cg.Commands.Add("HCI_Host_Number_Of_Completed_Packets", 0x035,
                    "The Host_Number_Of_Completed_Packets command is used by the Host to indicate\n" +
                    "to the Controller the number of HCI Data Packets that have been completed for\n" +
                    "each Connection Handle since the previous Host_Number_Of_Completed_Packets\n" +
                    "command was sent to the Controller.\n" +
                    "This means that the corresponding buffer space has been freed in the Host.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Number_Of_Handles", 1, ParameterDataType.Number,
                    "The number of Connection Handles and Host_Num_Of_Completed_Packets\n" +
                    "parameters pairs contained in this command.\n" +
                    "Range: 0 - 255",
                    ParameterType.ArrayIndicator);
                c.CommandParameters.Add("Connection_Handle[i]", 2, ParameterDataType.Bytes,
                    "Connection Handle\n" +
                    "Range: 0x0000 - 0x0EFF(0x0F00 - 0x0FFF Reserved for future use)",
                    ParameterType.ArrayData);
                c.CommandParameters.Add("Host_Num_Of_Completed_Packets[i]", 2, ParameterDataType.Number,
                    "The number of HCI Data Packets that have been completed for the associated\n" +
                    "Connection Handle since the previous time the event was returned.\n" +
                    "Range for N: 0x0000-0xFFFF",
                    ParameterType.ArrayData);

                c = cg.Commands.Add("HCI_Set_Event_Mask_Page_2", 0x063,
                    "The Set_Event_Mask_Page_2 command is used to control which events are\n" +
                    "generated by the HCI for the Host. The Event_Mask_Page_2 is a logical\n" +
                    "extension to the Event_Mask parameter of the Set_Event_Mask command.\n" +
                    "If the bit in the Event_Mask_Page_2 is set to a one, then the event\n" +
                    "associated with that bit shall be enabled.\n" +
                    "The Host has to deal with each event that occurs by the Controllers.\n" +
                    "The event mask allows the Host to control how much it is interrupted.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Event_Mask_Page_2", 8, ParameterDataType.Bytes,
                    "0x0000000000000000 No events specified (default)\n" +
                    "0x0000000000000001 Physical Link Complete Event\n" +
                    "0x0000000000000002 Channel Selected Event\n" +
                    "0x0000000000000004 Disconnection Physical Link Event\n" +
                    "0x0000000000000008 Physical Link Loss Early Warning Event\n" +
                    "0x0000000000000010 Physical Link Recovery Event\n" +
                    "0x0000000000000020 Logical Link Complete Event\n" +
                    "0x0000000000000040 Disconnection Logical Link Complete Event\n" +
                    "0x0000000000000080 Flow Spec Modify Complete Event\n" +
                    "0x0000000000000100 Number of Completed Data Blocks Event\n" +
                    "0x0000000000000200 AMP Start Test Event\n" +
                    "0x0000000000000400 AMP Test End Event\n" +
                    "0x0000000000000800 AMP Receiver Report Event\n" +
                    "0x0000000000001000 Short Range Mode Change Complete Event\n" +
                    "0x0000000000002000 AMP Status Change Event\n" +
                    "0x0000000000004000 Triggered Clock Capture Event\n" +
                    "0x0000000000008000 Synchronization Train Complete Event\n" +
                    "0x0000000000010000 Synchronization Train Received Event\n" +
                    "0x0000000000020000 Connectionless Slave Broadcast Receive Event\n" +
                    "0x0000000000040000 Connectionless Slave Broadcast Timeout Event\n" +
                    "0x0000000000080000 Truncated Page Complete Event\n" +
                    "0x0000000000100000 Slave Page Response Timeout Event\n" +
                    "0x0000000000200000 Connectionless Slave Broadcast Channel Map Change Event\n" +
                    "0x0000000000400000 Inquiry Response Notification Event\n" +
                    "0x0000000000800000 Authenticated Payload Timeout Expired Event\n" +
                    "0xFFFFFFFFFF000000 Reserved for future use");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_Set_Event_Mask_Page_2 command succeeded.\n" +
                    "0x01-0xFF HCI_Set_Event_Mask_Page_2 command failed.");

                c = cg.Commands.Add("HCI_Read_LE_Host_Support", 0x06C,
                    "The Read_LE_Host_Support command is used to read the LE Supported (Host) and\n" +
                    "Simultaneous LE and BR/EDR to Same Device Capable(Host) Link Manager Protocol feature bits.");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_Read_LE_Host_Support command succeeded.\n" +
                    "0x01-0xFF Read_LE_Host_Support command failed.");
                c.ReturnParameters.Add("LE_Supported_Host", 1, ParameterDataType.Bytes,
                    "0x00 LE Supported (Host) disabled (default)\n0x01 LE Supported(Host) enabled\n0x02–0xFF Reserved");
                c.ReturnParameters.Add("Simultaneous_LE_Host", 1, ParameterDataType.Bytes,
                    "0x00 Simultaneous LE and BR/EDR to Same Device Capable (Host) disabled(default).\n" +
                    "     This value shall be ignored.\n" +
                    "0x01–0xFF Reserved");

                c = cg.Commands.Add("HCI_Write_LE_Host_Support", 0x06D,
                    "The Read_LE_Host_Support command is used to read the LE Supported (Host) and\n" +
                    "Simultaneous LE and BR/EDR to Same Device Capable(Host) Link Manager Protocol feature bits.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("LE_Supported_Host", 1, ParameterDataType.Bytes,
                     "0x00 LE Supported (Host) disabled (default)\n" +
                     "0x01 LE Supported(Host) enabled\n0x02–0xFF Reserved");
                c.CommandParameters.Add("Simultaneous_LE_Host", 1, ParameterDataType.Bytes,
                    "0x00 Simultaneous LE and BR/EDR to Same Device Capable (Host) disabled(default).\n" +
                    "     This value shall be ignored.\n" +
                    "0x01–0xFF Reserved");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_Write_LE_Host_Support command succeeded.\n" +
                    "0x01-0xFF HCI_Write_LE_Host_Support command failed.");

                c = cg.Commands.Add("HCI_Read_Authenticated_Payload_Timeout", 0x07B,
                    "This command reads the Authenticated_Payload_Timeout(authenticatedPayloadTO)");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle used to identify a connection.\nRange: 0x0000 - 0x0EFF(0x0F00 - 0x0FFF Reserved for future use)");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_Read_Authenticated_Payload_Timeout command succeeded.\n" +
                    "0x01-0xFF HCI_Read_Authenticated_Payload_Timeout command failed.");
                c.ReturnParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle used to identify a connection.\nRange: 0x0000 - 0x0EFF(0x0F00 - 0x0FFF Reserved for future use)");
                c.ReturnParameters.Add("Authenticated_Payload_Timeout", 2, ParameterDataType.Time_10msec,
                    "Maximum amount of time specified between packets authenticated by a MIC.\n" +
                    "Default = 0x0BB8(30 seconds), Range: 0x0001 to 0xFFFF\n" +
                    "Time = N * 10 msec (Time Range: 10 msec to 655, 350 msec)");

                c = cg.Commands.Add("HCI_Write_Authenticated_Payload_Timeout", 0x07C,
                    "This command writes the Authenticated_Payload_Timeout(authenticatedPayloadTO)");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle used to identify a connection.\nRange: 0x0000 - 0x0EFF(0x0F00 - 0x0FFF Reserved for future use)");
                p = c.CommandParameters.Add("Authenticated_Payload_Timeout", 2, ParameterDataType.Time_10msec,
                    "Maximum amount of time specified between packets authenticated by a MIC.\n" +
                    "Default = 0x0BB8(30 seconds), Range: 0x0001 to 0xFFFF\n" +
                    "Time = N * 10 msec (Time Range: 10 msec to 655, 350 msec)");
                p.Data = new byte[] { 0xB8, 0x0B };

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_Write_Authenticated_Payload_Timeout command succeeded.\n" +
                    "0x01-0xFF HCI_Write_Authenticated_Payload_Timeout command failed.");
                c.ReturnParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle used to identify a connection.\nRange: 0x0000 - 0x0EFF(0x0F00 - 0x0FFF Reserved for future use)");

            }

            private void InitInformationalParameters(CommandGroup cg)
            {
                Command c;

                c = cg.Commands.Add("HCI_Read_Local_Version_Information", 0x001,
                    "This command reads the values for the version information for the local Controller.");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 Read_Local_Version_Information command succeeded.\n" +
                    "0x01-0xFF Read_Local_Version_Information command failed.");
                c.ReturnParameters.Add("HCI_Version", 1, ParameterDataType.CoreVersion,
                    "Bluetooth Core Specification version.");
                c.ReturnParameters.Add("HCI_Revision", 2, ParameterDataType.Bytes,
                    "Revision of the Current HCI in the BR/EDR Controller.");
                c.ReturnParameters.Add("LMP/PAL_Version", 1, ParameterDataType.CoreVersion,
                    "Version of the Current LMP or PAL in the Controller.");
                c.ReturnParameters.Add("Manufacturer_Name", 2, ParameterDataType.Bytes,
                    "Manufacturer Name of the BR/EDR Controller.");
                c.ReturnParameters.Add("LMP/PAL_Subversion", 2, ParameterDataType.Bytes,
                    "Subversion of the Current LMP or PAL in the Controller. This value is implementation dependent.");

                c = cg.Commands.Add("HCI_Read_Local_Supported_Commands", 0x002,
                    "This command reads the list of HCI commands supported for the local Controller.");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_Read_Local_Supported_Commands command succeeded.\n" +
                    "0x01-0xFF HCI_Read_Local_Supported_Commands command failed.");
                c.ReturnParameters.Add("Supported Commands", 64, ParameterDataType.Bytes,
                    "Bit mask for each HCI Command. If a bit is 1, the Controller supports the corresponding command\n" +
                    "and the features required for the command. Unsupported or undefined commands shall be set to 0.\n" +
                    "See section 6.27, “Supported Commands,” on page 489.");

                c = cg.Commands.Add("HCI_Read_Local_Supported_Features", 0x003,
                    "This command requests a list of the supported features for the local BR/EDR Controller.");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_Read_Local_Supported_Features command succeeded.\n" +
                    "0x01-0xFF HCI_Read_Local_Supported_Features command failed.");
                c.ReturnParameters.Add("LMP_Features", 8, ParameterDataType.Bytes,
                    "Bit Mask List of LMP features. For details see Part C, Link Manager Protocol Specification on page 224.");

                c = cg.Commands.Add("HCI_Read_Local_Extended_Features", 0x004,
                    "This command requests a list of the supported features for the local BR/EDR Controller.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Page Number", 1, ParameterDataType.Bytes,
                    "0x00 Requests the normal LMP features as returned by Read_Local_Supported_Features.\n" +
                    "0x01 - 0xFF Return the corresponding page of features.");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_Read_Local_Extended_Features command succeeded.\n" +
                    "0x01-0xFF HCI_Read_Local_Extended_Features command failed.");
                c.ReturnParameters.Add("Page Number", 1, ParameterDataType.Bytes,
                    "0x00 The normal LMP features as returned by Read_Local_Supported_Features.\n" +
                    "0x01 - 0xFF The page number of the features returned.");
                c.ReturnParameters.Add("Maximum Page Number", 1, ParameterDataType.Number,
                    "The highest features page number which contains non-zero bits for the local device.");
                c.ReturnParameters.Add("Extended_LMP_Features", 8, ParameterDataType.Bytes,
                    "Bit map of requested page of LMP features. See LMP specification for details.");

                c = cg.Commands.Add("HCI_Read_Buffer_Size", 0x005,
                    "The Read_Buffer_Size command is used to read the maximum size of the data portion of\n" +
                    "HCI ACL and synchronous Data Packets sent from the Host to the Controller.");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 Read_Buffer_Size command succeeded.\n" +
                    "0x01-0xFF Read_Buffer_Size command failed.");
                c.ReturnParameters.Add("HC_ACL_Data_Packet_Length", 2, ParameterDataType.Number,
                    "Maximum length (in octets) of the data portion of each HCI ACL Data Packet\n" +
                    "that the Controller is able to accept.");
                c.ReturnParameters.Add("HC_Synchronous_Data_Packet_Length", 1, ParameterDataType.Number,
                    "Maximum length (in octets) of the data portion of each HCI Synchronous Data Packet\n" +
                    "that the Controller is able to accept.");
                c.ReturnParameters.Add("HC_Total_Num_ACL_Data_Packets", 2, ParameterDataType.Number,
                    "Total number of HCI ACL Data Packets that can be stored in the data buffers of the Controller.");
                c.ReturnParameters.Add("HC_Total_Num_Synchronous_Data_Packets", 2, ParameterDataType.Number,
                    "Total number of HCI Synchronous Data Packets that can be stored in the data buffers of the Controller.");

                c = cg.Commands.Add("HCI_Read_BD_ADDR", 0x009, "This command reads the Bluetooth Controller address(BD_ADDR).");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 Read_BD_ADDR command succeeded.\n" +
                    "0x01-0xFF Read_BD_ADDR command failed.");
                c.ReturnParameters.Add("BD_ADDR", 6, ParameterDataType.Bytes, "BD_ADDR of the Device");

                c = cg.Commands.Add("HCI_Read_Data_Block_Size", 0x00A,
                    "The Read_Data_Block_Size command is used to read values regarding the maximum permitted\n" +
                    "data transfers over the Controller and the data buffering available in the Controller.");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_Read_Data_Block_Size command succeeded.\n" +
                    "0x01-0xFF HCI_Read_Data_Block_Size command failed.");
                c.ReturnParameters.Add("Max_ACL_Data_Packet_Length", 2, ParameterDataType.Number,
                    "Maximum length (in octets) of the data portion of an HCI ACL Data Packet\n" +
                    "that the Controller is able to accept for transmission.For AMP Controllers\n" +
                    "this always equals to Max_PDU_Size.");
                c.ReturnParameters.Add("Data_Block_Length", 2, ParameterDataType.Number,
                    "Maximum length (in octets) of the data portion of each HCI ACL Data Packet \n" +
                    "that the Controller is able to hold in each of its data block buffers.");
                c.ReturnParameters.Add("Total_Num_Data_Blocks", 2, ParameterDataType.Number,
                    "Total number of data block buffers available in the Controller for the\n" +
                    "of data packets scheduled for transmission.");

                c = cg.Commands.Add("HCI_Read_Local_Supported_Codecs", 0x00B,
                    "This command reads a list of the Bluetooth SIG approved codecs supported by the Controller,\n" +
                    "as well as vendor specific codecs, which are defined by an individual manufacturer.");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_Read_Local_Supported_Codecs command succeeded.\n" +
                    "0x01-0xFF HCI_Read_Local_Supported_Codecs command failed.");
                c.ReturnParameters.Add("Number_of_Supported_Codecs", 1, ParameterDataType.Number,
                    "Total number of codecs supported",
                    ParameterType.ArrayIndicator);
                c.ReturnParameters.Add("Supported_Codecs[i]", 1, ParameterDataType.Bytes,
                    "An array of codec identifiers. See Assigned Numbers for Codec ID",
                    ParameterType.ArrayData);
                c.ReturnParameters.Add("Number_of_Supported_Vendor_Specific_Codecs", 1, ParameterDataType.Number,
                    "Total number of vendor specific codecs supported",
                    ParameterType.ArrayIndicator);
                c.ReturnParameters.Add("Vendor_Specific_Codecs[i]", 4, ParameterDataType.Bytes,
                    "Octets 0 and 1: Company ID, see Assigned Numbers for Company Identifier\n" +
                    "Octets 2 and 3: Vendor defined codec ID",
                    ParameterType.ArrayData);

            }

            private void InitStatusParameters(CommandGroup cg)
            {
                Command c;

                c = cg.Commands.Add("HCI_Read_RSSI", 0x0005,
                    "This command reads the Received Signal Strength Indication (RSSI) value from a Controller.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Handle", 2, ParameterDataType.Bytes,
                    "The Handle for the connection for which the RSSI is to be read.\n" +
                    "The Handle is a Connection_Handle for a BR/ EDR Controller and\n" +
                    "a Physical_Link_Handle for an AMP Controller.\n" +
                    "Range: 0x0000-0x0EFF(0x0F00-0x0FFF Reserved for future use)");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_Read_RSSI command succeeded.\n" +
                    "0x01-0xFF HCI_Read_RSSI command failed.");
                c.ReturnParameters.Add("Handle", 2, ParameterDataType.Bytes,
                    "The Handle for the connection for which the RSSI is to be read.\n" +
                    "The Handle is a Connection_Handle for a BR/ EDR Controller and\n" +
                    "a Physical_Link_Handle for an AMP Controller.\n" +
                    "Range: 0x0000-0x0EFF(0x0F00-0x0FFF Reserved for future use)");
                c.ReturnParameters.Add("RSSI", 1, ParameterDataType.S8,
                    "BR/EDR Range: -128 ≤ N ≤ 127(signed integer) Units: dB\n" +
                    "AMP    Range: AMP type specific(signed integer) Units: dBm\n" +
                    "LE     Range: -127 to 20, 127(signed integer) Units: dBm");

            }

            private void InitLEControllerCommand(CommandGroup cg)
            {
                Command c;
                Parameter p;

                c = cg.Commands.Add("HCI_LE_Set_Event_Mask", 0x0001,
                    "The LE_Set_Event_Mask command is used to control which LE events are generated\n" +
                    "by the HCI for the Host. If the bit in the LE_Event_Mask is set to a one,\n" +
                    "then the event associated with that bit will be enabled.\n" +
                    "The Host has to deal with each event that is generated by an LE Controller.\n" +
                    "The event mask allows the Host to control which events will interrupt it.\n" +
                    "For LE events to be generated, the LE Meta-Event bit in the Event_Mask shall\n" +
                    "also be set.If that bit is not set, then LE events shall not be generated,\n" +
                    "regardless of how the LE_Event_Mask is set.");
                c.SendCommand = SendCommand;

                p = c.CommandParameters.Add("LE_Event_Mask", 8, ParameterDataType.Bytes,
                    "0x0000000000000000 No LE events specified\n" +
                    "0x0000000000000001 LE Connection Complete Event\n" +
                    "0x0000000000000002 LE Advertising Report Event\n" +
                    "0x0000000000000004 LE Connection Update Complete Event\n" +
                    "0x0000000000000008 LE Read Remote Used Features Complete Event\n" +
                    "0x0000000000000010 LE Long Term Key Request Event\n" +
                    "0x0000000000000020 LE Remote Connection Parameter Request Event\n" +
                    "0x0000000000000040 LE Data Length Change Event\n" +
                    "0x0000000000000080 LE Read Local P-256 Public Key Complete Event\n" +
                    "0x0000000000000100 LE Generate DHKey Complete Event\n" +
                    "0x0000000000000200 LE Enhanced Connection Complete Event\n" +
                    "0x0000000000000400 LE Direct Advertising Report Event\n" +
                    "0x000000000000001F Default\n" +
                    "0xFFFFFFFFFFFFF800 Reserved for future use\n");
                p.Data = new byte[] { 0x1F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 = LE_Set_Event_Mask command succeeded.\n" +
                    "0x01–0xFF LE_Set_Event_Mask command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Read_Buffer_Size", 0x0002,
                    "The LE_Read_Buffer_Size command is used to read the maximum size of the\n" +
                    "data portion of HCI LE ACL Data Packets sent from the Host to the Controller\n" +
                    "The Host will segment the data transmitted to the Controller according to these\n" +
                    "values, so that the HCI Data Packets will contain data with up to this size.\n" +
                    "The LE_Read_Buffer_Size command also returns the total number of HCI LE ACL\n" +
                    "Data Packets that can be stored in the data buffers of the Controller.\n" +
                    "The LE_Read_Buffer_Size command must be issued by the Host before it sends\n" +
                    "any data to an LE Controller(see Section 4.1.1).\n" +
                    "If the Controller returns a length value of zero, the Host shall use the\n" +
                    "Read_Buffer_Size command to determine the size of the data buffers(shared\n" +
                    "between BR / EDR and LE transports).");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 LE_Read_Buffer_Size command succeeded.\n" +
                    "0x01–0xFF LE_Read_Buffer_Size command failed. See Pard D, Error Codes");
                c.ReturnParameters.Add("HC_LE_Data_Packet_Length", 2, ParameterDataType.Number,
                    "0x0000 No dedicated LE Buffer – use Read_Buffer_Size command.\n" +
                    "0x0001–0xFFFF Maximum length(in octets) of the data portion of each HCI ACL\n" +
                    "              Data Packet that the Controller is able to accept.");
                c.ReturnParameters.Add("HC_Total_Num_LE_Data_Packets", 1, ParameterDataType.Number,
                    "0x00 No dedicated LE Buffer – use Read_Buffer_Size command.\n" +
                    "0x01–0xFF Total number of HCI ACL Data Packets that can be stored in the data buffers of the Controller.");

                c = cg.Commands.Add("HCI_LE_Read_Local_Supported_Features", 0x0003,
                    "This command requests the list of the supported LE features for the Controller.");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Read_Local_Supported_Features command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Read_Local_Supported_Features command failed. See Pard D, Error Codes");
                c.ReturnParameters.Add("LE_Features", 8, ParameterDataType.Bytes,
                    "Bit0 LE Encryption\n" +
                    "Bit1 Connection Parameters Request Procedure\n" +
                    "Bit2 Extended Reject Indication\n" +
                    "Bit3 Slave - initiated Features Exchange\n" +
                    "Bit4 LE Ping\n" +
                    "Bit5 LE Data Packet Length Extension\n" +
                    "Bit6 LL Privacy\n" +
                    "Bit7 Extended Scanner Filter Policies\n" +
                    "Bit8–Bit63 RFU");

                c = cg.Commands.Add("HCI_LE_Set_Random_Address", 0x0005,
                    "The LE_Set_Random_Address command is used by the Host to set the LE\n" +
                    "Random Device Address in the Controller(see[Vol 6] Part B, Section 1.3).");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Random_Addess", 6, ParameterDataType.Bytes,
                    "Random Device Address as defined by [Vol 6] Part B, Section 1.3.");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Set_Random_Address command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Set_Random_Address command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Set_Advertising_Parameters", 0x0006,
                    "The LE_Set_Advertising_Parameters command is used by the Host to set the advertising parameters.\n" +
                    "The Advertising_Interval_Min shall be less than or equal to the Advertising_Interval_Max.\n" +
                    "The Advertising_Interval_Min and Advertising_Interval_Max should not be the same value to enable\n" +
                    "the Controller to determine the best advertising interval given other activities.");
                c.SendCommand = SendCommand;

                p = c.CommandParameters.Add("Advertising_Interval_Min", 2, ParameterDataType.Time_625usec,
                    "Minimum advertising interval for undirected and low duty cycle directed advertising.\n" +
                    "Range: 0x0020 to 0x4000, Default: N = 0x0800(1.28 second)\n" +
                    "Time = N * 0.625 msec, Time Range: 20 ms to 10.24 sec");
                p.Data = new byte[] { 0x00, 0x08 };
                p = c.CommandParameters.Add("Advertising_Interval_Max", 2, ParameterDataType.Time_625usec,
                    "Maximum advertising interval for undirected and low duty cycle directed advertising.\n" +
                    "Range: 0x0020 to 0x4000, Default: N = 0x0800(1.28 second)\n" +
                    "Time = N * 0.625 msec, Time Range: 20 ms to 10.24 sec");
                p.Data = new byte[] { 0x00, 0x08 };
                c.CommandParameters.Add("Advertising_Type", 1, ParameterDataType.Bytes,
                    "0x00 Connectable undirected advertising (ADV_IND) (default)\n" +
                    "0x01 Connectable high duty cycle directed advertising (ADV_DIRECT_IND, high duty cycle)\n" +
                    "0x02 Scannable undirected advertising(ADV_SCAN_IND)\n" +
                    "0x03 Non connectable undirected advertising(ADV_NONCONN_IND)\n" +
                    "0x04 Connectable low duty cycle directed advertising(ADV_DIRECT_IND, low duty cycle)\n" +
                    "0x05–0xFF Reserved for future use");
                c.CommandParameters.Add("Own_Address_Type", 1, ParameterDataType.Bytes,
                    "0x00 Public Device Address (default)\n" +
                    "0x01 Random Device Address\n" +
                    "0x02 Controller generates Resolvable Private Address based on the local IRK from resolving list.\n" +
                    "     If resolving list contains no matching entry,use public address.\n" +
                    "0x03 Controller generates Resolvable Private Address based on the local IRK from resolving list.\n" +
                    "     If resolving list contains no matching entry, use random address from LE_Set_Random_Address.\n" +
                    "0x04–0xFF Reserved for future use");
                c.CommandParameters.Add("Peer_Address_Type", 1, ParameterDataType.Bytes,
                    "0x00 Public Device Address (default) or Public Identity Address\n" +
                    "0x01 Random Device Address or Random(static) Identity Address\n" +
                    "0x02–0xFF Reserved for future use");
                c.CommandParameters.Add("Peer_Address", 6, ParameterDataType.Bytes,
                    "Public Device Address, Random Device Address, Public Identity Address,\n" +
                    "or Random(static) Identity Address of the device to be connected");
                p = c.CommandParameters.Add("Advertising_Channel_Map", 1, ParameterDataType.Bytes,
                    "00000000b Reserved for future use\n" +
                    "xxxxxxx1b Channel 37 shall be used\n" +
                    "xxxxxx1xb Channel 38 shall be used\n" +
                    "xxxxx1xxb Channel 39 shall be used\n" +
                    "00000111b Default(all channels enabled)");
                p.Data = new byte[] { 0x07 };
                c.CommandParameters.Add("Advertising_Filter_Policy", 1, ParameterDataType.Bytes,
                    "0x00 Process scan and connection requests from all devices (i.e., the White List is not in use)(default).\n" +
                    "0x01 Process connection requests from all devices and only scan requests from\n" +
                    "     devices that are in the White List.\n" +
                    "0x02 Process scan requests from all devices and only connection requests from\n" +
                    "     devices that are in the White List.\n" +
                    "0x03 Process scan and connection requests only from devices in the White List.\n" +
                    "0x04–0xFF Reserved for future use");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Set_Advertising_Parameters command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Set_Advertising_Parameters command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Read_Advertising_Channel_Tx_Power", 0x0007,
                    "The LE_Read_Advertising_Channel_Tx_Power command is used by the Host\n" +
                    "to read the transmit power level used for LE advertising channel packets.");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Read_Advertising_Channel_Tx_Power command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Read_Advertising_Channel_Tx_Power command failed. See Pard D, Error Codes");
                c.ReturnParameters.Add("Transmit_Power_Level", 1, ParameterDataType.S8,
                    "Range: -20 ≤ N ≤ 10 Units: dBm, Accuracy: +/ -4 dB");

                c = cg.Commands.Add("HCI_LE_Set_Advertising_Data", 0x0008,
                    "The LE_Set_Advertising_Data command is used to set the data used in\n" +
                    "advertising packets that have a data field.");
                c.SendCommand = SendCommand;

                p = c.CommandParameters.Add("Advertising_Data_Length", 1, ParameterDataType.Bytes,
                    "0x00–0x1F The number of significant octets in the Advertising_Data.");
                p.Data = new byte[] { 0x00 };
                c.CommandParameters.Add("Advertising_Data", 31, ParameterDataType.Bytes,
                    "31 octets of advertising data formatted as defined in [Vol 3] Part C, Section 11.\n" +
                    "All octets zero(default).");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Set_Advertising_Data command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Set_Advertising_Data command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Set_Scan_Response_Data", 0x0009,
                    "This command is used to provide data used in Scanning Packets that have a data field.");
                c.SendCommand = SendCommand;

                p = c.CommandParameters.Add("Scan_Response_Data_Length", 1, ParameterDataType.Bytes,
                    "0x00–0x1F The number of significant octets in the Scan_Response_Data.");
                p.Data = new byte[] { 0x00 };
                c.CommandParameters.Add("Scan_Response_Data", 31, ParameterDataType.Bytes,
                    "31 octets of Scan_Response_Data formatted as defined in [Vol 3] Part C, Section 11.");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Set_Scan_Response_Data command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Set_Scan_Response_Data command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Set_Advertising_Enable", 0x000A,
                    "The LE_Set_Advertise_Enable command is used to request the Controller to\n" +
                    "start or stop advertising.\n" +
                    "The Controller manages the timing of advertisements as per the advertising\n" +
                    "parameters given in the LE_Set_Advertising_Parameters command.\n" +
                    "The Controller shall continue advertising until the Host issues an\n" +
                    "LE_Set_Advertise_Enable command with Advertising_Enable set to 0x00\n" +
                    "(Advertising is disabled) or until a connection is created or until the Advertising\n" +
                    "is timed out due to high duty cycle Directed Advertising.In these cases,\n" +
                    "advertising is then disabled.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Advertising_Enable", 1, ParameterDataType.Bytes,
                    "0x00 Advertising is disabled (default)\n" +
                    "0x01 Advertising is enabled.\n" +
                    "0x02–0xFF Reserved for future use");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Set_Advertising_Enable command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Set_Advertising_Enable command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Set_Scan_Parameters", 0x000B,
                    "The LE_Set_Scan_Parameters command is used to set the scan parameters.\n" +
                    "The LE_Scan_Type parameter controls the type of scan to perform.\n" +
                    "The LE_Scan_Interval and LE_Scan_Window parameters are recommendations\n" +
                    "from the Host on how long(LE_Scan_Window) and how frequently(LE_Scan_Interval)\n" +
                    "the Controller should scan(See[Vol 6] Part B, Section 4.5.3).\n" +
                    "The LE_Scan_Window parameter shall always be set to a value smaller or\n" +
                    "equal to the value set for the LE_Scan_Interval parameter.\n" +
                    "If they are set to the same value scanning should be run continuously.\n" +
                    "Own_Address_Type parameter indicates the type of address being used in the\n" +
                    "scan request packets.\n" +
                    "The Host shall not issue this command when scanning is enabled in the\n" +
                    "Controller; if it is the Command Disallowed error code shall be used.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("LE_Scan_Type", 1, ParameterDataType.Bytes,
                    "0x00 Passive Scanning. No SCAN_REQ packets shall be sent.(default)\n" +
                    "0x01 Active scanning.SCAN_REQ packets may be sent.\n" +
                    "0x02–0xFF Reserved for future use");
                p = c.CommandParameters.Add("LE_Scan_Interval", 2, ParameterDataType.Time_625usec,
                    "This is defined as the time interval from when the Controller\n" +
                    "started its last LE scan until it begins the subsequent LE scan.\n" +
                    "Range: 0x0004 to 0x4000, Default: 0x0010(10 ms)\n" +
                    "Time = N * 0.625 msec, Time Range: 2.5 msec to 10.24 seconds");
                p.Data = new byte[] { 0x10, 0x00 };
                p = c.CommandParameters.Add("LE_Scan_Window", 2, ParameterDataType.Time_625usec,
                    "The duration of the LE scan. LE_Scan_Window shall be less than or equal to LE_Scan_Interval\n" +
                    "Range: 0x0004 to 0x4000, Default: 0x0010(10 ms)\n" +
                    "Time = N * 0.625 msec, Time Range: 2.5 msec to 10.24 seconds");
                p.Data = new byte[] { 0x10, 0x00 };
                c.CommandParameters.Add("Own_Address_Type", 1, ParameterDataType.Bytes,
                    "0x00 Public Device Address (default)\n" +
                    "0x01 Random Device Address\n" +
                    "0x02 Controller generates Resolvable Private Address based on the local IRK from resolving list.\n" +
                    "     If resolving list contains no matching entry, use public address.\n" +
                    "0x03 Controller generates Resolvable Private Address based on the local IRK from resolving list.\n" +
                    "     If resolving list contains no matching entry, use random address from LE_Set_Random_Address.\n" +
                    "0x04-0xFF Reserved for future use.");
                c.CommandParameters.Add("Scanning_Filter_Policy", 1, ParameterDataType.Bytes,
                    "0x00 Accept all\n" +
                    "     • advertisement packets except directed advertising packets not\n" +
                    "       addressed to this device(default)." +
                    "0x01 Accept only\n" +
                    "     • advertisement packets from devices where the advertiser’s address is in the White list.\n" +
                    "     • Directed advertising packets which are not addressed for this device shall be ignored.\n" +
                    "0x02 Accept all\n" +
                    "     • undirected advertisement packets, and\n" +
                    "     • directed advertising packets where the initiator address is a resolvable private address, and\n" +
                    "     • directed advertising packets addressed to this device.\n" +
                    "0x03 Accept all\n" +
                    "     • advertisement packets from devices where the advertiser’s address is in the White list, and\n" +
                    "     • directed advertising packets where the initiator address is a resolvable private address, and\n" +
                    "     • directed advertising packets addressed to this device.\n" +
                    "0x04-0xFF Reserved for future use.");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Set_Scan_Parameters command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Set_Scan_Parameters command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Set_Scan_Enable", 0x000C,
                    "The LE_Set_Scan_Enable command is used to start scanning.\n" +
                    "Scanning is used to discover advertising devices nearby.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("LE_Scan_Enable", 1, ParameterDataType.Bytes,
                    "0x00 Scanning is disabled\n" +
                    "0x01 Scanning is enabled.\n" +
                    "0x02–0xFF Reserved for future use");
                c.CommandParameters.Add("Filter_Duplicates", 1, ParameterDataType.Bytes,
                    "0x00 Filter_Duplicates is disabled\n" +
                    "0x01 Filter_Duplicates is enabled.\n" +
                    "0x02–0xFF Reserved for future use");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Set_Scan_Enable command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Set_Scan_Enable command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Create_Connection", 0x000D,
                    "The LE_Create_Connection command is used to create a Link Layer connection to\n" +
                    "a connectable advertiser.\n" +
                    "The LE_Scan_Interval and LE_Scan_Window parameters are recommendations from\n" +
                    "the Host on how long(LE_Scan_Window) and how frequently(LE_Scan_Interval)\n" +
                    "the Controller should scan.\n" +
                    "The LE_Scan_Window parameter shall be set to a value smaller or equal to the\n" +
                    "value set for the LE_Scan_Interval parameter.If both are set to the same value,\n" +
                    "scanning should run continuously.");
                c.SendCommand = SendCommand;

                p = c.CommandParameters.Add("LE_Scan_Interval", 2, ParameterDataType.Time_625usec,
                    "This is defined as the time interval from when the Controller\n" +
                    "started its last LE scan until it begins the subsequent LE scan.\n" +
                    "Range: 0x0004 to 0x4000\n" +
                    "Time = N * 0.625 msec, Time Range: 2.5 msec to 10.24 seconds");
                p.Data = new byte[] { 0x40, 0x00 };
                p = c.CommandParameters.Add("LE_Scan_Window", 2, ParameterDataType.Time_625usec,
                    "Amount of time for the duration of the LE scan.\n" +
                    "LE_Scan_Window shall be less than or equal to LE_Scan_Interval\n" +
                    "Range: 0x0004 to 0x4000\n" +
                    "Time = N * 0.625 msec, Time Range: 2.5 msec to 10.24 seconds");
                p.Data = new byte[] { 0x40, 0x00 };
                c.CommandParameters.Add("Initiator_Filter_Policy", 1, ParameterDataType.Bytes,
                    "0x00 White list is not used to determine which advertiser to connect to.\n" +
                    "     Peer_Address_Type and Peer_Address shall be used.\n" +
                    "0x01 White list is used to determine which advertiser to connect to.\n" +
                    "     Peer_Address_Type and Peer_Address shall be ignored.\n" +
                    "0x02–0xFF Reserved for future use");
                c.CommandParameters.Add("Peer_Address_Type", 1, ParameterDataType.Bytes,
                    "0x00 Public Device Address\n" +
                    "0x01 Random Device Address\n" +
                    "0x02 Public Identity Address(Corresponds to peer’s Resolvable Private Address)\n" +
                    "0x03 Random(static) Identity Address (Corresponds to peer’s Resolvable Private Address)\n" +
                    "0x04–0xFF Reserved for future use");
                c.CommandParameters.Add("Peer_Address", 6, ParameterDataType.Bytes,
                    "Public Device Address, Random Device Address, Public Identity Address,\n" +
                    "or Random(static) Identity Address of the device to be connected");
                c.CommandParameters.Add("Own_Address_Type", 1, ParameterDataType.Bytes,
                    "0x00 Public Device Address (default)\n" +
                    "0x01 Random Device Address\n" +
                    "0x02 Controller generates Resolvable Private Address based on the local IRK from resolving list.\n" +
                    "     If resolving list contains no matching entry,use public address.\n" +
                    "0x03 Controller generates Resolvable Private Address based on the local IRK from resolving list.\n" +
                    "     If resolving list contains no matching entry, use random address from LE_Set_Random_Address.\n" +
                    "0x04–0xFF Reserved for future use");
                p = c.CommandParameters.Add("Conn_Interval_Min", 2, ParameterDataType.Time_1_25msec,
                    "Minimum value for the connection event interval. This shall be less than or equal to Conn_Interval_Max.\n" +
                    "Range: 0x0006 to 0x0C80,\n" +
                    "Time = N * 1.25 msec, Time Range: 7.5 msec to 4 seconds.");
                p.Data = new byte[] { 0x40, 0x00 };
                p = c.CommandParameters.Add("Conn_Interval_Max", 2, ParameterDataType.Time_1_25msec,
                    "Maximum value for the connection event interval. This shall be greater than or equal to Conn_Interval_Min.\n" +
                    "Range: 0x0006 to 0x0C80,\n" +
                    "Time = N * 1.25 msec, Time Range: 7.5 msec to 4 seconds.");
                p.Data = new byte[] { 0x40, 0x00 };
                c.CommandParameters.Add("Conn_Latency", 2, ParameterDataType.Bytes,
                    "Slave latency for the connection in number of connection events.\n" +
                    "Range: 0x0000 to 0x01F3");
                p = c.CommandParameters.Add("Supervision_Timeout", 2, ParameterDataType.Time_10msec,
                    "Supervision timeout for the LE Link. (See [Vol 6] Part B, Section 4.5.2)\n" +
                    "Range: 0x000A to 0x0C80\n" +
                    "Time = N * 10 msec, Time Range: 100 msec to 32 seconds");
                p.Data = new byte[] { 0x80, 0x0C };
                c.CommandParameters.Add("Minimum_CE_Length", 2, ParameterDataType.Time_625usec,
                    "Information parameter about the minimum length of connection event needed for this LE connection.\n" +
                    "Range: 0x0000 – 0xFFFF, Time = N * 0.625 msec.");
                c.CommandParameters.Add("Maximum_CE_Length", 2, ParameterDataType.Time_625usec,
                    "Information parameter about the maximum length of connection event needed for this LE connection.\n" +
                    "Range: 0x0000 – 0xFFFF, Time = N * 0.625 msec.");

                c = cg.Commands.Add("HCI_LE_Create_Connection_Cancel", 0x000E,
                    "The LE_Create_Connection_Cancel command is used to cancel the LE_Create_Connection command.\n" +
                    "This command shall only be issued after the LE_Create_Connection command has been issued,\n" +
                    "a Command Status event has been received for the LE Create Connection command and before\n" +
                    "the LE Connection Complete event.");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Create_Connection_Cancel command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Create_Connection_Cancel command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Read_White_List_Size", 0x000F,
                    "The LE_Read_White_List_Size command is used to read the total number of\n" +
                    "white list entries that can be stored in the Controller.");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Read_White_List_Size command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Read_White_List_Size command failed. See Pard D, Error Codes");
                c.ReturnParameters.Add("White_List_Size", 1, ParameterDataType.Number,
                    "0x01–0xFF Total number of white list entries that can be stored in the Controller.\n" +
                    "0x00 Reserved for future use");

                c = cg.Commands.Add("HCI_LE_Clear_White_List", 0x0010,
                    "The LE_Clear_White_List command is used to clear the white list stored in the Controller.\n" +
                    "This command can be used at any time except when:\n" +
                    "   • the advertising filter policy uses the white list and advertising is enabled.\n" +
                    "   • the scanning filter policy uses the white list and scanning is enabled.\n" +
                    "   • the initiator filter policy uses the white list and an LE_Create_Connection command is outstanding.");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Clear_White_List command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Clear_White_List command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Add_Device_To_White_List", 0x0011,
                    "The LE_Add_Device_To_White_List command is used to add a single device to the white list stored in the Controller.\n" +
                    "This command can be used at any time except when:\n" +
                    "   • the advertising filter policy uses the white list and advertising is enabled.\n" +
                    "   • the scanning filter policy uses the white list and scanning is enabled.\n" +
                    "   • the initiator filter policy uses the white list and a create connection command is outstanding.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Address_Type", 1, ParameterDataType.Bytes,
                    "0x00 Public Device Address\n" +
                    "0x01 Random Device Address\n" +
                    "0x02-0xFF Reserved for future use.");
                c.CommandParameters.Add("Address", 6, ParameterDataType.Bytes,
                    "Public Device Address or Random Device Address of the device to be added to the white list.");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Add_Device_To_White_List command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Add_Device_To_White_List command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Remove_Device_From_White_List", 0x0012,
                    "The LE_Remove_Device_From_White_List command is used to remove a single device from the white list stored in the Controller.\n" +
                    "This command can be used at any time except when:\n" +
                    "   • the advertising filter policy uses the white list and advertising is enabled.\n" +
                    "   • the scanning filter policy uses the white list and scanning is enabled.\n" +
                    "   • the initiator filter policy uses the white list and a create connection command is outstanding.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Address_Type", 1, ParameterDataType.Bytes,
                    "0x00 Public Device Address\n" +
                    "0x01 Random Device Address\n" +
                    "0x02-0xFF Reserved for future use.");
                c.CommandParameters.Add("Address", 6, ParameterDataType.Bytes,
                    "Public Device Address or Random Device Address of the device to be added to the white list.");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Remove_Device_From_White_List command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Remove_Device_From_White_List command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Connection_Update", 0x0013,
                    "The LE_Connection_Update command is used to change the Link Layer connection parameters of a connection.\n" +
                    "This command may be issued on both the master and slave.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle to be used to identify a connection.\n" +
                    "Range 0x0000 - 0x0EFF(0x0F00 – 0x0FFF Reserved for  future use)");
                p = c.CommandParameters.Add("Conn_Interval_Min", 2, ParameterDataType.Time_1_25msec,
                    "Minimum value for the connection event interval. This shall be less than or equal to Conn_Interval_Max.\n" +
                    "Range: 0x0006 to 0x0C80,\n" +
                    "Time = N * 1.25 msec, Time Range: 7.5 msec to 4 seconds.");
                p.Data = new byte[] { 0x40, 0x00 };
                p = c.CommandParameters.Add("Conn_Interval_Max", 2, ParameterDataType.Time_1_25msec,
                    "Maximum value for the connection event interval. This shall be greater than or equal to Conn_Interval_Min.\n" +
                    "Range: 0x0006 to 0x0C80,\n" +
                    "Time = N * 1.25 msec, Time Range: 7.5 msec to 4 seconds.");
                p.Data = new byte[] { 0x40, 0x00 };
                c.CommandParameters.Add("Conn_Latency", 2, ParameterDataType.Bytes,
                    "Slave latency for the connection in number of connection events.\n" +
                    "Range: 0x0000 to 0x01F3");
                p = c.CommandParameters.Add("Supervision_Timeout", 2, ParameterDataType.Time_10msec,
                    "Supervision timeout for the LE Link. (See [Vol 6] Part B, Section 4.5.2)\n" +
                    "Range: 0x000A to 0x0C80\n" +
                    "Time = N * 10 msec, Time Range: 100 msec to 32 seconds");
                p.Data = new byte[] { 0x80, 0x0C };
                c.CommandParameters.Add("Minimum_CE_Length", 2, ParameterDataType.Time_625usec,
                    "Information parameter about the minimum length of connection event needed for this LE connection.\n" +
                    "How this value is used is outside the scope of this specification.\n" +
                    "Range: 0x0000 – 0xFFFF, Time = N * 0.625 msec.");
                c.CommandParameters.Add("Maximum_CE_Length", 2, ParameterDataType.Time_625usec,
                    "Information parameter about the maximum length of connection event needed for this LE connection.\n" +
                    "How this value is used is outside the scope of this specification.\n" +
                    "Range: 0x0000 – 0xFFFF, Time = N * 0.625 msec.");

                c = cg.Commands.Add("HCI_LE_Set_Host_Channel_Classification", 0x0014,
                    "The LE_Set_Host_Channel_Classification command allows the Host to specify\n" +
                    "a channel classification for data channels based on its \"local information\".\n" +
                    "This classification persists until overwritten with a subsequent\n" +
                    "LE_Set_Host_Channel_Classification command or until the Controller is reset\n" +
                    "using the Reset command(see[Vol 6] Part B, Section 4.5.8.1).");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Channel_Map", 5, ParameterDataType.Bytes,
                    "This parameter contains 37 1-bit fields.\n" +
                    "The n-th such field(in the range 0 to 36) contains the value for the link layer channel index n.\n" +
                    "Channel n is bad = 0.\n" +
                    "Channel n is unknown = 1.\n" +
                    "The most significant bits are reserved and shall be set to 0.\n" +
                    "At least one channel shall be marked as unknown.");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Set_Host_Channel_Classification command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Set_Host_Channel_Classification command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Read_Channel_Map", 0x0015,
                    "The LE_Read_Channel_Map command returns the current Channel_Map for\n" +
                    "the specified Connection_Handle.The returned value indicates the state of the\n" +
                    "Channel_Map specified by the last transmitted or received Channel_Map(in a\n" +
                    "CONNECT_REQ or LL_CHANNEL_MAP_REQ message) for the specified Connection_Handle,\n" +
                    "regardless of whether the Master has received an acknowledgement.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "The Connection_Handle for the Connection for which the Channel_Map is to be read.\n" +
                    "Range 0x0000 - 0x0EFF(0x0F00 – 0x0FFF Reserved for future use) ");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Read_Channel_Map command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Read_Channel_Map command failed. See Pard D, Error Codes");
                c.ReturnParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle to be used to identify a connection.\n" +
                    "Range 0x0000 - 0x0EFF(0x0F00 – 0x0FFF Reserved for future use)");
                c.ReturnParameters.Add("Channel_Map", 5, ParameterDataType.Bytes,
                    "This parameter contains 37 1-bit fields.\n" +
                    "The nth such field(in the range 0 to 36) contains the value for the link layer channel index n.\n" +
                    "Channel n is unused = 0.\n" +
                    "Channel n is used = 1.\n" +
                    "The most significant bits are reserved and shall be set to 0.");

                c = cg.Commands.Add("HCI_LE_Read_Remote_Used_Features", 0x0016,
                    "This command requests a list of the used LE features from the remote device.\n" +
                    "This command shall return a list of the used LE features. For details see[Vol 6] Part B, Section 4.6.\n" +
                    "This command may be issued on both the master and slave.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle to be used to identify a connection.\n" +
                    "Range 0x0000 - 0x0EFF(0x0F00 – 0x0FFF Reserved for future use) ");

                c = cg.Commands.Add("HCI_LE_Encrypt", 0x0017,
                    "The LE_Encrypt command is used to request the Controller to encrypt the Plaintext_Data\n" +
                    "in the command using the Key given in the command and returns the Encrypted_Data to the Host.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Key", 16, ParameterDataType.Bytes,
                    "128 bit key for the encryption of the data given in the command.\n" +
                    "The most significant octet of the key corresponds to key[0] using\n" +
                    "the notation specified in FIPS 197.");
                c.CommandParameters.Add("Plaintext_Data", 16, ParameterDataType.Bytes,
                    "128 bit data block that is requested to be encrypted.\n" +
                    "The most significant octet of the PlainText_Data corresponds to\n" +
                    "in[0] using the notation specified in FIPS 197.");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Encrypt command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Encrypt command failed. See Pard D, Error Codes");
                c.ReturnParameters.Add("Encrypted_Data", 16, ParameterDataType.Bytes,
                    "128 bit encrypted data block.\n" +
                    "The most significant octet of the Encrypted_Data corresponds to\n" +
                    "out[0] using the notation specified in FIPS 197.");

                c = cg.Commands.Add("HCI_LE_Rand", 0x0018,
                    "The LE_Rand command is used to request the Controller to generate 8 octets\n" +
                    "of random data to be sent to the Host.The Random_Number shall be generated\n" +
                    "according to[Vol 2] Part H, Section 2 if the LE Feature (LE Encryption) is supported.");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Rand command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Rand command failed. See Pard D, Error Codes");
                c.ReturnParameters.Add("Random_Number", 8, ParameterDataType.Bytes,
                    "Random Number");

                c = cg.Commands.Add("HCI_LE_Start_Encryption", 0x0019,
                    "The LE_Start_Encryption command is used to authenticate the given encryption key\n" +
                    "associated with the remote device specified by the connection handle,\n" +
                    "and once authenticated will encrypt the connection.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle to be used to identify a connection.\n" +
                    "Range 0x0000 - 0x0EFF(0x0F00 – 0x0FFF Reserved for future use)");
                c.CommandParameters.Add("Random_Number", 8, ParameterDataType.Bytes,
                    "64 bit random number.");
                c.CommandParameters.Add("Encrypted_Diversifier", 2, ParameterDataType.Bytes,
                    "16 bit encrypted diversifier.");
                c.CommandParameters.Add("Long_Term_Key", 16, ParameterDataType.Bytes,
                    "128 bit long term key.");

                c = cg.Commands.Add("HCI_LE_Long_Term_Key_Request_Reply", 0x001A,
                    "The LE_Long_Term_Key_Request Reply command is used to reply to an LE\n" +
                    "Long Term Key Request event from the Controller, and specifies the\n" +
                    "Long_Term_Key parameter that shall be used for this Connection_Handle.\n" +
                    "The Long_Term_Key is used as defined in [Vol 6] Part B, Section 5.1.3.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle to be used to identify a connection.\n" +
                    "Range 0x0000 - 0x0EFF(0x0F00 – 0x0FFF Reserved for future use)");
                c.CommandParameters.Add("Long Term Key", 16, ParameterDataType.Bytes,
                    "128 bit long term key for the given connection.");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Long_Term_Key_Request_Reply command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Long_Term_Key_Request_Reply command failed. See Pard D, Error Codes");
                c.ReturnParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle to be used to identify a connection.\n" +
                    "Range 0x0000 - 0x0EFF(0x0F00 – 0x0FFF Reserved for future use)");

                c = cg.Commands.Add("HCI_LE_Long_Term_Key_Request_Negative_Reply", 0x001B,
                    "The LE_Long_Term_Key_Request_Negative_Reply command is used to reply\n" +
                    "to an LE Long Term Key Request event from the Controller if the Host cannot\n" +
                    "provide a Long Term Key for this Connection_Handle.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle to be used to identify a connection.\n" +
                    "Range 0x0000 - 0x0EFF(0x0F00 – 0x0FFF Reserved for future use)");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Long_Term_Key_Request_Negative_Reply command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Long_Term_Key_Request_Negative_Reply command failed. See Pard D, Error Codes");
                c.ReturnParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle to be used to identify a connection.\n" +
                    "Range 0x0000 - 0x0EFF(0x0F00 – 0x0FFF Reserved for future use)");

                c = cg.Commands.Add("HCI_LE_Read_Supported_States", 0x001C,
                    "The LE_Long_Term_Key_Request_Negative_Reply command is used to reply\n" +
                    "to an LE Long Term Key Request event from the Controller if the Host cannot\n" +
                    "provide a Long Term Key for this Connection_Handle.");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Read_Supported_States command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Read_Supported_States command failed. See Pard D, Error Codes");
                c.ReturnParameters.Add("LE_States", 8, ParameterDataType.Bytes,
                    "0x0000000000000000 Reserved for future use\n" +
                    "0x0000000000000001 Non-connectable Advertising State\n" +
                    "0x0000000000000002 Scannable Advertising State\n" +
                    "0x0000000000000004 Connectable Advertising State\n" +
                    "0x0000000000000008 High Duty Cycle Directed Advertising State\n" +
                    "0x0000000000000010 Passive Scanning State\n" +
                    "0x0000000000000020 Active Sacnning State\n" +
                    "0x0000000000000040 Initiating State\n" +
                    "0x0000000000000080 Connection State(Slave Role)\n" +
                    "0x0000000000000100 Non-connectable Advertising State, Passive Scanning State\n" +
                    "0x0000000000000200 Scannable Advertising State, Passive Scanning State\n" +
                    "0x0000000000000400 Connectable Advertising State, Passive Scanning State\n" +
                    "0x0000000000000800 High Duty Cycle Directed Advertising State, Passive Scanning State\n" +
                    "0x0000000000001000 Non-connectable Advertising State, Active Sacnning State\n" +
                    "0x0000000000002000 Scannable Advertising State, Active Sacnning State\n" +
                    "0x0000000000004000 Connectable Advertising State, Active Sacnning State\n" +
                    "0x0000000000008000 High Duty Cycle Directed Advertising State, Active Sacnning State\n" +
                    "0x0000000000010000 Non-connectable Advertising State, Initiating State\n" +
                    "0x0000000000020000 Scannable Advertising State, Initiating State\n" +
                    "0x0000000000040000 Non-connectable Advertising State, Connection State(Master Role)\n" +
                    "0x0000000000080000 Scannable Advertising State, Connection State(Master Role)\n" +
                    "0x0000000000100000 Non-connectable Advertising State, Connection State(Slave Role)\n" +
                    "0x0000000000200000 Scannable Advertising State, Connection State(Slave Role)\n" +
                    "0x0000000000400000 Passive Scanning State, Initiating State\n" +
                    "0x0000000000800000 Active Sacnning State, Initiating State\n" +
                    "0x0000000001000000 Passive Scanning State, Connection State(Master Role)\n" +
                    "0x0000000002000000 Active Sacnning State, Connection State(Master Role)\n" +
                    "0x0000000004000000 Passive Scanning State, Connection State(Slave Role)\n" +
                    "0x0000000008000000 Active Sacnning State, Connection State(Slave Role)\n" +
                    "0x0000000010000000 Initiating State, Connection State(Master Role)\n" +
                    "0x0000000020000000 Low Duty Cycle Directed Advertising State\n" +
                    "0x0000000040000000 Low Duty Cycle Directed Advertising State, Passive Scanning State\n" +
                    "0x0000000080000000 Low Duty Cycle Directed Advertising State, Active Sacnning State\n" +
                    "0x0000000100000000 Connectable Advertising State, Initiating State\n" +
                    "0x0000000200000000 High Duty Cycle Directed Advertising State, Initiating State\n" +
                    "0x0000000400000000 Low Duty Cycle Directed Advertising State, Initiating State\n" +
                    "0x0000000800000000 Connectable Advertising State, Connection State(Master Role)\n" +
                    "0x0000001000000000 High Duty Cycle Directed Advertising State, Connection State(Master Role)\n" +
                    "0x0000002000000000 Low Duty Cycle Directed Advertising State, Connection State(Master Role)\n" +
                    "0x0000004000000000 Connectable Advertising State, Connection State(Slave Role)\n" +
                    "0x0000008000000000 High Duty Cycle Directed Advertising State, Connection State(Slave Role)\n" +
                    "0x0000010000000000 Low Duty Cycle Directed Advertising State, Connection State(Slave Role)\n" +
                    "0x0000020000000000 Initiating State, Connection State(Slave Role)\n" +
                    "0xFFFFFC0000000000 Reserved for future use");

                c = cg.Commands.Add("HCI_LE_Receiver_Test", 0x001D,
                    "This command is used to start a test where the DUT receives test reference\n" +
                    "packets at a fixed interval.The tester generates the test reference packets.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("RX_Channel", 1, ParameterDataType.BLE_Channel,
                    "N = (F – 2402) / 2\n" +
                    "Range: 0x00 – 0x27.Frequency Range: 2402 MHz to 2480 MHz");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Receiver_Test command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Receiver_Test command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Transmitter_Test", 0x001E,
                    "This command is used to start a test where the DUT generates test reference\n" +
                    "packets at a fixed interval.The Controller shall transmit at maximum power.\n" +
                    "An LE Controller supporting the LE_Transmitter_Test command shall support\n" +
                    "Packet_Payload values 0x00, 0x01 and 0x02.\n" +
                    "An LE Controller may support other values of Packet_Payload.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("TX_Channel", 1, ParameterDataType.BLE_Channel,
                    "N = (F – 2402) / 2\n" +
                    "Range: 0x00 – 0x27.Frequency Range: 2402 MHz to 2480 MHz");
                c.CommandParameters.Add("Length_Of_Test_Data", 1, ParameterDataType.Bytes,
                    "0x00-0xFF Length in bytes of payload data in each packet");
                c.CommandParameters.Add("Packet_Payload", 1, ParameterDataType.Bytes,
                    "0x00 PRBS9 sequence ‘11111111100000111101…’ (in transmission order)\n" +
                    "     as described in [Vol 6] Part F, Section 4.1.5\n" +
                    "0x01 Repeated ‘11110000’ (in transmission order) sequence as described in [Vol 6] Part F, Section 4.1.5\n" +
                    "0x02 Repeated ‘10101010’ (in transmission order) sequence as described in [Vol 6] Part F, Section 4.1.5\n" +
                    "0x03 PRBS15 sequence as described in [Vol 6] Part F, Section 4.1.5\n" +
                    "0x04 Repeated ‘11111111’ (in transmission order) sequence\n" +
                    "0x05 Repeated ‘00000000’ (in transmission order) sequence\n" +
                    "0x06 Repeated ‘00001111’ (in transmission order) sequence\n" +
                    "0x07 Repeated ‘01010101’ (in transmission order) sequence\n" +
                    "0x08-0xFF Reserved for future use");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Transmitter_Test command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Transmitter_Test command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Test_End", 0x001F,
                    "This command is used to stop any test which is in progress.\n" +
                    "The Number_Of_Packets for a transmitter test shall be reported as 0x0000.\n" +
                    "The Number_Of_Packets is an unsigned number and contains the number of received packets.");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Test_End command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Test_End command failed. See Pard D, Error Codes");
                c.ReturnParameters.Add("Number_Of_Packets", 2, ParameterDataType.Number,
                    "Number of packets received");

                c = cg.Commands.Add("LE_Remote_Connection_Parameter_Request_Reply", 0x0020,
                    "Both the master Host and the slave Host use this command to reply to the HCI\n" +
                    "LE Remote Connection Parameter Request event. This indicates that the Host\n" +
                    "has accepted the remote device’s request to change connection parameters.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle to be used to identify a connection.\n" +
                    "Range 0x0000 - 0x0EFF(0x0F00 – 0x0FFF Reserved for future use)");
                c.CommandParameters.Add("Interval_Min", 2, ParameterDataType.Time_1_25msec,
                    "Minimum value of the connection interval.\n" +
                    "Range: 0x0006 to 0x0C80\n" +
                    "Time = N * 1.25 ms, Time Range: 7.5 msec to 4 seconds");
                c.CommandParameters.Add("Interval_Max", 2, ParameterDataType.Time_1_25msec,
                    "Maximum value of the connection interval.\n" +
                    "Range: 0x0006 to 0x0C80\n" +
                    "Time = N * 1.25 ms, Time Range: 7.5 msec to 4 seconds");
                c.CommandParameters.Add("Latency", 2, ParameterDataType.Bytes,
                    "Maximum allowed slave latency for the connection specified as the\n" +
                    "number of connection events. Range: 0x0000 to 0x01F3(499)");
                c.CommandParameters.Add("Timeout", 2, ParameterDataType.Time_10msec,
                    "Supervision timeout for the connection.\n" +
                    "Range: 0x000A to 0x0C80\n" +
                    "Time = N * 10 ms, Time Range: 100 ms to 32 seconds");
                c.CommandParameters.Add("Minimum_CE_Length", 2, ParameterDataType.Time_625usec,
                    "Information parameter about the minimum length of connection event\n" +
                    "needed for this LE connection.\n" +
                    "Range: 0x0000 to 0xFFFF\n" +
                    "Time = N * 0.625 ms, Time Range: 0 ms to 40.9 seconds");
                c.CommandParameters.Add("Maximum_CE_Length", 2, ParameterDataType.Time_625usec,
                    "Information parameter about the maximum length of connection event\n" +
                    "needed for this LE connection.\n" +
                    "Range: 0x0000 to 0xFFFF\n" +
                    "Time = N * 0.625 ms, Time Range: 0 ms to 40.9 seconds");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 LE_Remote_Connection_Parameter_Request_Reply command succeeded.\n" +
                    "0x01–0xFF LE_Remote_Connection_Parameter_Request_Reply command failed. See Pard D, Error Codes");
                c.ReturnParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle to be used to identify a connection.\n" +
                    "Range 0x0000 - 0x0EFF(0x0F00 – 0x0FFF Reserved for future use)");

                c = cg.Commands.Add("LE_Remote_Connection_Parameter_Request_Negative_Reply", 0x0021,
                    "Both the master Host and the slave Host use this command to reply to the HCI\n" +
                    "LE Remote Connection Parameter Request event. This indicates that the Host\n" +
                    "has rejected the remote device’s request to change connection parameters.\n" +
                    "The reason for the rejection is given in the Reason parameter.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle to be used to identify a connection.\n" +
                    "Range 0x0000 - 0x0EFF(0x0F00 – 0x0FFF Reserved for future use)");
                c.CommandParameters.Add("Reason", 1, ParameterDataType.Status,
                    "Reason that the connection parameter request was rejected.\n" +
                    "See [Vol 2] Part D, Error Codes for a list of error codes and descriptions.");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 LE_Remote_Connection_Parameter_Request_Negative_Reply command succeeded.\n" +
                    "0x01–0xFF LE_Remote_Connection_Parameter_Request_Negative_Reply command failed. See Pard D, Error Codes");
                c.ReturnParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle to be used to identify a connection.\n" +
                    "Range 0x0000 - 0x0EFF(0x0F00 – 0x0FFF Reserved for future use)");

                c = cg.Commands.Add("HCI_LE_Set_Data_Length", 0x0022,
                    "The LE_Set_Data_Length command allows the Host to suggest maximum\n" +
                    "transmission packet size and maximum packet transmission time\n" +
                    "(connMaxTxOctets and connMaxTxTime - see[Vol 6] Part B, Section 4.5.10) to\n" +
                    "be used for a given connection.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle to be used to identify a connection.\n" +
                    "Range 0x0000 - 0x0EFF(0x0F00 – 0x0FFF Reserved for future use)");
                c.CommandParameters.Add("TxOctets", 2, ParameterDataType.Bytes,
                    "Preferred maximum number of payload octets that the local Controller\n" +
                    "should include in a single Link Layer Data Channel PDU.\n" +
                    "Range 0x001B - 0x00FB");
                c.CommandParameters.Add("TxTime", 2, ParameterDataType.Time_1usec,
                    "Preferred maximum number of microseconds that the local Controller\n" +
                    "should use to transmit a single Link Layer Data Channel PDU.\n" +
                    "Range 0x0148 - 0x0848");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Set_Data_Length command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Set_Data_Length command failed. See Pard D, Error Codes");
                c.ReturnParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle to be used to identify a connection.\n" +
                    "Range 0x0000 - 0x0EFF(0x0F00 – 0x0FFF Reserved for future use)");

                c = cg.Commands.Add("HCI_LE_Read_Suggested_Default_Data_Length", 0x0023,
                    "The LE_Read_Suggested_Default_Data_Length command allows the Host to\n" +
                    "read the Host's preferred values for the Controller's maximum transmitted\n" +
                    "number of payload octets and maximum packet transmission time to be used\n" +
                    "for new connections(connInitialMaxTxOctets and connInitialMaxTxTime - see\n" +
                    "([Vol 6] Part B, Section 4.5.10).");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Read_Suggested_Default_Data_Length command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Read_Suggested_Default_Data_Length command failed. See Pard D, Error Codes");
                c.ReturnParameters.Add("SuggestedMaxTxOctets", 2, ParameterDataType.Bytes,
                    "The Host's suggested value for the Controller's maximum transmitted\n" +
                    "number of payload octets to be used for new connections - connInitialMaxTxOctets.\n" +
                    "Range 0x001B - 0x00FB, Default: 0x001B");
                c.ReturnParameters.Add("SuggestedMaxTxTime", 2, ParameterDataType.Time_1usec,
                    "The Host's suggested value for the Controller's maximum packet transmission\n" +
                    "time to be used for new connections - connInitialMaxTxTime.\n" +
                    "Range 0x0148 - 0x0848, Default: 0x0148");

                c = cg.Commands.Add("HCI_LE_Write_Suggested_Default_Data_Length", 0x0024,
                    "The LE_Write_Suggested_Default_Data_Length command allows the Host to\n" +
                    "specify its preferred values for the Controller's maximum transmission number\n" +
                    "of payload octets and maximum packet transmission time to be used for new\n" +
                    "connections(connInitialMaxTxOctets and connInitialMaxTxTime - see[Vol 6]\n" +
                    "Part B, Section 4.5.10).\n" +
                    "The Controller may use smaller or larger values based on local information.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("SuggestedMaxTxOctets", 2, ParameterDataType.Bytes,
                    "The Host's suggested value for the Controller's maximum transmitted\n" +
                    "number of payload octets to be used for new connections - connInitialMaxTxOctets.\n" +
                    "Range 0x001B - 0x00FB");
                c.CommandParameters.Add("SuggestedMaxTxTime", 2, ParameterDataType.Time_1usec,
                    "The Host's suggested value for the Controller's maximum packet\n" +
                    "transmission time to be used for new connections - connInitialMaxTxTime.\n" +
                    "Range 0x0148 - 0x0848");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Write_Suggested_Default_Data_Length command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Write_Suggested_Default_Data_Length command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Read_Local_P-256_Public_Key", 0x0025,
                    "The LE_Read_Local_P-256_Public_Key command is used to return the local\n" +
                    "P-256 public key from the Controller.The Controller shall generate a new P-256\n" +
                    "public/private key pair upon receipt of this command.\n" +
                    "The keys returned via this command shall not be used when Secure\n" +
                    "Connections is used over the BR/EDR transport.");
                c.SendCommand = SendCommand;

                c = cg.Commands.Add("HCI_LE_Generate_DHKey", 0x0026,
                    "The LE_Generate_DHKey command is used to initiate generation of a Diffie-\n" +
                    "Hellman key in the Controller for use over the LE transport.This command\n" +
                    "takes the remote P-256 public key as input.\n" +
                    "The Diffie-Hellman key generation uses the private key generated by\n" +
                    "LE_Read_Local_P256_Public_Key command.\n" +
                    "The Diffie-Hellman key returned via this command shall not be generated using\n" +
                    "any keys used for Secure Connections over the BR/EDR transport.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Remote_P-256_Public_Key", 64, ParameterDataType.Bytes,
                    "The remote P-256 public key: X, Y format\n" +
                    "Octets 31 - 0: X co - ordinate\n" +
                    "Octets 63 - 32: Y co - ordinate\n" +
                    "Little Endian Format");

                c = cg.Commands.Add("HCI_LE_Add_Device_To_Resolving_List", 0x0027,
                    "The LE_Add_Device_To_Resolving_List command is used to add one device to the list of\n" +
                    "address translations used to resolve Resolvable Private Addresses in the Controller.\n" +
                    "This command cannot be used when address translation is enabled in the Controller and:\n" +
                    "   • Advertising is enabled\n" +
                    "   • Scanning is enabled\n" +
                    "   • Create connection command is outstanding\n" +
                    "This command can be used at any time when address translation is disabled in the Controller.\n" +
                    "When a Controller cannot add a device to the resolving list because the list is full,\n" +
                    "it shall respond with error code 0x07(Memory Capacity Exceeded).");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Peer_Identity_Address_Type", 1, ParameterDataType.Bytes,
                    "0x00 Public Identity Address\n" +
                    "0x01 Random(static) Identity Address\n" +
                    "0x02 – 0xFF Reserved for Future Use");
                c.CommandParameters.Add("Peer_Identity_Address", 6, ParameterDataType.Bytes,
                    "Public or Random (static) Identity address of the peer device");
                c.CommandParameters.Add("Peer_IRK", 16, ParameterDataType.Bytes,
                    "IRK of the peer device");
                c.CommandParameters.Add("Local_IRK", 16, ParameterDataType.Bytes,
                    "IRK of the local device");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Add_Device_To_Resolving_List command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Add_Device_To_Resolving_List command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Remove_Device_From_Resolving_List", 0x0028,
                    "The LE_Remove_Device_From_Resolving_List command is used to remove one device from the list\n" +
                    "of address translations used to resolve Resolvable Private Addresses in the controller.\n" +
                    "This command cannot be used when address translation is enabled in the Controller and:\n" +
                    "   • Advertising is enabled\n" +
                    "   • Scanning is enabled\n" +
                    "   • Create connection command is outstanding\n" +
                    "This command can be used at any time when address translation is disabled in the Controller.\n" +
                    "When a Controller cannot remove a device from the resolving list because it is not found,\n" +
                    "it shall respond with error code 0x02(Unknown Connection Identifier).");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Peer_Identity_Address_Type", 1, ParameterDataType.Bytes,
                    "0x00 Public Identity Address\n" +
                    "0x01 Random(static) Identity Address\n" +
                    "0x02 – 0xFF Reserved for Future Use");
                c.CommandParameters.Add("Peer_Device_Address", 6, ParameterDataType.Bytes,
                    "Public or Random (static) Identity Address of the peer device");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Remove_Device_From_Resolving_List command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Remove_Device_From_Resolving_List command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Clear_Resolving_List", 0x0029,
                    "The LE_Clear_Resolving_List command is used to remove all devices from the list of\n" +
                    "address translations used to resolve Resolvable Private Addresses in the Controller.\n" +
                    "This command cannot be used when address translation is enabled in the Controller and:\n" +
                    "   • Advertising is enabled\n" +
                    "   • Scanning is enabled\n" +
                    "   • Create connection command is outstanding\n" +
                    "This command can be used at any time when address translation is disabled in the Controller.");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Clear_Resolving_List command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Clear_Resolving_List command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Read_Resolving_List_Size", 0x002A,
                    "The LE_Read_Resolving_List_Size command is used to read the total number of address\n" +
                    "translation entries in the resolving list that can be stored in the Controller.");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Read_Resolving_List_Size command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Read_Resolving_List_Size command failed. See Pard D, Error Codes");
                c.ReturnParameters.Add("Resolving_List_Size", 1, ParameterDataType.Number,
                    "Number of address translation entries in the resolving list");

                c = cg.Commands.Add("HCI_LE_Read_Peer_Resolvable_Address", 0x002B,
                    "The LE_Read_Peer_Resolvable_Address command is used to get the current\n" +
                    "peer Resolvable Private Address being used for the corresponding peer Public\n" +
                    "and Random(static) Identity Address. The peer’s resolvable address being\n" +
                    "used may change after the command is called.\n" +
                    "This command can be used at any time.\n" +
                    "When a Controller cannot find a Resolvable Private Address associated with\n" +
                    "the Peer Identity Address, it shall respond with error code 0x02(Unknown\n" +
                    "Connection Identifier).");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Peer_Identity_Address_Type", 1, ParameterDataType.Bytes,
                    "0x00 Public Identity Address\n" +
                    "0x01 Random(static) Identity Address\n" +
                    "0x02–0xFF Reserved for Future Use");
                c.CommandParameters.Add("Peer_Identity_Address", 6, ParameterDataType.Bytes,
                    "Public or Random (static) Identity Address of the peer device");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Read_Peer_Resolvable_Address command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Read_Peer_Resolvable_Address command failed. See Pard D, Error Codes");
                c.ReturnParameters.Add("Peer_Resolvable_Address", 6, ParameterDataType.Bytes,
                    "Resolvable Private Address being used by the peer device");

                c = cg.Commands.Add("HCI_LE_Read_Local_Resolvable_Address", 0x002C,
                    "The LE_Read_Local_Resolvable_Address command is used to get the current\n" +
                    "local Resolvable Private Address being used for the corresponding peer\n" +
                    "Identity Address.The local’s resolvable address being used may change after\n" +
                    "the command is called.\n" +
                    "This command can be used at any time.\n" +
                    "When a Controller cannot find a Resolvable Private Address associated with\n" +
                    "the Peer Identity Address, it shall respond with error code 0x02(Unknown\n" +
                    "Connection Identifier).");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Peer_Identity_Address_Type", 1, ParameterDataType.Bytes,
                    "0x00 Public Identity Address\n" +
                    "0x01 Random(static) Identity Address\n" +
                    "0x02–0xFF Reserved for Future Use");
                c.CommandParameters.Add("Peer_Identity_Address", 6, ParameterDataType.Bytes,
                    "Public Identity Address or Random (static) Identity Address of\n" +
                    "the peer device, 48 bit value.");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Read_Local_Resolvable_Address command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Read_Local_Resolvable_Address command failed. See Pard D, Error Codes");
                c.ReturnParameters.Add("Local_Resolvable_Address", 6, ParameterDataType.Bytes,
                    "Resolvable Private Address being used by the local device");

                c = cg.Commands.Add("HCI_LE_Set_Address_Resolution_Enable", 0x002D,
                    "The LE_Set_Address_Resolution_Enable command is used to enable \n" +
                    "resolution of Resolvable Private Addresses in the Controller.\n" +
                    "This causes the Controller to use the resolving list whenever the\n" +
                    "Controller receives a local or peer Resolvable Private Address.\n" +
                    "This command can be used at any time except when:\n" +
                    "   • Advertising is enabled\n" +
                    "   • Scanning is enabled\n" +
                    "   • Create connection command is outstanding");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Address_Resolution_Enable", 1, ParameterDataType.Bytes,
                    "0x00 Address Resolution in controller disabled (default)\n" +
                    "0x01 Address Resolution in controller enabled\n" +
                    "0x02 – 0xFF Reserved for Future Use");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Set_Address_Resolution_Enable command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Set_Address_Resolution_Enable command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Set_Resolvable_Private_Address_Timeout", 0x002E,
                    "The LE_Set_Address_Resolution_Enable command is used to enable \n" +
                    "resolution of Resolvable Private Addresses in the Controller.\n" +
                    "This causes the Controller to use the resolving list whenever the\n" +
                    "Controller receives a local or peer Resolvable Private Address.\n" +
                    "This command can be used at any time except when:\n" +
                    "   • Advertising is enabled\n" +
                    "   • Scanning is enabled\n" +
                    "   • Create connection command is outstanding");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("RPA_Timeout", 2, ParameterDataType.Time_1sec,
                    "RPA_Timeout measured in seconds\n" +
                    "Range for N: 0x0001 – 0xA1B8(1 sec – approximately 11.5hours)\n" +
                    "Default: N = 0x0384(900 secs or 15 minutes)");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Set_Resolvable_Private_Address_Timeout command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Set_Resolvable_Private_Address_Timeout command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Read_Maximum_Data_Length", 0x002F,
                    "The LE_Read_Maximum_Data_Length command allows the Host to read the Controllers\n" +
                    "maximum supported payload octets and packet duration times for transmission and\n" +
                    "reception(supportedMaxTxOctets and supportedMaxTxTime, supportedMaxRxOctets,\n" +
                    "and supportedMaxRxTime, see[Vol 6] Part B, Section 4.5.10).");
                c.SendCommand = SendCommand;

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Read_Maximum_Data_Length command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Read_Maximum_Data_Length command failed. See Pard D, Error Codes");
                c.ReturnParameters.Add("supportedMaxTxOctets", 2, ParameterDataType.Bytes,
                    "Maximum number of payload octets that the local Controller supports\n" +
                    "for transmission of a single Link Layer Data Channel PDU.\n" +
                    "Range 0x001B - 0x00FB");
                c.ReturnParameters.Add("supportedMaxTxTime", 2, ParameterDataType.Time_1usec,
                    "Maximum time, in microseconds, that the local Controller supports for\n" +
                    "transmission of a single Link Layer Data Channel PDU.\n" +
                    "Range 0x0148 - 0x0848");
                c.ReturnParameters.Add("supportedMaxRxOctets", 2, ParameterDataType.Bytes,
                    "Maximum number of payload octets that the local Controller supports\n" +
                    "for reception of a single Link Layer Data Channel PDU.\n" +
                    "Range 0x001B - 0x00FB");
                c.ReturnParameters.Add("supportedMaxRxTime", 2, ParameterDataType.Time_1usec,
                    "Maximum time, in microseconds, that the local Controller supports for\n" +
                    "reception of a single Link Layer Data Channel PDU.\n" +
                    "Range 0x0148 - 0x0848");

                c = cg.Commands.Add("HCI_LE_Read_PHY", 0x0030,
                    "The LE_Read_Maximum_Data_Length command allows the Host to read the Controllers\n" +
                    "maximum supported payload octets and packet duration times for transmission and\n" +
                    "reception(supportedMaxTxOctets and supportedMaxTxTime, supportedMaxRxOctets,\n" +
                    "and supportedMaxRxTime, see[Vol 6] Part B, Section 4.5.10).");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Range 0x0000 - 0x0EFF(all other values reserved for future use)");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Read_PHY command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Read_PHY command failed. See Pard D, Error Codes");
                c.ReturnParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Range 0x0000 - 0x0EFF(all other values reserved for future use)");
                c.ReturnParameters.Add("TX_PHY", 1, ParameterDataType.Bytes,
                    "0x01 The transmitter PHY for the connection is LE 1M\n" +
                    "0x02 The transmitter PHY for the connection is LE 2M\n" +
                    "0x03 The transmitter PHY for the connection is LE Coded\n" +
                    "All other values : Reserved for future use");
                c.ReturnParameters.Add("RX_PHY", 1, ParameterDataType.Bytes,
                    "0x01 The transmitter PHY for the connection is LE 1M\n" +
                    "0x02 The transmitter PHY for the connection is LE 2M\n" +
                    "0x03 The transmitter PHY for the connection is LE Coded\n" +
                    "All other values : Reserved for future use");

                c = cg.Commands.Add("HCI_LE_Set_Default_PHY", 0x0031,
                    "The LE_Set_Default_PHY command allows the Host to specify its preferred\n" +
                    "values for the transmitter PHY and receiver PHY to be used for all subsequent\n" +
                    "connections over the LE transport.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("ALL_PHYS", 1, ParameterDataType.Bytes,
                    "Bit0 The Host has no preference among the transmitter PHYs supported by the Controller\n" +
                    "Bit1 The Host has no preference among the receiver PHYs supported by the Controller\n" +
                    "Bit2-7 Reserved for future use");
                c.CommandParameters.Add("TX_PHYS", 1, ParameterDataType.Bytes,
                    "Bit0 The Host prefers to use the LE 1M transmitter PHY (possibly among others)\n" +
                    "Bit1 The Host prefers to use the LE 2M transmitter PHY (possibly among others)\n" +
                    "Bit1 The Host prefers to use the LE Coded transmitter PHY (possibly among others)\n" +
                    "Bit3-7 Reserved for future use");
                c.CommandParameters.Add("RX_PHYS", 1, ParameterDataType.Bytes,
                    "Bit0 The Host prefers to use the LE 1M receiver PHY (possibly among others)\n" +
                    "Bit1 The Host prefers to use the LE 2M receiver PHY (possibly among others)\n" +
                    "Bit1 The Host prefers to use the LE Coded receiver PHY (possibly among others)\n" +
                    "Bit3-7 Reserved for future use");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Set_Default_PHY command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Set_Default_PHY command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Set_PHY", 0x0032,
                    "The LE_Set_PHY command is used to set the PHY preferences for the\n" +
                    "connection identified by the Connection_Handle.The Controller might not be\n" +
                    "able to make the change(e.g.because the peer does not support the\n" +
                    "requested PHY) or may decide that the current PHY is preferable.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Range 0x0000 - 0x0EFF(all other values reserved for future use)");
                c.CommandParameters.Add("ALL_PHYS", 1, ParameterDataType.Bytes,
                    "Bit0 The Host has no preference among the transmitter PHYs supported by the Controller\n" +
                    "Bit1 The Host has no preference among the receiver PHYs supported by the Controller\n" +
                    "Bit2-7 Reserved for future use");
                c.CommandParameters.Add("TX_PHYS", 1, ParameterDataType.Bytes,
                    "Bit0 The Host prefers to use the LE 1M transmitter PHY (possibly among others)\n" +
                    "Bit1 The Host prefers to use the LE 2M transmitter PHY (possibly among others)\n" +
                    "Bit1 The Host prefers to use the LE Coded transmitter PHY (possibly among others)\n" +
                    "Bit3-7 Reserved for future use");
                c.CommandParameters.Add("RX_PHYS", 1, ParameterDataType.Bytes,
                    "Bit0 The Host prefers to use the LE 1M receiver PHY (possibly among others)\n" +
                    "Bit1 The Host prefers to use the LE 2M receiver PHY (possibly among others)\n" +
                    "Bit1 The Host prefers to use the LE Coded receiver PHY (possibly among others)\n" +
                    "Bit3-7 Reserved for future use");
                c.CommandParameters.Add("PHY_options", 2, ParameterDataType.Bytes,
                    "Bit0-1 0=the Host has no preferred coding when transmitting on the LE Coded PHY\n" +
                    "       1=the Host prefers that S=2 coding be used when transmitting on the LE Coded PHY\n" +
                    "       2=the Host prefers that S=2 coding be used when transmitting on the LE Coded PHY\n" +
                    "       3=Reserved for future use\n" +
                    "Bit2-15 Reserved for future use");

                c = cg.Commands.Add("HCI_LE_Enhanced_Receiver_Test", 0x0033,
                    "This command is used to start a test where the DUT receives test reference\n" +
                    "packets at a fixed interval.The tester generates the test reference packets.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("RX_Channel", 1, ParameterDataType.BLE_Channel,
                    "N = (F – 2402) / 2\n" +
                    "Range: 0x00 – 0x27. Frequency Range: 2402 MHz to 2480 MHz");
                c.CommandParameters.Add("PHY", 1, ParameterDataType.Bytes,
                    "0x01 The transmitter PHY for the connection is LE 1M\n" +
                    "0x02 The transmitter PHY for the connection is LE 2M\n" +
                    "0x03 The transmitter PHY for the connection is LE Coded\n" +
                    "All other values : Reserved for future use");
                c.CommandParameters.Add("Modulation_Index", 1, ParameterDataType.Bytes,
                    "0x00 Assume transmitter will have a standard modulation index\n" +
                    "0x01 Assume transmitter will have a stable modulation index\n" +
                    "All other values : Reserved for future use");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Enhanced_Receiver_Test command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Enhanced_Receiver_Test command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Enhanced_Transmitter_Test", 0x0034,
                    "This command is used to start a test where the DUT generates test reference\n" +
                    "packets at a fixed interval.The Controller shall transmit at maximum power.\n" +
                    "An LE Controller supporting the LE_Enhanced Transmitter_Test command\n" +
                    "shall support Packet_Payload values 0x00, 0x01 and 0x02.An LE Controller\n" +
                    "supporting the LE Coded PHY shall also support Packet_Payload value 0x04.\n" +
                    "An LE Controller may support other values of Packet_Payload.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("TX_Channel", 1, ParameterDataType.BLE_Channel,
                    "N = (F – 2402) / 2\n" +
                    "Range: 0x00 – 0x27. Frequency Range: 2402 MHz to 2480 MHz");
                c.CommandParameters.Add("Length_Of_Test_Data", 1, ParameterDataType.Bytes,
                    "0x00-0xFF Length in bytes of payload data in each packet");
                c.CommandParameters.Add("Packet_Payload", 1, ParameterDataType.Bytes,
                    "0x00 PRBS9 sequence ‘11111111100000111101…’ (in transmission order)\n" +
                    "     as described in [Vol 6] Part F, Section 4.1.5\n" +
                    "0x01 Repeated ‘11110000’ (in transmission order) sequence as described in [Vol 6] Part F, Section 4.1.5\n" +
                    "0x02 Repeated ‘10101010’ (in transmission order) sequence as described in [Vol 6] Part F, Section 4.1.5\n" +
                    "0x03 PRBS15 sequence as described in [Vol 6] Part F, Section 4.1.5\n" +
                    "0x04 Repeated ‘11111111’ (in transmission order) sequence\n" +
                    "0x05 Repeated ‘00000000’ (in transmission order) sequence\n" +
                    "0x06 Repeated ‘00001111’ (in transmission order) sequence\n" +
                    "0x07 Repeated ‘01010101’ (in transmission order) sequence\n" +
                    "0x08-0xFF Reserved for future use");
                c.CommandParameters.Add("PHY", 1, ParameterDataType.Bytes,
                    "0x01 Transmitter set to use the LE 1M PHY\n" +
                    "0x02 Transmitter set to use the LE 2M PHY\n" +
                    "0x03 Transmitter set to use the LE Coded PHY with S=8 data coding\n" +
                    "0x03 Transmitter set to use the LE Coded PHY with S=2 data coding\n" +
                    "All other values : Reserved for future use");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Enhanced_Transmitter_Test command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Enhanced_Transmitter_Test command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Set_Advertising_Set_Random_Address", 0x0035,
                    "The LE_Set_Advertising_Set_Random_Address command is used by the Host\n" +
                    "to set the random device address specified by the Random_Address parameter.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Advertising_Handle", 1, ParameterDataType.Bytes,
                    "0x00-0xEF Used to identify an advertising set\n" +
                    "All other values : Reserved for future use");
                c.CommandParameters.Add("Advertising_Random_Address", 6, ParameterDataType.Bytes,
                    "Random Device Address as defined by [Vol 6] Part B, Section 1.3.2");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Set_Advertising_Set_Random_Address command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Set_Advertising_Set_Random_Address command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Set_Extended_Advertising_Parameters", 0x0036,
                    "The LE_Set_Extended_Advertising_Parameters command is used by the Host\n" +
                    "to set the advertising parameters.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Advertising_Handle", 1, ParameterDataType.Bytes,
                    "0x00-0xEF Used to identify an advertising set\n" +
                    "All other values : Reserved for future use");
                c.CommandParameters.Add("Advertising_Event_Properties", 2, ParameterDataType.Bytes,
                    "Bit0 Connectable advertising\n" +
                    "Bit1 Scannable advertising\n" +
                    "Bit2 Directed advertising\n" +
                    "Bit3 High Duty Cycle Directed Connectable advertising (≤ 3.75 ms Advertising Interval)\n" +
                    "Bit4 Use legacy advertising PDUs\n" +
                    "Bit5 Omit advertiser's address from all PDUs (anonymous advertising)\n" +
                    "Bit6 Include TxPower in the extended header of the advertising PDU\n" +
                    "All other bits : Reserved for future use");
                p = c.CommandParameters.Add("Primary_Advertising_Interval_Min", 3, ParameterDataType.Time_625usec,
                    "Minimum advertising interval for undirected and low duty cycle directed advertising.\n" +
                    "Range: 0x000020 to 0xFFFFFF\n" +
                    "Time = N * 0.625 msec, Time Range: 20 ms to 10,485.759375 sec");
                p.Data = new byte[] { 0x00, 0x08, 0x00 };
                p = c.CommandParameters.Add("Primary_Advertising_Interval_Max", 3, ParameterDataType.Time_625usec,
                    "Maximum advertising interval for undirected and low duty cycle directed advertising.\n" +
                    "Range: 0x000020 to 0xFFFFFF\n" +
                    "Time = N * 0.625 msec, Time Range: 20 ms to 10,485.759375 sec");
                p.Data = new byte[] { 0x00, 0x08, 0x00 };
                p = c.CommandParameters.Add("Primary_Advertising_Channel_Map", 1, ParameterDataType.Bytes,
                    "Bit0 Channel 37 shall be used\n" +
                    "Bit1 Channel 38 shall be used\n" +
                    "Bit2 Channel 39 shall be used\n" +
                    "All other bits : Reserved for future use");
                p.Data = new byte[] { 0x07 };
                c.CommandParameters.Add("Own_Address_Type", 1, ParameterDataType.Bytes,
                    "0x00 Public Device Address\n" +
                    "0x01 Random Device Address\n" +
                    "0x02 Controller generates Resolvable Private Address based on the local IRK from resolving list.\n" +
                    "     If resolving list contains no matching entry,use public address.\n" +
                    "0x03 Controller generates Resolvable Private Address based on the local IRK from resolving list.\n" +
                    "     If resolving list contains no matching entry, use random address from LE_Set_Advertising_Set_Random_Address.\n" +
                    "All other values : Reserved for future use");
                c.CommandParameters.Add("Peer_Address_Type", 1, ParameterDataType.Bytes,
                    "0x00 Public Device Address (default) or Public Identity Address\n" +
                    "0x01 Random Device Address or Random(static) Identity Address\n" +
                    "All other values : Reserved for future use");
                c.CommandParameters.Add("Peer_Address", 6, ParameterDataType.Bytes,
                    "Public Device Address, Random Device Address, Public Identity Address,\n" +
                    "or Random(static) Identity Address of the device to be connected");
                c.CommandParameters.Add("Advertising_Filter_Policy", 1, ParameterDataType.Bytes,
                    "0x00 Process scan and connection requests from all devices (i.e., the White List is not in use).\n" +
                    "0x01 Process connection requests from all devices and only scan requests from\n" +
                    "     devices that are in the White List.\n" +
                    "0x02 Process scan requests from all devices and only connection requests from\n" +
                    "     devices that are in the White List.\n" +
                    "0x03 Process scan and connection requests only from devices in the White List.\n" +
                    "All other values : Reserved for future use");
                c.CommandParameters.Add("Advertising_Tx_Power", 1, ParameterDataType.S8,
                    "Range: -127 ≤ N ≤ +126 Units: dBm\n" +
                    "127 Host has no preference");
                c.CommandParameters.Add("Primary_Advertising_PHY", 1, ParameterDataType.Bytes,
                    "0x01 Primary advertisement PHY is LE 1M\n" +
                    "0x03 Primary advertisement PHY is LE Coded\n" +
                    "All other values : Reserved for future use");
                c.CommandParameters.Add("Secondary_Advertising_Max_Skip", 1, ParameterDataType.Bytes,
                    "0x00 AUX_ADV_IND shall be sent prior to the next advertising event\n" +
                    "0x01-0xFF Maximum advertising events the Controller can skip before sending the\n" +
                    "          AUX_ADV_IND packets on the secondary advertising channel");
                c.CommandParameters.Add("Secondary_Advertising_PHY", 1, ParameterDataType.Bytes,
                    "0x01 Secondary advertisement PHY is LE 1M\n" +
                    "0x02 Secondary advertisement PHY is LE 1M\n" +
                    "0x03 Secondary advertisement PHY is LE Coded\n" +
                    "All other values : Reserved for future use");
                c.CommandParameters.Add("Advertising_SID", 1, ParameterDataType.Bytes,
                    "0x00-0x0F Value of the Advertising SID subfield in the ADI field of the PDU\n" +
                    "All other values : Reserved for future use");
                c.CommandParameters.Add("Scan_Request_Notification_Enable", 1, ParameterDataType.Bytes,
                    "0x00 Scan request notifications disabled\n" +
                    "0x01 Scan request notifications enabled\n" +
                    "All other values : Reserved for future use");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Set_Extended_Advertising_Parameters command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Set_Extended_Advertising_Parameters command failed. See Pard D, Error Codes");
                c.ReturnParameters.Add("Selected_Tx_Power", 1, ParameterDataType.S8,
                    "Range: -127 ≤ N ≤ +126 Units: dBm");

                c = cg.Commands.Add("HCI_LE_Set_Extended_Advertising_Data", 0x0037,
                    "The LE_Set_Extended_Advertising_Data command is used to set the data\n" +
                    "used in advertising PDUs that have a data field.This command may be issued\n" +
                    "at any time after an advertising set identified by the Advertising_Handle\n" +
                    "parameter has been created using the LE Set Extended Advertising\n" +
                    "Parameters Command(see Section 7.8.53), regardless of whether advertising\n" +
                    "in that set is enabled or disabled.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Advertising_Handle", 1, ParameterDataType.Bytes,
                    "0x00-0xEF Used to identify an advertising set\n" +
                    "All other values : Reserved for future use");
                c.CommandParameters.Add("Operation", 1, ParameterDataType.Bytes,
                    "0x00 Intermediate fragment of fragmented extended advertising data\n" +
                    "0x01 First fragment of fragmented extended advertising data\n" +
                    "0x02 Last fragment of fragmented extended advertising data\n" +
                    "0x03 Complete extended advertising data\n" +
                    "0x04 Unchanged data (just update the Advertising DID)\n" +
                    "All other values : Reserved for future use");
                c.CommandParameters.Add("Fragment_Preference", 1, ParameterDataType.Bytes,
                    "0x00 The Controller may fragment all Host advertising data\n" +
                    "0x01 The Controller should not fragment or should minimize fragmentation of\n" +
                    "     Host advertising data\n" +
                    "All other values : Reserved for future use");
                c.CommandParameters.Add("Advertising_Data_Length", 1, ParameterDataType.Bytes,
                   "0-251 The number of octets in the Advertising Data parameter\n" +
                   "All other values : Reserved for future use",
                   ParameterType.ResizableIndicator);
                c.CommandParameters.Add("Advertising_Data", 31, ParameterDataType.Bytes,
                   "Advertising data formatted as defined in [Vol 3] Part C, Section 11\n" +
                   "Note: This parameter has a variable length.",
                   ParameterType.ResizableData);

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Set_Extended_Advertising_Data command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Set_Extended_Advertising_Data command failed. See Pard D, Error Codes");

                c = cg.Commands.Add("HCI_LE_Set_Extended_Scan_Response_Data", 0x0038,
                    "This command is used to provide data used in Scanning Packets that have a data field.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Advertising_Handle", 1, ParameterDataType.Bytes,
                    "0x00-0xEF Used to identify an advertising set\n" +
                    "All other values : Reserved for future use");
                c.CommandParameters.Add("Operation", 1, ParameterDataType.Bytes,
                    "0x00 Intermediate fragment of fragmented scan response data\n" +
                    "0x01 First fragment of fragmented scan response data\n" +
                    "0x02 Last fragment of fragmented scan response data\n" +
                    "0x03 Complete scan response data\n" +
                    "All other values : Reserved for future use");
                c.CommandParameters.Add("Fragment_Preference", 1, ParameterDataType.Bytes,
                    "0x00 The Controller may fragment all scan response data\n" +
                    "0x01 The Controller should not fragment or should minimize fragmentation of\n" +
                    "     scan response data\n" +
                    "All other values : Reserved for future use");
                p = c.CommandParameters.Add("Scan_Response_Data_Length", 1, ParameterDataType.Bytes,
                    "0-251 The number of octets in the Scan_Response Data parameter.",
                    ParameterType.ResizableIndicator);
                p.Data = new byte[] { 0x1F };
                c.CommandParameters.Add("Scan_Response_Data", 31, ParameterDataType.Bytes,
                    "Scan response data formatted as defined in [Vol 3] Part C, Section 11\n" +
                    "Note: This parameter has a variable length.",
                    ParameterType.ResizableData);

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 HCI_LE_Set_Extended_Scan_Response_Data command succeeded.\n" +
                    "0x01–0xFF HCI_LE_Set_Extended_Scan_Response_Data command failed. See Pard D, Error Codes");

            }

            private void InitVendorSpecificCommands(CommandGroup cg)
            {
                Command c;

                c = cg.Commands.Add("HCI_Vendor_Write_BB_Register", 0x066,
                    "This command is used to write BB register.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Address", 2, ParameterDataType.Bytes,
                    "RFC register address");
                c.CommandParameters.Add("Data", 2, ParameterDataType.Bytes,
                   "RFC register data");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 Read_Local_Version_Information command succeeded.\n" +
                    "0x01-0xFF Read_Local_Version_Information command failed.");

                c = cg.Commands.Add("HCI_Vendor_Read_BB_Register", 0x067,
                    "This command is used to read BB register.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Address", 2, ParameterDataType.Bytes,
                    "RFC register address");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 Read_Local_Version_Information command succeeded.\n" +
                    "0x01-0xFF Read_Local_Version_Information command failed.");
                c.ReturnParameters.Add("Data", 2, ParameterDataType.Bytes,
                    "RFC register data");

                c = cg.Commands.Add("HCI_Vendor_Read_RFC_Register", 0x149,
                    "This command is used to read RFC register.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Address", 1, ParameterDataType.Bytes,
                    "RFC register address");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 Read_Local_Version_Information command succeeded.\n" +
                    "0x01-0xFF Read_Local_Version_Information command failed.");
                c.ReturnParameters.Add("Data", 4, ParameterDataType.Bytes,
                    "RFC register data");

                c = cg.Commands.Add("HCI_Vendor_Write_RFC_Register", 0x14A,
                    "This command is used to write RFC register.");
                c.SendCommand = SendCommand;

                c.CommandParameters.Add("Address", 1, ParameterDataType.Bytes,
                    "RFC register address");
                c.CommandParameters.Add("Data", 4, ParameterDataType.Bytes,
                   "RFC register data");

                c.ReturnParameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 Read_Local_Version_Information command succeeded.\n" +
                    "0x01-0xFF Read_Local_Version_Information command failed.");

            }

            private void InitCommand()
            {
                CommandGroup LinkControlGroup = new CommandGroup("Link Control Commands", 0x01);
                InitLinkControlCommands(LinkControlGroup);
                CmdGroupList.Add(LinkControlGroup);

                CommandGroup ControlAndBasebandGroup = new CommandGroup("Controller & Baseband Commands", 0x03);
                InitControlAndBasebandCommands(ControlAndBasebandGroup);
                CmdGroupList.Add(ControlAndBasebandGroup);

                CommandGroup InformationGroup = new CommandGroup("Informational Parameters", 0x04);
                InitInformationalParameters(InformationGroup);
                CmdGroupList.Add(InformationGroup);

                CommandGroup StatusGroup = new CommandGroup("Status Parameters", 0x05);
                InitStatusParameters(StatusGroup);
                CmdGroupList.Add(StatusGroup);

                CommandGroup LEControlCommands = new CommandGroup("LE Controller Commands", 0x08);
                InitLEControllerCommand(LEControlCommands);
                CmdGroupList.Add(LEControlCommands);

                CommandGroup VendorSpecificCommands = new CommandGroup("Vendor Specific Commands", 0x3F);
                InitVendorSpecificCommands(VendorSpecificCommands);
                CmdGroupList.Add(VendorSpecificCommands);
            }

            private void InitLEMetaEvents(Event LEMetaEvent)
            {
                Event e;

                LEMetaEvent.SubEvents = new EventCollection();

                e = LEMetaEvent.SubEvents.Add("LE Connection Complete Event", 0x01,
                    "The LE Connection Complete event indicates to both of the Hosts forming the\n" +
                    "connection that a new connection has been created.Upon the creation of the\n" +
                    "connection a Connection_Handle shall be assigned by the Controller, and\n" +
                    "passed to the Host in this event. If the connection establishment fails this event\n" +
                    "shall be provided to the Host that had issued the LE_Create_Connection command.\n" +
                    "This event indicates to the Host which issued a LE_Create_Connection command and\n" +
                    "received a Command Status event if the connection establishment failed or was successful.");

                e.Parameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 Connection successfully completed.\n" +
                    "0x01–0xFF Connection failed to complete.");
                e.Parameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle to be used to identify a connection between two Bluetooth devices.\n" +
                    "The Connection_Handle is used as an identifier for transmitting and receiving data.\n" +
                    "Range: 0x0000 - 0x0EFF(0x0F00 - 0x0FFF Reserved for future use)");
                e.Parameters.Add("Role", 1, ParameterDataType.Bytes,
                    "0x00 Connection is master\n" +
                    "0x01 Connection is slave\n" +
                    "0x02-0xFF Reserved for future use");
                e.Parameters.Add("Peer_Address_Type", 1, ParameterDataType.Bytes,
                    "0x00 Peer is using a Public Device Address\n" +
                    "0x01 Peer is using a Random Device Address\n" +
                    "0x02-0xFF Reserved for future use");
                e.Parameters.Add("Peer_Address", 6, ParameterDataType.Bytes,
                    "Public Device Address or Random Device Address of the peer device");
                e.Parameters.Add("Conn_Interval", 2, ParameterDataType.Time_1_25msec,
                    "Connection interval used on this connection.\n" +
                    "Range: 0x0006 to 0x0C80\n" +
                    "Time = N * 1.25 msec, Time Range: 7.5 msec to 4000 msec.");
                e.Parameters.Add("Conn_Latency", 2, ParameterDataType.Bytes,
                    "Slave latency for the connection in number of connection events.\n" +
                    "Range: 0x0000 to 0x01F3");
                e.Parameters.Add("Supervision_Timeout", 2, ParameterDataType.Time_10msec,
                    "Connection supervision timeout.\n" +
                    "Range: 0x000A to 0x0C80\n" +
                    "Time = N * 10 msec, Time Range: 100 msec to 32 seconds");
                e.Parameters.Add("Master_Clock_Accuracy", 1, ParameterDataType.Bytes,
                    "0x00 500 ppm\n" +
                    "0x01 250 ppm\n" +
                    "0x02 150 ppm\n" +
                    "0x03 100 ppm\n" +
                    "0x04 75 ppm\n" +
                    "0x05 50 ppm\n" +
                    "0x06 30 ppm\n" +
                    "0x07 20 ppm\n" +
                    "0x08 - 0xFF Reserved for future use");

                e = LEMetaEvent.SubEvents.Add("LE Advertising Report Event", 0x02,
                    "The LE Advertising Report event indicates that a Bluetooth device or multiple\n" +
                    "Bluetooth devices have responded to an active scan or received some information\n" +
                    "during a passive scan.The Controller may queue these advertising reports and\n" +
                    "send information from multiple devices in one LE Advertising Report event.");
                e.Parameters.Add("Num_Reports", 1, ParameterDataType.Number,
                    "0x01-0x19 Number of responses in event.\n" +
                    "0x00 and 0x1A–0xFF Reserved for future use",
                    ParameterType.ArrayIndicator);
                e.Parameters.Add("Event_Type[i]", 1, ParameterDataType.Bytes,
                    "0x00 Connectable undirected advertising (ADV_IND).\n" +
                    "0x01 Connectable directed advertising(ADV_DIRECT_IND)\n" +
                    "0x02 Scannable undirected advertising(ADV_SCAN_IND)\n" +
                    "0x03 Non connectable undirected advertising(ADV_NONCONN_IND)\n" +
                    "0x04 Scan Response(SCAN_RSP)\n" +
                    "0x05 - 0xFF Reserved for future use",
                    ParameterType.ArrayData);
                e.Parameters.Add("Address_Type[i]", 1, ParameterDataType.Bytes,
                    "0x00 Public Device Address\n" +
                    "0x01 Random Device Address\n" +
                    "0x02 Public Identity Address(Corresponds to Resolved Private Address)\n" +
                    "0x03 Random(static) Identity Address (Corresponds to Resolved Private Address)\n" +
                    "0x04 - 0xFF Reserved for future use",
                    ParameterType.ArrayData);
                e.Parameters.Add("Address[i]", 6, ParameterDataType.Bytes,
                    "Public Device Address, Random Device Address, Public Identity Address or Random(static)\n" +
                    "Identity Address of the advertising device.",
                    ParameterType.ArrayData);
                e.Parameters.Add("Length_Data[i]", 1, ParameterDataType.Number,
                    "0x00 - 0x1F Length of the Data[i] field for each device which responded.\n" +
                    "0x20 - 0xFF Reserved for future use.",
                    ParameterType.ArrayDataAndResizableIndicator);
                e.Parameters.Add("Data[i]", 31, ParameterDataType.Bytes,
                    "Length_Data[i] octets of advertising or scan response data formatted as defined in [Vol 3] Part C, Section 11.",
                    ParameterType.ArrayDataAndResizableData);
                e.Parameters.Add("RSSI[i]", 1, ParameterDataType.S8,
                    "1 Octet (signed integer) Range: -127 ≤ N ≤ +20, Units: dBm\n" +
                    "127 RSSI is not available,\n" +
                    "21 to 126 Reserved for future use",
                    ParameterType.ArrayData);

                e = LEMetaEvent.SubEvents.Add("LE Connection Update Complete Event", 0x03,
                    "The LE Connection Update Complete event is used to indicate that the\n" +
                    "Controller process to update the connection has completed.\n" +
                    "On a slave, if no connection parameters are updated, then this event shall not be issued.\n" +
                    "On a master, this event shall be issued if the Connection_Update command was sent.");

                e.Parameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 Connection_Update command successfully completed.\n" +
                    "0x01–0xFF Connection_Update command failed to complete.");
                e.Parameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle to be used to identify a connection between two Bluetooth devices.\n" +
                    "The Connection_Handle is used as an identifier for transmitting and receiving data.\n" +
                    "Range: 0x0000 - 0x0EFF(0x0F00 - 0x0FFF Reserved for future use)");
                e.Parameters.Add("Conn_Interval", 2, ParameterDataType.Time_1_25msec,
                    "Connection interval used on this connection.\n" +
                    "Range: 0x0006 to 0x0C80\n" +
                    "Time = N * 1.25 msec, Time Range: 7.5 msec to 4000 msec.");
                e.Parameters.Add("Conn_Latency", 2, ParameterDataType.Bytes,
                    "Slave latency for the connection in number of connection events.\n" +
                    "Range: 0x0000 to 0x01F3");
                e.Parameters.Add("Supervision_Timeout", 2, ParameterDataType.Time_10msec,
                    "Connection supervision timeout.\n" +
                    "Range: 0x000A to 0x0C80\n" +
                    "Time = N * 10 msec, Time Range: 100 msec to 32 seconds");

                e = LEMetaEvent.SubEvents.Add("LE Read Remote Used Features Complete Event", 0x04,
                    "The LE Read Remote Used Features Complete event is used to indicate the\n" +
                    "completion of the process of the Controller obtaining the used features of the\n" +
                    "remote Bluetooth device specified by the Connection_Handle event parameter.");

                e.Parameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 LE_Read_Remote_Used_Features command successfully completed.\n" +
                    "0x01–0xFF LE_Read_Remote_Used_Features command failed to complete.");
                e.Parameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle to be used to identify a connection between two Bluetooth devices.\n" +
                    "The Connection_Handle is used as an identifier for transmitting and receiving data.\n" +
                    "Range: 0x0000 - 0x0EFF(0x0F00 - 0x0FFF Reserved for future use)");
                e.Parameters.Add("LE_Features", 8, ParameterDataType.Bytes,
                    "Bit0 LE Encryption\n" +
                    "Bit1 Connection Parameters Request Procedure\n" +
                    "Bit2 Extended Reject Indication\n" +
                    "Bit3 Slave - initiated Features Exchange\n" +
                    "Bit4 LE Ping\n" +
                    "Bit5 LE Data Packet Length Extension\n" +
                    "Bit6 LL Privacy\n" +
                    "Bit7 Extended Scanner Filter Policies\n" +
                    "Bit8–Bit63 RFU");

                e = LEMetaEvent.SubEvents.Add("LE Long Term Key Request Event", 0x05,
                    "The LE Long Term Key Request event indicates that the master device is\n" +
                    "attempting to encrypt or re - encrypt the link and is requesting the Long Term\n" +
                    "Key from the Host. (See[Vol 6] Part B, Section 5.1.3).");

                e.Parameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle to be used to identify a connection between two Bluetooth devices.\n" +
                    "The Connection_Handle is used as an identifier for transmitting and receiving data.\n" +
                    "Range: 0x0000 - 0x0EFF(0x0F00 - 0x0FFF Reserved for future use)");
                e.Parameters.Add("Random_Number", 8, ParameterDataType.Bytes,
                    "64-bit random number.");
                e.Parameters.Add("Encrypted_Diversifier", 2, ParameterDataType.Bytes,
                    "16-bit encrypted diversifier.");

                e = LEMetaEvent.SubEvents.Add("LE Long Term Key Request Event", 0x06,
                    "This event indicates to the master’s Host or the slave’s Host that the remote\n" +
                    "device is requesting a change in the connection parameters.\n" +
                    "The Host replies either with the HCI LE Remote Connection Parameter Request Reply\n" +
                    "command or the HCI LE Remote Connection Parameter Request Negative Reply command.");

                e.Parameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle to be used to identify a connection between two Bluetooth devices.\n" +
                    "The Connection_Handle is used as an identifier for transmitting and receiving data.\n" +
                    "Range: 0x0000 - 0x0EFF(0x0F00 - 0x0FFF Reserved for future use)");
                e.Parameters.Add("Interval_Min", 2, ParameterDataType.Time_1_25msec,
                    "Minimum value of the connection interval requested by the remote device.\n" +
                    "Range: 0x0006 to 0x0C80\n" +
                    "Time = N * 1.25 ms, Time Range: 7.5 msec to 4 seconds");
                e.Parameters.Add("Interval_Max", 2, ParameterDataType.Time_1_25msec,
                    "Maximum value of the connection interval requested by the remote device.\n" +
                    "Range: 0x0006 to 0x0C80\n" +
                    "Time = N * 1.25 ms, Time Range: 7.5 msec to 4 seconds");
                e.Parameters.Add("Latency", 2, ParameterDataType.Bytes,
                    "Maximum allowed slave latency for the connection specified as the number of connection\n" +
                    "events requested by the remote device.\n" +
                    "Range: 0x0000 to 0x01F3(499)");
                e.Parameters.Add("Timeout", 2, ParameterDataType.Time_10msec,
                    "Supervision timeout for the connection requested by the remote device.\n" +
                    "Range: 0x000A to 0x0C80\n" +
                    "Time = N * 10 ms, Time Range: 100 ms to 32 seconds");

                e = LEMetaEvent.SubEvents.Add("LE Data Length Change Event", 0x07,
                    "This event indicates to the master’s Host or the slave’s Host that the remote\n" +
                    "device is requesting a change in the connection parameters.\n" +
                    "The Host replies either with the HCI LE Remote Connection Parameter Request Reply\n" +
                    "command or the HCI LE Remote Connection Parameter Request Negative Reply command.");

                e.Parameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle to be used to identify a connection between two Bluetooth devices.\n" +
                    "The Connection_Handle is used as an identifier for transmitting and receiving data.\n" +
                    "Range: 0x0000 - 0x0EFF(0x0F00 - 0x0FFF Reserved for future use)");
                e.Parameters.Add("MaxTxOctets", 2, ParameterDataType.Bytes,
                    "The maximum number of payload octets in a Link Layer Data Channel PDU that\n" +
                    "the local Controller will send on this connection(connEffectiveMaxTxOctets\n" +
                    "defined in [Vol 6] Part B, Section 4.5.10).\n" +
                    "Range 0x001B - 0x00FB(0x0000 - 0x001A and 0x00FC - 0xFFFF Reserved for future use)");
                e.Parameters.Add("MaxTxTime", 2, ParameterDataType.Bytes,
                    "The maximum time that the local Controller will take to send a Link Layer Data Channel PDU\n" +
                    "on this connection(connEffectiveMaxTx - Time defined in [Vol 6] Part B, Section 4.5.10).\n" +
                    "Range 0x0148 - 0x0848(0x0000 - 0x0127 and 0x0849 - 0xFFFF Reserved for future use)");
                e.Parameters.Add("MaxRxOctets", 2, ParameterDataType.Bytes,
                    "The maximum number of payload octets in a Link Layer Data Channel\n" +
                    "PDU that the local controller expects to receive on this connection\n" +
                    "(connEfectiveMaxRxOctets defined in [Vol 6] Part B, Section 4.5.10).\n" +
                    "Range 0x001B - 0x00FB(0x0000 - 0x001A and 0x00FC - 0xFFFF Reserved for future use)");
                e.Parameters.Add("MaxRxTime", 2, ParameterDataType.Bytes,
                    "The maximum time that the local Controller expects to take to receive a Link Layer Data Channen" +
                    "PDU on this connection(connEffectiveMax - RxTime defined in [Vol 6] Part B, Section 4.5.10).\n" +
                    "Range 0x0148 - 0x0848(0x0000 - 0x0127 and 0x0849 - 0xFFFF Reserved for future use)");

                e = LEMetaEvent.SubEvents.Add("LE Read Local P-256 Public Key Complete Event", 0x08,
                    "This event is generated when local P-256 key generation is complete.");

                e.Parameters.Add("Status", 1, ParameterDataType.Status,
                   "0x00 LE Read Local P-256 Public Key command successfully completed.\n" +
                   "0x01–0xFF LE Read Local P-256 Public Key command failed to complete.");
                e.Parameters.Add("Local_P-256_Public_Key", 64, ParameterDataType.Bytes,
                    "Local P-256 public key.");

                e = LEMetaEvent.SubEvents.Add("LE Generate DHKey Complete Event", 0x09,
                    "This event indicates that LE Diffie Hellman key generation has been completed by the Controller.");

                e.Parameters.Add("Status", 1, ParameterDataType.Status,
                   "0x00 LE_Generate_DHKey command successfully completed.\n" +
                   "0x01–0xFF LE_Generate_DHKey command failed to complete.");
                e.Parameters.Add("DHKey", 32, ParameterDataType.Bytes,
                    "Diffie Hellman Key.");

                e = LEMetaEvent.SubEvents.Add("LE Enhanced Connection Complete Event", 0x0A,
                    "The LE Enhanced Connection Complete event indicates to both of the Hosts\n" +
                    "forming the connection that a new connection has been created.\n" +
                    "Upon the creation of the connection a Connection_Handle shall be assigned by the\n" +
                    "Controller, and passed to the Host in this event. If the connection establishment fails,\n" +
                    "this event shall be provided to the Host that had issued the LE_Create_Connection command.");

                e.Parameters.Add("Status", 1, ParameterDataType.Status,
                   "0x00 Connection successfully completed.\n" +
                   "0x01–0xFF Connection failed to complete.");
                e.Parameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle to be used to identify a connection between two Bluetooth devices.\n" +
                    "The Connection_Handle is used as an identifier for transmitting and receiving data.\n" +
                    "Range: 0x0000 - 0x0EFF(0x0F00 - 0x0FFF Reserved for future use)");
                e.Parameters.Add("Role", 1, ParameterDataType.Bytes,
                    "0x00 Connection is master\n" +
                    "0x01 Connection is slave\n" +
                    "0x02 – 0xFF Reserved for future use");
                e.Parameters.Add("Role", 1, ParameterDataType.Bytes,
                    "0x00 Connection is master\n" +
                    "0x01 Connection is slave\n" +
                    "0x02-0xFF Reserved for future use");
                e.Parameters.Add("Peer_Address_Type", 1, ParameterDataType.Bytes,
                    "0x00 Public Device Address (default)\n" +
                    "0x01 Random Device Address\n" +
                    "0x02 Public Identity Address(Corresponds to Resolved Private Address)\n" +
                    "0x03 Random(Static) Identity Address(Corresponds to Resolved Private Address)\n" +
                    "0x04-0xFF Reserved for future use");
                e.Parameters.Add("Peer_Address", 6, ParameterDataType.Bytes,
                    "Public Device Address, or Random Device Address, Public Identity\n" +
                    "Address or Random(static) Identity Address of the device to be connected.");
                e.Parameters.Add("Local_Resolvable_Private_Address", 6, ParameterDataType.Bytes,
                    "Resolvable Private Address being used by the local device for this connection.\n" +
                    "This is only valid when the Own_Address_Type(from the HCI_LE_Create_Connection\n" +
                    "or HCI_LE_Set_Advertising_Parameters commands) is set to 0x02 or 0x03.\n" +
                    "For other Own_Address_Type values, the Controller shall return all zeros.");
                e.Parameters.Add("Peer_Resolvable_Private_Address", 6, ParameterDataType.Bytes,
                    "Resolvable Private Address being used by the peer device for this connection.\n" +
                    "This is only valid for Peer_Address_Type 0x02 and 0x03.\n" +
                    "For other Peer_Address_Type values, the Controller shall return all zeros.");
                e.Parameters.Add("Conn_Interval", 2, ParameterDataType.Time_1_25msec,
                    "Connection interval used on this connection.\n" +
                    "Range: 0x0006 to 0x0C80\n" +
                    "Time = N * 1.25 msec, Time Range: 7.5 msec to 4000 msec.");
                e.Parameters.Add("Conn_Latency", 2, ParameterDataType.Bytes,
                    "Slave latency for the connection in number of connection events.\n" +
                    "Range: 0x0000 to 0x01F3");
                e.Parameters.Add("Supervision_Timeout", 2, ParameterDataType.Time_10msec,
                    "Connection supervision timeout.\n" +
                    "Range: 0x000A to 0x0C80\n" +
                    "Time = N * 10 msec, Time Range: 100 msec to 32 seconds");
                e.Parameters.Add("Master_Clock_Accuracy", 1, ParameterDataType.Bytes,
                    "0x00 500 ppm\n" +
                    "0x01 250 ppm\n" +
                    "0x02 150 ppm\n" +
                    "0x03 100 ppm\n" +
                    "0x04 75 ppm\n" +
                    "0x05 50 ppm\n" +
                    "0x06 30 ppm\n" +
                    "0x07 20 ppm\n" +
                    "0x08 - 0xFF Reserved for future use");

                e = LEMetaEvent.SubEvents.Add("LE Direct Advertising Report Event", 0x0B,
                    "The LE Direct Advertising Report event indicates that directed advertisements\n" +
                    "have been received where the advertiser is using a resolvable private address\n" +
                    "for the InitA field in the ADV_DIRECT_IND PDU and the Scanning_Filter_Policy is\n" +
                    "equal to 0x02 or 0x03, see Section 7.8.10.");

                e.Parameters.Add("Num_Reports", 1, ParameterDataType.Number,
                    "0x01-0x19 Number of responses in event.\n" +
                    "0x00 and 0x1A–0xFF Reserved for future use",
                    ParameterType.ArrayIndicator);
                e.Parameters.Add("Event_Type[i]", 1, ParameterDataType.Bytes,
                    "0x01 Connectable directed advertising (ADV_DIRECT_IND)\n" +
                    "0x00 and 0x02 – 0xFF Reserved for future use",
                    ParameterType.ArrayData);
                e.Parameters.Add("Direct_Address_Type[i]", 1, ParameterDataType.Bytes,
                    "0x01 Random Device Address (default)\n" +
                    "0x00 and 0x02 – 0xFF Reserved for future use",
                    ParameterType.ArrayData);
                e.Parameters.Add("Direct_Address[i]", 6, ParameterDataType.Bytes,
                    "Random Device Address",
                    ParameterType.ArrayData);
                e.Parameters.Add("Address_Type[i]", 1, ParameterDataType.Bytes,
                    "0x00 Public Device Address\n" +
                    "0x01 Random Device Address\n" +
                    "0x02 Public Identity Address(Corresponds to Resolved Private Address)\n" +
                    "0x03 Random(static) Identity Address (Corresponds to Resolved Private Address)\n" +
                    "0x04 - 0xFF Reserved for future use",
                    ParameterType.ArrayData);
                e.Parameters.Add("Address[i]", 6, ParameterDataType.Bytes,
                    "Public Device Address, Random Device Address, Public Identity Address or Random(static)\n" +
                    "Identity Address of the advertising device.",
                    ParameterType.ArrayData);
                e.Parameters.Add("RSSI[i]", 1, ParameterDataType.S8,
                    "1 Octet (signed integer) Range: -127 ≤ N ≤ +20, Units: dBm\n" +
                    "127 RSSI is not available,\n" +
                    "21 to 126 Reserved for future use",
                    ParameterType.ArrayData);

                e = LEMetaEvent.SubEvents.Add("LE PHY Update Complete Event", 0x0C,
                    "The LE PHY Update Complete Event is used to indicate that the Controller has\n" +
                    "changed the transmitter PHY or receiver PHY in use.\n" +
                    "If the Controller changes the transmitter PHY, the receiver PHY, or both PHYs,\n" +
                    "this event shall be issued.\n" +
                    "If an LE_Set_PHY command was sent and the Controller determines that\n" +
                    "neither PHY will change as a result, it issues this event immediately.");

                e.Parameters.Add("Status", 1, ParameterDataType.Bytes,
                    "0x00 LE_Set_PHY command succeeded or autonomous PHY update made by the Controller.\n" +
                    "0x01-0xFF LE_Set_PHY command failed. See Part D, Error Codes for a list of error codes and descriptions.");
                e.Parameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Range: 0x0000-0x0EFF (all other values reserved for future use)");
                e.Parameters.Add("TX_PHY", 1, ParameterDataType.Bytes,
                    "0x01 The receiver PHY for the connection is LE 1M\n" +
                    "0x02 The receiver PHY for the connection is LE 2M\n" +
                    "0x03 The receiver PHY for the connection is LE Coded\n" +
                    "All other values : Reserved for future use");
                e.Parameters.Add("RX_PHY", 1, ParameterDataType.Bytes,
                    "0x01 The transmitter PHY for the connection is LE 1M\n" +
                    "0x02 The transmitter PHY for the connection is LE 2M\n" +
                    "0x03 The transmitter PHY for the connection is LE Coded\n" +
                    "All other values : Reserved for future use");

                e = LEMetaEvent.SubEvents.Add("LE Extended Advertising Report Event", 0x0D,
                    "The LE Extended Advertising Report event indicates that one or more\n" +
                    "Bluetooth devices have responded to an active scan or have broadcast\n" +
                    "advertisements that were received during a passive scan.");

                e.Parameters.Add("Num_Reports", 1, ParameterDataType.Number,
                    "0x01-0x0A Number of separate reports in the event\n" +
                    "All other values : Reserved for future use",
                    ParameterType.ArrayIndicator);
                e.Parameters.Add("Event_Type[i]", 2, ParameterDataType.Bytes,
                    "7'b0 Connectable advertising\n" +
                    "7'b1 Scannable advertising\n" +
                    "7'b2 Directed advertising\n" +
                    "7'b3 Scan response\n" +
                    "7'b4 Legacy advertising PDUs used\n" +
                    "7'b5-6 00b = Complete\n" +
                    "       01b = Incomplete, more data to come\n" +
                    "       10b = Incomplete, data truncated, no more to come\n" +
                    "       11b = Reserved for future use",
                    ParameterType.ArrayData);
                e.Parameters.Add("Address_Type[i]", 1, ParameterDataType.Bytes,
                    "0x00 Public Device Address\n" +
                    "0x01 Random Device Address\n" +
                    "0x02 Public Identity Address(Corresponds to Resolved Private Address)\n" +
                    "0x03 Random(static) Identity Address (Corresponds to Resolved Private Address)\n" +
                    "0xFF No address provided (anonymous advertisement)\n" +
                    "All other values : Reserved for future use",
                    ParameterType.ArrayData);
                e.Parameters.Add("Address[i]", 6, ParameterDataType.Bytes,
                    "Public Device Address, Random Device Address, Public Identity\n" +
                    "Address or Random(static) Identity Address of the advertising device.",
                    ParameterType.ArrayData);
                e.Parameters.Add("Primary_PHY[i]", 1, ParameterDataType.Bytes,
                    "0x01 Advertiser PHY is LE 1M\n" +
                    "0x03 Advertiser PHY is LE Coded\n" +
                    "All other values : Reserved for future use",
                    ParameterType.ArrayData);
                e.Parameters.Add("Secondary_PHY[i]", 1, ParameterDataType.Bytes,
                    "0x01 Advertiser PHY is LE 1M\n" +
                    "0x02 Advertiser PHY is LE 2M\n" +
                    "0x03 Advertiser PHY is LE Coded\n" +
                    "All other values : Reserved for future use",
                    ParameterType.ArrayData);
                e.Parameters.Add("Advertising_SID[i]", 1, ParameterDataType.Bytes,
                    "0x00-0x0F Value of the Advertising SID subfield in the ADI field of the PDU\n" +
                    "0xFF No ADI field in the PDU\n" +
                    "All other values : Reserved for future use",
                    ParameterType.ArrayData);
                e.Parameters.Add("Tx_Power[i]", 1, ParameterDataType.S8,
                   "Size: 1 Octet (signed integer) Range: -127 ≤ N ≤ +126 Units: dBm\n" +
                   "127 Tx Power information not available",
                   ParameterType.ArrayData);
                e.Parameters.Add("RSSI[i]", 1, ParameterDataType.S8,
                    "1 Octet (signed integer) Range: -127 ≤ N ≤ +20, Units: dBm\n" +
                    "127 RSSI is not available",
                    ParameterType.ArrayData);
                e.Parameters.Add("Periodic_Advertising_Interval[i]", 2, ParameterDataType.Time_1_25msec,
                    "0 No periodic advertising\n" +
                    "Interval of the periodic advertising Range: 0x0006 to 0xFFFF\n" +
                    "Time = N * 1.25ms, Time range: 7.5ms to 81918.75 sec",
                    ParameterType.ArrayData);
                e.Parameters.Add("Direct_Address_Type[i]", 1, ParameterDataType.Bytes,
                    "0x00 Public Device Address\n" +
                    "0x01 Random Device Address\n" +
                    "0x02 Public Identity Address(Corresponds to Resolved Private Address)\n" +
                    "0x03 Random(static) Identity Address (Corresponds to Resolved Private Address)\n" +
                    "0xFE Random Device Address (Controller unable to resolve)\n" +
                    "All other values : Reserved for future use",
                    ParameterType.ArrayData);
                e.Parameters.Add("Direct_Address[i]", 6, ParameterDataType.Bytes,
                    "Public Device Address, Random Device Address, Public Identity\n" +
                    "Address or Random(static) Identity Address of the target device",
                    ParameterType.ArrayData);
                e.Parameters.Add("Data_Length[i]", 1, ParameterDataType.Number,
                    "0-229 Length of the Data[i] field for each device which responded\n" +
                    "All other values : Reserved for future use",
                    ParameterType.ArrayDataAndResizableIndicator);
                e.Parameters.Add("Data[i]Size", 31, ParameterDataType.Bytes,
                    "Data_Length[i] octets of advertising or scan response data formatted\n" +
                    "as defined in [Vol 3] Part C, Section 11.\n" +
                    "Note: Each element of this array has a variable length.",
                    ParameterType.ArrayDataAndResizableData);

                e = LEMetaEvent.SubEvents.Add("LE Periodic Advertising Sync Established Event", 0x0E,
                    "The LE Periodic Advertising Sync Established event indicates that the Controller\n" +
                    "has received the first periodic advertising packet from an advertiser after the\n" +
                    "LE_Periodic_Advertising_Create_Sync Command has been sent to the Controller.");

                e.Parameters.Add("Status", 1, ParameterDataType.Bytes,
                    "0x00 LE_Set_PHY command succeeded or autonomous PHY update made by the Controller.\n" +
                    "0x01-0xFF LE_Set_PHY command failed. See Part D, Error Codes for a list of error codes and descriptions.");
                e.Parameters.Add("Sync_Handle", 2, ParameterDataType.Bytes,
                    "Sync_Handle to be used to identify the periodic advertiser Range: 0x0000 - 0x0EFF");
                e.Parameters.Add("Advertising_SID", 1, ParameterDataType.Bytes,
                   "0x00-0x0F Value of the Advertising SID subfield in the ADI field of the PDU\n" +
                   "All other values : Reserved for future use");
                e.Parameters.Add("Advertiser_Address_Type", 1, ParameterDataType.Bytes,
                    "0x00 Public Device Address\n" +
                    "0x01 Random Device Address\n" +
                    "0x02 Public Identity Address(Corresponds to Resolved Private Address)\n" +
                    "0x03 Random(static) Identity Address (Corresponds to Resolved Private Address)\n" +
                    "All other values : Reserved for future use");
                e.Parameters.Add("Advertiser_Address", 6, ParameterDataType.Bytes,
                    "Public Device Address, Random Device Address, Public Identity\n" +
                    "Address or Random(static) Identity Address of the advertising device.");
                e.Parameters.Add("Advertiser_PHY", 1, ParameterDataType.Bytes,
                    "0x01 Advertiser PHY is LE 1M\n" +
                    "0x02 Advertiser PHY is LE 2M\n" +
                    "0x03 Advertiser PHY is LE Coded\n" +
                    "All other values : Reserved for future use");
                e.Parameters.Add("Periodic_Advertising_Interval", 2, ParameterDataType.Time_1_25msec,
                     "Interval of the periodic advertising Range: 0x0006 to 0xFFFF\n" +
                     "Time = N * 1.25ms, Time range: 7.5ms to 81918.75 sec");
                e.Parameters.Add("Advertiser_Clock_Accuracy", 1, ParameterDataType.Bytes,
                    "0x00 500 ppm\n" +
                    "0x01 250 ppm\n" +
                    "0x02 150 ppm\n" +
                    "0x03 100 ppm\n" +
                    "0x04  75 ppm\n" +
                    "0x05  50 ppm\n" +
                    "0x06  30 ppm\n" +
                    "0x07  20 ppm\n" +
                    "All other values : Reserved for future use");

                e = LEMetaEvent.SubEvents.Add("LE Periodic Advertising Report Event", 0x0F,
                    "The LE Periodic Advertising Report event indicates that the Controller has\n" +
                    "received a Periodic Advertising packet.");

                e.Parameters.Add("Sync_Handle", 2, ParameterDataType.Bytes,
                    "Sync_Handle to be used to identify the periodic advertiser Range: 0x0000 - 0x0EFF\n" +
                    "All other values : Reserved for future use");
                e.Parameters.Add("Tx_Power", 1, ParameterDataType.S8,
                   "Size: 1 Octet (signed integer) Range: -127 ≤ N ≤ +126 Units: dBm\n" +
                   "127 Tx Power information not available");
                e.Parameters.Add("RSSI", 1, ParameterDataType.S8,
                    "1 Octet (signed integer) Range: -127 ≤ N ≤ +20, Units: dBm\n" +
                    "127 RSSI is not available");
                e.Parameters.Add("Unused", 1, ParameterDataType.Number,
                    "0xFF This value must be used by the Controller\n" +
                    "All other values : Reserved for future use");
                e.Parameters.Add("Data_Status", 1, ParameterDataType.Bytes,
                    "0x00 Data complete\n" +
                    "0x01 Data incomplete, more data to come\n" +
                    "0x02 Data incomplete, data truncated, no more to come\n" +
                    "All other values : Reserved for future use");
                e.Parameters.Add("Data_Length", 1, ParameterDataType.Number,
                   "0-248 Length of the Data field\n" +
                   "All other values : Reserved for future use",
                   ParameterType.ResizableIndicator);
                e.Parameters.Add("Data", 31, ParameterDataType.Bytes,
                    "Variable : Data received from a Periodic Advertising packet",
                    ParameterType.ResizableData);

                e = LEMetaEvent.SubEvents.Add("LE Periodic Advertising Sync Lost Event", 0x10,
                    "The LE Periodic Advertising Sync Lost event indicates that the Controller has\n" +
                    "not received a Periodic Advertising packet identified by Sync_Handle within the\n" +
                    "timeout period.");

                e.Parameters.Add("Sync_Handle", 2, ParameterDataType.Bytes,
                    "Sync_Handle to be used to identify the periodic advertiser Range: 0x0000 - 0x0EFF\n" +
                    "All other values : Reserved for future use");

                e = LEMetaEvent.SubEvents.Add("LE Scan Timeout Event", 0x11,
                    "The LE Scan Timeout event indicates that scanning has ended because the\n" +
                    "duration has expired.\n" +
                    "This event shall only be generated if scanning was enabled using the LE Set\n" +
                    "Extended Scan Enable command.");

                e = LEMetaEvent.SubEvents.Add("LE Advertising Set Terminated Event", 0x12,
                    "The LE Advertising Set Terminated event indicates that the Controller has\n" +
                    "terminated advertising in the advertising sets specified by the\n" +
                    "Advertising_Handle parameter.\n" +
                    "This event shall be generated every time connectable advertising in an\n" +
                    "advertising set results in a connection being created.\n" +
                    "This event shall only be generated if advertising was enabled using the LE Set\n" +
                    "Extended Advertising Enable command.");

                e.Parameters.Add("Status", 1, ParameterDataType.Bytes,
                    "0x00 LE_Set_PHY command succeeded or autonomous PHY update made by the Controller.\n" +
                    "0x01-0xFF LE_Set_PHY command failed. See Part D, Error Codes for a list of error codes and descriptions.");
                e.Parameters.Add("Advertising_Handle", 1, ParameterDataType.Bytes,
                    "0x00-0xEF Advertising_Handle in which advertising has ended\n" +
                    "All other values : Reserved for future use");
                e.Parameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle of the connection whose creation ended the advertising, Range: 0x0000 - 0x0EFF\n" +
                    "All other values : Reserved for future use");
                e.Parameters.Add("Num_Completed_Extended_Advertising_Events", 1, ParameterDataType.Number,
                    "Number of completed extended advertising events transmitted by the Controller");

                e = LEMetaEvent.SubEvents.Add("LE Scan Request Received Event", 0x13,
                    "The LE Scan Request Received event indicates that a SCAN_REQ PDU or an\n" +
                    "AUX_SCAN_REQ PDU has been received by the advertiser.\n" +
                    "The request contains a device address from a scanner that is allowed by\n" +
                    "the advertising filter policy.\n" +
                    "The advertising set is identified by Advertising_Handle.\n" +
                    "This event shall only be generated if advertising was enabled using the LE Set\n" +
                    "Extended Advertising Enable command.");

                e.Parameters.Add("Advertising_Handle", 1, ParameterDataType.Bytes,
                    "0x00-0xEF Advertising_Handle in which advertising has ended\n" +
                    "All other values : Reserved for future use");
                e.Parameters.Add("Scanner_Address_Type", 1, ParameterDataType.Bytes,
                   "0x00 Public Device Address\n" +
                   "0x01 Random Device Address\n" +
                   "0x02 Public Identity Address(Corresponds to Resolved Private Address)\n" +
                   "0x03 Random(static) Identity Address (Corresponds to Resolved Private Address)\n" +
                   "All other values : Reserved for future use");
                e.Parameters.Add("Scanner_Address", 6, ParameterDataType.Bytes,
                   "Public Device Address, Random Device Address, Public Identity\n" +
                   "Address or Random(static) Identity Address of the advertising device.");

                e = LEMetaEvent.SubEvents.Add("LE Channel Selection Algorithm Event", 0x14,
                    "The LE Channel Selection Algorithm Event indicates which channel selection\n" +
                    "algorithm is used on a data channel connection(see[Vol 6] Part B, Section 4.5.8).");

                e.Parameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle of the connection whose creation ended the advertising, Range: 0x0000 - 0x0EFF\n" +
                    "All other values : Reserved for future use");
                e.Parameters.Add("Channel_Selection_Algorithm", 1, ParameterDataType.Bytes,
                   "0x00 LE Channel Selection Algorithm #1 is used\n" +
                   "0x01 LE Channel Selection Algorithm #2 is used\n" +
                   "All other values : Reserved for future use");

            }

            private void InitEvents()
            {
                Event e;

                e = EvtCollection.Add("Disconnection Complete Event", 0x05,
                    "The Disconnection Complete event occurs when a connection is terminated.\n" +
                    "The status parameter indicates if the disconnection was successful or not.\n" +
                    "The reason parameter indicates the reason for the disconnection if the\n" +
                    "disconnection was successful.");

                e.Parameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 Disconnection has occurred.\n" +
                    "0x01 - 0xFF Disconnection failed to complete.See Part D, Error Codes on page 370");
                e.Parameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle which was disconnected.\n" +
                    "Range: 0x0000 - 0x0EFF(0x0F00 - 0x0FFF Reserved for future use)");
                e.Parameters.Add("Reason", 1, ParameterDataType.Status,
                    "Reason for disconnection.\n" +
                    "See Part D, Error Codes on page 370 for error codes and descriptions.");

                e = EvtCollection.Add("Encryption Change Event", 0x08,
                    "The Encryption Change event is used to indicate that the change of the\n" +
                    "encryption mode has been completed.");

                e.Parameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 Encryption Change has occurred.\n" +
                    "0x01-0xFF Encryption Change failed. See Part D, Error Codes on page 370");
                e.Parameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle for which the link layer encryption has been enabled/disabled\n" +
                    "for all Connection_Handles with the same BR / EDR Controller endpoint as the\n" +
                    "specified Connection_Handle.\n" +
                    "Range: 0x0000 - 0x0EFF(0x0F00 - 0x0FFF Reserved for future use)");
                e.Parameters.Add("Encryption_Enabled", 1, ParameterDataType.Bytes,
                    "0x00 Link Level Encryption is OFF.\n" +
                    "0x01 Link Level Encryption is ON with E0 for BR / EDR.\n" +
                    "Link Level Encryption is ON with AES - CCM for LE.\n" +
                    "0x02 Link Level Encryption is ON with AES - CCM for BR / EDR.\n" +
                    "0x03 - 0xFF Reserved.");

                e = EvtCollection.Add("Read Remote Version Information Complete Event", 0x0C,
                    "The Read Remote Version Information Complete event is used to indicate the\n" +
                    "completion of the process obtaining the version information of the remote\n" +
                    "Controller specified by the Connection_Handle event parameter.\n" +
                    "The Connection_Handle shall be for an ACL connection.");

                e.Parameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 Disconnection has occurred.\n" +
                    "0x01 - 0xFF Disconnection failed to complete.See Part D, Error Codes on page 370");
                e.Parameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle which was disconnected.\n" +
                    "Range: 0x0000 - 0x0EFF(0x0F00 - 0x0FFF Reserved for future use)");
                e.Parameters.Add("Version", 1, ParameterDataType.Bytes,
                    "Version of the Current LMP in the remote Controller.\n" +
                    "See LMP VersNr and Link LayerVersNr in the Bluetooth Assigned Numbers.");
                e.Parameters.Add("Manufacturer_Name", 2, ParameterDataType.Bytes,
                   "Manufacturer Name of the remote Controller.\n" +
                   "See CompId in the Bluetooth Assigned Numbers.");
                e.Parameters.Add("Subversion", 2, ParameterDataType.Bytes,
                   "Subversion of the LMP in the remote Controller. See Part C, Table 5.2,\n" +
                   "page 358 and[Vol 6] Part B, Section 2.4.2.13(SubVersNr).");

                e = EvtCollection.Add("Command Complete Event", 0x0E,
                    "The Command Complete event is used by the Controller for most commands to transmit return status\n" +
                    "of a command and the other event parameters that are specified for the issued HCI command.");

                e.Parameters.Add("Num_HCI_Command_Packets", 1, ParameterDataType.Number,
                    "The Number of HCI command packets which are allowed to be sent to the Controller from the Host.\n" +
                    "Range for N: 0 – 255");
                e.Parameters.Add("Command_Opcode", 2, ParameterDataType.U16,
                    "Opcode of the command which caused this event.");

                e = EvtCollection.Add("Command Status Event", 0x0F,
                    "The Command Status event is used to indicate that the command described by\n" +
                    "the Command_Opcode parameter has been received, and that the Controller is\n" +
                    "currently performing the task for this command.");

                e.Parameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 Command currently in pending.\n" +
                    "0x01-0xFF Command failed.");
                e.Parameters.Add("Num_HCI_Command_Packets", 1, ParameterDataType.Number,
                   "The Number of HCI command packets which are allowed to be sent to the Controller from the Host.\n" +
                   "Range for N: 0 – 255");
                e.Parameters.Add("Command_Opcode", 2, ParameterDataType.U16,
                    "Opcode of the command which caused this event and is pending completion.");

                e = EvtCollection.Add("Hardware Error Event", 0x10,
                    "The Hardware Error event is used to indicate some type of hardware failure for\n" +
                    "the BR / EDR Controller.This event is used to notify the Host that a hardware\n" +
                    "failure has occurred in the Controller.");

                e.Parameters.Add("Hardware_Code", 1, ParameterDataType.Bytes,
                    "These Hardware_Codes will be implementation-specific, and can be\n" +
                    "assigned to indicate various hardware problems.");

                e = EvtCollection.Add("Number Of Completed Packets Event", 0x13,
                    "The Number Of Completed Packets event is used by the Controller to indicate to\n" +
                    "the Host how many HCI Data Packets have been completed for each Connection_Handle\n" +
                    "since the previous Number Of Completed Packets event was sent.");

                e.Parameters.Add("Number_of_Handles", 1, ParameterDataType.Number,
                    "The number of Connection_Handles and Num_HCI_Data_Packets parameters pairs\n" +
                    "contained in this event.",
                    ParameterType.ArrayIndicator);
                e.Parameters.Add("Connection_Handle[i]", 2, ParameterDataType.Bytes,
                    "Connection_Handle.\n" +
                    "Range: 0x0000 - 0x0EFF(0x0F00 - 0x0FFF Reserved for future use)",
                    ParameterType.ArrayData);
                e.Parameters.Add("HC_Num_Of_Completed_Packets[i]", 2, ParameterDataType.Bytes,
                    "The number of HCI Data Packets that have been completed (transmitted or flushed)\n" +
                    "for the associated Connection_Handle since the previous time the event was returned.\n" +
                    "Range for N: 0x0000-0xFFFF",
                    ParameterType.ArrayData);

                e = EvtCollection.Add("Data Buffer Overflow Event", 0x1A,
                    "This event is used to indicate that the Controller’s data buffers have been\n" +
                    "overflowed.This can occur if the Host has sent more packets than allowed.\n" +
                    "The Link_Type parameter is used to indicate that the overflow was caused by\n" +
                    "ACL or synchronous data.");

                e.Parameters.Add("Link_Type", 1, ParameterDataType.Bytes,
                    "0x00 Synchronous Buffer Overflow (Voice Channels).\n" +
                    "0x01 ACL Buffer Overflow(Data Channels).\n" +
                    "0x02-0xFF Reserved for future use.");

                e = EvtCollection.Add("Encryption Key Refresh Complete Event", 0x30,
                    "The Encryption Change event is used to indicate that the change of the\n" +
                    "encryption mode has been completed.");

                e.Parameters.Add("Status", 1, ParameterDataType.Status,
                    "0x00 Encryption Key Refresh completed successfully.\n" +
                    "0x01-0xFF Encryption Key Refresh failed. See Part D, Error Codes on page 370");
                e.Parameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection Handle for the ACL connection to have the encryption key refreshed on.\n" +
                    "Range: 0x0000 - 0x0EFF(0x0F00 - 0x0FFF Reserved for future use)");

                e = EvtCollection.Add("LE Meta Event", 0x3E,
                    "The LE Meta Event is used to encapsulate all LE Controller specific events.\n" +
                    "The Subevent_Code shall be set to one of the valid Subevent_Codes from an LE specific event.\n" +
                    "All other Subevent_Parameters are defined in the LE Controller specific events.");

                e.Parameters.Add("Subevent_Code", 1, ParameterDataType.Bytes,
                    "Subevent code for LE Connection Complete event");

                InitLEMetaEvents(e);

                e = EvtCollection.Add("Authenticated Payload Timeout Expired Event", 0x57,
                    "The Authenticated Payload Timeout Expired event is used to indicate that a\n" +
                    "packet containing a valid MIC on the Connection_Handle was not received\n" +
                    "within the authenticatedPayloadTO(see[Vol 2] Part B, Section Appendix B for\n" +
                    "the BR/ EDR and[Vol 6] Part B, Section 5.4, LE Authenticated Payload Timeout\n" +
                    "for the LE connection).Note: A Host may choose to disconnect the link when\n" +
                    "this occurs.");

                e.Parameters.Add("Connection_Handle", 2, ParameterDataType.Bytes,
                    "Connection_Handle of the connection where the packet with a valid MIC was not\n" +
                    "received within the timeout.\n" +
                    "Range: 0x0000 - 0x0EFF(0x0F00 - 0x0FFF Reserved for future use)");

            }

            public bool SetConnectionParameter(int ConDevIndex)
            {
                if (CurLLState == LinkLayerState.Standby)
                {
                    Command Cmd = CmdGroupList[4].Commands.GetCommand("HCI_LE_Create_Connection");

                    Parameter Para = Cmd.CommandParameters.GetParameter("Peer_Address");

                    string[] Address = AdvertisingReports[ConDevIndex].BDAddress.Split(' ');
                    if (Address.Length == 6)
                    {
                        for (int i = 0; i < Address.Length; i++)
                            Para.Data[Para.Size - 1 - i] = byte.Parse(Address[i], System.Globalization.NumberStyles.HexNumber);
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
