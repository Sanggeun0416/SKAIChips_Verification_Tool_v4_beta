using System.Collections;

namespace BlueTooth
{
    namespace HCI
    {

        public delegate bool WriteCommand(Command command);

        public delegate void SetParametersData(ParameterCollection Parameters, byte[] DataBuffer, int StartIndex);

        public enum ParameterType
        {

            Data,

            ArrayIndicator,

            ArrayData,

            ResizableIndicator,

            ResizableData,

            ArrayDataAndResizableIndicator,

            ArrayDataAndResizableData,
        }

        public enum ParameterDataType
        {

            S8,

            U16,

            String,

            Bytes,

            Number,

            BLE_Channel,

            Status,

            CoreVersion,

            Time_625usec = 625,

            Time_1_25msec = 1250,

            Time_10msec = 10000,

            Time_1usec,

            Time_1sec = 1000000,
        }

        public enum EventCodes
        {
            Disconnection_Complete_Event = 0x05,
            Encryption_Change_Event = 0x08,
            Read_Remote_Version_Information_Complete_Event = 0x0C,
            Command_Complete_Event = 0x0E,
            Command_Status_Event = 0x0F,
            Hardware_Error_Event = 0x10,
            Number_Of_Completed_Packets_Event = 0x13,
            Data_Buffer_Overflow_Event = 0x1A,
            Encryption_Key_Refresh_Complete_Event = 0x30,
            LE_Meta_Event = 0x3E,
            Authenticated_Payload_Timeout_Expired_Event = 0x57,
        }

        public enum LEMetaEvents
        {
            LE_Connection_Complete_Event = 0x01,
            LE_Advertising_Report_Event = 0x02,
            LE_Connection_Update_Complete_Event = 0x03,
            LE_Read_Remote_Used_Features_Complete_Event = 0x04,
            LE_Long_Term_Key_Request_Event = 0x05,
            LE_Remote_Connection_Parameter_Request_Event = 0x06,
            LE_Data_Length_Change_Event = 0x07,
            LE_Read_Local_P256_Public_Key_Complete_Event = 0x08,
            LE_Generate_DHKey_Complete_Event = 0x09,
            LE_Enhanced_Connection_Complete_Event = 0x0A,
            LE_Direct_Advertising_Report_Event = 0x0B,
            LE_PHY_Update_Complete_Event = 0x0C,
            LE_Extended_Advertising_Report_Event = 0x0D,
            LE_Periodic_Advertising_Sync_Established_Event = 0x0E,
            LE_Periodic_Advertising_Report_Event = 0x0F,
            LE_Periodic_Advertising_Sync_Lost_Event = 0x10,
            LE_Scan_Timeout_Event = 0x11,
            LE_Advertising_Set_Terminated_Event = 0x12,
            LE_Scan_Request_Received_Event = 0x13,
            LE_Channel_Selection_Algorithm_Event = 0x14,
        }

        public enum LinkLayerState
        {
            Unknown,
            Standby,
            Advertising,
            Scanning,
            Connection
        }

        public class Parameter
        {

            public string Name
            {
                get;
            }
            private byte ParaSize = 0;

            public byte[] Data
            {
                get; set;
            }

            public string Description
            {
                get; set;
            }

            public string Information
            {
                get; set;
            }

            public Color InfoColor
            {
                get; set;
            }
            private ParameterDataType ParaDataType;
            private ParameterType ParaType;

            public TreeNode Node
            {
                get; set;
            }

            public ParameterCollection Parent
            {
                get; set;
            }

            public event EventHandler SizeChanged;

            public Parameter(string Name, byte Size, ParameterDataType DataType, string Description = "", ParameterType Type = ParameterType.Data)
            {
                Node = new TreeNode(Name)
                {
                    ToolTipText = Description
                };
                this.Name = Name;
                ParaSize = Size;
                this.Description = Description;
                ParaDataType = DataType;
                ParaType = Type;

                Data = new byte[ParaSize];
                InfoColor = Color.Black;
                Information = "";
            }

