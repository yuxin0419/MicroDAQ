using System;
using System.Collections.Generic;
using System.Text;
using Modbus.Device;
using Modbus.Utility;
using System.Data;
using System.Data.SqlClient;
using MicroDAQ.Common;
using JonLibrary.Common;
namespace MicroDAQ.Gateways.Modbus
{
    public class SerialPortMasterManager
    {
        public IList<Item> Items { get; set; }
        IModbusMaster SerialMaster;
        DataTable dtMeta;
        DataTable dtCommands;
        string  device_ID;
        byte slaveAddress;
        IniFile ini;
        public SqlConnection Connection { get; set; }
        /// <summary>
        /// 构造函数 初始化变量
        /// </summary>
        /// <param name="master"></param>
        /// <param name="slave"></param>
        /// <param name="commandsData"></param>
        /// <param name="metaData"></param>
        public SerialPortMasterManager(IModbusMaster master, int slave, DataTable commandsData, DataTable metaData, string deviceID)
        {
           // string ConnectionString = "server=VWINTECH-201\\SQL2000;database=opcmes3;uid=sa;pwd= ";
            string ConnectionString = GetConnectiongStr();
            Connection = new SqlConnection(ConnectionString);
            SerialMaster = master;
            master.Transport.ReadTimeout = 300;
            slaveAddress = Convert.ToByte(slave);
            dtCommands = commandsData;
            dtMeta = metaData;
            device_ID = deviceID;
            Items = new List<Item>();
            for (int i = 0; i < metaData.Rows.Count; i++)
            {
                Items.Add(new Item());
            }
        }
        /// <summary>
        /// 读数据
        /// </summary>
        public void Read()
        {
            int flag = 0;//items索引

            for (int i = 0; i < dtCommands.Rows.Count; i++)
            {

                string regesiter = dtCommands.Rows[i]["RegisterName"].ToString();
                ushort adress = Convert.ToUInt16(dtCommands.Rows[i]["RegesiterAddress"]);
                ushort length = Convert.ToUInt16(dtCommands.Rows[i]["Length"]);
                string serialID = dtCommands.Rows[i]["SerialID"].ToString();
                DataRow[] rows = dtMeta.Select("ModbusCommands_SerialID=" + "'" + serialID + "'", "Address ASC");//一条命令所对应的原始数据表数据
                ushort[] values = new ushort[length];
                int index = 0;//values 索引
                try
                {
                    if (regesiter.ToLower() == "holdingregister")
                    { values = SerialMaster.ReadHoldingRegisters(slaveAddress, adress, length); }
                    else
                    { values = SerialMaster.ReadInputRegisters(slaveAddress, adress, length); }
                    //存储过程
                    // ProCommandState(serialID, "true");
                }
                catch
                {
                    //存储过程
                    // ProCommandState(serialID, "false");
                    for (int j = 0; j < rows.Length; j++)
                    {
                        Items[flag].ID = Convert.ToInt32(rows[j]["Code"]);
                        Items[flag].DataTime = DateTime.Now;
                        Items[flag].State = ItemState.仪表掉线;
                        flag = flag + 1;
                        Items[flag].Quality = 0;
                    }
                        continue;
                }
                for (int j = 0; j < rows.Length; j++)
                {
                    if (rows[j]["Final"].ToString() == "1")
                    {
                        Items[flag].Value = Convert.ToSingle(values[index]);
                        Items[flag].ID = Convert.ToInt32(rows[j]["Code"]);
                        Items[flag].DataTime = DateTime.Now;
                        Items[flag].State = ItemState.正常;
                        Items[flag].Quality = 192;
                        index += 1;
                    }
                    else
                    {
                        if (rows[j]["Arithmetic"].ToString().ToLower() == "getquantities")//换算成工程量-风速
                        {
                            double y;
                            float fs;
                            if (values[index] < 32768)
                            {
                                y = ((values[index] * 5) * 1000.0 / 4080000.0) / 240.0;
                            }
                            else
                            {
                                y=((65535-values[index] + 1) * 5) * 1000.0 / 4080000.0 / 240.0;
                            }
                            fs =Convert.ToSingle((y - 4) * (2 / 16));
                            Items[flag].Value = fs;
                            Items[flag].ID = Convert.ToInt32(rows[j]["Code"]);
                            Items[flag].DataTime = DateTime.Now;
                            Items[flag].State = ItemState.正常;
                            Items[flag].Quality = 192;
                            index += 1;

                        }
                        else
                        {
                            ushort high;
                            ushort low;
                            float value;
                            string type = rows[j]["Arithmetic"].ToString().ToLower();
                            switch (type)
                            {
                                case "getfloatmsb":
                                    high = values[index];
                                    low = values[index + 1];
                                    value = ModbusUtility.GetSingle(high, low);
                                    break;

                                case "getfloatlsb":
                                    low = values[index];
                                    high = values[index + 1];
                                    value = ModbusUtility.GetSingle(high, low);
                                    break;

                                case "getuintmsb":
                                    high = values[index];
                                    low = values[index + 1];
                                    value = ModbusUtility.GetUInt32(high, low);
                                    break;

                                case "getuintlsb":
                                    low = values[index];
                                    high = values[index + 1];
                                    value = ModbusUtility.GetUInt32(high, low);
                                    break;
                                default:
                                    value = 0;
                                    break;

                            }

                            Items[flag].Value = value;
                            Items[flag].ID = Convert.ToInt32(rows[j]["Code"]);
                            Items[flag].DataTime = DateTime.Now;
                            Items[flag].State = ItemState.正常;
                            Items[flag].Quality = 192;
                            index += 2;
                        }
                    }
                    flag += 1;
                }
            }
        }
        public void Write()
        {
            DataTable dtWriteData= GetWriteCommandsByID(device_ID);
            for (int i = 0; i < dtWriteData.Rows.Count; i++)
            {
                string type = dtWriteData.Rows[i]["type"].ToString();
                string regesiter = dtWriteData.Rows[i]["RegisterName"].ToString();
                ushort adress = Convert.ToUInt16(dtWriteData.Rows[i]["RegesiterAddress"]);
                ushort value = Convert.ToUInt16(dtWriteData.Rows[i]["cycle"]);
                try
                {
                    ushort[] shorts;
                    if (type == "WLT")
                    {
                        switch (value)
                        {
                            case 1:
                                shorts = new ushort[4] { 0, 0, 0, 1 };
                                break;
                            case 2:
                                shorts = new ushort[4] { 0, 0, 1, 0 };
                                break;
                            case 4:
                                shorts = new ushort[4] { 0, 1, 0, 0 };
                                break;
                            case 8:
                                shorts = new ushort[4] { 1, 0, 0, 0 };
                                break;
                            case 12:
                                shorts = new ushort[4] { 1, 1, 0, 0 };
                                break;
                            default:
                                shorts = new ushort[4] { 0, 0, 0, 0 };
                                break;

                        }
                       
                        SerialMaster.WriteMultipleRegisters(slaveAddress, adress, shorts);
                    }
                    else
                    {
                        shorts = new ushort[1] { value };
                        SerialMaster.WriteMultipleRegisters(slaveAddress, adress, shorts);
                    }
                }
                catch
                { }
            }
        }
        public void ReadWriteData()
        {
            Read();
            Write();
        }
       
