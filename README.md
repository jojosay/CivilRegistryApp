# Civil Registry Application

A comprehensive Windows desktop application for managing civil registry documents such as birth certificates, marriage certificates, death certificates, and CENOMAR (Certificate of No Marriage). This application provides a user-friendly interface for document management, user administration, and reporting.

![Civil Registry App Dashboard](https://github.com/jojosay/CivilRegistryApp/raw/master/screenshots/dashboard.png)

## Features

### Document Management
- Add, edit, view, and delete various civil registry documents
- Support for multiple document types:
  - Birth Certificates
  - Marriage Certificates
  - Death Certificates
  - CENOMAR (Certificate of No Marriage)
- Document preview functionality
- Certificate number copy feature with notification
- Advanced search and filtering capabilities

### User Interface
- Modern, responsive design with dark blue-gray theme (#2E3A59)
- Consistent styling across all windows and controls
- Intuitive navigation with sidebar menu
- Dashboard with statistics, charts, and recent activity
- Accessibility features for improved usability

### User Management
- User authentication and authorization
- Role-based access control
- User activity logging
- Profile management

### Reporting
- Generate reports on document statistics
- Export options (PDF, Excel)
- Customizable report parameters
- Visual data representation with charts

### Security
- Secure user authentication
- Activity logging for audit trails
- Data validation and sanitization

## Technologies Used

- **Framework**: .NET 9.0 with WPF (Windows Presentation Foundation)
- **UI Components**: Material Design for XAML
- **Database**: SQLite for local data storage
- **Architecture**: MVVM (Model-View-ViewModel) pattern
- **Reporting**: Custom reporting engine
- **Version Control**: Git

## Installation and Setup

### Prerequisites
- Windows 10 or later
- .NET 9.0 SDK or later
- Visual Studio 2022 or later (recommended for development)

### Installation Steps
1. Clone the repository:
   ```
   git clone https://github.com/jojosay/CivilRegistryApp.git
   ```

2. Open the solution in Visual Studio:
   ```
   cd CivilRegistryApp
   start CivilRegistryApp.sln
   ```

3. Restore NuGet packages:
   ```
   dotnet restore
   ```

4. Build the application:
   ```
   dotnet build
   ```

5. Run the application:
   ```
   dotnet run --project CivilRegistryApp
   ```

### Default Login Credentials
- Username: admin
- Password: admin123

## Usage

### Document Management
1. **Adding a Document**: Click on the "Add Document" button in the Documents view, fill in the required fields, and upload document images.
2. **Editing a Document**: Select a document from the list and click the "Edit" button to modify its details.
3. **Viewing a Document**: Click on a document in the list to view its details in a popup window.
4. **Deleting a Document**: Select a document and click the "Delete" button to remove it from the system.

### User Management
1. **Adding a User**: Navigate to the User Management section and click "Add User".
2. **Editing User Permissions**: Select a user and click "Edit" to modify their role and permissions.
3. **Viewing Activity Logs**: Access the Activity Logs section to view user actions and system events.

### Reporting
1. **Generating Reports**: Navigate to the Reports section and select the desired report type.
2. **Exporting Reports**: Use the export buttons to save reports in PDF or Excel format.
3. **Customizing Reports**: Adjust parameters and filters to customize report content.

## Screenshots

### Dashboard
![Dashboard](https://github.com/jojosay/CivilRegistryApp/raw/master/screenshots/dashboard.png)

### Document Management
![Document Management](https://github.com/jojosay/CivilRegistryApp/raw/master/screenshots/documents.png)

### User Management
![User Management](https://github.com/jojosay/CivilRegistryApp/raw/master/screenshots/users.png)

### Reports
![Reports](https://github.com/jojosay/CivilRegistryApp/raw/master/screenshots/reports.png)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgements

- [Material Design In XAML Toolkit](http://materialdesigninxaml.net/)
- [SQLite](https://www.sqlite.org/index.html)
- [WPF](https://github.com/dotnet/wpf)

---

© 2025 Civil Registry Application. All rights reserved.
