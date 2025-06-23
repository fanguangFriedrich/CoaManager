using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PipeCommunicationLibrary;
using System.Threading;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace CoATool.Helper
{
    public class PipeServerService : IDisposable
    {
        private NamedPipeServerStream _pipeServer;
        private bool _isRunning;
        private readonly object _lockObject = new object();
        private CancellationTokenSource _cancellationTokenSource;
        public event Action<CommonFamily> RecvCommonFamilyOccurred;

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
                NamedPipeServerStream pipeServer = null;
                try
                {
                    // 创建命名管道服务器，支持多个客户端连接
                    pipeServer = new NamedPipeServerStream(
                        PipeConstants.PIPE_NAME,
                        PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances, // 允许多个实例
                        PipeTransmissionMode.Message);

                    // 等待客户端连接
                    await pipeServer.WaitForConnectionAsync(cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        pipeServer.Dispose();
                        break;
                    }

                    HandyControl.Controls.Growl.Success("新客户端已连接");

                    // 为每个客户端连接创建一个独立的处理任务
                    var clientTask = HandleClientConnection(pipeServer, cancellationToken);

                    // 不要await这个任务，让它在后台运行
                    _ = clientTask.ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            HandyControl.Controls.Growl.Error($"客户端处理任务出错: {t.Exception?.GetBaseException().Message}");
                        }
                        HandyControl.Controls.Growl.Warning("客户端处理任务结束");
                    }, TaskScheduler.Default);
                }
                catch (OperationCanceledException)
                {
                    // 正常的取消操作，不需要记录日志
                    break;
                }
                catch (Exception ex)
                {
                    HandyControl.Controls.Growl.Error($"创建管道服务器时出错: {ex.Message}");
                    pipeServer?.Dispose();
                    await Task.Delay(1000, cancellationToken); // 等待1秒后重试
                }
                finally
                {
                    // 安全地关闭管道
                    SafeClosePipe();
                }
            }
        }

        private async Task HandleClientConnection(NamedPipeServerStream pipeServer, CancellationToken cancellationToken)
        {
            try
            {
                using (pipeServer)
                using (StreamReader reader = new StreamReader(pipeServer))
                using (StreamWriter writer = new StreamWriter(pipeServer))
                {
                    writer.AutoFlush = true; // 自动刷新缓冲区

                    // 在同一个连接中循环读取多条消息
                    while (pipeServer.IsConnected && !cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            // 使用超时读取消息
                            var readTask = reader.ReadLineAsync();
                            var timeoutTask = Task.Delay(10000, cancellationToken); // 10秒超时
                            var completedTask = await Task.WhenAny(readTask, timeoutTask);

                            if (completedTask == readTask && !cancellationToken.IsCancellationRequested)
                            {
                                string message = await readTask;
                                HandyControl.Controls.Growl.Success($@"接收到的消息:{message}");

                                // 如果读取到null，说明客户端断开连接
                                if (message == null)
                                {
                                    HandyControl.Controls.Growl.Warning("客户端断开连接");
                                    break;
                                }

                                // 处理接收到的消息
                                await ProcessMessageAsync(message, writer);
                            }
                            else if (completedTask == timeoutTask)
                            {
                                // 超时了，可以发送心跳检测
                                if (pipeServer.IsConnected)
                                {
                                    try
                                    {
                                        await writer.WriteLineAsync("HEARTBEAT");
                                    }
                                    catch
                                    {
                                        HandyControl.Controls.Growl.Warning("客户端断开连接");
                                        break;
                                    }
                                }
                            }
                        }
                        catch (IOException ioEx)
                        {
                            HandyControl.Controls.Growl.Error($"管道连接中断: {ioEx.Message}");
                            break;
                        }
                        catch (ObjectDisposedException)
                        {
                            HandyControl.Controls.Growl.Warning("管道已被释放");
                            break;
                        }
                        catch (Exception ex)
                        {
                            HandyControl.Controls.Growl.Error($"读取消息时出错: {ex.Message}");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.Growl.Error($"处理客户端连接时出错: {ex.Message}");
            }
            finally
            {
                HandyControl.Controls.Growl.Warning("客户端连接处理结束");
            }
        }

        private async Task ProcessMessageAsync(string message, StreamWriter writer)
        {
            try
            {
                PipeMessage pipeMessage = JsonConvert.DeserializeObject<PipeMessage>(message);
                switch (pipeMessage.MessageType)
                {
                    case PipeMessageType.SendFamilyInfo:
                        CommonFamily commonFamily = JsonConvert.DeserializeObject<CommonFamily>(pipeMessage.Content);
                        RecvCommonFamilyOccurred?.Invoke(commonFamily);
                        break;
                    default:
                        Console.WriteLine($"Unknown message type: {pipeMessage.MessageType}");
                        await writer.WriteLineAsync("UNKNOWN_TYPE");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理消息时出错: {ex.Message}");
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
