using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using MicroDAQ.DataItem;
using JonLibrary.OPC;
using MicroDAQ.Specifical;


namespace MicroDAQ.DataItem
{
    /// <summary>
    /// 数量项管理器
    /// </summary>
    public class DataItemManager : IDataItemManage
    {   
        public IList<Item> Items { get; set; }
        public ConnectionState ConnectionState { get; set; }
        public Dictionary<int, Item> ItemPair = null;
        public IList<int> idlist;
        /// <summary>
        /// 使用由指定的xx建立管理器
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dataHead"></param>
        /// <param name="data"></param>
        public DataItemManager(string name, string[] data ,IList<int> IDlist)
            : base()
        {
            idlist = IDlist;
            machine = new DataItem(this, name, data,IDlist);
           // int count = (dataHead.Length < data.Length) ? (dataHead.Length) : (data.Length);
            int count = data.Length;
            Items = new List<Item>();
            ItemPair = new Dictionary<int, Item>();
            for (int i = 0; i < count; i++)
                Items.Add(new Item());
        }
        Machine machine;
        public bool Connect(string OpcServerProgramID, string OPCServerAddress)
        {
            return machine.Connect(OpcServerProgramID, OPCServerAddress);
        }

        class DataItem : Machine
        {
            DataItemManager Manager;
            public IList<int> idlist;
            public DataItem(DataItemManager manager, string name, string[] data,IList<int> IDlist)
                : base()
            {
                idlist = IDlist;
                this.Manager = manager;
                this.Name = Name;
               // ItemCtrl = dataHead;
                ItemStatus = data;

            }

            internal protected override bool Connect(string OpcServerProgramID, string OPCServerIP)
            {
                bool success = true;
                success &= PLC.Connect(OpcServerProgramID, OPCServerIP);
                //success &= PLC.AddGroup(GROUP_NAME_CTRL, 1, 0);
               // success &= PLC.AddItems(GROUP_NAME_CTRL, ItemCtrl);
                success &= PLC.AddGroup(GROUP_NAME_STATE, 1, 0);
                success &= PLC.AddItems(GROUP_NAME_STATE, ItemStatus);
                //PLC.SetState(GROUP_NAME_CTRL, true);
                PLC.SetState(GROUP_NAME_STATE, true);
                ConnectionState = (success) ? (ConnectionState.Open) : (ConnectionState.Closed);
                return success;
            }


            protected override void PLC_DataChange(string groupName, int[] item, object[] value, short[] Qualities)
            {
                base.PLC_DataChange(groupName, item, value, Qualities);
                for (int i = 0; i < item.Length; i++)
                {
                    ushort val;
                    if (value[i] != null)
                    {
                        val = (ushort)value[i];
                        Manager.Items[item[i]].Value = val;
                        Manager.Items[item[i]].ID = idlist[i];

                    }
                }
                    //switch (groupName)
                    //{
                    //    case GROUP_NAME_CTRL:
                    //        for (int i = 0; i < item.Length; i++)
                    //        {
                    //            ushort[] val = null;
                    //            if (value[i] != null)
                    //            {
                    //                val = (ushort[])value[i];
                    //                Manager.Items[item[i]].ID = val[0];
                    //                Manager.Items[item[i]].Type = (DataType)val[1];
                    //                Manager.Items[item[i]].State = (DataState)val[2];
                    //                Manager.Items[item[i]].Quality = Qualities[i];
                    //                Manager.UpdateItemPair(Manager.Items[item[i]].ID, Manager.Items[item[i]]);
                    //            }
                    //        }
                    //        break;
                    //    case GROUP_NAME_STATE:
                    //        for (int i = 0; i < item.Length; i++)
                    //        {
                    //            if (value[i] != null)
                    //            {
                    //                Manager.Items[item[i]].Value = (float)value[i];
                    //                Manager.Items[item[i]].Quality = Qualities[i];
                    //            }
                    //        }
                    //        break;
                    //}
                    OnStatusChannge();
            }


        }
        protected void UpdateItemPair(int key, Item item)
        {
            if (!ItemPair.ContainsKey(key))
            { ItemPair.Add(key, item); }
            ItemPair[key] = item;
        }
    }
}
