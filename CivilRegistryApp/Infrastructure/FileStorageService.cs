using Microsoft.Win32;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CivilRegistryApp.Infrastructure
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _baseStoragePath;

        public FileStorageService(string? baseStoragePath = null)
        {
            _baseStoragePath = baseStoragePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DocumentStorage");

            if (!Directory.Exists(_baseStoragePath))
                Directory.CreateDirectory(_baseStoragePath);
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string? documentType)
        {
            try
            {
                string docType = documentType ?? "Other";
                string documentTypePath = Path.Combine(_baseStoragePath, docType);

                if (!Directory.Exists(documentTypePath))
                    Directory.CreateDirectory(documentTypePath);

                string uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                string filePath = Path.Combine(documentTypePath, uniqueFileName);

                using (var fileStream2 = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await fileStream.CopyToAsync(fileStream2);
                }

                return filePath;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving file {FileName}", fileName);
                throw;
            }
        }

        public async Task<Stream?> GetFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Log.Warning("File not found: {FilePath}", filePath);
                    return null;
                }

                return new FileStream(filePath, FileMode.Open, FileAccess.Read);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving file {FilePath}", filePath);
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Log.Warning("File not found for deletion: {FilePath}", filePath);
                    return false;
                }

                File.Delete(filePath);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting file {FilePath}", filePath);
                throw;
            }
        }

        public string? OpenFileDialog(string filter = "All files (*.*)|*.*")
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = filter
            };

            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }

            return null;
        }

        public BitmapImage? LoadImageFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(filePath);
            image.EndInit();
            return image;
        }
    }

    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(Stream fileStream, string fileName, string? documentType);
        Task<Stream?> GetFileAsync(string filePath);
        Task<bool> DeleteFileAsync(string filePath);
        string? OpenFileDialog(string filter = "All files (*.*)|*.*");
        BitmapImage? LoadImageFromFile(string filePath);
    }
}
