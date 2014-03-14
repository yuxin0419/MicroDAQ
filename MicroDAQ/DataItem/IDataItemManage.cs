using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using MicroDAQ.Common;
namespace MicroDAQ.DataItem
{
    public interface IDataItemManage 
    {
        IList<Item> Items { get; set; }
        ConnectionState ConnectionState { get;set;}
        bool Connect(string OpcServerProgramID, string OPCServerIP);
    }

}
