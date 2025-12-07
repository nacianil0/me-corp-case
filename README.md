# MeCorp Affiliate System

Referral platform with role-based access control.

<img width="3420" height="1828" alt="screencapture-localhost-5153-Dashboard-2025-12-07-17_01_41" src="https://github.com/user-attachments/assets/099fd94c-d3db-4a08-8d98-49529013fe5e" />


## Quick Setup

### 1. Database (SQL Server via Docker)

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Pass!" -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest
```

### 2. Build & Run

```bash
dotnet restore
dotnet build
dotnet run
```

Application runs at `http://localhost:5000` or `https://localhost:5153`

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

