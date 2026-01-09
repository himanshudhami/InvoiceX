# InvoiceApp Architecture Guide

## Status
- Last Updated: 2026-01-09
- Backend: .NET Core 9, Dapper, PostgreSQL
- Frontend: React 18, Vite, TypeScript, TanStack Query

---

## Core Principles

**All code changes MUST follow these principles:**

### SOLID Principles

| Principle | Backend (.NET) | Frontend (React) |
|-----------|----------------|------------------|
| **S**ingle Responsibility | One class = one reason to change | One component = one specific problem |
| **O**pen/Closed | Extend via interfaces, not modification | Extend via composition, not prop drilling |
| **L**iskov Substitution | Subtypes must be substitutable | Components sharing interface must be swappable |
| **I**nterface Segregation | Small, focused interfaces | Components receive only props they use |
| **D**ependency Inversion | Depend on abstractions (interfaces) | Depend on hooks/services, not implementations |

### Separation of Concerns (SoC)

```
┌─────────────────────────────────────────────────────────────┐
│                        FRONTEND                              │
├──────────────┬──────────────┬──────────────┬────────────────┤
│   Pages      │  Components  │   Hooks      │   Services     │
│  (routing)   │    (UI)      │  (logic)     │   (API)        │
└──────────────┴──────────────┴──────────────┴────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                        BACKEND                               │
├──────────────┬──────────────┬──────────────┬────────────────┤
│ Controllers  │  Services    │ Repositories │   Entities     │
│  (HTTP)      │  (logic)     │   (data)     │   (domain)     │
└──────────────┴──────────────┴──────────────┴────────────────┘
```

**Frontend SoC Rules:**
- Pages: Route handling, layout composition only
- Components: Pure UI rendering, receive data via props
- Hooks: Business logic, state management, side effects
- Services: API communication only, no UI logic

**Backend SoC Rules:**
- Controllers: HTTP concerns only (request/response mapping)
- Services: Business logic, validation, orchestration
- Repositories: Data access only, no business logic
- Entities: Domain models, no infrastructure concerns

---

## Technology Stack

### Frontend Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| **React** | 18.x | UI framework |
| **TypeScript** | 5.x | Type safety |
| **Vite** | 5.x | Build tool, dev server |
| **TanStack Query** | 5.x | Server state management |
| **TanStack Table** | 8.x | Headless table UI |
| **nuqs** | 2.x | URL state management |
| **React Hook Form** | 7.x | Form state management |
| **Zod** | 3.x | Schema validation |
| **Tailwind CSS** | 3.x | Utility-first styling |
| **shadcn/ui** | latest | Component library (Radix-based) |
| **Axios** | 1.x | HTTP client |

### Backend Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| **.NET Core** | 9.0 | Runtime framework |
| **Dapper** | 2.x | Micro ORM |
| **PostgreSQL** | 16.x | Database |
| **DbUp** | 5.x | Database migrations |
| **FluentValidation** | 11.x | Request validation |
| **AutoMapper** | 13.x | Object mapping |

---

## Project Structure

```
react-invoice-generator/
├── backend/                      # .NET Core 9 API (Clean Architecture)
│   ├── src/
│   │   ├── Core/                 # Domain layer (no dependencies)
│   │   ├── Infrastructure/       # Data access + external services
│   │   ├── Application/          # Business logic + DTOs
│   │   └── WebApi/               # REST API controllers
│   ├── migrations/               # SQL migration scripts (140+)
│   └── tests/                    # Unit + integration tests
│
├── apps/
│   └── admin-portal/             # React frontend
│       └── src/
│           ├── pages/            # Route-based page components
│           ├── features/         # Domain-specific hooks
│           ├── components/       # Reusable UI components
│           ├── services/api/     # API service layer
│           ├── contexts/         # React Context providers
│           └── lib/              # Utilities
│
└── docs/                         # Documentation
    ├── modules/                  # Per-module comprehensive docs
    ├── backlog/                  # Roadmap and active work
    └── research/                 # Competitive analysis
```

---

## Backend Architecture

### Clean Architecture Layers

```
WebApi → Application → Core ← Infrastructure → Core
```

