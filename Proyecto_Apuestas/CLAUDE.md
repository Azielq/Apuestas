# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Build and Run
```bash
# Build the solution
dotnet build Proyecto_Apuestas.sln

# Restore packages
dotnet restore Proyecto_Apuestas.sln

# Run the application (from Proyecto_Apuestas/ directory)
dotnet run --project Proyecto_Apuestas.csproj

# Run in development mode with HTTPS
dotnet run --project Proyecto_Apuestas.csproj --launch-profile "Development-HTTPS"

# Run with specific environment
ASPNETCORE_ENVIRONMENT=Development dotnet run --project Proyecto_Apuestas.csproj
```

### Database Operations
```bash
# Add new migration
dotnet ef migrations add MigrationName --project Proyecto_Apuestas

# Update database with latest migration
dotnet ef database update --project Proyecto_Apuestas

# Generate model from database (scaffold)
dotnet ef dbcontext scaffold "Server=...;Database=apuestas;..." Pomelo.EntityFrameworkCore.MySql --project Proyecto_Apuestas
```

## Application Architecture

### Core Structure
This is an ASP.NET Core 9.0 MVC betting application with the following key architectural patterns:

- **MVC Pattern**: Controllers handle HTTP requests, Views render UI, Models represent data
- **Service Layer**: Business logic abstracted into services with interface contracts
- **Repository Pattern**: Data access through Entity Framework DbContext
- **Dependency Injection**: Services registered in `ServiceConfiguration.cs`
- **Configuration Management**: Centralized through `ConfigurationHelper` and strongly-typed settings

### Key Components

#### Database Layer (`Data/`)
- **apuestasDbContext**: Main EF Core context with MySQL configuration
- **Models**: Entity classes for Bets, Events, Users, Teams, Sports, etc.
- **Views**: Database views (`vw_ActiveUsersStats`, `vw_UpcomingEvents`)

#### Service Layer (`Services/`)
- **Interfaces**: Service contracts (`IBettingService`, `IEventService`, `IUserService`, etc.)
- **Implementations**: Business logic implementations
- **External**: API integrations (OddsApi for real-time sports data)

#### Controllers (`Controllers/`)
- **API Controllers**: REST endpoints for external consumption
- **Admin Controllers**: Administrative functions with role-based access
- **Core Controllers**: Main application functionality (Betting, Events, etc.)

#### Configuration (`Configuration/`)
- **ServiceConfiguration**: DI container setup, authentication, authorization
- **AppSettings**: Application-specific settings classes
- **AutoMapperProfile**: Object mapping configuration
- **ValidationConfiguration**: FluentValidation setup

#### Middleware (`Middleware/`)
- **ExceptionHandlingMiddleware**: Global error handling
- **SecurityHeadersMiddleware**: Security headers injection
- **RateLimitingMiddleware**: API rate limiting
- **UserActivityMiddleware**: User session tracking

### Key Features
- **Sports Betting**: Core betting functionality with odds management
- **Real-time Odds**: Integration with The Odds API for live sports data
- **User Management**: Authentication, authorization, and role-based access
- **Payment Processing**: Stripe integration for transactions
- **Admin Dashboard**: Management interface for events, users, and reports
- **Email Notifications**: SendGrid integration for user communications

### Technology Stack
- **Framework**: ASP.NET Core 9.0 MVC
- **Database**: MySQL with Entity Framework Core
- **Authentication**: Cookie-based authentication with custom user management
- **Validation**: FluentValidation for model validation
- **Mapping**: AutoMapper for object transformations
- **Email**: SendGrid for email services
- **Payments**: Stripe for payment processing
- **API Integration**: The Odds API for sports data

### Configuration Structure
- **appsettings.json**: Main configuration with database, email, payment, and API settings
- **Environment-specific**: appsettings.Development.json, appsettings.Production.json
- **Startup Validation**: Comprehensive configuration validation on application start
- **Development Endpoints**: `/config/validate`, `/config/diagnostics`, `/health` for debugging

### Database Schema Overview
Main entities include:
- **UserAccount**: User management with roles and authentication
- **Sport/Team/Competition**: Sports hierarchy and organization
- **Event**: Sporting events with team associations
- **Bet**: User bets with payment transaction links
- **OddsHistory**: Historical odds tracking
- **PaymentTransaction/PaymentMethod**: Financial operations
- **Notification**: User communication system

### Development Notes
- MySQL connection uses AWS RDS with SSL required
- Session management for bet slip functionality (30-minute timeout)
- CORS configured for API access from specific origins
- Comprehensive logging and error handling throughout
- Role-based authorization with Admin, Moderator, and Regular user levels