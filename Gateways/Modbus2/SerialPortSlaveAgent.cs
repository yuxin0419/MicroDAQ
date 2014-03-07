﻿using System;
using System.Collections.Generic;
using System.Text;
using Modbus.Device;
using Modbus.Utility;
using System.Data;
using System.Data.SqlClient;
using MicroDAQ.Common;
using MicroDAQ.Configuration;
using System.Data;
using JonLibrary.Common;
using log4net;
namespace MicroDAQ.Gateways.Modbus2
{
    public class SerialPortSlaveAgent
    {
        ILog log;

        /// <summary>
        /// 根据Master对象信息和Salve配置信息生成对象
        /// </summary>
        /// <param name="masterAgent">所属Master对象</param>
        /// <param name="slaveInfo">配置信息</param>
        public SerialPortSlaveAgent(ModbusMasterAgent masterAgent, ModbusSlaveInfo slaveInfo)
        {
            log = LogManager.GetLogger(this.GetType());
            ///上属Master相关
            this.ModbusMasterAgent = masterAgent;
            try
            {
                switch (slaveInfo.type.ToUpper())
                {
                    case "MODBUSRTU":
                        if (ModbusMasterAgent.MasterInfo.type.ToLower() == "tcp")
                        {
                            this.ModbusMasterAgent.ModbusMaster = ModbusSerialMaster.CreateRtu(slaveInfo.tcpClient);
                        }
                        else
                        {
                            this.ModbusMasterAgent.ModbusMaster = ModbusSerialMaster.CreateRtu(this.ModbusMasterAgent.SerialPort);
                        }
                        break;
                    case "MODBUSASCII":
                        if (ModbusMasterAgent.MasterInfo.type.ToLower() == "tcp")
                        {
                            this.ModbusMasterAgent.ModbusMaster = ModbusSerialMaster.CreateAscii(slaveInfo.tcpClient);
                        }
                        else
                        {
                            this.ModbusMasterAgent.ModbusMaster = ModbusSerialMaster.CreateAscii(this.ModbusMasterAgent.SerialPort);
                        }
                        break;
                    case "MODBUSTCP":
                        //this.ModbusMasterAgent.ModbusMaster
                        //   = ModbusIpMaster.CreateIp(slaveInfo.tcpClient);
                        this.TcpModbusMaster = ModbusIpMaster.CreateIp(slaveInfo.tcpClient);
                        break;

                    default:
                        throw new InvalidOperationException(string.Format("无法识别的Modbus从机类型-{0}", slaveInfo.type));

                }
            }
            catch (Exception ex)
            {
                log.Error(new Exception("运行期间出现一个连接错误！", ex));
            }

            ///自身Slave相关
            this.ModbusSlaveInfo = slaveInfo;

            ///下属Variable相关
            this.Variables = new List<ModbusVariable>();
            foreach (var var in this.ModbusSlaveInfo.modbusVariables)
            {
                this.Variables.Add(new ModbusVariable(var));
            }
            for (int i = 0; i < this.Variables.Count; i++)
            {
                Variables[i].State=(ItemState)2;
            }

        }

