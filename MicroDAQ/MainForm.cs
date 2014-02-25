using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using JonLibrary.OPC;
using JonLibrary.Automatic;
using JonLibrary.Common;
using System.Threading;
using MicroDAQ.UI;
using OpcOperate.Sync;
using MicroDAQ.DataItem;
using MicroDAQ.Database;
using MicroDAQ.Gateway;
using log4net;
using System.Data.SqlClient;
using MicroDAQ.Specifical;
using MicroDAQ.DBUtility;

namespace MicroDAQ
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// 使用哪个OPCServer
        /// </summary>
        string opcServerType = "SimaticNet";


       // private List<PLCStationInformation> Plcs;
        PLCStationInformation plc;
        private OpcOperate.Sync.OPCServer SyncOpc;
        IniFile ini = null;

        ILog log;
        public MainForm()
        {
            log = LogManager.GetLogger(this.GetType());
            InitializeComponent();
           // Plcs = new List<PLCStationInformation>();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            Console.WriteLine(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            ni.Icon = this.Icon;
            ni.Text = this.Text;

            bool autoStart = false;
            try
            {
                ini = new IniFile(AppDomain.CurrentDomain.BaseDirectory + "MicroDAQ.ini");
                this.Text = ini.GetValue("General", "Title");
                this.tsslProject.Text = "项目代码：" + ini.GetValue("General", "ProjetCode");
                this.tsslVersion.Text = "程序版本：" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                autoStart = bool.Parse(ini.GetValue("AutoRun", "AutoStart"));
                int plcCount = int.Parse(ini.GetValue("PLCConfig", "Amount"));
                opcServerType = ini.GetValue("OpcServer", "Type").Trim();


            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            finally
            {
                if (autoStart)
                    btnStart_Click(null, null);
            }
            ni.Text = this.Text;
        }
        private bool Config()
        {
            bool success = false;
            try
            {
                string dbFile = "sqlite.db";
                SQLiteHelper sqlite = new SQLiteHelper(dbFile);
                string strSql = "select * from opcGateway ";
                int id =Convert.ToInt32(sqlite.ExecuteScalar(strSql));
                string strDBSql = "select * from DBconfig where OPCGateway_serialID="+id;
                DataTable dt = sqlite.ExecuteQuery(strDBSql);
                int plcCount = int.Parse(ini.GetValue("PLCConfig", "Amount"));
                DataRow[] dtRead = dt.Select("accessibility<>'write'");        
                DataRow[] dtwrite = dt.Select("accessibility<>'read'");
                createCtrlItem(dtwrite);
                CreateReadItem(dtRead);               
                
            }
            catch (Exception ex)
            {
                log.Error(ex);
                success = false;
            }
           return  success= true;
        }

        private void CreateReadItem(DataRow[] dtRead)
        {
            plc = new PLCStationInformation();
            List<int> list = new List<int>();
            for (int i = 0; i < dtRead.Length; i++)
            {
                plc.ItemsID.Add(Convert.ToInt32(dtRead[i]["code"]));
                string strDB = dtRead[i]["address"].ToString();
                plc.ItemsData.Add(strDB);

            }
        }

        /// <summary>
        /// 控制指令
        /// </summary>
        private void createCtrlItem(DataRow[] drWrite)
        {
            int j = 0;

            List<string> strl = new List<string>();
            List<int> idList = new List<int>();
            for (int i = 0; i < drWrite.Length; i++)
            {
                idList.Add(Convert.ToInt32(drWrite[i]["code"]));
                string strDB = drWrite[i]["address"].ToString();
                strl.Add(strDB);

            }
            string[] dbStrl = new string[strl.Count];
            strl.CopyTo(dbStrl, 0);
            Controller MetersCtrl = new Controller("MetersCtrl", dbStrl, idList);
            Program.MeterManager.CTMeters.Add(90 + j++, MetersCtrl);
            string pid = ini.GetValue(opcServerType, "ProgramID");
            MetersCtrl.ItemCtrl = dbStrl;
            MetersCtrl.Connect(pid.ToString(), "127.0.0.1");

        }
       
        /// <summary>
        /// 创建数据项管理器
        /// </summary>
        private IList<IDataItemManage> createItemsMangers()
        {
            bool success = false;
            IList<IDataItemManage> listDataItemManger = new List<IDataItemManage>();
            try
            {                          
                    string[] data = new string[plc.ItemsData.Count];
                    plc.ItemsData.CopyTo(data, 0);
                    IList<int> IDlist=plc.ItemsID;
                    IDataItemManage dim = new DataItemManager("ItemData",  data, IDlist);
                    listDataItemManger.Add(dim);
               

                success = true;
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            if (success)
                return listDataItemManger;
            else
                return null;
        }

      


        private IList<IDatabaseManage> createDBManagers()
        {
            bool success = false;
            IList<IDatabaseManage> listDatabaseManger = new List<IDatabaseManage>();
            string[] dbs = ini.GetValue("Database", "Members").Trim().Split(',');
            try
            {
                foreach (string dbName in dbs)
                {
                    DatabaseManage dbm = new DatabaseManage(ini.GetValue(dbName, "Address"),
                                                                 ini.GetValue(dbName, "PersistSecurityInfo"),
                                                                 ini.GetValue(dbName, "Database"),
                                                                 ini.GetValue(dbName, "Username"),
                                                                 ini.GetValue(dbName, "Password"));

                    listDatabaseManger.Add(dbm);
                    success = true;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            if (success)
                return listDatabaseManger;
            else
                return null;

        }
        public void Start()
        {
            //Thread.Sleep(Program.waitMillionSecond);
            SyncOpc = new OPCServer();
            string pid = ini.GetValue(opcServerType, "ProgramID");
            Config();
            Program.opcGateway = new OpcGateway(createItemsMangers(), createDBManagers());
            Program.opcGateway.StateChanged += new EventHandler(opcGateway_StateChanged);
            Program.opcGateway.UpdateCycle.WorkStateChanged += new CycleTask.WorkStateChangeEventHandle(UpdateCycle_WorkStateChanged);
            Program.opcGateway.RemoteCtrlCycle.WorkStateChanged += new CycleTask.WorkStateChangeEventHandle(RemoteCtrlCycle_WorkStateChanged);
            Program.opcGateway.StartButton(pid);
        }

        void opcGateway_StateChanged(object sender, EventArgs e)
        {
            Console.WriteLine((sender as OpcGateway).RunningState);
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker(delegate
                {
                    if (Program.opcGateway.RunningState == Gateway.RunningState.Running)
                    {
                        //添加获取采集点的数量
                        this.tsddbPLC.DropDownItems.Clear();
                       
                            ToolStripMenuItem tsiPLC = new ToolStripMenuItem(plc.Connection);
                            this.tsddbPLC.DropDownItems.Add(tsiPLC);
                           
                                ToolStripMenuItem tsiItemGrop = new ToolStripMenuItem(string.Format("共{0}个DB块", plc.ItemsData.Count));
                                tsiPLC.DropDownItems.Add(tsiItemGrop);

                            
                        
                    }
                }));
            }
        }

        void RemoteCtrlCycle_WorkStateChanged(JonLibrary.Automatic.RunningState state)
        {
            this.BeginInvoke(new MethodInvoker(delegate
            {
                switch (state)
                {
                    case JonLibrary.Automatic.RunningState.Paused:
                        this.tsslRemote.Text = "P";
                        this.btnPC.Text = "继续";
                        break;
                    case JonLibrary.Automatic.RunningState.Running:
                        this.tsslRemote.Text = "R";
                        this.btnPC.Enabled = true;
                        this.btnPC.Text = "暂停";
                        break;
                    case JonLibrary.Automatic.RunningState.Stopped:
                        this.tsslRemote.Text = "S";
                        break;
                }
            }));
        }




        void UpdateCycle_WorkStateChanged(JonLibrary.Automatic.RunningState state)
        {
            this.BeginInvoke(new MethodInvoker(delegate
            {
                switch (state)
                {
                    case JonLibrary.Automatic.RunningState.Paused:
                        this.tsslUpdate.Text = "P";
                        this.btnPC.Text = "继续";
                        break;
                    case JonLibrary.Automatic.RunningState.Running:
                        this.tsslUpdate.Text = "R";
                        this.btnPC.Enabled = true;
                        this.btnPC.Text = "暂停";
                        break;
                    case JonLibrary.Automatic.RunningState.Stopped:
                        this.tsslUpdate.Text = "S";
                        break;
                }
            }));
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            this.btnStart.Enabled = false;
            this.Start();
        }

        private void btnPC_Click(object sender, EventArgs e)
        {
            //MessageBox.Show(Program.opcGateway.RunningState.ToString());
            if (Program.opcGateway.RunningState == Gateway.RunningState.Running)
                Program.opcGateway.Pause();
            else
                Program.opcGateway.Continue();
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!Program.BeQuit)
            {
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                e.Cancel = true;
            }
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case FormWindowState.Normal:
                    this.ShowInTaskbar = true;
                    break;
                case FormWindowState.Minimized:
                    this.ShowInTaskbar = false;
                    break;
            }
        }

        private void 退出EToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("这将使数据采集系统退出运行状态，确定要退出吗？", "退出", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                    == System.Windows.Forms.DialogResult.Yes)
            {
                this.Hide();
                Program.BeQuit = true;
                Thread.Sleep(200);
                this.Close();
            }
        }

        private void ni_DoubleClick(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case FormWindowState.Normal:
                    this.WindowState = FormWindowState.Minimized;
                    break;
                case FormWindowState.Minimized:
                    this.WindowState = FormWindowState.Normal;
                    break;
            }
        }

        Form frmDataDisplay = null;
        private void tsslMeters_Click(object sender, EventArgs e)
        {
            if (frmDataDisplay != null && !frmDataDisplay.IsDisposed)
                frmDataDisplay.Show();
            else
            {
                (frmDataDisplay = new DataDisplayForm()).Show();
            }
        }
    }
}
