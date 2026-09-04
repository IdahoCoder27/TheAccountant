# TheAccountant

TheAccountant is a locally hosted personal finance application built with ASP.NET Core MVC.

It is designed for private, single-user use and provides account management, transaction tracking, financial dashboards, analytics, and manual transaction imports from financial institution exports.

## Features

### Authentication

- Local username/password authentication
- ASP.NET Core Identity
- No external authentication providers
- No email confirmation or public registration

### Accounts

- Add and edit financial accounts
- Checking and savings accounts
- Credit cards
- Brokerage accounts
- Retirement accounts
- Loans and mortgages
- Close and reopen accounts
- Prevent deletion of accounts containing transaction history

### Transactions

- Manual transaction entry
- Expense and income classification
- Pending and posted transactions
- Transaction categories
- Recurring transaction flag
- Edit and delete transactions
- Transaction source tracking

### Dashboard

- Net worth
- Cash balances
- Credit debt
- Monthly spending
- Spending by category
- Recent transactions
- Six-month income vs. spending chart
- Light and dark themes
- Glassmorphism UI with subtle animated background

### Transaction Import

Transaction import is designed around files exported directly from financial institution websites.

The application does **not** use Plaid or another paid aggregation service.

Planned import workflow:

1. Export transaction data from a financial institution
2. Upload the file to TheAccountant
3. Select the destination account
4. Parse the institution-specific file format
5. Preview normalized transactions
6. Detect probable duplicates
7. Confirm the import
8. Save transactions

Excel and CSV formats may be supported depending on the financial institution.

## Technology

- .NET / ASP.NET Core MVC
- C#
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity
- Razor
- Bootstrap
- jQuery
- Chart.js

## Architecture

The application uses standard MVC separation:

```text
Controllers
Models
Models/ViewModels
Models/Enums
Data
Views
Services
wwwroot