            protected virtual void OnSizeChanged(EventArgs e)
            {
                SizeChanged?.Invoke(this, e);
            }

            public byte Size
            {
                get
                {
                    return ParaSize;
                }
                set
                {
                    if (ParaSize != value)
                    {
                        ParaSize = value;
                        Data = new byte[ParaSize];
                        OnSizeChanged(EventArgs.Empty);
                    }
                }
            }

            public ParameterDataType DataType
            {
                get
                {
                    return ParaDataType;
                }
            }

            public ParameterType Type
            {
                get
                {
                    return ParaType;
                }
            }

            public static string GetDataString(Parameter Para)
            {
                string Payload = "";

                switch (Para.DataType)
                {
                    case ParameterDataType.S8:
                        if (Para.Size == 1)
                        {
                            Payload = ((sbyte)Para.Data[0]).ToString() + ", ";
                            goto default;
                        }
                        else
                        {
                            Payload = "Unknown Data: ";
                            Para.InfoColor = Color.Tomato;
                            goto default;
                        }
                    case ParameterDataType.U16:
                        if (Para.Size == 2)
                        {
                            Payload = "0x" + ((Para.Data[1] << 8) | Para.Data[0]).ToString("X4") + ", ";
                            goto default;
                        }
                        else
                        {
                            Payload = "Unknown Data: ";
                            Para.InfoColor = Color.Tomato;
                            goto default;
                        }
                    case ParameterDataType.String:
                        for (int i = 0; i < Para.Size; i++)
                        {
                            if (Para.Data[i] == 0)
                                break;
                            Payload += (char)Para.Data[i];
                        }
                        break;
                    case ParameterDataType.Number:
                        if (Para.Size <= 4)
                        {
                            int Number = 0;
                            for (int i = 0; i < Para.Size; i++)
                                Number |= (Para.Data[i] << (i * 8));
                            Payload = Number.ToString() + ", ";
                            goto default;
                        }
                        else
                        {
                            Payload = "Unknown Data: ";
                            Para.InfoColor = Color.Tomato;
                            goto default;
                        }
                    case ParameterDataType.BLE_Channel:
                        if (Para.Size == 1)
                        {
                            Payload = (Para.Data[0] * 2 + 2402).ToString() + " MHz, ";
                            goto default;
                        }
                        else
                        {
                            Payload = "Unknown Data: ";
                            Para.InfoColor = Color.Tomato;
                            goto default;
                        }
                    case ParameterDataType.Status:
                        if (Para.Data[0] < HCIManager.ErrorCodes.Length)
                        {
                            Payload += Para.Data[0].ToString("X2") + " (" + HCIManager.ErrorCodes[Para.Data[0]] + ")";
                            if (Para.Data[0] == 0)
                                Para.InfoColor = Color.LimeGreen;
                            else
                                Para.InfoColor = Color.Tomato;
                        }
                        else
                        {
                            Payload = "Unknown Status: ";
                            Para.InfoColor = Color.Tomato;
                            goto default;
                        }
                        break;
                    case ParameterDataType.CoreVersion:
                        if (Para.Data[0] < HCIManager.CoreVersion.Length)
                            Payload += Para.Data[0].ToString("X2") + " (" + HCIManager.CoreVersion[Para.Data[0]] + ")";
                        else
                        {
                            Payload += "Unknown Core Version: ";
                            Para.InfoColor = Color.Tomato;
                            goto default;
                        }
                        break;

                    case ParameterDataType.Time_625usec:
                    case ParameterDataType.Time_1_25msec:
                    case ParameterDataType.Time_10msec:
                    case ParameterDataType.Time_1sec:
                    case ParameterDataType.Time_1usec:
                        if (Para.Size <= 4)
                        {
                            int Number = 0;
                            for (int i = 0; i < Para.Size; i++)
                                Number |= (Para.Data[i] << (i * 8));
                            if (Para.DataType == ParameterDataType.Time_1usec)
                                Payload = Number.ToString() + " us, ";
                            else if (Para.DataType == ParameterDataType.Time_1sec)
                                Payload = Number.ToString() + " sec, ";
                            else
                            {
                                double Time = (double)Number * (int)Para.DataType / 1000F;
                                Payload = Time.ToString() + " ms, ";
                            }
                            goto default;
                        }
                        else
                        {
                            Payload = "Unknown Data: ";
                            Para.InfoColor = Color.Tomato;
                            goto default;
                        }
                    case ParameterDataType.Bytes:
                    default:
                        if (Para.Data.Length >= 1)
                        {
                            for (int i = Para.Data.Length - 1; i > 0; i--)
                                Payload += Para.Data[i].ToString("X2") + " ";
                            Payload += Para.Data[0].ToString("X2");
                        }
                        break;
                }
                return Payload;
            }

