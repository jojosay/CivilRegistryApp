using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Data.Repositories;
using Quartz;
using Quartz.Impl;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivilRegistryApp.Modules.Reports
{
    public class SchedulerService : ISchedulerService
    {
        private readonly IReportRepository _reportRepository;
        private readonly IReportService _reportService;
        private IScheduler _scheduler = null!;

        public SchedulerService(IReportRepository reportRepository, IReportService reportService)
        {
            _reportRepository = reportRepository;
            _reportService = reportService;
        }

        public async Task StartAsync()
        {
            try
            {
                Log.Information("Starting scheduler service");

                // Create scheduler factory
                var factory = new StdSchedulerFactory();
                _scheduler = await factory.GetScheduler();

                // Start scheduler
                await _scheduler.Start();

                // Schedule all active reports
                await RescheduleAllReportsAsync();

                Log.Information("Scheduler service started successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error starting scheduler service");
                throw;
            }
        }

        public async Task StopAsync()
        {
            try
            {
                if (_scheduler != null)
                {
                    Log.Information("Stopping scheduler service");
                    await _scheduler.Shutdown();
                    Log.Information("Scheduler service stopped successfully");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error stopping scheduler service");
                throw;
            }
        }

        public async Task ScheduleReportAsync(int reportId)
        {
            try
            {
                var report = await _reportRepository.GetScheduledReportWithDetailsAsync(reportId);
                if (report == null || !report.IsActive)
                {
                    Log.Warning("Cannot schedule report {ReportId} - report not found or inactive", reportId);
                    return;
                }

                // Create job
                var jobKey = new JobKey($"ReportJob_{reportId}");
                var job = JobBuilder.Create<ReportJob>()
                    .WithIdentity(jobKey)
                    .UsingJobData("reportId", reportId)
                    .Build();

                // Create trigger with cron schedule
                var triggerKey = new TriggerKey($"ReportTrigger_{reportId}");
                var trigger = TriggerBuilder.Create()
                    .WithIdentity(triggerKey)
                    .WithCronSchedule(report.Schedule)
                    .Build();

                // Schedule job
                if (await _scheduler.CheckExists(jobKey))
                {
                    await _scheduler.DeleteJob(jobKey);
                }

                await _scheduler.ScheduleJob(job, trigger);
                Log.Information("Scheduled report {ReportName} with ID {ReportId} using cron expression {CronExpression}",
                    report.Name, reportId, report.Schedule);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error scheduling report {ReportId}", reportId);
                throw;
            }
        }

        public async Task UnscheduleReportAsync(int reportId)
        {
            try
            {
                var jobKey = new JobKey($"ReportJob_{reportId}");
                if (await _scheduler.CheckExists(jobKey))
                {
                    await _scheduler.DeleteJob(jobKey);
                    Log.Information("Unscheduled report with ID {ReportId}", reportId);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error unscheduling report {ReportId}", reportId);
                throw;
            }
        }

        public async Task RescheduleAllReportsAsync()
        {
            try
            {
                Log.Information("Rescheduling all active reports");

                // Get all active scheduled reports
                var reports = await _reportRepository.GetActiveScheduledReportsAsync();

                // Schedule each report
                foreach (var report in reports)
                {
                    await ScheduleReportAsync(report.ScheduledReportId);
                }

                Log.Information("Rescheduled all active reports successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error rescheduling all reports");
                throw;
            }
        }
    }

    // Job class that will be executed by the scheduler
    public class ReportJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                // Get report ID from job data
                int reportId = context.JobDetail.JobDataMap.GetInt("reportId");
                Log.Information("Executing scheduled report job for report ID {ReportId}", reportId);

                // Get report service from scheduler context
                var reportService = (IReportService)context.Scheduler.Context.Get("reportService");

                // Run the report
                string fileName = await reportService.RunScheduledReportAsync(reportId);
                Log.Information("Successfully generated scheduled report {ReportId}, file: {FileName}", reportId, fileName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing scheduled report job");
                throw;
            }
        }
    }
}
