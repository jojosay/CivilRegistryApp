using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Data.Repositories;
using CivilRegistryApp.Modules.Auth;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CivilRegistryApp.Modules.Admin
{
    public class FieldConfigurationService : IFieldConfigurationService
    {
        private readonly IFieldConfigurationRepository _fieldConfigRepository;
        private readonly IAuthenticationService _authService;
        private readonly IUserActivityService _userActivityService;

        public FieldConfigurationService(
            IFieldConfigurationRepository fieldConfigRepository,
            IAuthenticationService authService,
            IUserActivityService userActivityService)
        {
            _fieldConfigRepository = fieldConfigRepository;
            _authService = authService;
            _userActivityService = userActivityService;
        }

        public async Task<IEnumerable<FieldConfiguration>> GetAllFieldConfigurationsAsync()
        {
            try
            {
                return await _fieldConfigRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting all field configurations");
                throw;
            }
        }

        public async Task<IEnumerable<FieldConfiguration>> GetFieldConfigurationsByDocumentTypeAsync(string documentType)
        {
            try
            {
                return await _fieldConfigRepository.GetByDocumentTypeAsync(documentType);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting field configurations for document type {DocumentType}", documentType);
                throw;
            }
        }

        public async Task<FieldConfiguration> GetFieldConfigurationAsync(int id)
        {
            try
            {
                return await _fieldConfigRepository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting field configuration with ID {Id}", id);
                throw;
            }
        }

        public async Task<FieldConfiguration> GetFieldConfigurationAsync(string documentType, string fieldName)
        {
            try
            {
                return await _fieldConfigRepository.GetByDocumentTypeAndFieldNameAsync(documentType, fieldName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting field configuration for document type {DocumentType} and field {FieldName}", documentType, fieldName);
                throw;
            }
        }

        public async Task<FieldConfiguration> CreateFieldConfigurationAsync(FieldConfiguration fieldConfiguration)
        {
            try
            {
                if (_authService.CurrentUser == null)
                    throw new UnauthorizedAccessException("User must be logged in to create field configurations");

                // Set created by
                fieldConfiguration.CreatedBy = _authService.CurrentUser.UserId;
                fieldConfiguration.CreatedAt = DateTime.Now;

                await _fieldConfigRepository.AddAsync(fieldConfiguration);
                await _fieldConfigRepository.SaveChangesAsync();

                Log.Information("Field configuration created for document type {DocumentType}, field {FieldName} by user {Username}",
                    fieldConfiguration.DocumentType, fieldConfiguration.FieldName, _authService.CurrentUser.Username);

                return fieldConfiguration;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating field configuration for document type {DocumentType}, field {FieldName}",
                    fieldConfiguration.DocumentType, fieldConfiguration.FieldName);
                throw;
            }
        }

        public async Task<FieldConfiguration> UpdateFieldConfigurationAsync(FieldConfiguration fieldConfiguration)
        {
            try
            {
                if (_authService.CurrentUser == null)
                    throw new UnauthorizedAccessException("User must be logged in to update field configurations");

                var existingConfig = await _fieldConfigRepository.GetByIdAsync(fieldConfiguration.FieldConfigurationId);
                if (existingConfig == null)
                    throw new KeyNotFoundException($"Field configuration with ID {fieldConfiguration.FieldConfigurationId} not found");

                // Update properties
                existingConfig.DisplayName = fieldConfiguration.DisplayName;
                existingConfig.Description = fieldConfiguration.Description;
                existingConfig.IsRequired = fieldConfiguration.IsRequired;
                existingConfig.DisplayOrder = fieldConfiguration.DisplayOrder;
                existingConfig.UpdatedAt = DateTime.Now;
                existingConfig.UpdatedBy = _authService.CurrentUser.UserId;

                await _fieldConfigRepository.UpdateAsync(existingConfig);
                await _fieldConfigRepository.SaveChangesAsync();

                // Log the activity with EntityType
                await _userActivityService.LogActivityAsync(
                    "Admin",
                    $"Updated field configuration for {existingConfig.DocumentType}, field {existingConfig.FieldName}",
                    "FieldConfiguration",
                    existingConfig.FieldConfigurationId);

                Log.Information("Field configuration updated for document type {DocumentType}, field {FieldName} by user {Username}",
                    existingConfig.DocumentType, existingConfig.FieldName, _authService.CurrentUser.Username);

                return existingConfig;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating field configuration with ID {Id}", fieldConfiguration.FieldConfigurationId);
                throw;
            }
        }

        public async Task<bool> DeleteFieldConfigurationAsync(int id)
        {
            try
            {
                if (_authService.CurrentUser == null)
                    throw new UnauthorizedAccessException("User must be logged in to delete field configurations");

                var fieldConfiguration = await _fieldConfigRepository.GetByIdAsync(id);
                if (fieldConfiguration == null)
                    return false;

                await _fieldConfigRepository.DeleteAsync(fieldConfiguration);
                await _fieldConfigRepository.SaveChangesAsync();

                // Log the activity with EntityType
                await _userActivityService.LogActivityAsync(
                    "Admin",
                    $"Deleted field configuration for {fieldConfiguration.DocumentType}, field {fieldConfiguration.FieldName}",
                    "FieldConfiguration");

                Log.Information("Field configuration deleted for document type {DocumentType}, field {FieldName} by user {Username}",
                    fieldConfiguration.DocumentType, fieldConfiguration.FieldName, _authService.CurrentUser.Username);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting field configuration with ID {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetRequiredFieldsAsync(string documentType)
        {
            try
            {
                return await _fieldConfigRepository.GetRequiredFieldsAsync(documentType);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting required fields for document type {DocumentType}", documentType);
                throw;
            }
        }

        public async Task<Dictionary<string, string>> ValidateDocumentAsync(Document document)
        {
            try
            {
                var errors = new Dictionary<string, string>();
                var requiredFields = await GetRequiredFieldsAsync(document.DocumentType);

                // Get all properties of the Document class
                var documentProperties = typeof(Document).GetProperties();

                foreach (var fieldName in requiredFields)
                {
                    // Find the property with the matching name
                    var property = documentProperties.FirstOrDefault(p => p.Name == fieldName);
                    if (property != null)
                    {
                        // Get the value of the property
                        var value = property.GetValue(document);

                        // Check if the value is null or empty
                        if (value == null || (value is string stringValue && string.IsNullOrWhiteSpace(stringValue)))
                        {
                            // Get the field configuration to get the display name
                            var fieldConfig = await GetFieldConfigurationAsync(document.DocumentType, fieldName);
                            var displayName = fieldConfig?.DisplayName ?? fieldName;

                            errors.Add(fieldName, $"{displayName} is required.");
                        }
                    }
                }

                return errors;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error validating document against field configuration");
                throw;
            }
        }

        public async Task<bool> InitializeDefaultConfigurationsAsync()
        {
            try
            {
                // Check if configurations already exist
                var existingConfigs = await _fieldConfigRepository.GetAllAsync();
                if (existingConfigs.Any())
                {
                    Log.Information("Field configurations already exist. Skipping initialization.");
                    return false;
                }

                if (_authService.CurrentUser == null)
                {
                    Log.Warning("No user is logged in. Using system user for field configuration initialization.");
                    // Try to get admin user from database
                    // This is a fallback for initialization during application startup
                    var adminUserId = 1; // Assuming the admin user has ID 1

                    await CreateDefaultConfigurationsAsync(adminUserId);
                }
                else
                {
                    await CreateDefaultConfigurationsAsync(_authService.CurrentUser.UserId);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing default field configurations");
                throw;
            }
        }

        private async Task CreateDefaultConfigurationsAsync(int userId)
        {
            // Define document types
            var documentTypes = new[] { "Birth Certificate", "Marriage Certificate", "Death Certificate", "CENOMAR" };

            // Define common fields for all document types
            var commonFields = new List<(string FieldName, string DisplayName, bool IsRequired, string Description)>
            {
                ("DocumentType", "Document Type", true, "Type of document (Birth, Marriage, Death, CENOMAR)"),
                ("RegistryOffice", "Registry Office", true, "Office where the document is registered"),
                ("CertificateNumber", "Certificate Number", true, "Unique certificate number"),
                ("DateOfEvent", "Date of Event", true, "Date of birth, marriage, or death"),
                ("RegistrationDate", "Registration Date", true, "Date when the document was registered"),
                ("Barangay", "Barangay", false, "Barangay where the event occurred"),
                ("CityMunicipality", "City/Municipality", true, "City or municipality where the event occurred"),
                ("Province", "Province", true, "Province where the event occurred"),
                ("Remarks", "Remarks", false, "Additional remarks or notes")
            };

            // Define document-specific fields
            var documentSpecificFields = new Dictionary<string, List<(string FieldName, string DisplayName, bool IsRequired, string Description)>>
            {
                ["Birth Certificate"] = new List<(string, string, bool, string)>
                {
                    ("Prefix", "Prefix", false, "Name prefix (e.g., Mr., Mrs., Dr.)"),
                    ("GivenName", "Given Name", true, "First name of the person"),
                    ("MiddleName", "Middle Name", false, "Middle name of the person"),
                    ("FamilyName", "Family Name", true, "Last name of the person"),
                    ("Suffix", "Suffix", false, "Name suffix (e.g., Jr., Sr., III)"),
                    ("Gender", "Gender", true, "Gender of the person (M/F)"),
                    ("FatherGivenName", "Father's Given Name", false, "Father's first name"),
                    ("FatherMiddleName", "Father's Middle Name", false, "Father's middle name"),
                    ("FatherFamilyName", "Father's Family Name", false, "Father's last name"),
                    ("MotherMaidenGiven", "Mother's Maiden Given Name", true, "Mother's first name"),
                    ("MotherMaidenMiddleName", "Mother's Maiden Middle Name", false, "Mother's middle name"),
                    ("MotherMaidenFamily", "Mother's Maiden Family Name", true, "Mother's last name"),
                    ("AttendantName", "Attendant Name", false, "Name of the birth attendant"),
                    ("RegistryBook", "Registry Book", false, "Registry book number"),
                    ("VolumeNumber", "Volume Number", false, "Volume number in the registry"),
                    ("PageNumber", "Page Number", false, "Page number in the registry"),
                    ("LineNumber", "Line Number", false, "Line number in the registry")
                },
                ["Marriage Certificate"] = new List<(string, string, bool, string)>
                {
                    ("HusbandGivenName", "Husband's Given Name", true, "Husband's first name"),
                    ("HusbandMiddleName", "Husband's Middle Name", false, "Husband's middle name"),
                    ("HusbandFamilyName", "Husband's Family Name", true, "Husband's last name"),
                    ("WifeGivenName", "Wife's Given Name", true, "Wife's first name"),
                    ("WifeMiddleName", "Wife's Middle Name", false, "Wife's middle name"),
                    ("WifeFamilyName", "Wife's Family Name", true, "Wife's last name"),
                    ("PlaceOfMarriage", "Place of Marriage", true, "Place where the marriage was solemnized"),
                    ("SolemnizingOfficer", "Solemnizing Officer", true, "Name of the person who solemnized the marriage"),
                    ("RegistryBook", "Registry Book", false, "Registry book number"),
                    ("VolumeNumber", "Volume Number", false, "Volume number in the registry"),
                    ("PageNumber", "Page Number", false, "Page number in the registry"),
                    ("LineNumber", "Line Number", false, "Line number in the registry")
                },
                ["Death Certificate"] = new List<(string, string, bool, string)>
                {
                    ("Prefix", "Prefix", false, "Name prefix (e.g., Mr., Mrs., Dr.)"),
                    ("GivenName", "Given Name", true, "First name of the deceased"),
                    ("MiddleName", "Middle Name", false, "Middle name of the deceased"),
                    ("FamilyName", "Family Name", true, "Last name of the deceased"),
                    ("Suffix", "Suffix", false, "Name suffix (e.g., Jr., Sr., III)"),
                    ("Gender", "Gender", true, "Gender of the deceased (M/F)"),
                    ("CauseOfDeath", "Cause of Death", true, "Primary cause of death"),
                    ("PlaceOfDeath", "Place of Death", true, "Place where the death occurred"),
                    ("RegistryBook", "Registry Book", false, "Registry book number"),
                    ("VolumeNumber", "Volume Number", false, "Volume number in the registry"),
                    ("PageNumber", "Page Number", false, "Page number in the registry"),
                    ("LineNumber", "Line Number", false, "Line number in the registry")
                },
                ["CENOMAR"] = new List<(string, string, bool, string)>
                {
                    ("Prefix", "Prefix", false, "Name prefix (e.g., Mr., Mrs., Dr.)"),
                    ("GivenName", "Given Name", true, "First name of the person"),
                    ("MiddleName", "Middle Name", false, "Middle name of the person"),
                    ("FamilyName", "Family Name", true, "Last name of the person"),
                    ("Suffix", "Suffix", false, "Name suffix (e.g., Jr., Sr., III)"),
                    ("Gender", "Gender", true, "Gender of the person (M/F)"),
                    ("DateOfBirth", "Date of Birth", true, "Date of birth of the person"),
                    ("PlaceOfBirth", "Place of Birth", true, "Place of birth of the person"),
                    ("Purpose", "Purpose", true, "Purpose for requesting CENOMAR"),
                    ("RegistryBook", "Registry Book", false, "Registry book number"),
                    ("VolumeNumber", "Volume Number", false, "Volume number in the registry"),
                    ("PageNumber", "Page Number", false, "Page number in the registry"),
                    ("LineNumber", "Line Number", false, "Line number in the registry")
                }
            };

            // Create configurations for each document type
            foreach (var documentType in documentTypes)
            {
                // Add common fields
                int displayOrder = 1;
                foreach (var field in commonFields)
                {
                    await _fieldConfigRepository.AddAsync(new FieldConfiguration
                    {
                        DocumentType = documentType,
                        FieldName = field.FieldName,
                        DisplayName = field.DisplayName,
                        IsRequired = field.IsRequired,
                        Description = field.Description,
                        DisplayOrder = displayOrder++,
                        CreatedBy = userId,
                        CreatedAt = DateTime.Now
                    });
                }

                // Add document-specific fields
                if (documentSpecificFields.ContainsKey(documentType))
                {
                    foreach (var field in documentSpecificFields[documentType])
                    {
                        await _fieldConfigRepository.AddAsync(new FieldConfiguration
                        {
                            DocumentType = documentType,
                            FieldName = field.FieldName,
                            DisplayName = field.DisplayName,
                            IsRequired = field.IsRequired,
                            Description = field.Description,
                            DisplayOrder = displayOrder++,
                            CreatedBy = userId,
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }

            await _fieldConfigRepository.SaveChangesAsync();

            // Log the activity with EntityType if a user is logged in
            if (_authService.CurrentUser != null)
            {
                await _userActivityService.LogActivityAsync(
                    "Admin",
                    "Created default field configurations for all document types",
                    "FieldConfiguration");
            }

            Log.Information("Default field configurations created for all document types");
        }
    }
}
