# MeCorp Affiliate System

Referral platform with role-based access control.

## Quick Setup

### 1. Database (SQL Server via Docker)

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrongPassword123!" -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest
```

### 2. Build & Run

```bash
dotnet restore
dotnet build
dotnet run
```

Application runs at `http://localhost:5000` or `https://localhost:5001`

## Test Accounts

The database seeds with these accounts on first run:

| Role     | Email                  | Password      | Referral Code   |
|----------|------------------------|---------------|-----------------|
| Admin    | admin@mecorp.com       | Admin123!     | ADMIN2026SEED   |
| Manager  | manager@mecorp.com     | Manager123!   | MGR2026SEED01   |
| Customer | customer@mecorp.com    | Customer123!  | CUST2026SEED    |

## Features

- IP blocking after failed login attempts
- Rate limiting per endpoint
- Referral code system
- Role-based dashboards

## Tech Stack

.NET 10.0, EF Core, MediatR, FluentValidation, BCrypt, SQL Server

