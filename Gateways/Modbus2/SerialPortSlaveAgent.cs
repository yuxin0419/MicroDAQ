﻿using System;
using System.Collections.Generic;
using System.Text;
using Modbus.Device;
using Modbus.Utility;
using System.Data;
using System.Data.SqlClient;
using MicroDAQ.Common;
using MicroDAQ.Configuration;
namespace MicroDAQ.Gateways.Modbus2
{
    public class SerialPortSlaveAgent
    {

        /// <summary>
        /// 根据Master对象信息和Salve配置信息生成对象
        /// </summary>
        /// <param name="masterAgent">所属Master对象</param>
        /// <param name="slaveInfo">配置信息</param>
        public SerialPortSlaveAgent(ModbusMasterAgent masterAgent, ModbusSlaveInfo slaveInfo)
        {
            ///上属Master相关
            this.ModbusMasterAgent = masterAgent;
            switch (slaveInfo.type.ToUpper())
            {
                case "RTU":
                    this.ModbusMasterAgent.ModbusMaster
                        = ModbusSerialMaster.CreateRtu(this.ModbusMasterAgent.SerialPort);
                    break;
                case "ASCII":
                    this.ModbusMasterAgent.ModbusMaster
                       = ModbusSerialMaster.CreateAscii(this.ModbusMasterAgent.SerialPort);
                    break;
                default:
                    throw new InvalidOperationException(string.Format("无法识别的Modbus从机类型-{0}", slaveInfo.type));

            }

            ///自身Slave相关
            this.ModbusSlaveInfo = slaveInfo;

            ///下属Variable相关
            this.Variables = new List<ModbusVariable>();
            foreach (var var in this.ModbusSlaveInfo.modbusVariables)
            {
                this.Variables.Add(new ModbusVariable(var));
            }
        }

        /// <summary>
        /// 读数据
        /// </summary>
        public void Read()
        {
            for (int i = 0; i < this.Variables.Count; i++)
            {
                ModbusVariable variable = Variables[i];

                ushort[] tmpVal = new ushort[variable.VariableInfo.length];

                ///取出数据
                switch (variable.VariableInfo.regesiterType)
                {
                    //TODO:需要修改以下判断值
                    case 1:
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
                    case 3:
                        break;
                    case 4:
                        break;
                    default:
                        break;
                }

                ///转化数据
                switch (variable.VariableInfo.dataType.ToLower())
                {
                    case "int":
                    case "short":
                        variable.Value = tmpVal[0];
                        break;
                    case "float":
                        variable.Value = ModbusUtility.GetSingle(tmpVal[0], tmpVal[1]);
                        break;
                    case "long":
                        variable.Value = ModbusUtility.GetUInt32(tmpVal[0], tmpVal[1]);
                        break;
                    default:
                        throw new NotImplementedException(string.Format("无法识别的数据类型-{0}", variable.VariableInfo.dataType));
                }
            }
        }
        /// <summary>
        /// 写数据
        /// </summary>
        public void Write()
        {
            foreach (ModbusVariable variable in this.Variables)
            {
                this.ModbusMasterAgent.ModbusMaster.
                                 WriteMultipleRegisters(
                                     this.ModbusSlaveInfo.slave,
                                     variable.VariableInfo.regesiterAddress,
                                     (ushort[])variable.OriginalValue);
            }
        }
        public void ReadWrite()
        {
            Read();
            Write();
        }



        public MicroDAQ.Configuration.ModbusSlaveInfo ModbusSlaveInfo { get; set; }

        public IList<MicroDAQ.Gateways.Modbus2.ModbusVariable> Variables { get; set; }

        public MicroDAQ.Gateways.Modbus2.ModbusMasterAgent ModbusMasterAgent { get; set; }
    }
}