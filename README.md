# PharmaCom

PharmaCom is an ASP.NET Core MVC e-commerce pharmacy application that allows customers to browse and buy pharmaceutical products, upload prescriptions, and track their orders. The system includes management UIs for administrators and pharmacists to verify prescriptions, manage products and orders, and handle customer accounts.

---

## Quick Project Description

PharmaCom is built with Clean Architecture principles and consists of the following layers:

- `PharmaCom.Domain` — domain models, view models, repository interfaces, constants
- `PharmaCom.DataInfrastructure` — EF Core DbContext, repository implementations, data seeding
- `PharmaCom.Service` — business logic and services (OrderService, PrescriptionService, CartService, etc.)
- `PharmaCom.WebApp` — ASP.NET Core MVC web application (controllers, views, static files)

Key technologies:

- ASP.NET Core (MVC)
- Entity Framework Core (SQL Server)
- ASP.NET Core Identity (users & roles)
- Stripe (payments)
- AutoMapper (registered and used in a small, non-invasive endpoint)
- jQuery + Bootstrap (frontend)

---

## Key Features

- User registration and authentication (Identity)
- Product catalog with search, filters and pagination
- Shopping cart with AJAX quantity updates
- Checkout with Stripe payment integration
- Prescription upload & verification workflow (pharmacist approval)
- Admin dashboard (orders, products, users) and user management with role assignment
- Order cancellation, refund logic (Stripe)
- Data seeding (roles + sample users and product/category seed)
- AutoMapper configured and used in an isolated profile endpoint

---

## Getting Started

Follow these steps to run the project locally.

### Prerequisites

- .NET SDK (compatible version used by the project)
- SQL Server (or SQL Server Express / LocalDB)
- Visual Studio or VS Code
- (Optional) ngrok or a public tunnel if you want to test webhooks

### Configuration

1. Clone the repository:
   git clone https://github.com/your-org/PharmaCom.git

2. Copy `appsettings.json` or set environment variables for:

   - `ConnectionStrings:DefaultConnection` — SQL Server connection string
   - `Stripe:SecretKey` and `Stripe:PublishableKey` — Stripe credentials
   - SMTP settings (if email notifications are configured)

3. Install NuGet packages:
   dotnet restore

4. Apply EF Core migrations and seed database:
   - Using Package Manager Console:
     ```
     Update-Database
     ```
   - Or via code: the project includes automatic seeding on startup (roles and default users).

### Run the application

From the solution folder:

```
dotnet run --project PharmaCom.WebApp
```

Open a browser and navigate to the app (usually `https://localhost:5001` or as printed by the application).

---

## Data Seeding & Default Accounts

The project contains a data seeder that seeds required roles and (optionally) default users. By default the seeder creates:

- Admin
  - username: `admin`
  - email: `admin@pharmacom.com`
  - password: `Admin@123`
  - role: `Admin`
- Pharmacist
  - username: `pharmacist`
  - email: `pharmacist@pharmacom.com`
  - password: `Pharmacist@123`
  - role: `Pharmacist`
- Customer
  - username: `customer`
  - email: `customer@pharmacom.com`
  - password: `Customer@123`
  - role: `Customer`

The seeder will also insert categories and products if none exist. Roles are seeded separately (ensures role-based features work on first run).

---

## AutoMapper

AutoMapper is added and registered with a simple mapping profile used only once in a small profile endpoint:

- Mapping location: `PharmaCom.WebApp/Mappings/MappingProfile.cs`
- Example endpoint: `Account/GetMyProfileJson` — demonstrates AutoMapper mapping `ApplicationUser` → `UserProfileDto`

This usage is intentionally minimal and non-invasive.

---

## Important Endpoints & Pages

- Home / Store: browse products
- Cart: view and update cart items (AJAX)
- Order / Checkout: upload prescription (if required) and proceed to Stripe
- Account: login/register/profile
- Admin Dashboard: (requires role `Admin`) manage products, orders, users
- Prescriptions / Verify: (requires `Pharmacist` or `Admin`) review and approve prescriptions
- User Management: Admin interface to create/edit users, assign roles, lock/unlock accounts

---

## Development Notes / Best Practices

- Use CI to run EF Core migrations and tests during deployment.
- Sensitive keys (Stripe, SMTP) should be stored in secure configuration (Azure Key Vault, GitHub Secrets, or env vars).
- Logging is recommended (Serilog or similar) — add structured logging in services and controllers for production.
- When modifying prescription or order flows, ensure data integrity when OrderId on Prescription is nullable.

---

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feat/your-feature`
3. Commit your changes and push
4. Open a pull request with a clear description and tests (if applicable)

---