| Layer | Purpose | Dependencies |
|-------|---------|--------------|
| **Core** | Domain entities, interfaces, errors | None (pure) |
| **Infrastructure** | Repository implementations, external services | Core only |
| **Application** | Services, DTOs, validators, mappings | Core only |
| **WebApi** | Controllers, middleware, DI configuration | Application + Infrastructure |

### Key Patterns

#### 1. Result Pattern (Mandatory)
All service methods return `Result<T>` for consistent error handling:

```csharp
// Success
return Result<User>.Success(user);

// Errors (use Error factory methods)
return Error.NotFound("User not found");
return Error.Validation("Invalid email");
return Error.Conflict("Email already exists");
return Error.Internal("Database error");
```

Error types: `NotFound`, `Validation`, `Conflict`, `Internal`, `Unauthorized`

#### 2. Repository Pattern
- Interface in `Core/Interfaces/`
- Implementation in `Infrastructure/Data/`
- Uses Dapper with parameterized queries (never string concatenation)

```csharp
// Always use parameterized queries
return await connection.QueryAsync<Entity>(
    "SELECT * FROM entities WHERE company_id = @companyId",
    new { companyId });
```

#### 3. SqlQueryBuilder for Dynamic Queries
```csharp
var builder = SqlQueryBuilder
    .From("invoices", allowedColumns)
    .SearchAcross(new[] { "invoice_number", "party_name" }, searchTerm)
    .ApplyFilters(filters)
    .OrderBy(sortBy, sortDescending)
    .Paginate(pageNumber, pageSize);

var (sql, parameters) = builder.BuildSelect();
```

#### 4. Service Layer
- Orchestrates business logic
- Validates input, calls repositories, returns `Result<T>`
- Located in `Application/Services/`

### File Naming Conventions

| Type | Location | Naming |
|------|----------|--------|
| Entity | `Core/Entities/{Group}/` | `PascalCase.cs` |
| Repository Interface | `Core/Interfaces/{Group}/` | `I{Entity}Repository.cs` |
| Repository Impl | `Infrastructure/Data/{Group}/` | `{Entity}Repository.cs` |
| Service Interface | `Application/Interfaces/{Group}/` | `I{Entity}Service.cs` |
| Service Impl | `Application/Services/{Group}/` | `{Entity}Service.cs` |
| DTO | `Application/DTOs/{Group}/` | `{Entity}Dto.cs` |
| Controller | `WebApi/Controllers/{Group}/` | `{Entity}Controller.cs` |

### Database Conventions (PostgreSQL)

**IMPORTANT: All SQL must be PostgreSQL-compatible. We do NOT use SQL Server, MySQL, or generic SQL.**

**Naming:**
- **Tables**: `lowercase_with_underscores` (e.g., `invoice_items`)
- **Columns**: `lowercase_with_underscores` (e.g., `created_at`)
- **Primary Keys**: `id` (UUID, use `gen_random_uuid()`)
- **Foreign Keys**: `{table_singular}_id` (e.g., `invoice_id`)
- **Indexes**: `idx_{table}_{columns}`
- **Constraints**: `chk_{table}_{description}`, `uq_{table}_{columns}`

**PostgreSQL-Specific Patterns:**

| Feature | PostgreSQL Syntax | NOT This (SQL Server) |
|---------|-------------------|----------------------|
| UUID generation | `gen_random_uuid()` | `NEWID()` |
| Boolean | `BOOLEAN` | `BIT` |
| Auto-increment | `SERIAL` or `GENERATED ALWAYS AS IDENTITY` | `IDENTITY(1,1)` |
| String concat | `\|\|` or `CONCAT()` | `+` |
| Limit rows | `LIMIT n` | `TOP n` |
| Upsert | `ON CONFLICT ... DO UPDATE` | `MERGE` |
| JSON | `JSONB` (preferred) or `JSON` | `NVARCHAR(MAX)` |
| Date functions | `NOW()`, `CURRENT_DATE` | `GETDATE()` |
| String to date | `TO_DATE()`, `TO_TIMESTAMP()` | `CONVERT()` |
| Null coalesce | `COALESCE()` | `ISNULL()` |

