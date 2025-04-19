# Civil Registry Archiving App – Project Plan

## Project Overview

Develop a .NET-based desktop application for archiving civil registry documents aligned with Philippine Statistics Authority (PSA) guidelines. The system will operate in an offline LAN environment with a central server and multiple client installations. Key capabilities include:

- **Document Archiving:** Scan or attach hard-copy documents (birth, death, marriage certificates, CENOMAR, etc.) and store metadata.
- **Registration & Authentication:** User registration, login, and role-based access control.
- **Search & Retrieval:** Fast searching by document attributes (name, date, certificate type, local registry office, etc.) and viewing records.
- **Document Request:** Allow authorized staff or citizens (offline terminal) to file a request to obtain a certified copy.
- **Modular UI Components:** Separate, reusable UI components (RegistrationPage, LoginPage, DocumentList, DocumentDetail, SidebarMenu, etc.) for ease of debugging and testing.
- **Modern Dashboard:** Colorful, professional interface with animated sidebar navigation and key metrics widgets.
- **Offline LAN Deployment:** Central server hosts shared SQLite/SQL Server Express database; clients connect over LAN.

---

## Technology Stack

| Layer                | Technology                        |
|----------------------|-----------------------------------|
| **UI Framework**     | .NET WinForms or WPF (.NET Core)  |
| **Database**         | SQL Server Express (or SQLite)     |
| **ORM**              | Entity Framework Core              |
| **Dependency Injection** | Microsoft.Extensions.DependencyInjection |
| **Animation Library**| WPF Storyboards (if WPF)           |
| **LAN Communication**| TCP/IP + ADO.NET                   |
| **Logging**          | Serilog                            |
| **Unit Testing**     | xUnit                              |
| **Packaging/Installer** | WiX Toolset                     |

---

## Architecture

### 1. Client-Server (LAN)
- **Server:** Hosts the database, optional web API endpoints, and file storage for scanned images.
- **Client:** Desktop app communicates with server over LAN. Uses secure connections and handles network failure gracefully.

### 2. Layered Design
- **Presentation Layer:** UI components (views, windows, user controls).
- **Application Layer:** ViewModels, services (AuthenticationService, DocumentService, RequestService).
- **Domain Layer:** Business entities (User, Document, DocumentType, RegistryOffice, Request).
- **Infrastructure Layer:** Data access (EF Core Repositories), file storage, logging.

---

## Functional Requirements

### A. User Management
- **Registration Page:** Capture user details, assign roles (Admin, Clerk, Viewer).
- **Login Page:** Authenticate users and handle password hashing.
- **Role-Based UI:** Show/hide menu items based on roles.

### B. Document Management
- **Scan & Upload:** Interface to scan (TWAIN/WIA) or browse document images/PDFs.
- **Metadata Entry:** Capture fields such as DocumentType, RegistryOffice, RegistrationDate, OwnerName, Remarks.
- **DocumentList View:** Paginated grid with filters and sorting.
- **Detail View:** Display document image preview and metadata, with Edit/Delete options.

### C. Search & Retrieval
- **Global Search:** Search box on dashboard for quick lookup by name or ID.
- **Advanced Filters:** Filter by date range, document type, registry office.

### D. Document Request
- **Request Page:** Form to request a certified copy with purpose, requestor info, delivery preference.
- **Request Management:** For staff to approve, reject, mark as ready.
- **Status Tracking:** View status and logs per request ID.

### E. Dashboard & Analytics
- **Widgets:** Total documents, documents per month, recent uploads, pending requests.
- **Charts:** Bar or line charts for trends.
- **Animated Sidebar:** Collapsible, with smooth expand/collapse animations.

---

## UI/UX Design

- **Color Palette:** Use PSA brand colors plus complementary accents.
- **Typography:** Clear, sans-serif fonts (Segoe UI).
- **Layout:** Responsive grid with consistent spacing (Padding/Margin = 8px).
- **Accessibility:** High-contrast mode, keyboard navigation.
- **Animations:** Sidebar slide, hover effects on buttons.

---

## Component Structure

