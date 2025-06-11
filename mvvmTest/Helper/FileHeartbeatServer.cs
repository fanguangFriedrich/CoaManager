using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoATool.Helper
{
    public class FileHeartbeatServer
    {
        private readonly string _heartbeatFile;
        private Timer _heartbeatTimer;
        private readonly object _lockObject = new object();
        private bool _isRunning = false;

        public FileHeartbeatServer()
        {
            _heartbeatFile = Path.Combine(Path.GetTempPath(), "appb_heartbeat.txt");
        }

        // 启动心跳服务
        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;

            // 立即创建心跳文件
            UpdateHeartbeat(null);

            // 每3秒更新一次心跳文件
            _heartbeatTimer = new Timer(UpdateHeartbeat, null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));

            Console.WriteLine($"心跳服务已启动，文件路径: {_heartbeatFile}");
        }

        private void UpdateHeartbeat(object state)
        {
            try
            {
                lock (_lockObject)
                {
                    var heartbeatInfo = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}|{Process.GetCurrentProcess().Id}|running";
                    File.WriteAllText(_heartbeatFile, heartbeatInfo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新心跳文件失败: {ex.Message}");
            }
        }

        // 停止心跳服务
        public void Stop()
        {
            _isRunning = false;
            _heartbeatTimer?.Dispose();

            // 清理心跳文件
            try
            {
                if (File.Exists(_heartbeatFile))
                {
                    File.Delete(_heartbeatFile);
                }
                Console.WriteLine("心跳服务已停止");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理心跳文件失败: {ex.Message}");
            }
        }
    }
}
