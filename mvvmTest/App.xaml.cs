using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CoATool.Helper;

namespace mvvmTest
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private static FileHeartbeatServer _heartbeatServer;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 启动心跳服务
            _heartbeatServer = new FileHeartbeatServer();
            _heartbeatServer.Start();

            // 注册程序退出事件，确保清理心跳文件
            AppDomain.CurrentDomain.ProcessExit += (sender, e1) => {
                _heartbeatServer?.Stop();
            };

            Console.CancelKeyPress += (sender, e1) => {
                _heartbeatServer?.Stop();
                Environment.Exit(0);
            };

            // 确保管道服务在应用程序启动时就开始运行
            var pipeServerService = new PipeServerService();
            pipeServerService.Start();

            // 可以将服务实例保存在应用程序级别以便后续使用
            Application.Current.Properties["PipeServerService"] = pipeServerService;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (Application.Current.Properties.Contains("PipeServerService"))
            {
                var pipeServerService = Application.Current.Properties["PipeServerService"] as PipeServerService;
                pipeServerService?.Dispose();

                // 给一点时间让清理完成
                System.Threading.Thread.Sleep(100);
            }
            base.OnExit(e);
        }
    }
}
