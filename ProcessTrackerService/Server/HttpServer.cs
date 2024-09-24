using Azure;
using MediatR;
using ProcessTrackerService.Core.Dto.Requests;
using ProcessTrackerService.Core.Dto.Responses;
using ProcessTrackerService.Core.Helpers;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProcessTrackerService.Server
{
    public class HttpServer : IHttpServer
    {
        private readonly ILogger<HttpServer> _logger;
        private readonly IMediator _mediator;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private HttpListener listener;
        private readonly string url;
        bool runServer = true;

        public HttpServer(ILogger<HttpServer> logger, IMediator mediator, IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _mediator = mediator;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            url = "http://localhost:" + AppSettings.HttpPort + "/";
        }
        private AppSettings AppSettings
        {
            get
            {
                return _configuration.GetSection("AppSettings").Get<AppSettings>();
            }
        }
        public async Task Start(CancellationToken stoppingToken)
        {
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            _logger.LogInformation("Listening for connections on {0}", url);

            // Handle requests
            await HandleIncomingConnections(stoppingToken);

            // Close the listener
            listener.Close();
        }
        //public void Stop()
        //{
        //    runServer = false;
        //}
        #region private
        private async Task HandleIncomingConnections(CancellationToken stoppingToken)
        {
            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (runServer || !stoppingToken.IsCancellationRequested)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                ctx.Response.ContentType = "application/json";

                var serializerOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                try
                {
                    _logger.LogInformation(req.Url.ToString() + "   at: {time}", DateTimeOffset.Now);

                    string requestBody = "";
                    using (var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding))
                    {
                        requestBody = reader.ReadToEnd();
                    }

                    using var scope = _serviceProvider.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
                    if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/shutdown"))
                    {
                        _logger.LogInformation("Shutdown requested");
                        runServer = false;
                    }

                    //TAGS
                    else if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/api/tag"))
                    {
                        GenericResponse response = null;
                        if (req.QueryString.HasKeys() && !string.IsNullOrEmpty(req.QueryString["name"]))
                            response = await mediator.Send(new GetTagsRequest { Name = req.QueryString["name"] });
                        else
                            response = await mediator.Send(new GetTagsRequest());

                        Respond(ctx, response);
                    }
                    else if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/api/tag/active"))
                    {
                        var response = await mediator.Send(new GetTagsRequest { Inactive = false });
                        Respond(ctx, response);
                    }
                    else if ((req.HttpMethod == "DELETE") && (req.Url.AbsolutePath == "/api/tag"))
                    {
                        string name = "";
                        if (req.QueryString.HasKeys() && !string.IsNullOrEmpty(req.QueryString["name"]))
                            name = req.QueryString["name"];
                        var response = await mediator.Send(new DeleteTagRequest { Name = name });
                        Respond(ctx, response);
                    }
                    else if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/api/tag/add"))
                    {
                        var response = await mediator.Send(JsonSerializer.Deserialize<CreateTagRequest>(requestBody, serializerOptions));
                        Respond(ctx, response);
                    }
                    else if ((req.HttpMethod == "PUT") && (req.Url.AbsolutePath == "/api/tag/toggleactive"))
                    {
                        var response = await mediator.Send(JsonSerializer.Deserialize<TagToggleRequest>(requestBody, serializerOptions));
                        Respond(ctx, response);
                    }

                    // FILTERS
                    else if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/api/filter"))
                    {
                        GenericResponse response = null;
                        if (!req.QueryString.HasKeys() || (string.IsNullOrEmpty(req.QueryString["id"]) && string.IsNullOrEmpty(req.QueryString["name"])))
                            Respond404(ctx);
                        else
                        {
                            if (!string.IsNullOrEmpty(req.QueryString["id"]))
                                response = await mediator.Send(new GetFilterRequest { FilterID = Convert.ToInt32(req.QueryString["id"]) });
                            else
                                response = await mediator.Send(new GetFilterRequest() { TagName = req.QueryString["name"] });
                            Respond(ctx, response);
                        }
                    }
                    else if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/api/filter/active"))
                    {
                        GenericResponse response = null;
                        if (!req.QueryString.HasKeys() || string.IsNullOrEmpty(req.QueryString["name"]))
                            Respond404(ctx);
                        else
                        {
                            response = await mediator.Send(new GetFilterRequest() { TagName = req.QueryString["name"], inactive = false });
                            Respond(ctx, response);
                        }
                    }
                    else if ((req.HttpMethod == "DELETE") && (req.Url.AbsolutePath == "/api/filter"))
                    {
                        int id = 0;
                        if (req.QueryString.HasKeys() && !string.IsNullOrEmpty(req.QueryString["id"]))
                            id = Convert.ToInt32(req.QueryString["id"]);
                        var response = await mediator.Send(new DeleteFilterRequest { FilterID = id });
                        Respond(ctx, response);
                    }
                    else if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/api/filter/add"))
                    {
                        var response = await mediator.Send(JsonSerializer.Deserialize<CreateFilterRequest>(requestBody, serializerOptions));
                        Respond(ctx, response);
                    }
                    else if ((req.HttpMethod == "PUT") && (req.Url.AbsolutePath == "/api/filter/toggleactive"))
                    {
                        var response = await mediator.Send(JsonSerializer.Deserialize<FilterToggleRequest>(requestBody, serializerOptions));
                        Respond(ctx, response);
                    }
                    else if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/api/report"))
                    {
                        var response = await mediator.Send(JsonSerializer.Deserialize<ReportRequest>(requestBody, serializerOptions));
                        Respond(ctx, response);
                    }
                    else if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/api/Summarize"))
                    {
                        var response = await mediator.Send(JsonSerializer.Deserialize<SummarizeRequest>(requestBody, serializerOptions));
                        Respond(ctx, response);
                    }
                    else if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/api/session/add"))
                    {
                        var response = await mediator.Send(JsonSerializer.Deserialize<SessionAddRequest>(requestBody, serializerOptions));
                        Respond(ctx, response);
                    }
                    else if ((req.HttpMethod == "DELETE") && (req.Url.AbsolutePath == "/api/session/remove"))
                    {
                        var response = await mediator.Send(JsonSerializer.Deserialize<SessionRemoveRequest>(requestBody, serializerOptions));
                        Respond(ctx, response);
                    }
                    else if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/api/tracking"))
                    {
                        var response = await mediator.Send(new GetSettingRequest { Setting = Core.Entities.SettingEnum.TrackingPaused });
                        Respond(ctx, response);
                    }
                    else if ((req.HttpMethod == "PUT") && (req.Url.AbsolutePath == "/api/tracking"))
                    {
                        if (!req.QueryString.HasKeys() || string.IsNullOrEmpty(req.QueryString["value"]))
                            Respond404(ctx);
                        else
                        {
                            var response = await mediator.Send(new AddUpdateSettingRequest() { Setting = Core.Entities.SettingEnum.TrackingPaused, value = req.QueryString["value"] });
                            Respond(ctx, response);
                        }
                    }
                    else
                        Respond404(ctx);
                }
                catch (Exception ex)
                {
                    Respond500(ctx);
                    _logger.LogError(ex, "An error occurred in the http server.");
                }
                finally
                {
                    // always close the stream
                    ctx.Response.OutputStream.Close();
                }
            }
        }

        private void Respond(HttpListenerContext ctx, GenericResponse response)
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.OK;
            ctx.Response.StatusDescription = ((HttpStatusCode)ctx.Response.StatusCode).ToString();

            var options = new JsonSerializerOptions { 
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                IncludeFields = true,
            };
            byte[] buf = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response, options));

            ctx.Response.ContentLength64 = buf.Length;
            ctx.Response.OutputStream.Write(buf, 0, buf.Length);
        }

        private void Respond404(HttpListenerContext ctx)
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.StatusDescription = "The server has not found anything matching the URI given.";
        }

        private void Respond500(HttpListenerContext ctx)
        {
            ctx.Response.StatusCode = 500;
            ctx.Response.StatusDescription = "The server encountered an unexpected condition which prevented it from fulfilling the request.";
        }
        #endregion
    }
}
