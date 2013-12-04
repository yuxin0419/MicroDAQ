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
        public bool SetCommand(DataRow[] remoteControl)
        {
            object[] values=new object[IDList.Count];
           
            for (int i = 0; i < IDList.Count; i++)
            {
                foreach(DataRow row in remoteControl)
                {
                    ushort[] value = new ushort[1];
                    if (Convert.ToInt32(row["id"]) == IDList[i])
                    {
                        value[0] =Convert.ToUInt16(row["cycle"]);
                        values[i] = value[0];
                    }
                   
                }
            }
            return PLC.Write(GROUP_NAME_CTRL,values);






               
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
