# Zentic — Expense Requisition System

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![ASP.NET MVC](https://img.shields.io/badge/ASP.NET-MVC-512BD4.svg)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EF%20Core-10.0-512BD4.svg)](https://learn.microsoft.com/ef/core/)

Enterprise-grade expense requisition system built with **Clean Architecture** on **ASP.NET MVC 10**, **Entity Framework Core**, and **SQL Server**. Designed for finance divisions requiring multi-tier approval workflows, fraud detection, vendor management, and comprehensive audit trails.

---

## Features

### Core Workflow
- **Multi-Role Access**: Admin, Finance, Manager, Staff, Auditor — each with granular permissions
- **Expense Requisition CRUD**: Create, submit, track, and archive requisitions through a structured lifecycle
- **Approval Pipeline**: Draft → Submitted → L1 Manager → Finance Review → Approved/Rejected
- **Fraud Detection**: Real-time flagging of suspicious patterns (duplicate invoices, amount anomalies, velocity checks)
- **3-Way Matching**: PO ↔ Invoice ↔ Goods Receipt verification before payment approval

### Security
- **Cookie Authentication**: ASP.NET Core Identity with sliding expiration and HttpOnly cookies
- **Rate Limiting**: Brute-force protection on login (10 attempts per 15-minute window)
- **Password Hashing**: BCrypt.Net with configurable work factor
- **Encryption**: AES-256-GCM for sensitive stored data via configurable key management
- **Authorization Policies**: Role-based access for approval, audit logs, vendor management

### Finance Dashboard
- **KPI Cards**: Pending approvals, flagged items, total expenditure, overdue payments
- **Category Breakdown**: Bar chart of expenditure by expense category
- **Approval Pipeline**: Visual queue showing requisitions awaiting each approval tier
- **Real-time Stats**: Aggregated financial metrics with drill-down capability

### Vendor & Department Management
- **Vendor Registry**: Full vendor lifecycle (registration → verification → active/inactive)
- **Bank Account Validation**: Multi-account support per vendor
- **Department Tree**: Hierarchical departments with configurable approval thresholds
- **Conflict of Interest**: Declaration management for procurement compliance

### Audit & Compliance
- **Immutable Audit Logs**: Append-only logging with tamper-evident design
- **Action Traceability**: Every status change, approval, and rejection recorded with IP + timestamp
- **Price History Tracking**: Vendor price changes logged for auditability
- **Conflict of Interest Logging**: Mandatory declarations tracked and reported

### Technical Highlights
- **Clean Architecture**: Domain, Application, Infrastructure, Web, and Tests layers
- **Repository Pattern**: Generic repository with unit of work for data access abstraction
- **FluentValidation**: Request validation at application boundary
- **Server-Side Pagination**: Efficient large-dataset browsing with search and filter
- **SQL Server LocalDB**: Zero-configuration local development database

---

## Tech Stack

| Layer          | Technology                          |
|----------------|-------------------------------------|
| Framework      | ASP.NET MVC (.NET 10)               |
| ORM            | Entity Framework Core 10            |
| Database       | SQL Server (LocalDB / Express)       |
| Authentication | ASP.NET Cookie Authentication       |
| Authorization  | Policy-based role authorization     |
| UI             | Bootstrap 5 + Bootstrap Icons       |
| Password       | BCrypt.Net                          |
| Encryption     | AES-256-GCM                         |
| Validation     | FluentValidation                    |
| Testing        | xUnit + Moq                         |

---

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server LocalDB (included with Visual Studio) or SQL Server Express
- Visual Studio 2022+ / JetBrains Rider / VS Code

### Quick Start

```bash
# Clone the repository
git clone https://github.com/your-username/Zentic.git
cd Zentic/ZenticEnterprise

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run database migrations and seed data
# (Auto-migrates on first startup)

# Launch the application
dotnet run --project Zentic.Web
```

Open **https://localhost:7081** in your browser.

### Demo Accounts

| Email                         | Password       | Role      |
|-------------------------------|----------------|-----------|
| admin@company.internal        | Admin@12345    | Admin     |
| finance.manager@company.internal | Finance@12345 | Finance   |
| budi.santoso@company.internal | Staff@12345    | Staff     |
| siti.rahayu@company.internal  | Manager@12345  | Manager   |
| auditor@company.internal      | Auditor@12345  | Auditor   |

> **⚠️ IMPORTANT**: Change all demo passwords before deploying to production.

---

## Project Structure

```
ZenticEnterprise/
├── Zentic.slnx                              # Solution file
│
├── Zentic.Domain/                           # Innermost layer — enterprise logic
│   ├── Entities/                            #   Domain models
│   │   ├── AppUser.cs
│   │   ├── Department.cs
│   │   ├── ExpenseRequisition.cs
│   │   ├── Vendor.cs
│   │   ├── AuditLog.cs
│   │   ├── ConflictOfInterestDeclaration.cs
│   │   ├── PriceHistory.cs
│   │   ├── RequisitionApprovalHistory.cs
│   │   ├── RequisitionQuotation.cs
│   │   └── BaseEntity.cs                    #   Base class with audit fields
│   └── Enums/
│       └── Enums.cs                         #   UserRole, RequisitionStatus, etc.
│
├── Zentic.Application/                      # Use cases & orchestration
│   ├── DTOs/                                #   Data transfer objects
│   ├── Interfaces/                          #   Service contracts
│   │   ├── IAuthService.cs
│   │   ├── IExpenseService.cs
│   │   ├── IVendorService.cs
│   │   ├── IDashboardService.cs
│   │   ├── IAuditService.cs
│   │   ├── IFraudDetectionService.cs
│   │   ├── IEncryptionService.cs
│   │   ├── IRepository.cs
│   │   └── IUnitOfWork.cs
│   ├── Validators/                          #   FluentValidation rules
│   └── ViewModels/                          #   LoginViewModel, etc.
│
├── Zentic.Infrastructure/                   # External concerns implementation
│   ├── Data/
│   │   └── AppDbContext.cs                  #   EF Core context + seed data
│   ├── Repositories/
│   │   └── Repository.cs                    #   Generic repository implementation
│   ├── Services/
│   │   ├── AuthService.cs
│   │   ├── ExpenseService.cs
│   │   ├── VendorService.cs
│   │   ├── DashboardService.cs
│   │   ├── AuditService.cs
│   │   ├── FraudDetectionService.cs
│   │   └── EncryptionService.cs
│   └── DependencyInjection.cs              #   DI registration extension
│
├── Zentic.Web/                              # ASP.NET MVC presentation layer
│   ├── Controllers/                         #   Auth, Home, Expense, Vendor, etc.
│   ├── Views/                               #   Razor views (Auth, Expense, etc.)
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── wwwroot/
│   │   ├── css/site.css
│   │   └── js/site.js
│   └── Program.cs                           #   App bootstrap & middleware pipeline
│
└── Zentic.Tests/                            # Unit tests
    └── UnitTests/
        └── FraudDetectionServiceTests.cs
```

---

## API Reference

| Method | Route                  | Description              | Auth Required |
|--------|------------------------|--------------------------|---------------|
| GET    | `/Auth/Login`          | Login page               | No            |
| POST   | `/Auth/Login`          | Authenticate user        | No            |
| POST   | `/Auth/Logout`         | End session              | Yes           |
| GET    | `/`                    | Dashboard with KPIs      | Yes           |
| GET    | `/Expense`             | Requisition list         | Yes           |
| POST   | `/Expense/Create`      | New requisition          | Yes           |
| POST   | `/Expense/Approve/{id}`| Approve requisition      | Finance/Admin |
| POST   | `/Expense/Reject/{id}` | Reject requisition       | Finance/Admin |
| GET    | `/Vendor`              | Vendor list              | Yes           |
| POST   | `/Vendor/Create`       | New vendor               | Finance/Admin |
| GET    | `/AuditLog`            | Audit trail              | Finance/Admin |
| GET    | `/Report`              | Financial reports        | Finance/Admin |
| GET    | `/User`                | User management          | Admin         |
| GET    | `/Department`          | Department management    | Admin         |

---

## Fraud Detection Rules

The system automatically flags requisitions matching these patterns:

- **Duplicate Invoice Check**: Same invoice number used for different requisitions
- **Amount Anomaly**: Requested amount deviates significantly from historical averages
- **Amount Rounding**: Suspiciously round numbers (e.g., exactly 10,000,000)
- **Velocity Check**: Requester submits unusually high number of requests in a short period
- **New Vendor Large Amount**: Large requisition assigned to a recently created vendor
- **Category Mismatch**: Vendor's typical category doesn't match the requisition's category
- **Weekend Submission**: Requisition submitted outside business hours

---

## Configuration

### `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ZenticDB;Trusted_Connection=True;"
  },
  "AppSettings": {
    "CompanyName": "PT Zentic",
    "Division": "Finance",
    "AppVersion": "2.0.0",
    "MultipleQuotationThreshold": 50000000,
    "L2ApprovalThreshold": 100000000
  }
}
```

| Setting                    | Default   | Description                              |
|----------------------------|-----------|------------------------------------------|
| `MultipleQuotationThreshold` | 50,000,000 | Amount above which multiple quotes required |
| `L2ApprovalThreshold`        | 100,000,000 | Amount above which L2 approval required   |

> **🔐 Security**: Use `dotnet user-secrets` or environment variables for production secrets. Never commit real passwords or encryption keys.

---

## Testing

```bash
# Run all unit tests
cd ZenticEnterprise
dotnet test
```

The test project covers:
- Fraud detection rule engine
- Service layer validations
- Repository operations

---

## Roadmap

- [ ] **Power BI Integration**: Embedded analytics dashboard for executive reporting
- [ ] **Email Notifications**: Automated approval/rejection emails via SMTP
- [ ] **Mobile Approval**: Approve/reject requisitions from mobile devices
- [ ] **Multi-Currency**: Support for USD, EUR, and other currencies with live exchange rates
- [ ] **SSO Integration**: Azure AD / OAuth 2.0 single sign-on
- [ ] **Docker Support**: Containerized deployment with Docker Compose

---

## Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Commit your changes: `git commit -m 'Add amazing feature'`
4. Push to the branch: `git push origin feature/amazing-feature`
5. Open a Pull Request

Please ensure your code follows the existing patterns and includes unit tests.

---

## License

Distributed under the MIT License. See `LICENSE` for more information.

---

## Acknowledgments

- Built with [ASP.NET MVC](https://dotnet.microsoft.com/apps/aspnet)
- UI components by [Bootstrap](https://getbootstrap.com/)
- Icons by [Bootstrap Icons](https://icons.getbootstrap.com/)
- Password hashing by [BCrypt.Net](https://github.com/BcryptNet/bcrypt.net)
- Validation by [FluentValidation](https://fluentvalidation.net/)