            public static int SetData(Parameter Para, byte[] DataBuffer, int StartIndex)
            {
                int Index = StartIndex;
                if (Para.Size <= DataBuffer.Length - StartIndex)
                {
                    for (int i = 0; i < Para.Size; i++)
                        Para.Data[i] = DataBuffer[Index++];
                    Para.Information = Parameter.GetDataString(Para);
                }
                else
                {
                    for (int i = 0; i < Para.Size; i++)
                        Para.Data[i] = 0;
                    Para.Information = "No Response Data";
                    Para.InfoColor = Color.Tomato;
                }
                return Index;
            }

            public Parameter Clone()
            {
                Parameter temp = new Parameter(Name, Size, DataType, Description, Type);
                for (int i = 0; i < temp.Size; i++)
                    temp.Data[i] = Data[i];

                return temp;
            }

            public override string ToString()
            {
                return GetDataString(this);
            }

        }

        public class ParameterCollection : IEnumerator, IEnumerable
        {
            private List<Parameter> ParaList = new List<Parameter>();
            private TreeNodeCollection ParaNodes = null;
            private byte TotalParameterLength = 0;

            public ParameterCollection(TreeNodeCollection Nodes)
            {
                ParaNodes = Nodes;
                ParaList = new List<Parameter>();
            }

            private int Location = -1;

            public Parameter this[int Index]
            {
                get
                {
                    return ParaList[Index];
                }
                set
                {
                    ParaList[Index] = value;
                }
            }

            public void Reset()
            {
                Location = -1;
            }

            public object Current
            {
                get
                {
                    return ParaList[Location];
                }
            }

            public bool MoveNext()
            {
                if (Location == (ParaList.Count - 1))
                {
                    Reset();
                    return false;
                }
                Location++;
                return (Location < ParaList.Count);
            }

            public System.Collections.IEnumerator GetEnumerator()
            {
                foreach (var item in ParaList)
                {
                    yield return item;
                }
            }

            public int Count
            {
                get
                {
                    return ParaList.Count;
                }
            }

            public byte TotalLength
            {
                get
                {
                    return TotalParameterLength;
                }
            }

            public TreeNodeCollection Nodes
            {
                get
                {
                    return ParaNodes;
                }
            }

            private void Para_SizeChanged(object sender, EventArgs e)
            {
                TotalParameterLength = 0;
                foreach (Parameter Para in ParaList)
                    TotalParameterLength += Para.Size;
            }

            public Parameter Add(string Name, byte Size, ParameterDataType DataType, string Description = "", ParameterType Type = ParameterType.Data)
            {
                Parameter Para = new Parameter(Name, Size, DataType, Description, Type);
                Para.SizeChanged += Para_SizeChanged;
                Para.Parent = this;
                ParaList.Add(Para);
                ParaNodes.Add(Para.Node);
                TotalParameterLength += Size;

                return Para;
            }

            public Parameter Add(Parameter Para)
            {
                ParaList.Add(Para);
                ParaNodes.Add(Para.Node);
                TotalParameterLength += Para.Size;

                return Para;
            }