**Dapper Parameter Syntax:**
```csharp
// PostgreSQL uses @param (same as SQL Server, Dapper handles it)
await connection.QueryAsync<Entity>(
    "SELECT * FROM invoices WHERE company_id = @companyId AND status = @status",
    new { companyId, status });

// For IN clauses, use ANY with array
await connection.QueryAsync<Entity>(
    "SELECT * FROM invoices WHERE id = ANY(@ids)",
    new { ids = invoiceIds.ToArray() });

// ILIKE for case-insensitive search (PostgreSQL-specific)
await connection.QueryAsync<Entity>(
    "SELECT * FROM parties WHERE name ILIKE @search",
    new { search = $"%{searchTerm}%" });
```

### DbUp Migrations (PostgreSQL)

Migrations are managed with DbUp in `backend/migrations/`. Scripts run once and are tracked in `schemaversions` table.

**IMPORTANT: All migrations MUST use PostgreSQL syntax.**

**File Naming Convention:**
```
NNNN_description_of_change.sql
```
- `NNNN` = Sequential 4-digit number (0001, 0002, etc.)
- Use lowercase with underscores
- Be descriptive: `0142_add_bank_transaction_reconciliation_fields.sql`

**Migration Best Practices:**

| Do | Don't |
|----|-------|
| Use PostgreSQL syntax only | Use SQL Server/MySQL syntax |
| One logical change per migration | Multiple unrelated changes |
| Include rollback comments (for reference) | Assume rollback will be automated |
| Test migration on copy of prod data | Run untested migrations in prod |
| Add indexes for foreign keys | Forget indexes on FKs |
| Use `IF NOT EXISTS` for safety | Assume clean state |
| Keep migrations idempotent when possible | Write migrations that fail on re-run |
| Use `TIMESTAMPTZ` for timestamps | Use `DATETIME` (SQL Server) |
| Use `BOOLEAN` for true/false | Use `BIT` (SQL Server) |
| Use `TEXT` for long strings | Use `NVARCHAR(MAX)` |

**Migration Template (PostgreSQL):**
```sql
-- Migration: 0143_add_payment_status_to_invoices.sql
-- Description: Add payment_status enum column to invoices table
-- Author: [name]
-- Date: YYYY-MM-DD

-- Add enum type if not exists (PostgreSQL-specific)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'payment_status') THEN
        CREATE TYPE payment_status AS ENUM ('unpaid', 'partial', 'paid', 'overdue');
    END IF;
END$$;

-- Add column with PostgreSQL syntax
ALTER TABLE invoices
ADD COLUMN IF NOT EXISTS payment_status payment_status DEFAULT 'unpaid';

-- Add index (PostgreSQL CONCURRENTLY for production - optional)
CREATE INDEX IF NOT EXISTS idx_invoices_payment_status ON invoices(payment_status);

-- Rollback (for reference, not auto-executed):
-- ALTER TABLE invoices DROP COLUMN IF EXISTS payment_status;
-- DROP TYPE IF EXISTS payment_status;
```

**Common PostgreSQL Migration Patterns:**
```sql
-- Create table with UUID primary key
CREATE TABLE IF NOT EXISTS expense_claims (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id),
    employee_id UUID NOT NULL REFERENCES employees(id),
    amount NUMERIC(18,2) NOT NULL,
    status VARCHAR(50) DEFAULT 'draft',
    is_approved BOOLEAN DEFAULT FALSE,
    metadata JSONB,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Add column with default
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS tax_amount NUMERIC(18,2) DEFAULT 0;

-- Rename column
ALTER TABLE parties RENAME COLUMN old_name TO new_name;

-- Add foreign key
ALTER TABLE invoice_items
ADD CONSTRAINT fk_invoice_items_invoice
FOREIGN KEY (invoice_id) REFERENCES invoices(id) ON DELETE CASCADE;

-- Create partial index (PostgreSQL-specific)
CREATE INDEX IF NOT EXISTS idx_invoices_unpaid
ON invoices(due_date) WHERE status = 'unpaid';

-- Add check constraint
ALTER TABLE invoices ADD CONSTRAINT chk_invoices_amount_positive CHECK (amount >= 0);
```

**Running Migrations:**
```bash
cd backend && ./scripts/run-migrations.sh
# Or via dotnet
dotnet run --project src/WebApi -- --migrate
```