```
/src
  /App
    App.xaml (.cs)
  /Common
    /Controls
      SidebarMenu.xaml (.cs)
      AnimatedButton.xaml (.cs)
  /Modules
    /Auth
      LoginPage.xaml (.cs)
      RegistrationPage.xaml (.cs)
      AuthViewModel.cs
      AuthenticationService.cs
    /Documents
      DocumentListPage.xaml (.cs)
      DocumentDetailPage.xaml (.cs)
      UploadControl.xaml (.cs)
      DocumentService.cs
    /Requests
      RequestPage.xaml (.cs)
      RequestList.xaml (.cs)
      RequestService.cs
    /Dashboard
      DashboardPage.xaml (.cs)
      DashboardViewModel.cs
  /Data
    AppDbContext.cs
    Repositories
      UserRepository.cs
      DocumentRepository.cs
      RequestRepository.cs
  /Infrastructure
    FileStorageService.cs
    Logging
      SerilogConfig.cs
  /Tests
    AuthTests.cs
    DocumentTests.cs
    RequestTests.cs
/tests
  /Unit
  /Integration
```

---

## Database Schema

```sql
-- Users
CREATE TABLE Users (
  UserId INT PRIMARY KEY IDENTITY,
  Username NVARCHAR(100) UNIQUE NOT NULL,
  PasswordHash VARBINARY(MAX) NOT NULL,
  FullName NVARCHAR(200) NOT NULL,
  Role NVARCHAR(50) NOT NULL,
  CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- Documents
CREATE TABLE Documents (
  DocumentId INT PRIMARY KEY IDENTITY,
  DocumentType NVARCHAR(50) NOT NULL,
  RegistryOffice NVARCHAR(100) NOT NULL,
  CertificateNumber NVARCHAR(50) NOT NULL,
  DateOfEvent DATE NOT NULL,
  Barangay NVARCHAR(100) NOT NULL,
  CityMunicipality NVARCHAR(100) NOT NULL,
  Province NVARCHAR(100) NOT NULL,
  RegistrationDate DATE NOT NULL,

  Prefix NVARCHAR(20) NULL,
  GivenName NVARCHAR(100) NOT NULL,
  MiddleName NVARCHAR(100) NULL,
  FamilyName NVARCHAR(100) NOT NULL,
  Suffix NVARCHAR(20) NULL,

  FatherGivenName NVARCHAR(100) NULL,
  FatherMiddleName NVARCHAR(100) NULL,
  FatherFamilyName NVARCHAR(100) NULL,
  MotherMaidenGiven NVARCHAR(100) NULL,
  MotherMaidenMiddleName NVARCHAR(100) NULL,
  MotherMaidenFamily NVARCHAR(100) NULL,

  RegistryBook NVARCHAR(50) NULL,
  VolumeNumber NVARCHAR(20) NULL,
  PageNumber NVARCHAR(20) NULL,
  LineNumber NVARCHAR(20) NULL,

  Gender CHAR(1) NULL,
  AttendantName NVARCHAR(200) NULL,
  Remarks NVARCHAR(MAX) NULL,

  FilePath NVARCHAR(500) NOT NULL,
  UploadedBy INT FOREIGN KEY REFERENCES Users(UserId),
  UploadedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- Document Requests
CREATE TABLE DocumentRequests (
  RequestId INT PRIMARY KEY IDENTITY,
  RequestorName NVARCHAR(200) NOT NULL,
  RequestorAddress NVARCHAR(300) NOT NULL,
  RequestorContact NVARCHAR(100) NOT NULL,
  Purpose NVARCHAR(255) NOT NULL,
  RelatedDocumentId INT FOREIGN KEY REFERENCES Documents(DocumentId),
  Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
  RequestDate DATETIME NOT NULL DEFAULT GETDATE(),
  HandledBy INT FOREIGN KEY REFERENCES Users(UserId)
);
```

---

## Development Timeline

| Phase | Tasks | Duration |
|-------|-------|----------|
| 1. Setup | Project scaffolding, DI, DB context, basic models | 1 week |
| 2. Auth | Registration & Login, password hashing, role-based UI | 2 weeks |
| 3. Docs | Scan/upload, metadata CRUD, list & detail views | 3 weeks |
| 4. Search | Global search box, advanced filters | 1 week |
| 5. Dashboard | Dashboard page, widgets, simple charts, animated sidebar | 2 weeks |
| 6. Requests | Request filing, tracking, management pages | 1 week |
| 7. Testing | Unit & integration tests | 1 week |
| 8. Deployment | Installer creation, LAN connection testing, user documentation | 1 week |

---

## Deployment & Maintenance

- **Server Setup:** Install SQL Server Express, configure shared database.
- **Client Installer:** MSI via WiX with configuration wizard to set server IP.
- **Maintenance:** Auto backup service, log rotation, app auto-update module (optional).

---


