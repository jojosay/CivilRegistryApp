using System.Threading.Tasks;

namespace CivilRegistryApp.Modules.Reports
{
    public interface ISchedulerService
    {
        Task StartAsync();
        Task StopAsync();
        Task ScheduleReportAsync(int reportId);
        Task UnscheduleReportAsync(int reportId);
        Task RescheduleAllReportsAsync();
    }
}
