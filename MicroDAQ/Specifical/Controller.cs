using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using JonLibrary.Automatic;

namespace MicroDAQ.Specifical
{
    class Controller : JonLibrary.OPC.Machine
    {
       public  List<int> IDList;
       public  string[] remoteCtrl;
        public Controller(string Name, string[] Ctrl, List<int> idList)
        {
            this.Name = Name;
           // ItemCtrl = Ctrl;
            remoteCtrl = Ctrl;
            IDList = idList;
            
        }
        int running;
        public bool SetCommand(DataRow[] remoteControl)
        {
            
            object[] values=new object[remoteControl.Length];
           
            for (int i = 0; i < values.Length; i++)
            {
                if (remoteControl[i]["plc"].ToString() == "1200")
                {
                    ushort[] shortValues = new ushort[5];
                    shortValues[0] = (ushort)(++running % ushort.MaxValue);
                    shortValues[1] = Convert.ToUInt16(remoteControl[i]["id"]);
                    shortValues[2] = Convert.ToUInt16(remoteControl[i]["command"]);
                    shortValues[3] = Convert.ToUInt16(remoteControl[i]["cycle"]);
                    shortValues[4] = (ushort)10;
                    values[i] = shortValues;
                }
                else
                {
                    ushort shortValue = Convert.ToUInt16(remoteControl[i]["cycle"]);
                    values[i] = shortValue;
                }
               
            }

                return PLC.Write(GROUP_NAME_CTRL, values);
               
        }
        public bool SetCommand(DataRow remoteControl)
        {
            object[] values = new object[1];

            for (int i = 0; i < values.Length; i++)
            {
                ushort[] shorts = new ushort[5];
                shorts[0] = (ushort)(++running % ushort.MaxValue);
                shorts[1] = Convert.ToUInt16(remoteControl["id"]);
                shorts[2] = Convert.ToUInt16(remoteControl["command"]);
                shorts[3] = Convert.ToUInt16(remoteControl["cycle"]);
                shorts[4] = (ushort)10;
                values[i] = shorts;
            }

            return PLC.Write(GROUP_NAME_CTRL, values);
        }

        protected override void PLC_DataChange(string groupName, int[] item, object[] value, short[] Qualities)
        {

            switch (groupName)
            {
                case GROUP_NAME_CTRL:
                    break;
                case GROUP_NAME_STATE:
                    if (value[0] != null)
                    {
                        ushort[] val = (ushort[])value[0];

                        RunningNumber = (ushort)val[0];
                        MeterID = (ushort)val[1];

                        TaskState = (TaskState)(ushort)val[3];
                        DataTime = DateTime.Now;

                        bool r = true;
                        foreach (short q in Qualities)
                        {
                            r &= (q >= 192) ? (true) : (false);
                        }
                        ConnectionState = (r) ? (ConnectionState.Open) : (ConnectionState.Closed);

                    }
                    break;
            }
            DataTime = DateTime.Now;
            OnStatusChannge();
        }
        public int RunningNumber { get; private set; }
        public int MeterID { get; private set; }
        public TaskState TaskState { get; private set; }
        public int DataTick { get; private set; }
        public int SyncTick { get; set; }
        public DateTime DataTime { get; private set; }
        public DateTime SyncTime { get; private set; }
    }
}
