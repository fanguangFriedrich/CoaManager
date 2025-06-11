using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PipeCommunicationLibrary;
using System.Threading;

namespace CoATool.Helper
{
    public class PipeServerService : IDisposable
    {
        private NamedPipeServerStream _pipeServer;
        private bool _isRunning;
        private readonly object _lockObject = new object();
        private CancellationTokenSource _cancellationTokenSource;

        public void Start()
        {
            lock (_lockObject)
            {
                if (_isRunning)
                    return;
                _isRunning = true;
                _cancellationTokenSource = new CancellationTokenSource();
                Task.Run(() => ServerLoop(_cancellationTokenSource.Token));
            }
        }

        private async Task ServerLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 创建命名管道服务器
                    _pipeServer = new NamedPipeServerStream(
                        PipeConstants.PIPE_NAME,
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Message);

                    // 使用CancellationToken等待客户端连接
                    await _pipeServer.WaitForConnectionAsync(cancellationToken);

                    // 检查是否已经取消
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    using (StreamReader reader = new StreamReader(_pipeServer))
                    using (StreamWriter writer = new StreamWriter(_pipeServer))
                    {
                        // 使用超时读取消息
                        var readTask = reader.ReadLineAsync();
                        var timeoutTask = Task.Delay(5000, cancellationToken); // 5秒超时

                        var completedTask = await Task.WhenAny(readTask, timeoutTask);

                        if (completedTask == readTask && !cancellationToken.IsCancellationRequested)
                        {
                            string message = await readTask;
                            if (message == PipeConstants.CHECK_STATUS_MESSAGE)
                            {
                                await writer.WriteLineAsync(PipeConstants.APP_B_RUNNING_MESSAGE);
                                await writer.FlushAsync();
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // 正常的取消操作，不需要记录日志
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Pipe server error: {ex.Message}");
                }
                finally
                {
                    // 安全地关闭管道
                    SafeClosePipe();
                }
            }
        }

        private void SafeClosePipe()
        {
            if (_pipeServer != null)
            {
                try
                {
                    // 直接关闭底层句柄，强制断开连接
                    _pipeServer.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during close: {ex.Message}");
                }
                finally
                {
                    try
                    {
                        _pipeServer.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during dispose: {ex.Message}");
                    }
                    _pipeServer = null;
                }
            }
        }

        public void Dispose()
        {
            lock (_lockObject)
            {
                _isRunning = false;

                // 取消所有异步操作
                _cancellationTokenSource?.Cancel();

                // 安全地关闭管道
                SafeClosePipe();

                _cancellationTokenSource?.Dispose();
            }
        }
    }
}
