﻿using System;
using System.Collections.Generic;
using System.Text;
using JonLibrary.OPC;
using JonLibrary.Automatic;
using System.Threading;
using System.Data;
using MicroDAQ.DataItem;
using log4net;
using MicroDAQ.Specifical;
using BeIT.MemCached;
using MicroDAQ.Common;

namespace MicroDAQ.Gateway
{
    public class OpcGateway : GatewayBase
    {
        ILog log;
        MemcachedClient client;
        public override void Dispose()
        {
            UpdateCycle.Quit();
            RemoteCtrlCycle.Quit();
        }

        /// <summary>
        /// 使用多个ItemManage创建OpcGateway实例
        /// </summary>
        /// <param name="itemManagers"></param>
        public OpcGateway(IList<MicroDAQ.DataItem.IDataItemManage> itemManagers, IList<IDatabase>  databaseManagers)
        {
            log = LogManager.GetLogger(this.GetType());
            this.ItemManagers = itemManagers;
            this.DatabaseManagers = databaseManagers;

            UpdateCycle = new CycleTask();
            RemoteCtrlCycle = new CycleTask();
            MemcachedCycle = new CycleTask();

            UpdateCycle.WorkStateChanged += new CycleTask.WorkStateChangeEventHandle(UpdateCycle_WorkStateChanged);
            RemoteCtrlCycle.WorkStateChanged += new CycleTask.WorkStateChangeEventHandle(RemoteCtrlCycle_WorkStateChanged);
           // client = MemcachedClient.GetInstance("MyConfigFileCache");
        }


        void UpdateCycle_WorkStateChanged(JonLibrary.Automatic.RunningState state)
        {
            if ((UpdateCycle.State == JonLibrary.Automatic.RunningState.Running) || (RemoteCtrlCycle.State == JonLibrary.Automatic.RunningState.Running))
            {
                this.RunningState = Gateway.RunningState.Running;
            }
            else
            {
                this.RunningState = Gateway.RunningState.Stopped;
            }
        }
        void RemoteCtrlCycle_WorkStateChanged(JonLibrary.Automatic.RunningState state)
        {
            if ((UpdateCycle.State == JonLibrary.Automatic.RunningState.Running) || (RemoteCtrlCycle.State == JonLibrary.Automatic.RunningState.Running))
            {
                this.RunningState = Gateway.RunningState.Running;
            }
            else
            {
                this.RunningState = Gateway.RunningState.Stopped;
            }
        }
        /// <summary>
        /// 数据项管理器
        /// </summary>
        public IList<MicroDAQ.DataItem.IDataItemManage> ItemManagers { get; private set; }
        /// <summary>
        /// 数据库管理器
        /// </summary>
        public IList<IDatabase> DatabaseManagers { get; set; }
        public CycleTask UpdateCycle { get; private set; }
        public CycleTask RemoteCtrlCycle { get; private set; }
        public CycleTask MemcachedCycle{ get; private set; }


      


       
        int running;
        public void remoteCtrl(object pid)
        {
            try
            {
                DataRow[] Rows = this.DatabaseManagers[0].GetRemoteControl();
                if (Rows != null && Rows.Length != 0)
                {
                    //添加有控制指令的DB块（筛选,排序）
                    foreach (Controller mt in Program.MeterManager.CTMeters.Values)
                    {
                        string[] itemCtrl = new string[Rows.Length];

                        for (int i = 0; i < Rows.Length; i++)
                        {

                            for (int j = 0; j < mt.IDList.Count; j++)
                            {
                                if (Convert.ToInt32(Rows[i]["id"]) == mt.IDList[j])
                                {
                                    itemCtrl[i] = mt.ItemCtrl[j];
                                    break;

                                }
                            }
                        }

                        mt.SetCommand(Rows, itemCtrl);
                        //控制指令用相同的DB块
                        //if (itemCtrl.Length >= 2 && itemCtrl[0] == itemCtrl[1])
                        //{
                        //    string[] sameItem = new string[] { itemCtrl[0] };
                        //    mt.ItemCtrl = sameItem;
                        //    mt.Connect(pid.ToString(), "127.0.0.1");
                        //    foreach (var row in Rows)
                        //    {
                        //        mt.SetCommand(row);
                        //    }
                        //}
                        //控制指令用不同的DB块
                        //else 
                        //{
                        //    mt.ItemCtrl = itemCtrl;
                        //    mt.Connect(pid.ToString(), "127.0.0.1");
                        //    mt.SetCommand(Rows);
                        //}
                    }


                }

                System.Threading.Thread.Sleep(500);
            }

            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
                System.Threading.Thread.Sleep(3000);
            }
        }

        #region Start()

        public override void Start(object pid)
        {
           
           // UpdateCycle.Run(this.Update, System.Threading.ThreadPriority.BelowNormal);
           // RemoteCtrlCycle.Run(this.remoteCtrl,pid,System.Threading.ThreadPriority.BelowNormal);
           // MemcachedCycle.Run(this.Memcached, System.Threading.ThreadPriority.BelowNormal);
            
        }

        /// <summary>
        /// 启动
        /// </summary>
        public void StartButton(string pid)
        {
            foreach (var manager in this.ItemManagers)
                manager.Connect(pid, "127.0.0.1");
            Start(pid);
        }
        #endregion

        #region Pasue()
        /// <summary>
        /// 暂停更新和控制
        /// </summary>
        public override void Pause()
        {
            this.Pause(this.UpdateCycle);
            this.Pause(this.RemoteCtrlCycle);
        }

        /// <summary>
        /// 暂停参数指定的任务对象
        /// </summary>
        /// <param name="task">要暂停的任务对象</param>
        public void Pause(CycleTask task)
        {
            if (task != null)
                task.Pause();
        }
        #endregion

        #region Continue()
        /// <summary>
        /// 暂停更新和控制
        /// </summary>
        public override void Continue()
        {
            this.Continue(this.UpdateCycle);
            this.Continue(this.RemoteCtrlCycle);
        }

        /// <summary>
        /// 继续参数指定的任务对象
        /// </summary>
        /// <param name="task">要继续的任务对象</param>
        public void Continue(CycleTask task)
        {
            if (task != null)
                task.Continue();
        }

        #endregion

        #region Stop()
        /// <summary>
        /// 暂停更新和控制
        /// </summary>
        public override void Stop()
        {
            this.Stop(this.UpdateCycle);
            this.Stop(this.RemoteCtrlCycle);
        }

        /// <summary>
        /// 暂停参数指定的任务对象
        /// </summary>
        /// <param name="task">要暂停的任务对象</param>
        private void Stop(CycleTask task)
        {
            if (task != null)
                task.Quit();
        }
        #endregion

        #region  Memcached()
        public void Memcached()
        {
                foreach (MicroDAQ.Common.IDataItemManage mgr in this.ItemManagers)
                {
                    foreach (Item item in mgr.Items)
                    {
                        client.Set(item.ID.ToString(), item);
                    }
                }


        }
        #endregion




    }
}
