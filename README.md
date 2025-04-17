# ⏱️ Time Card System

A comprehensive solution for employee time tracking, scheduling, and attendance management built with **ASP.NET Core 8.0**. The system provides role-based access for Employees, Managers, and Administrators, allowing streamlined workforce management.

---

## 🚀 Features

- **User Management:** Role-based access for Employee, Manager, and Administrator
- **Time Tracking:** Clock in/out functionality with break tracking
- **Schedule Management:** Create and manage employee schedules
- **Smart Calculations:** Automatic shift length & break-time adjustments
- **Dashboards:** Role-specific views with relevant tools and metrics
- **Team View:** Managers can view and manage entire teams
- **Conflict Detection:** Alerts for overlapping or duplicate shifts

---

## 🛠️ Technology Stack

- **Backend:** ASP.NET Core 8.0 (Razor Pages)
- **Frontend:** HTML, CSS, JavaScript
- **Database:** SQL Server with Entity Framework Core
- **Authentication:** ASP.NET Core Identity
- **Architecture:** Clean Architecture with Repository Pattern

---

## ⚙️ Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or later  
- SQL Server 2019 or later (or SQL Server Express)  
- Visual Studio 2022 or VS Code with C# extensions  

---

## 📥 Installation

### 1. Clone the repository

```bash
git clone https://github.com/yourusername/timecardsystem.git
cd timecardsystem
2. Update the Connection String
In appsettings.json:

json
Copy
Edit
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=TimeCardSystem;Trusted_Connection=True;MultipleActiveResultSets=true"
}
3. Apply Database Migrations
bash
Copy
Edit
dotnet ef database update
4. Run the Application
bash
Copy
Edit
dotnet run --project TimeCardSystem.API
Or open the solution in Visual Studio and press F5.

🔐 Default Login Credentials
text
Copy
Edit
Email: test@example.com  
Password: Password123!
⚠️ Change these credentials after first login.

👤 Usage by Role
👷 Employee
Clock in/out for shifts

View personal schedule

Track breaks and work hours

Request time off

👨‍💼 Manager
Create and manage team schedules

Approve or reject time entries

Generate reports

View all team member logs

Set up recurring shifts

👨‍💻 Administrator
Manage users and system roles

Configure system-wide settings

Full access to all data and analytics

🧱 Project Structure
TimeCardSystem.Core – Domain models & interfaces

TimeCardSystem.Infrastructure – Data access and EF Core

TimeCardSystem.API – Razor Pages and endpoints

TimeCardSystem.Tests – Unit and integration tests

🧩 Key Components
📦 Models
User: Identity user extended with roles

TimeEntry: Clock in/out, breaks

Schedule: Work shifts

📚 Repositories
Follows repository pattern

Supports complex queries

Keeps data logic separate from business logic

📄 ViewModels
For frontend/backend communication

Handles validation and data formatting

🧪 Development
Create a Migration
bash
Copy
Edit
dotnet ef migrations add MigrationName --project TimeCardSystem.Infrastructure --startup-project TimeCardSystem.API
Run Unit Tests
bash
Copy
Edit
dotnet test
Workflow
Create a branch from main

Build feature + write tests

Run tests and code review

Submit pull request

🚀 Deployment
1. Update Production Settings
Edit appsettings.Production.json

2. Publish
bash
Copy
Edit
dotnet publish -c Release -o ./publish
Requirements
Web server (IIS, Nginx, Apache)

SQL Server

SSL certificate (HTTPS)

📸 Screenshots
🔹 Employee Dashboard

🔹 Manager Clock In / Log In

🔹 Manager Schedule View

🔹 Swagger API Endpoints

🧯 Troubleshooting
Connection Errors: Double-check your connection string

Migration Fails: Ensure EF tools are installed and DB is reachable

Authentication Issues: Check user roles and Identity configuration

🔮 Future Enhancements
Mobile application

Google Calendar integration

Geo-fencing for clock in/out

Advanced analytics & heatmaps

AI-powered schedule suggestions

📄 License
This project is licensed under the MIT License – see the LICENSE file.

🙏 Acknowledgments
ASP.NET Core Docs

Entity Framework Core

Bootstrap