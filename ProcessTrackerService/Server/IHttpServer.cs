namespace ProcessTrackerService.Server
{
    public interface IHttpServer
    {
        Task Start(CancellationToken stoppingToken);
    }
}