        /// <summary>
        /// 读数据
        /// </summary>
        public void Read()
        {
            try
            {
                for (int i = 0; i < this.Variables.Count; i++)
                {
                    ModbusVariable variable = Variables[i];

                    ushort[] tmpVal = new ushort[variable.VariableInfo.length];
                    if (variable.VariableInfo.accessibility != "WriteOnly")
                    {
                        if (ModbusSlaveInfo.type.ToUpper() != "MODBUSTCP")
                        {
                            ///取出数据
                            switch (variable.VariableInfo.regesiterType)
                            {
                                //TODO:需要修改以下判断值
                                case 3:
                                    tmpVal = this.ModbusMasterAgent.ModbusMaster.
                                                                    ReadHoldingRegisters(
                                                                             this.ModbusSlaveInfo.slave,
                                                                             variable.VariableInfo.regesiterAddress,
                                                                             variable.VariableInfo.length);
                                    break;
                                case 2:
                                    tmpVal = this.ModbusMasterAgent.ModbusMaster.
                                                                    ReadInputRegisters(
                                                                             this.ModbusSlaveInfo.slave,
                                                                             variable.VariableInfo.regesiterAddress,
                                                                             variable.VariableInfo.length);
                                    break;
                                case 1:


                                    break;
                                case 0:
                                    bool[] boolValue = new bool[variable.VariableInfo.length];
                                    boolValue = this.ModbusMasterAgent.ModbusMaster.ReadCoils(
                                                                            this.ModbusSlaveInfo.slave,
                                                                            variable.VariableInfo.regesiterAddress,
                                                                            variable.VariableInfo.length);
                                    for (int j = 0; j < boolValue.Length; j++)
                                    {
                                        if (boolValue[i])
                                        {
                                            tmpVal[i] = 1;
                                        }
                                        else
                                        {
                                            tmpVal[i] = 0;
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            switch (variable.VariableInfo.regesiterType)
                            {
                                //TODO:需要修改以下判断值
                                case 3:
                                    tmpVal = this.TcpModbusMaster.ReadHoldingRegisters(
                                                                             this.ModbusSlaveInfo.slave,
                                                                             variable.VariableInfo.regesiterAddress,
                                                                             variable.VariableInfo.length);
                                    break;
                                case 2:
                                    tmpVal = this.TcpModbusMaster.ReadInputRegisters(
                                                                             this.ModbusSlaveInfo.slave,
                                                                             variable.VariableInfo.regesiterAddress,
                                                                             variable.VariableInfo.length);
                                    break;
                                case 1:
                                    break;
                                case 0:
                                    bool[] boolValue = new bool[variable.VariableInfo.length];
                                    boolValue = this.TcpModbusMaster.ReadCoils(
                                                                            this.ModbusSlaveInfo.slave,
                                                                            variable.VariableInfo.regesiterAddress,
                                                                            variable.VariableInfo.length);
                                    for (int j = 0; j < boolValue.Length; j++)
                                    {
                                        if (boolValue[i])
                                        {
                                            tmpVal[i] = 1;
                                        }
                                        else
                                        {
                                            tmpVal[i] = 0;
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }

                        }


                        ///转化数据
                        switch (variable.VariableInfo.dataType.ToLower())
                        {
                            case "integer":
                                switch (variable.VariableInfo.length)
                                {
                                    case 1:
                                        variable.Value = tmpVal[0];
                                        break;
                                    case 2:
                                        variable.Value = ModbusUtility.GetUInt32(tmpVal[0], tmpVal[1]);
                                        break;
                                    case 4:
                                        byte[] byte1 = BitConverter.GetBytes(tmpVal[0]);
                                        byte[] byte2 = BitConverter.GetBytes(tmpVal[1]);
                                        byte[] byte3 = BitConverter.GetBytes(tmpVal[2]);
                                        byte[] byte4 = BitConverter.GetBytes(tmpVal[3]);
                                        byte[] bytes = new byte[8] { byte1[0], byte1[1], byte2[0], byte2[1], byte3[0], byte3[1], byte4[0], byte4[1] };
                                        variable.Value = BitConverter.ToInt64(bytes, 0);
                                        break;
                                    default:
                                        throw new NotImplementedException(string.Format("无法识别的数据类型-{0}", variable.VariableInfo.dataType));
                                }
                                break;
                            case "real":
                               
                                    variable.Value = ModbusUtility.GetSingle(tmpVal[0], tmpVal[1]);
                                
                                break;
                            case "Discrete":
                                variable.Value = tmpVal[0];
                                break;
                            default:
                                throw new NotImplementedException(string.Format("无法识别的数据类型-{0}", variable.VariableInfo.dataType));
                        }
                        variable.State = (ItemState)1;
                    }


                }
            }
            catch (Exception ex)
            {
                log.Error(new Exception("运行期间出现一个连接错误！", ex));
            }
        }
        /// <summary>
        /// 写数据
        /// </summary>
        //public void Write()
        //{
        //    foreach (ModbusVariable variable in this.Variables)
        //    {
        //        this.ModbusMasterAgent.ModbusMaster.
        //                         WriteMultipleRegisters(
        //                             this.ModbusSlaveInfo.slave,
        //                             variable.VariableInfo.regesiterAddress,
        //                             (ushort[])variable.OriginalValue);
        //    }
        //}
        public void Write()
        {
            try
            {
                foreach (ModbusVariable variable in this.Variables)
                {
                    if (variable.VariableInfo.accessibility != "ReadOnly")
                    {
                        DataRow dr = SelectControl(variable.VariableInfo.code);
                        ushort[] shortValues;
                        if (dr["type"].ToString().ToUpper() == "WLT")
                        {
                            switch (Convert.ToInt32(dr["command"]))
                            {
                                case 1:
                                    shortValues = new ushort[4] { 0, 0, 0, 1 };
                                    break;
                                case 2:
                                    shortValues = new ushort[4] { 0, 0, 1, 0 };
                                    break;
                                case 4:
                                    shortValues = new ushort[4] { 0, 1, 0, 0 };
                                    break;
                                case 8:
                                    shortValues = new ushort[4] { 1, 0, 0, 0 };
                                    break;
                                case 12:
                                    shortValues = new ushort[4] { 1, 1, 0, 0 };
                                    break;
                                default:
                                    shortValues = new ushort[4] { 0, 0, 0, 0 };
                                    throw new NotImplementedException(string.Format("无法识别的指令-{0}", Convert.ToInt32(dr["command"])));
                            }
                            variable.originalValue = shortValues;
                        }
                        else
                        {
                            variable.originalValue[0] = Convert.ToUInt16(dr["command"]);
                        }
                        this.ModbusMasterAgent.ModbusMaster.
                                   WriteMultipleRegisters(
                                       this.ModbusSlaveInfo.slave,
                                       variable.VariableInfo.regesiterAddress,
                                        (ushort[])variable.OriginalValue);

                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(new Exception("运行期间出现一个连接错误！", ex));
            }
        }
        public void ReadWrite()
        {     
            Write();
            Read();
        }
           IniFile ini = null;
        
        public DataRow SelectControl(int code)
        {
            ini = new IniFile(AppDomain.CurrentDomain.BaseDirectory + "MicroDAQ.ini");
            string[] dbs = ini.GetValue("Database", "Members").Trim().Split(',');
            string address = ini.GetValue(dbs[0], "Address");
            string database = ini.GetValue(dbs[0],"Database");
            string username = ini.GetValue(dbs[0], "Username");
            string password = ini.GetValue(dbs[0], "Password");
            string ConnectionString = string.Format("server={0};database={1};uid={2};pwd={3};", address, database, username, password);
           // string ConnectionString = "server=192.168.1.179;database=opcmes3;uid=microdaq;pwd=microdaq";
            SqlConnection con = new SqlConnection(ConnectionString);
            con.Open();
            string sqlStr = "select * from v_remoteControl a where a.id= " + code;
            SqlDataAdapter da = new SqlDataAdapter(sqlStr, con);
            DataSet ds = new System.Data.DataSet();
            da.Fill(ds);
            return ds.Tables[0].Rows[0];
 
        }

        public MicroDAQ.Configuration.ModbusSlaveInfo ModbusSlaveInfo { get; set; }

        public IList<MicroDAQ.Gateways.Modbus2.ModbusVariable> Variables { get; set; }

        public MicroDAQ.Gateways.Modbus2.ModbusMasterAgent ModbusMasterAgent { get; set; }

        public IModbusMaster TcpModbusMaster { get; set; }
    }
}