            public void Insert(int Index, Parameter Para)
            {
                if (Index >= 0)
                {
                    if (Index < ParaList.Count)
                    {
                        ParaList.Insert(Index, Para);
                        ParaNodes.Insert(Index, Para.Node);
                        TotalParameterLength += Para.Size;
                    }
                    else
                        Add(Para);
                }
            }

            public void Remove(string Name)
            {
                for (int i = 0; i < ParaList.Count; i++)
                {
                    if (ParaList[i].Name == Name)
                    {
                        TotalParameterLength -= ParaList[i].Size;
                        ParaNodes.RemoveAt(i);
                        ParaList.RemoveAt(i);
                        break;
                    }
                }
            }

            public void RemoveAt(int Index)
            {
                if ((Index >= 0) && (Index < ParaList.Count))
                {
                    TotalParameterLength -= ParaList[Index].Size;
                    ParaNodes.RemoveAt(Index);
                    ParaList.RemoveAt(Index);
                }
            }

            public void Clear()
            {
                TotalParameterLength = 0;
                ParaList.Clear();
                ParaNodes.Clear();
            }

            public Parameter GetParameter(string Name)
            {
                foreach (Parameter Para in ParaList)
                    if (Para.Name == Name)
                        return Para;
                return null;
            }

            public Parameter GetNextParameter(string Name)
            {
                for (int i = 0; i < ParaList.Count - 1; i++)
                {
                    if (ParaList[i].Name == Name)
                        return ParaList[i + 1];
                }
                return null;
            }

            public static void SetParameters(ParameterCollection Parameters, byte[] DataBuffer, int StartIndex)
            {
                int Index = StartIndex;

                for (int ParaIndex = 0; ParaIndex < Parameters.Count; ParaIndex++)
                {
                    Index = Parameter.SetData(Parameters[ParaIndex], DataBuffer, Index);

                    if (Parameters[ParaIndex].Type == ParameterType.ArrayIndicator)
                    {
                        int OrgParaSize = Parameters.Count;
                        int ArraySize = Parameters[ParaIndex].Data[0];
                        List<Parameter> ArrayParaList = new List<Parameter>();
                        bool Stored = true;
                        int RemoveCount = 0;
                        int StoredIndex = ParaIndex + 1;

                        for (int i = 1; i < Parameters.Count; i++)
                        {
                            if ((Parameters[ParaIndex + i].Type == ParameterType.ArrayData)
                                || (Parameters[ParaIndex + i].Type == ParameterType.ArrayDataAndResizableIndicator)
                                || (Parameters[ParaIndex + i].Type == ParameterType.ArrayDataAndResizableData))
                            {
                                RemoveCount++;

                                Stored = true;
                                foreach (Parameter Para in ArrayParaList)
                                {
                                    if (Para.Name == Parameters[ParaIndex + i].Name)
                                    {
                                        Stored = false;
                                        break;
                                    }
                                }
                                if (Stored)
                                    ArrayParaList.Add(Parameters[ParaIndex + i]);
                            }
                        }

                        for (int i = 0; i < RemoveCount; i++)
                            Parameters.RemoveAt(ParaIndex + 1);

                        for (int i = 0; i < ArraySize; i++)
                            for (int j = 0; j < ArrayParaList.Count; j++)
                                Parameters.Insert(StoredIndex++, ArrayParaList[j].Clone());
                    }
                    else if ((Parameters[ParaIndex].Type == ParameterType.ResizableIndicator)
                        || (Parameters[ParaIndex].Type == ParameterType.ArrayDataAndResizableIndicator))
                    {
                        if ((Parameters[ParaIndex + 1].Type == ParameterType.ResizableData)
                            || (Parameters[ParaIndex + 1].Type == ParameterType.ArrayDataAndResizableData))
                            Parameters[ParaIndex + 1].Size = Parameters[ParaIndex].Data[0];
                    }
                }
            }

        }

        public class Command
        {

            private ParameterCollection CommandParaCollection = null;
            private ParameterCollection ReturnParaCollection = null;
            private string CommandName = "";
            private int OpGroupField = 0;
            private int OpCommandField = 0;
            private string CommandDescription = "";
            private TreeNode CommandTreeNode;
            private TreeNode ReturnTreeNode;

