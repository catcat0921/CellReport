using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
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
using reportWeb.Controllers;
using ZXing;



namespace reportWeb.hubs
{
    //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ChatHub : Hub
    {
        IHttpContextAccessor _httpContextAccessor;
        ScopedObj reportGrp;
        ILogger<ChatHub> logger;
        public ChatHub(IHttpContextAccessor httpContextAccessor, ScopedObj reportGrp, ILogger<ChatHub> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            this.reportGrp = reportGrp;
            this.logger = logger;
        }
        public override Task OnDisconnectedAsync(Exception exception)
        {
            return base.OnDisconnectedAsync(exception);
        }
        public override Task OnConnectedAsync()
        {
            //// 从http中根据“access_token“字段获取，token，然后校验token
            //string token = _httpContextAccessor.HttpContext.Request.Query["access_token"];
            //// 实际校验过程根据个人项目决定，成功则连接，不成功直接返回
            //if (string.IsNullOrEmpty(token))
            //{
            //    return null;
            //}
            //Console.Error.WriteLine("OnConnectedAsync :" + Context.ConnectionId);
            //Console.Error.WriteLine("HttpContext:" +
            //    _httpContextAccessor.HttpContext.User.Claims.First(x => x.Type == ClaimTypes.Name));
            //Console.Error.WriteLine("HubContext :" +
            //    Context.User.Claims.First(x => x.Type == ClaimTypes.Name));
            return base.OnConnectedAsync();
        }
        public async Task SendMessage(string user, string message)
        {
            //this.HttpContext.User
            //Context.UserIdentifier
            await Clients.All.SendAsync("ReceiveMessage", message, Context.ConnectionId);
        }
        [AllowAnonymous]
        public async Task SendMessageJson(string send_func_name, string end_func_name, string json_str)
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
            //scopedObj.rpt_group = context.RequestServices.GetService<ReportDb>().findGroupById(grp);
            var query = _httpContextAccessor.HttpContext.RequestServices.GetService(typeof(ReportDbContext)) as ReportDbContext;
            var rpt_group = query.GetRpt_Group(grp);

            using var reportDefine = await XmlReport.loadReport(rpt_group.report_path, HttpUtility.ParseQueryString(uri.Query)["reportName"], grp);
            reportDefine.getEnv().logger = new Pages.MyLogger(logger);
            var exprFaced = reportDefine.getEnv().getExprFaced();
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            exprFaced.addNewScopeForScript("", "chatHub");
            exprFaced.addVariable("caller", Clients.Caller);
            exprFaced.addVariable("json_str", json_str);
            exprFaced.addVariable("taskCompletionSource", tcs);
            try
            {
                var result = await exprFaced.calculateAsync($"={send_func_name}(json_str,caller,taskCompletionSource)");
                if (result != null && result is CR_Object cr_obj)
                {
                    if (cr_obj.TryGetValue("end", out var end) && bool.TrueString.Equals(end?.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        var txt = await tcs.Task;
                        exprFaced.addVariable("last_result", txt);
                        await Clients.Caller.SendAsync("CloseConnection");
                        if (end_func_name != null)
                            exprFaced.calculate($"={end_func_name}(json_str,last_result)");
                    }
                    if (cr_obj.TryGetValue("disposable_list", out var _list))
                    {
                        if (_list is ReturnList<Object> real_list)
                        {
                            foreach (var item in real_list)
                            {
                                if (item is IDisposable disposable)
                                {
                                    disposable.Dispose();
                                }
                            }
                        }
                        if (_list is IDisposable disposable2)
                        {
                            disposable2.Dispose();
                        }
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
        }

        public String GetConnectionId()
        {
            return Context.ConnectionId;
        }
    }
}