**Migration Checklist:**
- [ ] Sequential number doesn't conflict
- [ ] Tested locally with existing data
- [ ] Includes necessary indexes
- [ ] Column defaults specified where needed
- [ ] Foreign keys have `ON DELETE` behavior defined
- [ ] Large table changes consider locking impact

### API Response Format

```json
// Success (single item)
{ "data": { ... }, "correlationId": "uuid" }

// Success (list)
{ "items": [...], "totalCount": 100, "pageNumber": 1, "pageSize": 20, "totalPages": 5 }

// Error
{ "error": { "type": "Validation", "message": "...", "details": [] }, "correlationId": "uuid" }
```

---

## Frontend Architecture

### Technology Stack
- **Framework**: React 18 with TypeScript
- **Bundler**: Vite
- **Styling**: Tailwind CSS + shadcn/ui components
- **State**: React Query (server) + Context API (global) + nuqs (URL)
- **Forms**: React Hook Form + Zod validation
- **HTTP**: Axios with typed services

### Directory Structure

```
src/
├── App.tsx                       # Router configuration (80+ routes)
├── main.tsx                      # Entry point with providers
├── contexts/                     # Global state (Auth, Company, Theme)
├── features/{module}/            # Module-specific hooks
│   └── hooks/
│       ├── use{Entity}.ts        # Single item hook
│       ├── use{Entity}sPaged.ts  # Paginated list hook
│       ├── useCreate{Entity}.ts  # Create mutation
│       ├── useUpdate{Entity}.ts  # Update mutation
│       ├── useDelete{Entity}.ts  # Delete mutation
│       └── {entity}Keys.ts       # Query key factory
├── services/api/                 # API service classes
│   └── {domain}/
│       └── {entity}Service.ts
├── pages/{module}/               # Page components
├── components/
│   ├── ui/                       # Base UI (shadcn/radix)
│   └── forms/                    # Domain forms
└── types/                        # TypeScript definitions
```

### Key Patterns

#### 1. API Service Classes
Each domain has a typed service class:

```typescript
class InvoiceService {
  async getAll(companyId?: string): Promise<Invoice[]>
  async getById(id: string): Promise<Invoice>
  async getPaged(params: FilterParams): Promise<PagedResponse<Invoice>>
  async create(data: CreateInvoiceDto): Promise<Invoice>
  async update(id: string, data: UpdateInvoiceDto): Promise<Invoice>
  async delete(id: string): Promise<void>
}
```

#### 2. Query Key Factory Pattern
```typescript
export const invoiceKeys = {
  all: ['invoices'],
  lists: () => [...invoiceKeys.all, 'list'],
  list: (companyId?: string) => [...invoiceKeys.lists(), { companyId }],
  paged: (params?: FilterParams) => [...invoiceKeys.lists(), 'paged', params],
  details: () => [...invoiceKeys.all, 'detail'],
  detail: (id: string) => [...invoiceKeys.details(), id],
}
```

#### 3. React Query Hooks
```typescript
// Paginated list with filtering
export function useInvoicesPaged(params: FilterParams) {
  return useQuery({
    queryKey: invoiceKeys.paged(params),
    queryFn: () => invoiceService.getPaged(params),
    placeholderData: keepPreviousData,
  });
}

// Mutations with cache invalidation
export function useCreateInvoice() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: invoiceService.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: invoiceKeys.lists() });
      toast.success('Invoice created');
    },
  });
}
```

#### 4. URL State Persistence
Filters persist in URL using `nuqs`:
```typescript
const [page, setPage] = useQueryState('page', parseAsInteger.withDefault(1));
const [search, setSearch] = useQueryState('search', parseAsString);
```

#### 5. Multi-Company Context
```typescript
const { selectedCompany } = useCompany();
// All API calls include companyId from context or explicit param
```

### UI Components

| Component | Purpose |
|-----------|---------|
| `DataTable` | TanStack Table with sort/filter/pagination |
| `AsyncTypeahead` | Async search dropdown |
| `{Entity}Select` | Domain-specific selectors (CustomerSelect, etc.) |
| `{Entity}Form` | React Hook Form with Zod schema |

---

## Frontend Best Practices