            public WriteCommand SendCommand = null;

            public Command(string Name, int OGF, int OCF, string Description = "")
            {
                CommandTreeNode = new TreeNode(Name + " [0x" + OCF.ToString("X3") + "]")
                {
                    ToolTipText = Description
                };
                ReturnTreeNode = new TreeNode("Rsp:" + Name + " [0x" + OCF.ToString("X3") + "]");
                CommandName = Name;
                OpGroupField = OGF;
                OpCommandField = OCF;
                CommandDescription = Description;

                CommandParaCollection = new ParameterCollection(CommandTreeNode.Nodes);
                ReturnParaCollection = new ParameterCollection(ReturnTreeNode.Nodes);
            }

            public string Name
            {
                get
                {
                    return CommandName;
                }
            }

            public short OpCode
            {
                get
                {
                    return (short)((OpGroupField << 10) | OpCommandField);
                }
            }

            public int OGF
            {
                get
                {
                    return OpGroupField;
                }
            }

            public int OCF
            {
                get
                {
                    return OpCommandField;
                }
            }

            public string Description
            {
                get
                {
                    return CommandDescription;
                }
                set
                {
                    CommandDescription = value;
                }
            }

            public ParameterCollection CommandParameters
            {
                get
                {
                    return CommandParaCollection;
                }
                set
                {
                    CommandParaCollection = value;
                }
            }

            public ParameterCollection ReturnParameters
            {
                get
                {
                    return ReturnParaCollection;
                }
                set
                {
                    ReturnParaCollection = value;
                }
            }

            public TreeNode CommandNode
            {
                get
                {
                    return CommandTreeNode;
                }
                set
                {
                    CommandTreeNode = value;
                }
            }

            public TreeNode ReturnNode
            {
                get
                {
                    return ReturnTreeNode;
                }
                set
                {
                    ReturnTreeNode = value;
                }
            }

            public byte[] GetCommandPacket()
            {
                List<byte> Data = new List<byte>
                {
                    (byte)(OpCode & 0xff),
                    (byte)(OpCode >> 8),
                    CommandParaCollection.TotalLength
                };
                foreach (Parameter Para in CommandParaCollection)
                {
                    for (int i = 0; i < Para.Size; i++)
                        Data.Add(Para.Data[i]);
                    Para.Information = Parameter.GetDataString(Para);
                }
                return Data.ToArray();
            }

            public void SetReturnParameters(byte[] DataBuffer, int StartIndex)
            {
                ParameterCollection.SetParameters(ReturnParaCollection, DataBuffer, StartIndex);
            }

            public bool Send()
            {
                bool Status = false;

                if (SendCommand != null)
                    Status = SendCommand(this);

                return Status;
            }

            public Command Clone()
            {
                Command temp = new Command(Name, OGF, OCF, Description);
                temp.SendCommand = SendCommand;
                temp.CommandParameters = new ParameterCollection(temp.CommandNode.Nodes);
                for (int i = 0; i < CommandParameters.Count; i++)
                    temp.CommandParameters.Add(CommandParameters[i].Clone());
                temp.ReturnParameters = new ParameterCollection(temp.ReturnParameters.Nodes);
                for (int i = 0; i < ReturnParameters.Count; i++)
                    temp.ReturnParameters.Add(ReturnParameters[i].Clone());

                return temp;
            }

        }

        public class CommandCollection : IEnumerator, IEnumerable
        {
            private int OpGroupField = 0;
            private List<Command> CommandList = null;
            private TreeNodeCollection CommandNodes = null;

            public CommandCollection(int OGF, TreeNodeCollection Nodes)
            {
                CommandNodes = Nodes;
                OpGroupField = OGF;
                CommandList = new List<Command>();
            }

            private int Location = -1;

            public Command this[int Index]
            {
                get
                {
                    return CommandList[Index];
                }
                set
                {
                    CommandList[Index] = value;
                }
            }

