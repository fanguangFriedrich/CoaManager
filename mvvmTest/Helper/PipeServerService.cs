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
using System.Collections.Concurrent;
using System.Runtime.Remoting.Messaging;

namespace CoATool.Helper
{
    
    // 客户端连接信息类
    public class ClientConnection
    {
        public string ClientId { get; set; }
        public NamedPipeServerStream PipeServer { get; set; }
        public StreamWriter Writer { get; set; }
        public DateTime ConnectedTime { get; set; }
        public bool IsConnected => PipeServer?.IsConnected == true;
    }

    public class PipeServerService : IDisposable
    {
        private NamedPipeServerStream _pipeServer;
        private bool _isRunning;
        private readonly object _lockObject = new object();
        private CancellationTokenSource _cancellationTokenSource;
        public event Action<CommonFamily> RecvCommonFamilyOccurred;
        public event Action<string> SendStringMessageOccurred;
        // 管理客户端连接的字典
        private readonly ConcurrentDictionary<string, ClientConnection> _clientConnections = new ConcurrentDictionary<string, ClientConnection>();

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
                        PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous);

                    // 等待客户端连接
                    await pipeServer.WaitForConnectionAsync(cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        pipeServer.Dispose();
                        break;
                    }

                    HandyControl.Controls.Growl.Success("新客户端已连接");

                    // 生成客户端ID
                    string clientId = Guid.NewGuid().ToString();

                    // 为每个客户端连接创建一个独立的处理任务
                    var clientTask = HandleClientConnection(pipeServer, clientId, cancellationToken);


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

