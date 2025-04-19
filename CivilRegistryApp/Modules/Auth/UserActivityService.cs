using CivilRegistryApp.Data;
using CivilRegistryApp.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CivilRegistryApp.Modules.Auth
{
    public class UserActivityService : IUserActivityService
    {
        private readonly AppDbContext _dbContext;
        private readonly IAuthenticationService _authService;

        public UserActivityService(AppDbContext dbContext, IAuthenticationService authService)
        {
            _dbContext = dbContext;
            _authService = authService;
        }

        public async Task LogActivityAsync(string activityType, string description, string? entityType = null, int? entityId = null)
        {
            try
            {
                if (_authService.CurrentUser == null)
                {
                    Log.Warning("Attempted to log activity without a logged-in user: {ActivityType}, {Description}",
                        activityType, description);
                    return;
                }

                // Ensure entityType is never null to satisfy the NOT NULL constraint
                if (string.IsNullOrEmpty(entityType))
                {
                    entityType = "General";
                }

                var activity = new UserActivity
                {
                    UserId = _authService.CurrentUser.UserId,
                    ActivityType = activityType,
                    Description = description,
                    IpAddress = GetLocalIPAddress(),
                    UserAgent = "CivilRegistryApp Desktop Client",
                    Timestamp = DateTime.Now,
                    EntityType = entityType,
                    EntityId = entityId
                };

                await _dbContext.UserActivities.AddAsync(activity);
                await _dbContext.SaveChangesAsync();

                Log.Information("User activity logged: {ActivityType} by {Username}",
                    activityType, _authService.CurrentUser.Username);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error logging user activity: {ActivityType}, {Description}",
                    activityType, description);
                // Don't throw - we don't want activity logging to break the application
            }
        }

        public async Task<IEnumerable<UserActivity>> GetUserActivitiesAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _dbContext.UserActivities
                    .Include(a => a.User)
                    .Where(a => a.UserId == userId);

                if (startDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp <= endDate.Value);
                }

                return await query.OrderByDescending(a => a.Timestamp).ToListAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving user activities for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<UserActivity>> GetAllActivitiesAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? activityType = null,
            string? username = null)
        {
            try
            {
                var query = _dbContext.UserActivities
                    .Include(a => a.User)
                    .AsQueryable();

                if (startDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp <= endDate.Value);
                }

                if (!string.IsNullOrEmpty(activityType))
                {
                    query = query.Where(a => a.ActivityType == activityType);
                }

                if (!string.IsNullOrEmpty(username))
                {
                    query = query.Where(a => a.User.Username.Contains(username));
                }

                // Re-include the User entity after all the filters
                query = query.Include(a => a.User);

                return await query.OrderByDescending(a => a.Timestamp).ToListAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving all user activities");
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetActivityTypesAsync()
        {
            try
            {
                return await _dbContext.UserActivities
                    .Select(a => a.ActivityType)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving activity types");
                throw;
            }
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return "127.0.0.1";
            }
            catch
            {
                return "Unknown";
            }
        }
    }

    public interface IUserActivityService
    {
        Task LogActivityAsync(string activityType, string description, string? entityType = null, int? entityId = null);
        Task<IEnumerable<UserActivity>> GetUserActivitiesAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<UserActivity>> GetAllActivitiesAsync(DateTime? startDate = null, DateTime? endDate = null, string? activityType = null, string? username = null);
        Task<IEnumerable<string>> GetActivityTypesAsync();
    }
}
