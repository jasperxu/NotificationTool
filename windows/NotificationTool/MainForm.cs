using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;

namespace NotificationTool
{
    public partial class MainForm : Form
    {
        string ExePath = System.Windows.Forms.Application.StartupPath;
        string ConfigFile = "notification.tool.config.json";// 配置文件名称
        string ErrorFileName = "notification.tool.error.log";
        string OutputFileName = "notification.tool.output.log";
        Icon DefaultIcon, StartIcon, StopIcon, RestartIcon;
        Image OkImage, StartImage, StopImage, RestartImage;
        string RunCMD = "";
        Process MainProcess;
        Process TempProcess;
        bool IsRun = false;
        bool IsAutoRun = false;
        string AppKey;
        System.Windows.Forms.ToolStripMenuItem AutoRunMenu;



        public MainForm()
        {
            InitializeComponent();
            TestPath();
            System.Environment.CurrentDirectory = ExePath;
            InitAllProcess();
            SetImageIcon();
        }

        private void TestPath()
        {
            //获取模块的完整路径。  
            string path1 = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            //获取和设置当前目录(该进程从中启动的目录)的完全限定目录  
            string path2 = System.Environment.CurrentDirectory;
            //获取应用程序的当前工作目录  
            string path3 = System.IO.Directory.GetCurrentDirectory();
            //获取程序的基目录  
            string path4 = System.AppDomain.CurrentDomain.BaseDirectory;
            //获取和设置包括该应用程序的目录的名称  
            string path5 = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            //获取启动了应用程序的可执行文件的路径  
            string path6 = System.Windows.Forms.Application.StartupPath;
            //获取启动了应用程序的可执行文件的路径及文件名  
            string path7 = System.Windows.Forms.Application.ExecutablePath;

            StringBuilder str = new StringBuilder();
            str.AppendLine("System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName:" + path1);
            str.AppendLine("System.Environment.CurrentDirectory:" + path2);
            str.AppendLine("System.IO.Directory.GetCurrentDirectory():" + path3);
            str.AppendLine("System.AppDomain.CurrentDomain.BaseDirectory:" + path4);
            str.AppendLine("System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase:" + path5);
            str.AppendLine("System.Windows.Forms.Application.StartupPath:" + path6);
            str.AppendLine("System.Windows.Forms.Application.ExecutablePath:" + path7);
            string allPath = str.ToString();

            // MessageBox.Show(allPath);
        }

        private void InitAllProcess()
        {
            InitProcess(out MainProcess);
            InitProcess(out TempProcess);

            MainProcess.Exited += (sender, e) =>
            {
                var ErrorFile = File.Open(ErrorFileName, FileMode.Append);
                var ErrorFileStream = new StreamWriter(ErrorFile);
                ErrorFileStream.Write(MainProcess.StandardError.ReadToEnd());

                var OutputFile = File.Open(OutputFileName, FileMode.Append);
                var OutputFileStream = new StreamWriter(OutputFile);
                OutputFileStream.Write(MainProcess.StandardOutput.ReadToEnd());

                ErrorFileStream.Flush();
                ErrorFileStream.Close();
                OutputFileStream.Flush();
                OutputFileStream.Close();
            };
        }

        private void InitProcess(out Process p)
        {
            p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Domain = ExePath;
            p.StartInfo.UseShellExecute = false;        //是否使用操作系统shell启动
            p.StartInfo.RedirectStandardInput = true;   //接受来自调用程序的输入信息
            p.StartInfo.RedirectStandardOutput = true;  //由调用程序获取输出信息
            p.StartInfo.RedirectStandardError = true;   //重定向标准错误输出
            p.StartInfo.CreateNoWindow = true;         //不显示程序窗口

            p.EnableRaisingEvents = true;               //启用Exited事件
        }

        private void SetImageIcon()
        {
            DefaultIcon = new Icon(GetStream("icon.ico"));
            StartIcon = new Icon(GetStream("start.ico"));
            StopIcon = new Icon(GetStream("stop.ico"));
            RestartIcon = new Icon(GetStream("restart.ico"));

            OkImage = Image.FromStream(GetStream("ok.png"));
            StartImage = Image.FromStream(GetStream("start.png"));
            StopImage = Image.FromStream(GetStream("stop.png"));
            RestartImage = Image.FromStream(GetStream("restart.png"));

        }

