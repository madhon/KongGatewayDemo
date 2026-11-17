---
description: "This rule provides standards for the project"
alwaysApply: true
---

## Architecture Overview

This is a multi-API financial services solution with three main components:
- **AcquisAPI** - Core insurance transaction processing API with versioned endpoints (V0/V2)
- **EmailAPI** - Email service for sending notifications and documents

### Service Boundaries
- **AcquisAPI**: Handles quotes, cancellations, tracking, financial updates for insurance products
- **EmailAPI**: SMTP email sending with attachment support and path conversion (Windows/Linux)

## Coding Standards

### Target Framework & Dependencies
- **Target Framework:** .NET 9.0 with C# 13.0 language features
- **Package Management:** Central Package Management via `Directory.Packages.props`
- **Key Dependencies:** EF Core 9.0, Dapper, FluentValidation, Serilog, MailKit, ErrorOr
- **Unit Testing:** XUnit v3, Shouldy, and NSubstitute

### Architecture Patterns

#### API Versioning
- Use `Asp.Versioning` with both route-based (`v{version:apiVersion}/[controller]`) and attribute-based versioning
- V0 controllers: `[Route("[controller]")]` and `[Route("v{version:apiVersion}/[controller]")]` 
- V2 controllers: `[Route("v{version:apiVersion}/[controller]")]` only
- Always include `[ApiVersion("1.0")]` or `[ApiVersion("2.0")]` attributes

#### Data Access Patterns
- **Dual Pattern**: EF Core (`ApplicationDbContext`) for writes, Dapper for complex reads
- **Read/Write Segregation**: `IApplicationReadDbConnection` for queries, `ApplicationDbContext` for updates
- **Compiled Queries**: Use `EF.CompileQuery` for frequently executed EF queries (see `TrackingService.cs`)
```csharp
private static readonly Func<ApplicationDbContext, int, bool> CQCheckLessorExists = EF.CompileQuery(
    (ApplicationDbContext dbContext, int lessorId) =>
        dbContext.Lessors.Any(l => l.LessorId == lessorId));
```

#### Controller Structure
Standard controller dependencies follow this pattern:
```csharp
public Controller(
    I[Feature]Service service,           // Business logic
    CDFCheck cdfCheck,                   // Data validation
    ICalcEngineService[V2] calcEngine,   // External calculation service
    ICdfBatchService cdfBatchService,    // Batch processing
    IApplicationReadDbConnection readDb, // Read queries
    AuditDbContext auditDb,             // Audit logging
    IValidator<TRequest> validator)      // FluentValidation
```

#### Error Handling
- Use `ErrorOr<T>` pattern for service methods returning success/failure states
- Controllers return `Results<...>` with typed responses
- Log errors but avoid exposing sensitive details in API responses
- Use structured logging with Serilog

#### Validation
- FluentValidation for request validation
- Service-layer validation for business rules (lessor ID, product codes)
- Use `ValidationResult.IsValid` checks before processing

#### Performance Patterns
- **AnyAsync over CountAsync**: Use `AnyAsync()` for existence checks, not `CountAsync() > 0`
- **Compiled Queries**: Pre-compile frequently used EF queries with `EF.CompileQuery`
- **Parallel Execution**: Run independent validations concurrently with `Task.WhenAll`
- **Connection Pooling**: Use `AddDbContextPool` for EF contexts

### Project-Specific Conventions

#### File Organization
- Feature-based folders: `V0/Quote/`, `V2/AcquisCancellation/`
- Separate service interfaces and implementations
- Controllers, Services, Models organized by feature
- Shared infrastructure in `Infrastructure/`, `Data/`, `Helpers/`

#### Data Layer
- `ApplicationDbContext` implements `IApplicationDbContext`
- Connection string key: `"AIMIE"`
- Audit context separate: `AuditDbContext`
- Entity configurations in `Data/Configuration/`

#### Build & Deployment
- **Docker**: Multi-stage builds targeting `linux-x64`
- **Pipelines**: YAML-based Azure DevOps pipelines in `pipelines/`
- **Templates**: Reusable pipeline templates in `pipelines/templates/`
- **Versioning**: Format `$(Year:yy).$(Month).$(DayOfMonth).$(BuildID)`

### Email Service Specifics
- Use dependency injection for `IMailService` 
- Path conversion for cross-platform deployments via `PathSettings`
- Protocol logging configurable via `IProtocolLogger`
- Metrics tracking with `ApiMetrics.EmailSent()`

## Development Workflow

### Database Changes
When modifying `ApplicationDbContext`:
```bash
# From src/AcquisAPI directory
dotnet ef dbcontext optimize --output-dir .\Data\CompiledModels --namespace MyCompiledModels --context ApplicationDbContext
```

### Adding New Features
1. Create feature folder under appropriate version (`V0/` or `V2/`)
2. Implement service interface and implementation
3. Add controller following standard dependency pattern
4. Register services in `AppServiceExtensions.cs`
5. Add request/response models and validation
6. Update `AppJsonSerializerContext.cs` for AOT support

---

## Copilot Usage

- **Follow existing patterns** found in similar controllers and services
- **Use compiled queries** for repeated EF operations
- **Implement proper error handling** with ErrorOr pattern
- **Include FluentValidation** for new request models
- **Add versioning attributes** to new controllers
- **Use dependency injection** following established constructor patterns
- **Generate code ready for production** with proper logging and error handling