            public void Reset()
            {
                Location = -1;
            }

            public object Current
            {
                get
                {
                    return CommandList[Location];
                }
            }

            public bool MoveNext()
            {
                if (Location == (CommandList.Count - 1))
                {
                    Reset();
                    return false;
                }
                Location++;
                return (Location < CommandList.Count);
            }

            public System.Collections.IEnumerator GetEnumerator()
            {
                foreach (var reg in CommandList)
                {
                    yield return reg;
                }
            }

            public int Count
            {
                get
                {
                    return this.CommandList.Count;
                }
            }

            public TreeNodeCollection TreeNodes
            {
                get
                {
                    return this.CommandNodes;
                }
            }

            public Command Add(string Name, int OCF, string Description)
            {
                Command Cmd = new Command(Name, OpGroupField, OCF, Description);
                CommandList.Add(Cmd);
                CommandNodes.Add(Cmd.CommandNode);

                return Cmd;
            }

            public Command Add(Command Cmd)
            {
                CommandList.Add(Cmd);
                CommandNodes.Add(Cmd.CommandNode);

                return Cmd;
            }

            public void Insert(int Index, Command Cmd)
            {
                if (Index >= 0)
                {
                    if (Index < CommandList.Count)
                    {
                        CommandList.Insert(Index, Cmd);
                        CommandNodes.Insert(Index, Cmd.CommandNode);
                    }
                    else
                        Add(Cmd);
                }
            }

            public void Remove(short OpCode)
            {
                for (int i = 0; i < CommandList.Count; i++)
                {
                    if (CommandList[i].OpCode == OpCode)
                    {
                        CommandList.RemoveAt(i);
                        CommandNodes.RemoveAt(i);
                        break;
                    }
                }
            }

            public void Remove(string Name)
            {
                for (int i = 0; i < CommandList.Count; i++)
                {
                    if (CommandList[i].Name == Name)
                    {
                        CommandList.RemoveAt(i);
                        CommandNodes.RemoveAt(i);
                        break;
                    }
                }
            }

            public void RemoveAt(int Index)
            {
                if ((Index >= 0) && (Index < CommandList.Count))
                {
                    CommandList.RemoveAt(Index);
                    CommandNodes.RemoveAt(Index);
                }
            }

            public void Clear()
            {
                this.CommandList.Clear();
                this.CommandNodes.Clear();
            }

            public Command GetCommand(string Name)
            {
                Command Cmd = null;

                for (int i = 0; i < CommandList.Count; i++)
                {
                    if (CommandList[i].Name == Name)
                    {
                        Cmd = CommandList[i];
                        break;
                    }
                }
                return Cmd;
            }

            public Command GetCommand(short OpCode)
            {
                Command Cmd = null;

                for (int i = 0; i < CommandList.Count; i++)
                {
                    if (CommandList[i].OpCode == OpCode)
                    {
                        Cmd = CommandList[i];
                        break;
                    }
                }
                return Cmd;
            }

        }

        public class CommandGroup
        {
            private CommandCollection CmdCollection = null;
            private string CommandGroupName;
            private int OpGroupField = 0;
            private TreeNode CommandGroupNode = null;

            public CommandGroup(string Name, int OGF)
            {
                CommandGroupNode = new TreeNode(Name);
                CommandGroupName = Name;
                CmdCollection = new CommandCollection(OGF, CommandGroupNode.Nodes);
            }

            ~CommandGroup()
            {
                CmdCollection.Clear();
                CmdCollection = null;
                CommandGroupNode = null;
            }

            public string Name
            {
                get
                {
                    return CommandGroupName;
                }
                set
                {
                    CommandGroupName = value;
                    CommandGroupNode.Text = value;
                }
            }

            public int OGF
            {
                get
                {
                    return OpGroupField;
                }
            }

            public CommandCollection Commands
            {
                get
                {
                    return CmdCollection;
                }
                set
                {
                    CmdCollection = value;
                }
            }

