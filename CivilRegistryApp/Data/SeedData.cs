using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CivilRegistryApp.Data
{
    public static class SeedData
    {
        public static async Task SeedDocumentsAsync(AppDbContext dbContext)
        {
            try
            {
                Log.Information("Checking if document seeding is required...");

                // Only seed if there are no documents in the database
                if (!await dbContext.Documents.AnyAsync())
                {
                    Log.Information("No documents found in database. Starting document seeding...");

                    // Get admin user for document ownership
                    var adminUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                    if (adminUser == null)
                    {
                        Log.Warning("No admin user found for document seeding. Creating a default admin user.");

                        // Create a default admin user if none exists
                        adminUser = new User
                        {
                            Username = "admin",
                            PasswordHash = System.Text.Encoding.UTF8.GetBytes(BCrypt.Net.BCrypt.HashPassword("admin123")),
                            FullName = "System Administrator",
                            Role = "Admin",
                            Email = "admin@example.com",
                            PhoneNumber = "",
                            Position = "Administrator",
                            Department = "IT Department",
                            ProfilePicturePath = "",
                            CreatedAt = DateTime.Now,
                            LastPasswordChangeAt = DateTime.Now,
                            IsActive = true,
                            CanAddDocuments = true,
                            CanEditDocuments = true,
                            CanDeleteDocuments = true,
                            CanViewRequests = true,
                            CanProcessRequests = true,
                            CanManageUsers = true
                        };

                        dbContext.Users.Add(adminUser);
                        await dbContext.SaveChangesAsync();
                    }

                    // Create sample documents
                    var documents = GenerateSampleDocuments(adminUser.UserId);

                    // Add documents to database
                    await dbContext.Documents.AddRangeAsync(documents);
                    await dbContext.SaveChangesAsync();

                    Log.Information("Successfully seeded {Count} documents", documents.Count);
                }
                else
                {
                    Log.Information("Documents already exist in database. Skipping document seeding.");
                }
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "SeedData.SeedDocumentsAsync");
                Log.Error(ex, "Error seeding documents");
            }
        }

        private static List<Document> GenerateSampleDocuments(int adminUserId)
        {
            var documents = new List<Document>();
            var random = new Random();

            // Sample data for generating realistic documents
            var documentTypes = new[] { "Birth Certificate", "Marriage Certificate", "Death Certificate", "CENOMAR" };
            var registryOffices = new[] {
                "Manila City Civil Registry",
                "Quezon City Civil Registry",
                "Cebu City Civil Registry",
                "Davao City Civil Registry",
                "Makati City Civil Registry",
                "Pasig City Civil Registry",
                "Taguig City Civil Registry",
                "Caloocan City Civil Registry"
            };
            var barangays = new[] {
                "Barangay 123",
                "Barangay Poblacion",
                "Barangay San Antonio",
                "Barangay San Roque",
                "Barangay Santa Cruz",
                "Barangay Bagong Silang",
                "Barangay Pinagkaisahan",
                "Barangay Commonwealth"
            };
            var cities = new[] {
                "Manila",
                "Quezon City",
                "Cebu City",
                "Davao City",
                "Makati City",
                "Pasig City",
                "Taguig City",
                "Caloocan City",
                "Baguio City",
                "Iloilo City"
            };
            var provinces = new[] {
                "Metro Manila",
                "Cebu",
                "Davao",
                "Rizal",
                "Laguna",
                "Batangas",
                "Cavite",
                "Pampanga",
                "Bulacan",
                "Iloilo"
            };
            var prefixes = new[] { "Mr.", "Mrs.", "Ms.", "", "Dr.", "Engr.", "Atty." };
            var suffixes = new[] { "Jr.", "Sr.", "III", "", "II", "IV", "V", "MD", "PhD" };
            var maleFirstNames = new[] {
                "Juan", "Miguel", "Carlos", "Antonio", "Jose", "Roberto", "Eduardo", "Francisco", "Luis", "Rafael",
                "Ricardo", "Manuel", "Alejandro", "Fernando", "Javier", "Enrique", "Diego", "Pablo", "Andres", "Gabriel"
            };
            var femaleFirstNames = new[] {
                "Maria", "Ana", "Sofia", "Isabella", "Gabriela", "Camila", "Victoria", "Valentina", "Mariana", "Lucia",
                "Elena", "Carmen", "Beatriz", "Patricia", "Daniela", "Natalia", "Catalina", "Adriana", "Valeria", "Regina"
            };
            var lastNames = new[] {
                "Garcia", "Santos", "Reyes", "Ramos", "Gonzales", "Cruz", "Mendoza", "Torres", "Flores", "Fernandez",
                "Perez", "Diaz", "Gomez", "Aquino", "Villanueva", "Tan", "Lim", "Bautista", "Pascual", "Robles"
            };
            var middleNames = new[] {
                "De la Cruz", "Del Rosario", "San Jose", "De Guzman", "De Leon", "De Vera", "De Castro", "De Jesus", "San Pedro", "De los Santos",
                "Dela Paz", "Del Mundo", "San Miguel", "Del Pilar", "Del Valle", "De los Reyes", "De Dios", "De Asis", "San Agustin", "De Ocampo"
            };

            // Ensure we have a good mix of document types (approximately equal distribution)
            int docsPerType = 30 / documentTypes.Length; // 7 or 8 docs per type
            int remainder = 30 % documentTypes.Length;   // Handle any remainder

            foreach (var documentType in documentTypes)
            {
                // Calculate how many documents of this type to create
                int docsToCreate = docsPerType + (remainder > 0 ? 1 : 0);
                if (remainder > 0) remainder--;

                for (int i = 0; i < docsToCreate; i++)
                {
                    // Generate random dates within the last 10 years
                    var currentDate = DateTime.Now;
                    var randomDays = random.Next(1, 365 * 10); // Up to 10 years ago
                    var dateOfEvent = currentDate.AddDays(-randomDays);
                    var registrationDate = dateOfEvent.AddDays(random.Next(1, 60)); // Registration 1-60 days after event
                    var uploadedAt = registrationDate.AddDays(random.Next(1, 365 * 3)); // Upload date after registration

                    // Determine gender (for birth and death certificates)
                    var gender = random.Next(2) == 0 ? "M" : "F";

                    // Generate name based on gender
                    var firstName = gender == "M"
                        ? maleFirstNames[random.Next(maleFirstNames.Length)]
                        : femaleFirstNames[random.Next(femaleFirstNames.Length)];
                    var middleName = middleNames[random.Next(middleNames.Length)];
                    var lastName = lastNames[random.Next(lastNames.Length)];
                    var prefix = prefixes[random.Next(prefixes.Length)];
                    var suffix = suffixes[random.Next(suffixes.Length)];

                    // Generate certificate number with more realistic format
                    string certificateNumber;
                    switch (documentType)
                    {
                        case "Birth Certificate":
                            certificateNumber = $"BC-{random.Next(1000, 9999)}-{random.Next(10000, 99999)}";
                            break;
                        case "Marriage Certificate":
                            certificateNumber = $"MC-{random.Next(1000, 9999)}-{random.Next(10000, 99999)}";
                            break;
                        case "Death Certificate":
                            certificateNumber = $"DC-{random.Next(1000, 9999)}-{random.Next(10000, 99999)}";
                            break;
                        case "CENOMAR":
                            certificateNumber = $"CN-{random.Next(1000, 9999)}-{random.Next(10000, 99999)}";
                            break;
                        default:
                            certificateNumber = $"OT-{random.Next(1000, 9999)}-{random.Next(10000, 99999)}";
                            break;
                    }

                    // Generate location data
                    var registryOffice = registryOffices[random.Next(registryOffices.Length)];
                    var barangay = barangays[random.Next(barangays.Length)];
                    var city = cities[random.Next(cities.Length)];
                    var province = provinces[random.Next(provinces.Length)];

                    // Generate registry book details
                    var registryBook = $"Book {random.Next(1, 100)}";
                    var volumeNumber = random.Next(1, 20).ToString();
                    var pageNumber = random.Next(1, 500).ToString();
                    var lineNumber = random.Next(1, 50).ToString();

                    // Generate parent names
                    var fatherFirstName = maleFirstNames[random.Next(maleFirstNames.Length)];
                    var fatherMiddleName = middleNames[random.Next(middleNames.Length)];
                    var fatherLastName = lastName; // Same last name as the person

                    var motherFirstName = femaleFirstNames[random.Next(femaleFirstNames.Length)];
                    var motherMiddleName = middleNames[random.Next(middleNames.Length)];
                    var motherLastName = lastNames[random.Next(lastNames.Length)]; // Different last name (maiden name)

                    // Generate document type-specific information
                    string typeSpecificRemarks = "";

                    switch (documentType)
                    {
                        case "Birth Certificate":
                            // Generate birth certificate specific data
                            var birthWeight = Math.Round(random.NextDouble() * 4 + 2, 2); // 2-6 kg
                            var birthOrder = random.Next(1, 5); // 1-4
                            var birthTypes = new[] { "Single", "Twin", "Triplet", "Quadruplet" };
                            var typeOfBirth = birthTypes[random.Next(birthTypes.Length)];
                            var birthPlaces = new[] { "Hospital", "Home", "Birthing Center", "Clinic", "Emergency Vehicle" };
                            var birthPlace = birthPlaces[random.Next(birthPlaces.Length)];
                            var birthAttendants = new[] { "Physician", "Midwife", "Nurse", "Traditional Birth Attendant", "Other" };
                            var birthAttendant = birthAttendants[random.Next(birthAttendants.Length)];

                            typeSpecificRemarks = $"Birth Weight: {birthWeight} kg, Birth Order: {birthOrder}, Type of Birth: {typeOfBirth}, Birth Place: {birthPlace}, Birth Attendant: {birthAttendant}";
                            break;

                        case "Marriage Certificate":
                            // Generate marriage certificate specific data
                            var husbandFirstName = maleFirstNames[random.Next(maleFirstNames.Length)];
                            var husbandMiddleName = middleNames[random.Next(middleNames.Length)];
                            var husbandLastName = lastNames[random.Next(lastNames.Length)];
                            var husbandAge = random.Next(18, 70);

                            var wifeFirstName = femaleFirstNames[random.Next(femaleFirstNames.Length)];
                            var wifeMiddleName = middleNames[random.Next(middleNames.Length)];
                            var wifeLastName = lastNames[random.Next(lastNames.Length)];
                            var wifeAge = random.Next(18, 65);

                            var marriageDate = dateOfEvent;
                            var marriageTypes = new[] { "Civil", "Religious", "Tribal/Customary" };
                            var marriageType = marriageTypes[random.Next(marriageTypes.Length)];

                            var marriageOfficials = new[] { "Judge", "Mayor", "Priest", "Pastor", "Imam", "Ship Captain" };
                            var marriageOfficial = marriageOfficials[random.Next(marriageOfficials.Length)];

                            var marriageVenues = new[] { "Church", "City Hall", "Garden", "Beach", "Hotel", "Restaurant" };
                            var marriageVenue = marriageVenues[random.Next(marriageVenues.Length)];

                            typeSpecificRemarks = $"Husband: {husbandFirstName} {husbandMiddleName} {husbandLastName}, Age: {husbandAge}, " +
                                                $"Wife: {wifeFirstName} {wifeMiddleName} {wifeLastName}, Age: {wifeAge}, " +
                                                $"Marriage Date: {marriageDate:yyyy-MM-dd}, " +
                                                $"Marriage Type: {marriageType}, " +
                                                $"Solemnizing Officer: {marriageOfficial}, " +
                                                $"Venue: {marriageVenue}";

                            // For marriage certificates, use husband's name as the primary name
                            firstName = husbandFirstName;
                            middleName = husbandMiddleName;
                            lastName = husbandLastName;
                            break;

                        case "Death Certificate":
                            // Generate death certificate specific data
                            var causesOfDeath = new[] {
                                "Natural causes", "Heart attack", "Stroke", "Cancer", "Accident",
                                "Pneumonia", "Diabetes complications", "Kidney failure", "Liver disease", "Respiratory failure"
                            };
                            var causeOfDeath = causesOfDeath[random.Next(causesOfDeath.Length)];
                            var ageAtDeath = random.Next(1, 95); // 1-95 years
                            var placesOfDeath = new[] { "Hospital", "Home", "Nursing facility", "Public place", "Workplace", "During transport" };
                            var placeOfDeath = placesOfDeath[random.Next(placesOfDeath.Length)];
                            var mannerOfDeath = new[] { "Natural", "Accident", "Suicide", "Homicide", "Undetermined" };
                            var manner = mannerOfDeath[random.Next(mannerOfDeath.Length)];
                            var autopsyPerformed = random.Next(2) == 0 ? "Yes" : "No";

                            typeSpecificRemarks = $"Cause of Death: {causeOfDeath}, Age at Death: {ageAtDeath}, Place of Death: {placeOfDeath}, Manner of Death: {manner}, Autopsy Performed: {autopsyPerformed}";
                            break;

                        case "CENOMAR":
                            // Generate CENOMAR specific data
                            var documentPurposes = new[] {
                                "Marriage requirement", "Visa application", "Legal purposes", "Personal records",
                                "Employment requirement", "Immigration requirement", "Passport application", "Benefit claim"
                            };
                            var documentPurpose = documentPurposes[random.Next(documentPurposes.Length)];
                            var documentCategories = new[] { "Standard", "Authenticated", "Apostille", "Certified True Copy" };
                            var documentCategory = documentCategories[random.Next(documentCategories.Length)];
                            var validityPeriods = new[] { "3 months", "6 months", "1 year" };
                            var validityPeriod = validityPeriods[random.Next(validityPeriods.Length)];
                            var issuingAuthority = new[] { "PSA", "NSO", "Local Civil Registry", "DFA" };
                            var authority = issuingAuthority[random.Next(issuingAuthority.Length)];

                            typeSpecificRemarks = $"Document Purpose: {documentPurpose}, Document Category: {documentCategory}, Validity Period: {validityPeriod}, Issuing Authority: {authority}";
                            break;
                    }

                    // Create document with generated data
                    var document = new Document
                    {
                        DocumentType = documentType,
                        CertificateNumber = certificateNumber,
                        RegistryOffice = registryOffice,
                        DateOfEvent = dateOfEvent,
                        RegistrationDate = registrationDate,
                        Barangay = barangay,
                        CityMunicipality = city,
                        Province = province,
                        Prefix = prefix,
                        GivenName = firstName,
                        MiddleName = middleName,
                        FamilyName = lastName,
                        Suffix = suffix,
                        Gender = gender,
                        FatherGivenName = fatherFirstName,
                        FatherMiddleName = fatherMiddleName,
                        FatherFamilyName = fatherLastName,
                        MotherMaidenGiven = motherFirstName,
                        MotherMaidenMiddleName = motherMiddleName,
                        MotherMaidenFamily = motherLastName,
                        RegistryBook = registryBook,
                        VolumeNumber = volumeNumber,
                        PageNumber = pageNumber,
                        LineNumber = lineNumber,
                        Remarks = $"Sample {documentType} generated for testing purposes.\n\nAdditional Information:\n{typeSpecificRemarks}",
                        FilePath = $"SampleData/{documentType.Replace(" ", "")}_Front_{documents.Count}.jpg",
                        BackFilePath = $"SampleData/{documentType.Replace(" ", "")}_Back_{documents.Count}.jpg",
                        UploadedBy = adminUserId,
                        UploadedAt = uploadedAt,
                        AttendantName = documentType == "Birth Certificate" ?
                            $"Dr. {maleFirstNames[random.Next(maleFirstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}" :
                            "N/A"
                    };

                    documents.Add(document);
                }
            }

            return documents;
        }

        public static async Task SeedRequestsAsync(AppDbContext dbContext)
        {
            try
            {
                Log.Information("Checking if request seeding is required...");

                // Only seed if there are no requests in the database
                if (!await dbContext.DocumentRequests.AnyAsync())
                {
                    Log.Information("No requests found in database. Starting request seeding...");

                    // Get admin user for request handling
                    var adminUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                    if (adminUser == null)
                    {
                        Log.Warning("No admin user found for request seeding. Skipping request seeding.");
                        return;
                    }

                    // Get documents to create requests for
                    var documents = await dbContext.Documents.ToListAsync();
                    if (documents.Count == 0)
                    {
                        Log.Warning("No documents found for request seeding. Skipping request seeding.");
                        return;
                    }

                    // Create sample requests
                    var requests = GenerateSampleRequests(documents, adminUser.UserId);

                    // Add requests to database
                    await dbContext.DocumentRequests.AddRangeAsync(requests);
                    await dbContext.SaveChangesAsync();

                    Log.Information("Successfully seeded {Count} requests", requests.Count);
                }
                else
                {
                    Log.Information("Requests already exist in database. Skipping request seeding.");
                }
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "SeedData.SeedRequestsAsync");
                Log.Error(ex, "Error seeding requests");
            }
        }

        private static List<DocumentRequest> GenerateSampleRequests(List<Document> documents, int adminUserId)
        {
            var requests = new List<DocumentRequest>();
            var random = new Random();

            // Sample data for generating realistic requests
            var requestorNames = new[] { "John Smith", "Maria Santos", "Robert Garcia", "Sarah Johnson", "Michael Lee", "Jennifer Cruz", "David Kim", "Lisa Reyes", "James Wong", "Patricia Tan" };
            var addresses = new[] {
                "123 Main St, Manila",
                "456 Oak Ave, Quezon City",
                "789 Pine Rd, Cebu City",
                "321 Maple Ln, Davao City",
                "654 Cedar Blvd, Makati City"
            };
            var contactNumbers = new[] {
                "09171234567",
                "09182345678",
                "09193456789",
                "09204567890",
                "09215678901"
            };
            var purposes = new[] {
                "School requirement",
                "Employment requirement",
                "Visa application",
                "Legal proceedings",
                "Personal records"
            };
            var statuses = new[] { "Pending", "Approved", "Completed", "Rejected" };

            // Generate 15 sample requests (half of the documents)
            for (int i = 0; i < 15; i++)
            {
                // Select a random document
                var document = documents[random.Next(documents.Count)];

                // Generate random request date within the last year
                var currentDate = DateTime.Now;
                var randomDays = random.Next(1, 365); // Up to 1 year ago
                var requestDate = currentDate.AddDays(-randomDays);

                // Randomly select status
                var status = statuses[random.Next(statuses.Length)];

                // Create request with generated data
                var request = new DocumentRequest
                {
                    RequestorName = requestorNames[random.Next(requestorNames.Length)],
                    RequestorAddress = addresses[random.Next(addresses.Length)],
                    RequestorContact = contactNumbers[random.Next(contactNumbers.Length)],
                    Purpose = purposes[random.Next(purposes.Length)],
                    RelatedDocumentId = document.DocumentId,
                    Status = status,
                    RequestDate = requestDate,
                    HandledBy = status != "Pending" ? adminUserId : null
                };

                requests.Add(request);
            }

            return requests;
        }

        /// <summary>
        /// Clears all document and document request data from the database.
        /// This is useful for removing seed data before adding real data.
        /// </summary>
        public static async Task ClearDocumentDataAsync(AppDbContext dbContext)
        {
            try
            {
                Log.Information("Starting to clear document data from database...");

                // First, remove all document requests (due to foreign key constraints)
                var requests = await dbContext.DocumentRequests.ToListAsync();
                if (requests.Any())
                {
                    Log.Information("Removing {Count} document requests", requests.Count);
                    dbContext.DocumentRequests.RemoveRange(requests);
                    await dbContext.SaveChangesAsync();
                    Log.Information("Successfully removed all document requests");
                }
                else
                {
                    Log.Information("No document requests found to remove");
                }

                // Then, remove all documents
                var documents = await dbContext.Documents.ToListAsync();
                if (documents.Any())
                {
                    Log.Information("Removing {Count} documents", documents.Count);
                    dbContext.Documents.RemoveRange(documents);
                    await dbContext.SaveChangesAsync();
                    Log.Information("Successfully removed all documents");
                }
                else
                {
                    Log.Information("No documents found to remove");
                }

                Log.Information("Document data clearing completed successfully");
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "SeedData.ClearDocumentDataAsync");
                Log.Error(ex, "Error clearing document data");
                throw;
            }
        }
    }
}
