using MediatR;
using ProcessTrackerService.Core.Dto.Requests;
using ProcessTrackerService.Core.Helpers;
using ProcessTrackerService.Server;

namespace ProcessTrackerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHttpServer _httpServer;
        private readonly IMediator _mediator;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public Worker(ILogger<Worker> logger, IHttpServer httpServer, IMediator mediator, IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _httpServer = httpServer;
            _mediator = mediator;
            _configuration = configuration;
            _serviceProvider = serviceProvider;

            // Register the async method to be called on application stopping
            //_hostApplicationLifetime.ApplicationStopping.Register(OnStoppingAsync);
        }

        private AppSettings AppSettings
        {
            get
            {
                return _configuration.GetSection("AppSettings").Get<AppSettings>();
            }
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _ = Task.Run(async () => await StartServer(stoppingToken), stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Checking processes at: {time}", DateTimeOffset.Now);

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                        await mediator.Send(new TrackProcessRequest());
                    }

                    await Task.Delay(AppSettings.ProcessCheckDelay * 1000, stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Task was canceled.");
                // Handle cleanup here if necessary
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in the background worker.");
                // Handle other exceptions here
            }
            finally
            {
                _logger.LogInformation("Worker has stopped.");
            }
        }
        //private void OnStoppingAsync()
        //{
        //    _logger.LogInformation("Worker service is stopping at: {time}", DateTimeOffset.Now);
        //}
        private async Task StartServer(CancellationToken stoppingToken)
        {
            try
            {
                await _httpServer.Start(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Http Task was canceled.");
                // Handle cleanup here if necessary
                Task.Run(async () => await _mediator.Send(new TrackProcessRequest())).GetAwaiter().GetResult();
                //_mediator.Send(new TrackProcessRequest()).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in the http server.");
            }
        }
    }
}
