# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Run all commands from this directory (`training-repo/`, where `OrderHub.sln` lives).

```powershell
# Build
dotnet build

# Run the web app (auto-migrates DB + seeds data on first run)
dotnet run --project src/OrderHub.Web
# custom port if 5xxx is taken:
dotnet run --project src/OrderHub.Web --urls http://localhost:5299

# Run all tests (EF Core InMemory — no SQL Server needed)
dotnet test

# Run a single test class or method
dotnet test --filter "FullyQualifiedName~OrderServiceCancelTests"
dotnet test --filter "FullyQualifiedName~OrderServiceCreateTests.CreateOrder_GoldMember_SnapshotsRawUnitPrice_NotPreDiscounted"

# Reset the local dev database back to seed data
dotnet ef database drop -f -p src/OrderHub.Infrastructure -s src/OrderHub.Web
dotnet run --project src/OrderHub.Web
```

The web app itself needs a local SQL Server instance (see `appsettings.Development.json` for the connection string); tests do not.

Note: this folder is nested inside a larger git repository rooted one level up (`../`, which also contains `documents/`). Git commands run from here operate on that outer repo — `git status`/`git diff` show paths relative to the current directory, so double-check you're staging `training-repo/...`-prefixed paths when running git from the outer root.

## Architecture

Three-project layering, strictly enforced by convention (not by assembly boundaries alone):

- `src/OrderHub.Web` — Controllers, ViewModels, Razor Views. Controllers only translate HTTP <-> service calls; no business logic or EF Core queries here.
- `src/OrderHub.Core` — Domain models, service interfaces + implementations, and `Common/` (`ServiceResult<T>`, `PagedResult<T>`). All business rules (discounts, stock, status transitions, validation) live in services here.
- `src/OrderHub.Infrastructure` — `OrderHubDbContext`, repository implementations, EF Core migrations, `DbSeeder`.

Data flow: Controller -> `I*Service` (Core) -> `I*Repository` (Core interface, Infrastructure implementation) -> `OrderHubDbContext`. Services never touch `DbContext` directly; only repositories do.

Key patterns to follow when extending this code:

- **`ServiceResult<T>`**: service methods that can fail return `ServiceResult<T>.Ok(value)` / `.Fail(errors)` rather than throwing. Controllers surface `result.Errors` via `ModelState.AddModelError` or `TempData["Error"]`.
- **`PagedResult<T>`**: pagination wrapper (`Items`, `TotalCount`, `Page`, `PageSize`, computed `TotalPages`). `page` is 1-based; repository `Skip` must be `(page - 1) * pageSize`.
- **ViewModel-only binding**: Views never bind directly to domain models (`Order`, `Product`, `Customer`); each has a `*ViewModel`/`*RowViewModel` in `Web/ViewModels`, mapped by hand in the controller.
- **Validation**: server-side via DataAnnotations on ViewModels + `ModelState`/`TryValidateModel`, errors rendered on the form (`asp-validation-for`). Never let a 500 reach the user for a validation failure.
- **`TempData["Success"] / TempData["Error"]`**: set in controller actions after POST, rendered as a shared Bootstrap alert in `Views/Shared/_Layout.cshtml`.
- **Money/discount rules**: `OrderService.GetDiscountRate` returns Standard 0%, Silver 5%, Gold 10%. `OrderItem.UnitPriceSnapshot` always stores the *raw* unit price at order time — discount is applied exactly once, in `CalculateTotal`, against the subtotal. Do not pre-discount the snapshot.
- **Repository queries that aggregate across entities** (e.g. low-stock sold-quantity lookups) should avoid N+1 by fetching the primary set once, then a single grouped aggregate query keyed by id — see `ProductRepository.GetLowStockAsync` for the pattern.

Tests (`tests/OrderHub.Tests`) use `TestSetup.CreateContext()` (EF Core InMemory, fresh Guid-named DB per test) and `TestSetup.CreateOrderService`/`CreateProductService` wired to real repository implementations against that InMemory context — so tests exercise the actual repository query logic, not mocks.

## Progress / Session Log

Work completed in the training exercises (`documents/activities/activity-guideline.md`) so far, one commit each:

1. **Bug — order list pagination off-by-one**: `OrderRepository.GetPagedAsync` used `Skip(page * pageSize)` instead of `Skip((page - 1) * pageSize)`, skipping the newest page of orders and leaving the last page blank. Fixed; regression test added.
2. **Bug — Gold member orders double-discounted**: `OrderService.CreateOrderAsync` pre-discounted `UnitPriceSnapshot` for Gold customers, and `CalculateTotal` discounted the subtotal again, compounding the discount (0.9 × 0.9 instead of 0.9). Fixed by always snapshotting the raw unit price; discount now applies once, in `CalculateTotal`.
3. **Bug — stock not restored on order cancellation**: `OrderService.CancelOrderAsync` set `order.Status = Cancelled` *before* checking whether the order was Pending/Confirmed to decide whether to restock — making that check always false. Fixed by restocking before flipping the status.
4. **Feature — low-stock warning page**: `GET /Products/LowStock?threshold=` (default 10, `<= 0` shows a form validation error, never a 500). Lists active products below threshold, ascending by stock, with 30-day sold quantity (excluding Cancelled orders) and a `table-danger` row style below 5 units. Added `LowStockProduct` (Core), `IProductRepository.GetLowStockAsync` / `ProductService.GetLowStockAsync`, `LowStockViewModel`, `ProductsController.LowStock`, `Views/Products/LowStock.cshtml`, and a nav link.
5. **Refactor — `OrderService.CreateOrderAsync` validation**: extracted the growing inline validation into `ValidateLines` (fail-fast, single error — matches the original three sequential `if...return` checks) and `AddOrderItemsAsync` (accumulates per-line errors — matches the original loop). Behavior unchanged; all tests stayed green.

All changes are covered by unit tests in `tests/OrderHub.Tests` (35 tests passing as of this log). See git log for exact commit messages (symptom → root cause → fix format).