        private Icon GetIcon(string resourceName)
        {
            return new Icon(GetStream(resourceName));
        }

        private Image GetImage(string resourceName)
        {
            return Image.FromStream(GetStream(resourceName));
        }

        private Stream GetStream(string resourceName)
        {
            var _namespace = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace;
            var _assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var _resourceName = _namespace + ".NotificationTool." + resourceName;
            return _assembly.GetManifestResourceStream(_resourceName);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // 设置窗体图标和提示图标的默认值
            this.Icon = DefaultIcon;
            this.notifyIcon.Icon = DefaultIcon;

            // 读取配置文件
            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<List<JsonMenuItem>>(File.ReadAllText(ConfigFile));

            CreateMenus(config);

            InitAutoRun();


            this.WindowState = FormWindowState.Minimized;    // 设置窗体状态为最小化
            this.Visible = false;                            // 不显示窗体
            this.ShowInTaskbar = false;                      // 使Form不在任务栏上显示
        }

        private void CreateMenus(List<JsonMenuItem> menus)
        {
            foreach (var item in menus)
            {
                ToolStripItem temp;

                switch (item.CMDKey)
                {
                    case "-":
                        temp = Separator();
                        break;
                    case "AutoRun":
                        temp = AutoRun(item);
                        break;
                    case "Exit":
                        temp = Exit(item);
                        break;
                    case "Start":
                        temp = Start(item);
                        break;
                    case "Stop":
                        temp = Stop(item);
                        break;
                    case "Restart":
                        temp = Restart(item);
                        break;
                    default:
                        temp = Show(item);
                        break;
                }
                this.contextMenuStrip.Items.Add(temp);

            }
        }

        private void KillProcessAndChildren(int pid)
        {
            System.Management.ManagementObjectSearcher searcher = new System.Management.ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
            System.Management.ManagementObjectCollection moc = searcher.Get();
            foreach (System.Management.ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                Process p = Process.GetProcessById(pid);
                Console.WriteLine(pid);
                p.Kill();
            }
            catch (ArgumentException)
            {
                /* process already exited */
            }
        }

        private ToolStripSeparator Separator()
        {
            return new ToolStripSeparator();
        }

        private ToolStripMenuItem AutoRun(JsonMenuItem item)
        {
            AutoRunMenu = new System.Windows.Forms.ToolStripMenuItem();
            AppKey = item.CMD;
            AutoRunMenu.Text = item.Text;
            if (!String.IsNullOrEmpty(item.Image))
                AutoRunMenu.Image = Bitmap.FromFile(item.Image);
            else
                AutoRunMenu.Image = OkImage;

            return AutoRunMenu;
        }