            public TreeNode Node
            {
                get
                {
                    return CommandGroupNode;
                }
                set
                {
                    CommandGroupNode = value;
                }
            }

        }

        public class Event
        {

            private ParameterCollection ParaCollection = null;
            private string EventName = "";
            private byte EventCode = 0;
            private string EventDescription = "";
            private EventCollection SubEventCollection = null;
            private TreeNode EventTreeNode;

            public Event(string Name, byte Code, string Description = "")
            {
                EventTreeNode = new TreeNode(Name);
                EventName = Name;
                EventCode = Code;
                EventDescription = Description;

                ParaCollection = new ParameterCollection(EventTreeNode.Nodes);
            }

            public string Name
            {
                get
                {
                    return EventName;
                }
            }

            public byte Code
            {
                get
                {
                    return EventCode;
                }
            }

            public string Description
            {
                get
                {
                    return EventDescription;
                }
            }

            public ParameterCollection Parameters
            {
                get
                {
                    return ParaCollection;
                }
                set
                {
                    ParaCollection = value;
                }
            }

            public EventCollection SubEvents
            {
                get
                {
                    return SubEventCollection;
                }
                set
                {
                    SubEventCollection = value;
                }
            }

            public TreeNode Node
            {
                get
                {
                    return EventTreeNode;
                }
                set
                {
                    EventTreeNode = value;
                }
            }

            public void SetParameters(byte[] DataBuffer, int StartIndex)
            {
                ParameterCollection.SetParameters(ParaCollection, DataBuffer, StartIndex);
            }

        }

        public class EventCollection : IEnumerator, IEnumerable
        {
            private List<Event> EventList = null;

            public EventCollection()
            {
                EventList = new List<Event>();
            }

            private int Location = -1;

            public Event this[int Index]
            {
                get
                {
                    return EventList[Index];
                }
                set
                {
                    EventList[Index] = value;
                }
            }

            public void Reset()
            {
                Location = -1;
            }

            public object Current
            {
                get
                {
                    return EventList[Location];
                }
            }

            public bool MoveNext()
            {
                if (Location == (EventList.Count - 1))
                {
                    Reset();
                    return false;
                }
                Location++;
                return (Location < EventList.Count);
            }

            public System.Collections.IEnumerator GetEnumerator()
            {
                foreach (var reg in EventList)
                {
                    yield return reg;
                }
            }

            public int Count
            {
                get
                {
                    return this.EventList.Count;
                }
            }

            public Event Add(string Name, byte Code, string Description)
            {
                Event Evt = new Event(Name, Code, Description);
                EventList.Add(Evt);

                return Evt;
            }

            public void Remove(string Name)
            {
                for (int i = 0; i < EventList.Count; i++)
                {
                    if (EventList[i].Name == Name)
                    {
                        EventList.RemoveAt(i);
                        break;
                    }
                }
            }

            public void RemoveAt(int Index)
            {
                if ((Index >= 0) && (Index < EventList.Count))
                    EventList.RemoveAt(Index);
            }

            public void Clear()
            {
                EventList.Clear();
            }

            public Event GetEvent(string Name)
            {
                Event Evt = null;

                for (int i = 0; i < EventList.Count; i++)
                {
                    if (EventList[i].Name == Name)
                    {
                        Evt = EventList[i];
                        break;
                    }
                }
                return Evt;
            }

            public Event GetEvent(byte Code)
            {
                Event Evt = null;

                for (int i = 0; i < EventList.Count; i++)
                {
                    if (EventList[i].Code == Code)
                    {
                        Evt = EventList[i];
                        break;
                    }
                }
                return Evt;
            }

        }

        public class ConnectionInfo
        {
            public string BDAddress
            {
                get; set;
            }
            public string RSSI
            {
                get; set;
            }
            public string EventType
            {
                get; set;
            }
            public string AddrType
            {
                get; set;
            }
            public string ConnHandle
            {
                get; set;
            }
            public string AdvData
            {
                get; set;
            }
        }
    }
}