### TanStack Query - Replace useEffect for Data Fetching

**CRITICAL: NEVER use `useEffect` for data fetching. Always use TanStack Query.**

```typescript
// ❌ WRONG - useEffect for fetching
function BadComponent() {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    fetchData().then(setData).catch(setError).finally(() => setLoading(false));
  }, []);

  // Race conditions, no caching, no refetch, manual cleanup
}

// ✅ CORRECT - TanStack Query
function GoodComponent() {
  const { data, isLoading, error } = useQuery({
    queryKey: ['entities'],
    queryFn: () => entityService.getAll(),
  });

  // Automatic caching, deduplication, refetch, error boundaries
}
```

**Key TanStack Query Patterns:**

| Pattern | Purpose | Example |
|---------|---------|---------|
| `queryKey` factory | Consistent, type-safe keys | `invoiceKeys.detail(id)` |
| `placeholderData: keepPreviousData` | Smooth pagination | Prevents flash on page change |
| `staleTime` | Control refetch | `staleTime: 5 * 60 * 1000` (5 min) |
| `enabled` | Conditional fetching | `enabled: !!companyId` |
| Optimistic updates | Instant UI feedback | `onMutate` with cache update |

**Mutation Pattern:**
```typescript
export function useUpdateInvoice() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateInvoiceDto }) =>
      invoiceService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: invoiceKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: invoiceKeys.lists() });
      toast.success('Invoice updated');
    },
    onError: (error) => {
      toast.error(error.message || 'Failed to update');
    },
  });
}
```

### nuqs - URL State Management

**Use nuqs for any state that should be shareable/bookmarkable.**

```typescript
import { useQueryState, parseAsInteger, parseAsString, parseAsStringEnum } from 'nuqs';

// Basic usage
const [page, setPage] = useQueryState('page', parseAsInteger.withDefault(1));
const [search, setSearch] = useQueryState('search', parseAsString.withDefault(''));

// Enum parsing (type-safe)
const [status, setStatus] = useQueryState(
  'status',
  parseAsStringEnum(['draft', 'sent', 'paid']).withDefault('draft')
);

// Combined filter object
const [filters, setFilters] = useQueryStates({
  page: parseAsInteger.withDefault(1),
  pageSize: parseAsInteger.withDefault(20),
  search: parseAsString,
  sortBy: parseAsString.withDefault('created_at'),
  sortDesc: parseAsBoolean.withDefault(true),
});
```

**When to use URL state vs local state:**

| State Type | Use URL (nuqs) | Use Local State |
|------------|----------------|-----------------|
| Pagination | ✅ | ❌ |
| Search/filters | ✅ | ❌ |
| Sort order | ✅ | ❌ |
| Selected tab | ✅ | ❌ |
| Modal open/close | ❌ | ✅ |
| Form input (pre-submit) | ❌ | ✅ |
| Temporary UI state | ❌ | ✅ |

### TanStack Table Integration

```typescript
// Column definitions with type safety
const columns: ColumnDef<Invoice>[] = [
  {
    accessorKey: 'invoiceNumber',
    header: ({ column }) => (
      <DataTableColumnHeader column={column} title="Invoice #" />
    ),
  },
  {
    accessorKey: 'amount',
    header: 'Amount',
    cell: ({ row }) => formatCurrency(row.getValue('amount')),
  },
];

// Hook integration
const table = useReactTable({
  data: invoices ?? [],
  columns,
  getCoreRowModel: getCoreRowModel(),
  manualPagination: true,  // Server-side pagination
  manualSorting: true,     // Server-side sorting
  pageCount: totalPages,
  state: {
    pagination: { pageIndex: page - 1, pageSize },
    sorting: [{ id: sortBy, desc: sortDesc }],
  },
  onPaginationChange: (updater) => {
    const next = typeof updater === 'function'
      ? updater({ pageIndex: page - 1, pageSize })
      : updater;
    setPage(next.pageIndex + 1);
  },
});
```

### React Hook Form + Zod Validation