        /// <summary>
        /// 查询写命令
        /// </summary>
        /// <param name="deviceID"></param>
        /// <returns></returns>

        private DataTable GetWriteCommandsByID(string deviceID)
        {
            string sqlStr = "select * from modubs_control a where a.SerialID=" + "'" + deviceID + "'";
            Connection.Open();
            SqlDataAdapter da = new SqlDataAdapter(sqlStr, Connection);
            DataSet ds = new DataSet();
            da.Fill(ds);
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                if (row["type"].ToString() == "CTR")
                {
                    string sql = string.Format("Update remotecontrol SET cmdstate= {0} WHERE slave= {1}", 2, row["code"].ToString());//, Connection);
                    SqlCommand Command = new SqlCommand(sql, Connection);
                    Command.ExecuteNonQuery();
                }
            }

            Connection.Close();
            return ds.Tables[0];
        }
        /// <summary>
        /// 日志存储过程
        /// </summary>
        /// <param name="serialID"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private int ProCommandState(int serialID, string state)
        {
            SqlCommand command = new SqlCommand("proc_RecordCommandLog", Connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@serialID ", SqlDbType.Int));
            command.Parameters.Add(new SqlParameter("@State ", SqlDbType.VarChar, 10));
            command.UpdatedRowSource = UpdateRowSource.None;
            command.Parameters["@serialID "].Value = serialID;
            command.Parameters["@State "].Value = state;
            Connection.Open();
            int i = command.ExecuteNonQuery();
            Connection.Close();
            return i;
        }
        private string GetConnectiongStr()
        {
            ini = new IniFile(AppDomain.CurrentDomain.BaseDirectory + "MicroDAQ.ini");
            string[] dbName = ini.GetValue("Database", "Members").Trim().Split(',');
            string address = ini.GetValue(dbName[0], "Address");
            string database = ini.GetValue(dbName[0], "Database");
            string username = ini.GetValue(dbName[0], "Username");
            string password = ini.GetValue(dbName[0], "Password");
            string con = string.Format("server={0};database={1};uid={2};pwd={3};", address, database, username, password);
            return con;

        }
    }
}
