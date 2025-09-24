using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Web;
using CellReport.core.expr;
using CellReport.function;
using CellReport.running;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace reportWeb.hubs
{
    public class CustomChatHub : Hub
    {
        IHttpContextAccessor _httpContextAccessor;
        ScopedObj reportGrp;
        ILogger<CustomChatHub> logger;
        static Object lockObj = new object();
        public CustomChatHub(IHttpContextAccessor httpContextAccessor, ScopedObj reportGrp, ILogger<CustomChatHub> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            this.reportGrp = reportGrp;
            this.logger = logger;
        }
        private static readonly ConcurrentDictionary<string, Channel> Channels = new ConcurrentDictionary<string, Channel>();
        private string channelName;
        private Channel channel;
        public async Task JoinChannel(string channelName, string send_func_name, string end_func_name, string json_str)
        {
            this.channelName = channelName;
            await Groups.AddToGroupAsync(Context.ConnectionId, channelName);
            //await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelName);

            // 创建或获取频道
            channel = Channels.GetOrAdd(channelName, _ => new Channel
            {
                Name = channelName,
                Messages = new List<Message>(),
                CancellationTokenSource = new CancellationTokenSource(),
                GeneratorSemaphore = new SemaphoreSlim(1, 1)
            });

            await channel.GenerateAutoMessagesIfNotRunningAsync(async () =>
            {
                await SendMessageByReport(send_func_name, end_func_name, json_str);
            });
            // 启动自动消息生成器（如果尚未启动）
            //if (!channel.IsGeneratorRunning)
            //{
            //    channel.IsGeneratorRunning = true;
            //    //_ = GenerateAutoMessagesAsync(channel);
            //    await SendMessageByReport(channel, send_func_name,  end_func_name,  json_str);
            //}
            // 发送历史消息给新连接
            if (channel.IsGeneratorRunning)
                await Clients.Caller.SendAsync("ReceiveMessage", channel.TxtCache.ToString());
        }
        private async Task SendMessageByReport(string send_func_name, string end_func_name, string json_str)
        {
            var rootElement = JsonDocument.Parse(json_str).RootElement;
            var href = rootElement.GetProperty("href").GetString();
            var uri = new Uri(href);
            var cur_path = uri.LocalPath;
            var seg_arr = cur_path.Split(":");
            string grp = "default";
            if (seg_arr.Length >= 2)
            {
                grp = seg_arr[1];
            }
            var query = _httpContextAccessor.HttpContext.RequestServices.GetService(typeof(ReportDbContext)) as ReportDbContext;
            var rpt_group = query.GetRpt_Group(grp);

            using var reportDefine = await XmlReport.loadReport(rpt_group.report_path, HttpUtility.ParseQueryString(uri.Query)["reportName"], grp);
            reportDefine.getEnv().logger = new Pages.MyLogger(logger);
            var exprFaced = reportDefine.getEnv().getExprFaced();
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            exprFaced.addNewScopeForScript("", "chatHub");
            exprFaced.addVariable("json_str", json_str);
            exprFaced.addVariable("taskCompletionSource", tcs);
            exprFaced.addVariable("channelName", channelName);
            exprFaced.addVariable("Channels", Channels);
            Func<string, string, Task> func = async (string method, string txt) =>
            {
                if (method == "ReceiveMessage")
                {
                    channel.TxtCache.Append(txt);
                    await Clients.Group(channelName).SendAsync("ReceiveMessage", txt);
                }
                else if (method == "sendOne")
                {
                    await Clients.Caller.SendAsync("ReceiveMessage", txt);
                }
                else
                {
                    await Clients.Group(channelName).SendAsync("ReceiveMessage", txt);
                    tcs.SetResult(true);
                }
            };
            exprFaced.addVariable("reciveMessageFunc", func);
            try
            {
                var result = await exprFaced.calculateAsync($"={send_func_name}(json_str,reciveMessageFunc)");
                if (result != null && result is CR_Object cr_obj)
                {
                    if (cr_obj.TryGetValue("forward", out var forward) && bool.TrueString.Equals(forward?.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        var _ = await tcs.Task;
                    }
                    if (!string.IsNullOrWhiteSpace(end_func_name) &&
                        cr_obj.TryGetValue("close", out var close)
                        && bool.TrueString.Equals(close?.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        exprFaced.addVariable("last_result", channel.TxtCache.ToString());
                        exprFaced.addVariable("forward", forward);
                        await Clients.Group(channelName).SendAsync("CloseConnection");
                        if (end_func_name != null)
                            exprFaced.calculate($"={end_func_name}(json_str,last_result,forward)");
                        Channels.Remove(channelName, out var _);
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(ex.Message);
                Exception cur = ex.InnerException;
                while (cur != null)
                {
                    sb.AppendLine(cur.Message);
                    cur = cur.InnerException;
                }
                await Clients.Caller.SendAsync("ReceiveMessage", sb.ToString());
            }
            finally
            {
                channel.IsGeneratorRunning = false;
            }
        }
        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            Console.WriteLine($"Client {Context.ConnectionId} left group {groupName}");
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // 可以在这里实现连接断开的逻辑
            await base.OnDisconnectedAsync(exception);
        }

    }

    public class Channel
    {
        public string Name { get; set; }
        public List<Message> Messages { get; set; }
        public StringBuilder TxtCache { get; set; } = new();
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public bool IsGeneratorRunning { get; set; }

        public SemaphoreSlim GeneratorSemaphore { get; set; }

        public async Task GenerateAutoMessagesIfNotRunningAsync(Func<Task> startGenerator)
        {
            // 快速检查
            if (IsGeneratorRunning)
                return;

            // 使用信号量进行同步
            await GeneratorSemaphore.WaitAsync();
            try
            {
                // 再次检查，确保在等待信号量期间没有其他线程启动生成器
                if (!IsGeneratorRunning)
                {
                    IsGeneratorRunning = true;
                    await startGenerator();
                }
            }
            finally
            {
                GeneratorSemaphore.Release();
                IsGeneratorRunning = false;
            }
        }
    }

    public class Message
    {
        public string Text { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