```typescript
// Schema definition
const invoiceSchema = z.object({
  customerId: z.string().uuid('Select a customer'),
  invoiceDate: z.date(),
  dueDate: z.date(),
  items: z.array(z.object({
    description: z.string().min(1, 'Required'),
    quantity: z.number().positive(),
    rate: z.number().nonnegative(),
  })).min(1, 'At least one item required'),
});

type InvoiceFormData = z.infer<typeof invoiceSchema>;

// Form component
function InvoiceForm({ onSubmit, defaultValues }: Props) {
  const form = useForm<InvoiceFormData>({
    resolver: zodResolver(invoiceSchema),
    defaultValues,
  });

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)}>
        <FormField
          control={form.control}
          name="customerId"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Customer</FormLabel>
              <CustomerSelect {...field} />
              <FormMessage />
            </FormItem>
          )}
        />
      </form>
    </Form>
  );
}
```

---

## Anti-Patterns to Avoid

### Frontend Anti-Patterns

| Anti-Pattern | Problem | Correct Approach |
|--------------|---------|------------------|
| `useEffect` for data fetching | Race conditions, no caching | TanStack Query |
| `useEffect` + `useState` for derived data | Unnecessary re-renders | `useMemo` or compute inline |
| Prop drilling through many levels | Tight coupling, hard to maintain | Context, composition, or hooks |
| Storing server state in `useState` | Stale data, manual sync | TanStack Query cache |
| Manual loading/error states | Boilerplate, inconsistent | TanStack Query states |
| `any` TypeScript type | No type safety | Proper types or `unknown` |
| Index as key in lists | Bugs with reordering | Unique ID as key |
| Business logic in components | Hard to test, violates SRP | Extract to hooks |
| API calls in components | Scattered, untestable | Service classes |

### Backend Anti-Patterns

| Anti-Pattern | Problem | Correct Approach |
|--------------|---------|------------------|
| Throwing exceptions for flow control | Performance, unclear intent | Result pattern |
| String concatenation in SQL | SQL injection | Parameterized queries |
| Business logic in controllers | Fat controllers, untestable | Service layer |
| Business logic in repositories | Mixed concerns | Repository = data only |
| Returning entities from controllers | Leaking domain models | Return DTOs |
| `async void` methods | Lost exceptions | `async Task` |
| Not using `using` for disposables | Resource leaks | `using` or `await using` |

### Code Smell Checklist

Before committing, verify:

- [ ] No `useEffect` for data fetching
- [ ] No `any` types (use proper types)
- [ ] All lists have unique `key` props (not index)
- [ ] Server state uses TanStack Query, not `useState`
- [ ] URL-shareable state uses nuqs
- [ ] Forms use React Hook Form + Zod
- [ ] API calls go through service classes
- [ ] Components are focused (SRP)
- [ ] No prop drilling beyond 2 levels

---

## Authentication & Authorization

### Backend
- JWT tokens with refresh mechanism
- Role-based policies: `Admin`, `HR`, `Accountant`, `Manager`, `Employee`
- Multi-company access via `user_company_assignments`

### Frontend
- `AuthContext` manages tokens and user state
- Tokens stored in localStorage: `admin_access_token`, `admin_refresh_token`
- Axios interceptor attaches Authorization header

---

## Adding a New Feature

### Backend Checklist
1. Create entity in `Core/Entities/{Group}/`
2. Create repository interface in `Core/Interfaces/{Group}/`
3. Implement repository in `Infrastructure/Data/{Group}/`
4. Create DTOs in `Application/DTOs/{Group}/`
5. Create service interface in `Application/Interfaces/{Group}/`
6. Implement service in `Application/Services/{Group}/`
7. Add controller in `WebApi/Controllers/{Group}/`
8. Register in DI (`ServiceCollectionExtensions`)
9. Add database migration if needed

### Frontend Checklist
1. Add types in `services/api/types/{entity}.ts`
2. Create service class in `services/api/{domain}/`
3. Create hooks in `features/{module}/hooks/`
4. Add page component in `pages/{module}/`
5. Add form component if needed
6. Add route in `App.tsx`
7. Add navigation item in `data/navigation.ts`

---

## Related Documentation
- Module-specific docs: `docs/modules/`
- Implementation plans: `docs/PLAN_*.md`
- Gap analysis: `docs/system-gap-analysis.md`
- Backend conventions: `backend/CLAUDE.md`
