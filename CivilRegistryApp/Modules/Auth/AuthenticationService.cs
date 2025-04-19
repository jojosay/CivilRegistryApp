using CivilRegistryApp.Data;
using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;

namespace CivilRegistryApp.Modules.Auth
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly AppDbContext _dbContext;
        private User? _currentUser;

        public AuthenticationService(IUserRepository userRepository, AppDbContext dbContext)
        {
            _userRepository = userRepository;
            _dbContext = dbContext;
        }

        public User? CurrentUser => _currentUser;

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                // Special case for admin login with hardcoded credentials
                if (username.ToLower() == "admin" && password == "admin123")
                {
                    // Try to get the admin user first
                    var adminUser = await _userRepository.GetByUsernameAsync("admin");

                    // If admin user doesn't exist, create it
                    if (adminUser == null)
                    {
                        Log.Information("Admin user not found. Creating default admin user.");
                        await CreateDefaultAdminUser();
                        adminUser = await _userRepository.GetByUsernameAsync("admin");

                        if (adminUser == null)
                        {
                            Log.Error("Failed to create admin user.");
                            return false;
                        }
                    }
                    else
                    {
                        // Update admin's password hash to match admin123 if needed
                        string bcryptHash = BCrypt.Net.BCrypt.HashPassword("admin123");
                        adminUser.PasswordHash = Encoding.UTF8.GetBytes(bcryptHash);
                        await _userRepository.UpdateAsync(adminUser);
                        await _userRepository.SaveChangesAsync();
                    }

                    try
                    {
                        // Update last login time
                        adminUser.LastLoginAt = DateTime.Now;
                        await _userRepository.UpdateAsync(adminUser);
                        await _userRepository.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue with login
                        Log.Warning(ex, "Error updating last login time for admin user");
                    }

                    _currentUser = adminUser;
                    Log.Information("Admin user logged in successfully");
                    return true;
                }

                // Normal login flow for non-admin users
                var user = await _userRepository.GetByUsernameAsync(username);

                if (user == null)
                {
                    Log.Information("Login failed: User {Username} not found", username);
                    return false;
                }

                if (user.PasswordHash != null && VerifyPassword(password, user.PasswordHash))
                {
                    try
                    {
                        // Update last login time
                        user.LastLoginAt = DateTime.Now;
                        await _userRepository.UpdateAsync(user);
                        await _userRepository.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue with login
                        Log.Warning(ex, "Error updating last login time for user {Username}", username);
                    }

                    _currentUser = user;
                    Log.Information("User {Username} logged in successfully", username);
                    return true;
                }

                Log.Information("Login failed: Invalid password for user {Username}", username);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during login for user {Username}", username);
                // Return false instead of throwing to allow the application to continue
                return false;
            }
        }

        private async Task<bool> CreateDefaultAdminUser()
        {
            try
            {
                // Create a default admin user with hardcoded credentials
                string bcryptHash = BCrypt.Net.BCrypt.HashPassword("admin123");

                var adminUser = new User
                {
                    Username = "admin",
                    PasswordHash = Encoding.UTF8.GetBytes(bcryptHash),
                    FullName = "System Administrator",
                    Role = "Admin",
                    Email = "admin@example.com",
                    CreatedAt = DateTime.Now,
                    LastPasswordChangeAt = DateTime.Now,
                    IsActive = true,
                    CanAddDocuments = true,
                    CanEditDocuments = true,
                    CanDeleteDocuments = true,
                    CanViewRequests = true,
                    CanProcessRequests = true,
                    CanManageUsers = true,
                    PhoneNumber = "",
                    Position = "",
                    Department = "",
                    ProfilePicturePath = ""
                };

                await _userRepository.AddAsync(adminUser);
                await _userRepository.SaveChangesAsync();

                Log.Information("Default admin user created successfully");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating default admin user");
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            string username = _currentUser?.Username;
            int? userId = _currentUser?.UserId;

            _currentUser = null;
            Log.Information("User {Username} logged out", username);
        }

        public async Task<bool> RegisterAsync(string username, string password, string fullName, string role,
            string? email = null, string? phoneNumber = null, string? position = null, string? department = null)
        {
            try
            {
                // Special case for admin user with hardcoded credentials
                if (username.ToLower() == "admin" && password == "admin123")
                {
                    return await CreateDefaultAdminUser();
                }

                var existingUser = await _userRepository.GetByUsernameAsync(username);

                if (existingUser != null)
                {
                    Log.Information("Registration failed: Username {Username} already exists", username);
                    return false;
                }

                var passwordHash = HashPassword(password);

                var newUser = new User
                {
                    Username = username,
                    PasswordHash = passwordHash,
                    FullName = fullName,
                    Role = role,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    Position = position,
                    Department = department,
                    ProfilePicturePath = string.Empty,
                    CreatedAt = DateTime.Now,
                    LastPasswordChangeAt = DateTime.Now,
                    IsActive = true
                };

                // Set permissions based on role
                newUser.SetPermissionsByRole();

                await _userRepository.AddAsync(newUser);
                await _userRepository.SaveChangesAsync();

                Log.Information("User {Username} registered successfully with role {Role}", username, role);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during registration for user {Username}", username);
                return false; // Return false instead of throwing to allow the application to continue
            }
        }

        public bool IsUserInRole(string role)
        {
            return _currentUser != null && _currentUser.Role == role;
        }

        public bool HasPermission(string permission)
        {
            if (_currentUser == null)
                return false;

            return permission switch
            {
                "AddDocuments" => _currentUser.CanAddDocuments,
                "EditDocuments" => _currentUser.CanEditDocuments,
                "DeleteDocuments" => _currentUser.CanDeleteDocuments,
                "ViewRequests" => _currentUser.CanViewRequests,
                "ProcessRequests" => _currentUser.CanProcessRequests,
                "ManageUsers" => _currentUser.CanManageUsers,
                _ => false
            };
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);

                if (user == null)
                {
                    Log.Information("Password change failed: User ID {UserId} not found", userId);
                    return false;
                }

                // Verify current password
                if (user.PasswordHash == null || !VerifyPassword(currentPassword, user.PasswordHash))
                {
                    Log.Information("Password change failed: Current password is incorrect for user {Username}", user.Username);
                    return false;
                }

                // Update password
                user.PasswordHash = HashPassword(newPassword);
                user.LastPasswordChangeAt = DateTime.Now;
                user.LastUpdatedAt = DateTime.Now;

                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();

                Log.Information("Password changed successfully for user {Username}", user.Username);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during password change for user ID {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpdateUserProfileAsync(User updatedUser)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(updatedUser.UserId);

                if (user == null)
                {
                    Log.Information("Profile update failed: User ID {UserId} not found", updatedUser.UserId);
                    return false;
                }

                // Update user properties
                user.FullName = updatedUser.FullName;
                user.Email = updatedUser.Email;
                user.PhoneNumber = updatedUser.PhoneNumber;
                user.Position = updatedUser.Position;
                user.Department = updatedUser.Department;
                user.ProfilePicturePath = updatedUser.ProfilePicturePath;
                user.LastUpdatedAt = DateTime.Now;

                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();

                // Update current user if it's the same user
                if (_currentUser != null && _currentUser.UserId == user.UserId)
                {
                    _currentUser = user;
                }

                Log.Information("Profile updated successfully for user {Username}", user.Username);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during profile update for user ID {UserId}", updatedUser.UserId);
                throw;
            }
        }

        private byte[] HashPassword(string password)
        {
            // Use BCrypt for password hashing
            string bcryptHash = BCrypt.Net.BCrypt.HashPassword(password);
            return Encoding.UTF8.GetBytes(bcryptHash);
        }

        private bool VerifyPassword(string password, byte[] storedHash)
        {
            try
            {
                // First try BCrypt verification
                string storedHashString = Encoding.UTF8.GetString(storedHash);
                if (BCrypt.Net.BCrypt.Verify(password, storedHashString))
                {
                    return true;
                }

                // Fallback to SHA256 for backward compatibility with older accounts
                using (var sha256 = SHA256.Create())
                {
                    var computedHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

                    // Compare byte by byte
                    if (storedHash.Length == computedHash.Length)
                    {
                        bool allMatch = true;
                        for (int i = 0; i < computedHash.Length; i++)
                        {
                            if (computedHash[i] != storedHash[i])
                            {
                                allMatch = false;
                                break;
                            }
                        }
                        if (allMatch) return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error verifying password");
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);

                if (user == null)
                {
                    Log.Information("Password reset failed: User ID {UserId} not found", userId);
                    return false;
                }

                // Update password
                user.PasswordHash = HashPassword(newPassword);
                user.LastPasswordChangeAt = DateTime.Now;
                user.LastUpdatedAt = DateTime.Now;

                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();

                Log.Information("Password reset successfully for user {Username}", user.Username);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during password reset for user ID {UserId}", userId);
                throw;
            }
        }
    }

    public interface IAuthenticationService
    {
        User? CurrentUser { get; }
        Task<bool> LoginAsync(string username, string password);
        Task LogoutAsync();
        Task<bool> RegisterAsync(string username, string password, string fullName, string role,
            string? email = null, string? phoneNumber = null, string? position = null, string? department = null);
        bool IsUserInRole(string role);
        bool HasPermission(string permission);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<bool> ResetPasswordAsync(int userId, string newPassword);
        Task<bool> UpdateUserProfileAsync(User updatedUser);
    }
}