        private async Task HandleClientConnection(NamedPipeServerStream pipeServer, string clientId, CancellationToken cancellationToken)
        {
            ClientConnection clientConnection = null;
            try
            {
                using (pipeServer)
                using (var reader = new StreamReader(pipeServer))
                using (var writer = new StreamWriter(pipeServer) { AutoFlush = true })
                {
                    // 创建客户端连接信息
                    clientConnection = new ClientConnection
                    {
                        ClientId = clientId,
                        PipeServer = pipeServer,
                        Writer = writer,
                        ConnectedTime = DateTime.Now
                    };

                    // 添加到连接字典中
                    _clientConnections.TryAdd(clientId, clientConnection);

                    // 在同一个连接中循环读取多条消息
                    while (pipeServer!=null&&pipeServer.IsConnected && !cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            // 使用超时读取消息
                            string message = await reader.ReadLineAsync();
                            HandyControl.Controls.Growl.Success($@"接收到的消息:{message}");
                            // 如果读取到null，说明客户端断开连接
                            if (message == null)
                            {
                                HandyControl.Controls.Growl.Warning("客户端断开连接");
                                break;
                            }

                            // 处理接收到的消息
                            await ProcessMessageAsync(message, writer, clientId);
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
                // 从连接字典中移除
                if (clientConnection != null)
                {
                    _clientConnections.TryRemove(clientId, out _);
                }
            }
        }

        private async Task ProcessMessageAsync(string message, StreamWriter writer, string clientId)
        {
            try
            {
                PipeMessage pipeMessage = JsonConvert.DeserializeObject<PipeMessage>(message);
                switch (pipeMessage.MessageType)
                {
                    case PipeMessageType.SendFamilyInfo:
                        CommonFamily commonFamily = JsonConvert.DeserializeObject<CommonFamily>(pipeMessage.Content as string);
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
                Console.WriteLine($"处理消息时出错 ClientId: {clientId}, Error: {ex.Message}");
            }
        }

        // 发送PNG图片文件的方法
        public async Task<bool> SendImageFileToClientAsync(string clientId, string imagePath, PipeMessageType messageType)
        {
            try
            {
                if (!File.Exists(imagePath))
                {
                    Console.WriteLine($"图片文件不存在: {imagePath}");
                    return false;
                }

                byte[] imageData = File.ReadAllBytes(imagePath);
                return await SendMessageToClientAsync(clientId,imageData, messageType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取图片文件时出错: {ex.Message}");
                return false;
            }
        }
        // 服务器端发送消息给指定客户端
        public async Task<bool> SendMessageToClientAsync(string clientId, object message,PipeMessageType messageType)
        {
            try
            {
                var pipeMessage = new PipeMessage
                {
                    MessageType = messageType,
                    Content = message
                };
                string jsonMessage = JsonConvert.SerializeObject(pipeMessage);

                if (_clientConnections.TryGetValue(clientId, out ClientConnection clientConnection))
                {
                    if (clientConnection.IsConnected && clientConnection.Writer != null)
                    {
                        await clientConnection.Writer.WriteLineAsync(jsonMessage);
                        await clientConnection.Writer.FlushAsync();
                        Console.WriteLine($"发送消息给客户端 ClientId: {clientId}, Message: {jsonMessage}");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"客户端未连接 ClientId: {clientId}");
                        // 移除无效连接
                        _clientConnections.TryRemove(clientId, out _);
                    }
                }
                else
                {
                    Console.WriteLine($"客户端不存在 ClientId: {clientId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送消息给客户端时出错 ClientId: {clientId}, Error: {ex.Message}");
                // 发生异常时也移除连接
                _clientConnections.TryRemove(clientId, out _);
            }
            return false;
        }

        // 服务器端广播消息给所有客户端
        public async Task<int> BroadcastMessageAsync(string message,PipeMessageType pipeMessageType=PipeMessageType.SendFamilyInfo)
        {
            int successCount = 0;
            var clientIds = _clientConnections.Keys.ToList();

            foreach (string clientId in clientIds)
            {
                bool success;
                switch (pipeMessageType)
                {
                    case PipeMessageType.SendCoAPic:
                        success = await SendImageFileToClientAsync(clientId, message,pipeMessageType);
                        break;
                    default:
                        success = await SendMessageToClientAsync(clientId, message,pipeMessageType);
                        break;
                }
                
                if (success)
                {
                    successCount++;
                }
            }

            Console.WriteLine($"广播消息成功发送给 {successCount}/{clientIds.Count} 个客户端");
            return successCount;
        }

        // 发送PipeMessage对象给指定客户端
        public async Task<bool> SendPipeMessageToClientAsync(string clientId, PipeMessage pipeMessage, PipeMessageType messageType)
        {
            try
            {
                string jsonMessage = JsonConvert.SerializeObject(pipeMessage);
                return await SendMessageToClientAsync(clientId, jsonMessage, messageType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"序列化消息时出错 ClientId: {clientId}, Error: {ex.Message}");
                return false;
            }
        }

        // 广播PipeMessage对象给所有客户端
        public async Task<int> BroadcastPipeMessageAsync(PipeMessage pipeMessage)
        {
            try
            {
                string jsonMessage = JsonConvert.SerializeObject(pipeMessage);
                return await BroadcastMessageAsync(jsonMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"序列化广播消息时出错, Error: {ex.Message}");
                return 0;
            }
        }

        // 获取当前连接的客户端列表
        public List<ClientConnection> GetConnectedClients()
        {
            return _clientConnections.Values
                .Where(c => c.IsConnected)
                .ToList();
        }

        // 获取连接的客户端数量
        public int GetConnectedClientCount()
        {
            return _clientConnections.Values.Count(c => c.IsConnected);
        }

        // 断开指定客户端连接
        public async Task DisconnectClientAsync(string clientId)
        {
            try
            {
                if (_clientConnections.TryGetValue(clientId, out ClientConnection clientConnection))
                {
                    if (clientConnection.IsConnected)
                    {
                        // 发送断开连接通知
                        try
                        {
                            await clientConnection.Writer.WriteLineAsync("DISCONNECT");
                            await clientConnection.Writer.FlushAsync();
                        }
                        catch { /* 忽略发送错误 */ }

                        // 关闭连接
                        clientConnection.PipeServer?.Disconnect();
                        clientConnection.PipeServer?.Dispose();
                    }

                    _clientConnections.TryRemove(clientId, out _);
                    Console.WriteLine($"已断开客户端连接 ClientId: {clientId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"断开客户端连接时出错 ClientId: {clientId}, Error: {ex.Message}");
            }
        }

        // 清理所有无效连接
        public async Task CleanupInvalidConnectionsAsync()
        {
            var invalidConnections = _clientConnections
                .Where(kvp => !kvp.Value.IsConnected)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (string clientId in invalidConnections)
            {
                _clientConnections.TryRemove(clientId, out _);
                Console.WriteLine($"清理无效连接 ClientId: {clientId}");
            }

            Console.WriteLine($"清理了 {invalidConnections.Count} 个无效连接");
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