        private ToolStripMenuItem Exit(JsonMenuItem item)
        {
            var menu = new System.Windows.Forms.ToolStripMenuItem();
            var texts = item.Text.Split('|');
            menu.Text = texts[0];
            if (!String.IsNullOrEmpty(item.Image))
                menu.Image = Bitmap.FromFile(item.Image);

            menu.Click += (sender, e) =>
            {
                if (MessageBox.Show(texts[1], texts[0], MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    if (IsRun)
                    {
                        KillProcessAndChildren(MainProcess.Id);
                        notifyIcon.Icon = StopIcon;
                        IsRun = false;
                    }
                    Application.Exit();
                }
            };
            return menu;
        }

        private ToolStripMenuItem Start(JsonMenuItem item)
        {
            var menu = new System.Windows.Forms.ToolStripMenuItem();
            menu.Text = item.Text;
            if (!String.IsNullOrEmpty(item.Image))
                menu.Image = Bitmap.FromFile(item.Image);
            else
                menu.Image = StartImage;
            if (!String.IsNullOrEmpty(item.Icon))
                StartIcon = new Icon(item.Icon);

            RunCMD = item.CMD;
            menu.Click += (sender, e) =>
            {
                if (!IsRun)
                {
                    MainProcess.Start();//启动程序
                    MainProcess.StandardInput.WriteLine(RunCMD);
                    MainProcess.StandardInput.AutoFlush = true;
                    notifyIcon.Icon = StartIcon;
                    IsRun = true;
                }
            };
            return menu;
        }

        private ToolStripMenuItem Stop(JsonMenuItem item)
        {
            var menu = new System.Windows.Forms.ToolStripMenuItem();
            menu.Text = item.Text;
            if (!String.IsNullOrEmpty(item.Image))
                menu.Image = Bitmap.FromFile(item.Image);
            else
                menu.Image = StopImage;
            if (!String.IsNullOrEmpty(item.Icon))
                StopIcon = new Icon(item.Icon);

            menu.Click += (sender, e) =>
            {
                if (IsRun)
                {
                    // MainProcess.Kill();
                    KillProcessAndChildren(MainProcess.Id);
                    notifyIcon.Icon = StopIcon;
                    IsRun = false;
                }
            };
            return menu;
        }

        private ToolStripMenuItem Restart(JsonMenuItem item)
        {
            var menu = new System.Windows.Forms.ToolStripMenuItem();
            menu.Text = item.Text;
            if (!String.IsNullOrEmpty(item.Image))
                menu.Image = Bitmap.FromFile(item.Image);
            else
                menu.Image = RestartImage;
            if (!String.IsNullOrEmpty(item.Icon))
                RestartIcon = new Icon(item.Icon);

            menu.Click += (sender, e) =>
            {
                if (IsRun)
                {
                    notifyIcon.Icon = RestartIcon;
                    KillProcessAndChildren(MainProcess.Id);
                    IsRun = false;
                    MainProcess.Start();
                    MainProcess.StandardInput.WriteLine(RunCMD);
                    MainProcess.StandardInput.AutoFlush = true;
                    notifyIcon.Icon = StartIcon;
                    IsRun = true;
                }
            };
            return menu;
        }

        private ToolStripMenuItem Show(JsonMenuItem item)
        {
            var menu = new System.Windows.Forms.ToolStripMenuItem();
            menu.Text = item.Text;
            if (!String.IsNullOrEmpty(item.Image))
                menu.Image = Bitmap.FromFile(item.Image);
            menu.Click += (sender, e) =>
            {
                TempProcess.Start();
                TempProcess.StandardInput.WriteLine(item.CMD);
                TempProcess.StandardInput.AutoFlush = true;
                TempProcess.StandardInput.WriteLine("exit");
                TempProcess.Close();
                InitProcess(out TempProcess);
            };
            return menu;
        }

        private void InitAutoRun()
        {
            IsAutoRun = CheckAutoRun();
            if (IsAutoRun)
            {
                AutoRunMenu.Image = OkImage;
                MainProcess.Start();//启动程序
                MainProcess.StandardInput.WriteLine(RunCMD);
                MainProcess.StandardInput.AutoFlush = true;
                notifyIcon.Icon = StartIcon;
                IsRun = true;
            }
            else
            {
                AutoRunMenu.Image = null;
            }

            AutoRunMenu.Click += (sender, e) =>
            {
                if (IsAutoRun)
                {
                    SetNotAutoRun();
                    AutoRunMenu.Image = null;
                    IsAutoRun = false;
                }
                else
                {
                    SetAutoRun();
                    AutoRunMenu.Image = OkImage;
                    IsAutoRun = true;
                }
            };

        }

        private bool CheckAutoRun()
        {
            bool isautorun = false;
            string path = System.Windows.Forms.Application.ExecutablePath;
            // RegistryKey rk = Registry.LocalMachine;
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rk2 = rk.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            var rks = rk2.GetValue(AppKey);
            if (rks != null)
                isautorun = true;
            rk2.Close();
            rk.Close();
            return isautorun;
        }

        private void SetAutoRun()
        {
            string path = System.Windows.Forms.Application.ExecutablePath;
            // RegistryKey rk = Registry.LocalMachine;
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rk2 = rk.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            rk2.SetValue(AppKey, path);
            rk2.Close();
            rk.Close();
        }


        private void SetNotAutoRun()
        {
            string path = System.Windows.Forms.Application.ExecutablePath;
            // RegistryKey rk = Registry.LocalMachine;
            RegistryKey rk = Registry.CurrentUser;
            RegistryKey rk2 = rk.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            rk2.DeleteValue(AppKey, false);
            rk2.Close();
            rk.Close();
        }
    }
}
