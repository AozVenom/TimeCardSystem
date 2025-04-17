# Time Card System
A comprehensive solution for employee time tracking, scheduling, and attendance management built with ASP.NET Core 8.0. The system provides role-based access with different functionalities for employees, managers, and administrators, facilitating efficient workforce management.

##Features

User Management: Role-based system with Employee, Manager, and Administrator access levels
Time Tracking: Clock in/out functionality with break time tracking
Schedule Management: Create and manage employee work schedules
Automatic Calculations: Smart calculation of shift times based on standard work hours and lunch breaks
Dashboard Views: Role-specific dashboards showing relevant information
Team Management: View and manage team schedules from a single interface
Conflict Detection: Automatic detection of scheduling conflicts

##Technology Stack

Backend: ASP.NET Core 8.0 with Razor Pages
Database: SQL Server with Entity Framework Core
Authentication: ASP.NET Core Identity
Frontend: HTML, CSS, JavaScript
Architecture: Clean architecture with repository pattern

Prerequisites

.NET 8.0 SDK or later
SQL Server 2019 or later (or SQL Server Express)
Visual Studio 2022 or Visual Studio Code with C# extensions

Installation

Clone the repository
git clone https://github.com/yourusername/timecardsystem.git
cd timecardsystem

Update the connection string
Open appsettings.json and update the connection string to point to your SQL Server instance:
json"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=TimeCardSystem;Trusted_Connection=True;MultipleActiveResultSets=true"
}

Apply database migrations
dotnet ef database update

Run the application
dotnet run --project TimeCardSystem.API
Or open the solution in Visual Studio and press F5 to start.
Default login credentials

Email: test@example.com
Password: Password123!

(Change these credentials after first login)

Usage
Employee Functions

Clock in/out for work shifts
View personal schedule
Track work hours and breaks
Request time off

Manager Functions

Create and manage team schedules
View team time entries
Approve/reject time entries
Generate team reports
Set up recurring schedules

Administrator Functions

Manage users and permissions
Configure system settings
Access all system functions
View system-wide analytics

Project Structure
The solution follows a clean architecture approach with clear separation of concerns:

TimeCardSystem.Core: Contains domain models, interfaces, and business logic
TimeCardSystem.Infrastructure: Data access layer and external service implementations
TimeCardSystem.API: User interface and API endpoints
TimeCardSystem.Tests: Unit and integration tests

Key Components
Models

User: Extended Identity User with role-based access
TimeEntry: Track clock in/out and break times
Schedule: Manage employee work schedules

Repositories

Implement the repository pattern for data access
Support complex querying and CRUD operations
Separate data access concerns from business logic

View Models

Separate presentation logic from domain models
Provide data transfer and validation
Support frontend rendering requirements

Development
Adding Migrations
To add a new database migration:
dotnet ef migrations add MigrationName --project TimeCardSystem.Infrastructure --startup-project TimeCardSystem.API
Running Tests
dotnet test
Recommended Development Workflow

Create a feature branch from the main branch
Implement the feature with appropriate tests
Run all tests to ensure nothing is broken
Submit a pull request for review

Deployment
Preparing for Production

Update appsettings.Production.json with production settings
Build the application in Release configuration:
dotnet publish -c Release -o ./publish

Deploy the contents of the publish directory to your production server

Production Requirements

Web server (IIS, Nginx, etc.)
SQL Server database
HTTPS certificate for secure communication

Contributing

Fork the repository
Create a feature branch: git checkout -b feature/your-feature-name
Commit your changes: git commit -m 'Add some feature'
Push to the branch: git push origin feature/your-feature-name
Submit a pull request

Troubleshooting
Common Issues

Database Connection Errors: Verify your connection string is correct
Migration Issues: Ensure you're using the correct project for migrations
Authentication Problems: Check user roles and permissions

Future Enhancements

Mobile application
API integration with third-party systems
Advanced reporting and analytics
Geolocation-based clock in/out
Automated scheduling based on business needs

License
This project is licensed under the MIT License - see the LICENSE file for details.
Acknowledgments

ASP.NET Core Documentation
Entity Framework Core
Bootstrap