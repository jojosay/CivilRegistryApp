using CivilRegistryApp.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CivilRegistryApp.Data.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            try
            {
                // Use AsNoTracking to avoid caching issues
                return await _dbSet
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == username);
            }
            catch (Exception ex)
            {
                // Log the error and return null
                Serilog.Log.Error(ex, "Error retrieving user by username {Username}", username);
                return null;
            }
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(string role)
        {
            try
            {
                return await _dbSet.Where(u => u.Role == role).ToListAsync();
            }
            catch (Exception ex)
            {
                // Log the error and return empty list
                Serilog.Log.Error(ex, "Error retrieving users by role {Role}", role);
                return new List<User>();
            }
        }

        public async Task<IEnumerable<string>> GetAllRolesAsync()
        {
            try
            {
                return await _dbSet.Select(u => u.Role).Distinct().ToListAsync();
            }
            catch (Exception ex)
            {
                // Log the error and return empty list
                Serilog.Log.Error(ex, "Error retrieving all roles");
                return new List<string>();
            }
        }

        public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return await GetAllAsync();

                searchTerm = searchTerm.ToLower();

                return await _dbSet
                    .Where(u =>
                        u.Username.ToLower().Contains(searchTerm) ||
                        u.FullName.ToLower().Contains(searchTerm) ||
                        (u.Email != null && u.Email.ToLower().Contains(searchTerm)) ||
                        (u.Department != null && u.Department.ToLower().Contains(searchTerm)) ||
                        (u.Position != null && u.Position.ToLower().Contains(searchTerm)))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // Log the error and return empty list
                Serilog.Log.Error(ex, "Error searching users with term {SearchTerm}", searchTerm);
                return new List<User>();
            }
        }

        public async Task<bool> DeactivateUserAsync(int userId)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null)
                    return false;

                user.IsActive = false;
                await UpdateAsync(user);
                await SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Log the error and return false
                Serilog.Log.Error(ex, "Error deactivating user with ID {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ActivateUserAsync(int userId)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null)
                    return false;

                user.IsActive = true;
                await UpdateAsync(user);
                await SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Log the error and return false
                Serilog.Log.Error(ex, "Error activating user with ID {UserId}", userId);
                return false;
            }
        }
    }

    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<IEnumerable<User>> GetUsersByRoleAsync(string role);
        Task<IEnumerable<string>> GetAllRolesAsync();
        Task<IEnumerable<User>> SearchUsersAsync(string searchTerm);
        Task<bool> DeactivateUserAsync(int userId);
        Task<bool> ActivateUserAsync(int userId);
    }
}
