// File:    IPSetting.cs
// Author:  John
// Created: 2013��9��23�� 16:51:16
// Purpose: Definition of Class IPSetting

using System;
using System.Data;
namespace MicroDAQ.Configuration
{
    public class IPSettingInfo
    {
        public long serialID;
        public string iP;
        public int port;
        public string enable;

        public IPSettingInfo(DataSet config)
        {
            throw new System.NotImplementedException();
        }

        public IPSettingInfo()
        {
            throw new System.NotImplementedException();
        }

    }
}