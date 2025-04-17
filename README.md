Time Card System
A comprehensive solution for employee time tracking, scheduling, and attendance management built with ASP.NET Core 8.0. The system provides role-based access with different functionalities for employees, managers, and administrators, facilitating efficient workforce management.

🚀 Features
User Management: Role-based system with Employee, Manager, and Administrator access levels

Time Tracking: Clock in/out functionality with break time tracking

Schedule Management: Create and manage employee work schedules

Automatic Calculations: Smart calculation of shift times based on standard work hours and lunch breaks

Dashboard Views: Role-specific dashboards showing relevant information

Team Management: View and manage team schedules from a single interface

Conflict Detection: Automatic detection of scheduling conflicts

🛠️ Technology Stack
Backend: ASP.NET Core 8.0 with Razor Pages

Database: SQL Server with Entity Framework Core

Authentication: ASP.NET Core Identity

Frontend: HTML, CSS, JavaScript

Architecture: Clean architecture with repository pattern

⚙️ Prerequisites
.NET 8.0 SDK or later

SQL Server 2019 or later (or SQL Server Express)

Visual Studio 2022 or Visual Studio Code with C# extensions

🧪 Installation
Clone the repository

bash
Copy
Edit
git clone https://github.com/yourusername/timecardsystem.git
cd timecardsystem
Update the connection string
Open appsettings.json and replace YOUR_SERVER:

json
Copy
Edit
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=TimeCardSystem;Trusted_Connection=True;MultipleActiveResultSets=true"
}
Apply database migrations

bash
Copy
Edit
dotnet ef database update
Run the application

bash
Copy
Edit
dotnet run --project TimeCardSystem.API
Or open the solution in Visual Studio and press F5.

🔐 Default Login Credentials
⚠️ Change these credentials after first login for security.

Email: test@example.com

Password: Password123!

👤 Usage Overview
🧑 Employee
Clock in/out for work shifts

View personal schedule

Track work hours and breaks

Request time off

👨‍💼 Manager
Create and manage team schedules

View team time entries

Approve/reject time entries

Generate team reports

Set up recurring schedules

🛡️ Administrator
Manage users and permissions

Configure system settings

View system-wide analytics

Access all system functions

🗂️ Project Structure
TimeCardSystem.Core: Domain models, interfaces, and business logic

TimeCardSystem.Infrastructure: Data access and external service integrations

TimeCardSystem.API: API endpoints and Razor Pages frontend

TimeCardSystem.Tests: Unit and integration tests

🧱 Key Components
Models
User: Extended Identity User with role-based access

TimeEntry: Track clock in/out and break times

Schedule: Manage employee work schedules

Repositories
Implements repository pattern

Supports complex queries and CRUD

Separates data access from business logic

View Models
Presentation logic separated from domain models

Used for data transfer, validation, and UI rendering

🧑‍💻 Development
Add Migrations
bash
Copy
Edit
dotnet ef migrations add MigrationName --project TimeCardSystem.Infrastructure --startup-project TimeCardSystem.API
Run Tests
bash
Copy
Edit
dotnet test
Recommended Workflow
Create a feature branch

Implement feature with tests

Run tests locally

Submit pull request

🚀 Deployment
Prepare for Production
Update appsettings.Production.json

Build in release mode:

bash
Copy
Edit
dotnet publish -c Release -o ./publish
Deploy the contents of the publish directory to your server

Production Requirements
Web server (IIS, Nginx, etc.)

SQL Server instance

HTTPS certificate

🤝 Contributing
Fork the repository

Create a feature branch

bash
Copy
Edit
git checkout -b feature/your-feature-name
Commit your changes

bash
Copy
Edit
git commit -m "Add some feature"
Push to GitHub

bash
Copy
Edit
git push origin feature/your-feature-name
Submit a pull request

🛠 Troubleshooting

Issue	Solution
Database connection errors	Double-check connection string in appsettings.json
Migration issues	Use correct project for migrations (Infrastructure + API startup)
Authentication problems	Check assigned user roles and role-based authorization config
🧭 Future Enhancements
Mobile application support

API integrations with third-party systems

Advanced reporting and analytics

Geolocation-based clock-in/out

AI-powered schedule recommendations

📄 License
This project is licensed under the MIT License – see the LICENSE file for details.

🙏 Acknowledgments
ASP.NET Core Documentation

Entity Framework Core

Bootstrap
