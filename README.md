# Digiprise PMS — Standalone ASP.NET Core 8 Application

> **Jira-like Project Management System** built natively on ASP.NET Core 8  
> Enterprise Edition v1.0 | Clean Architecture | Zero external NuGet dependencies

---

## Quick Start

```bash
# Clone / extract the project
cd Digiprise.PMS

# Restore assets (once NuGet access is available) and run
dotnet run --project Digiprise.PMS.API

# Or use the publish folder directly:
dotnet Digiprise.PMS.API.dll
```

The API starts at **http://localhost:5000** (or HTTPS on 5001).

**Demo Credentials** (seeded automatically on startup):
| Role  | Email                          | Password    |
|-------|-------------------------------|-------------|
| Admin | admin@demo.digiprise.io        | Admin@123!  |
| Dev   | dev@demo.digiprise.io          | Dev@123!    |

**API Explorer:** http://localhost:5000/docs  
**Health Check:** http://localhost:5000/health/ready

---

## Architecture Overview

```
Digiprise.PMS/
├── Digiprise.PMS.Domain/           # Entities, Value Objects, Domain Events, Interfaces
├── Digiprise.PMS.Contracts/        # DTOs, Request/Response models
├── Digiprise.PMS.Application/      # Use-case Services, Application Interfaces
├── Digiprise.PMS.Infrastructure/   # Repositories (In-Memory / EF Core), JWT, Seeder
├── Digiprise.PMS.API/              # ASP.NET Core Web API — Controllers, Hubs, Middleware
└── Digiprise.PMS.Tests/            # 30 domain-level tests (zero-dependency runner)
```

### Layers & Responsibilities

| Layer | Project | Responsibility |
|---|---|---|
| **Domain** | `Digiprise.PMS.Domain` | Aggregate roots, business invariants, domain events |
| **Contracts** | `Digiprise.PMS.Contracts` | DTOs, API request/response models |
| **Application** | `Digiprise.PMS.Application` | Service implementations, orchestration, IQL search |
| **Infrastructure** | `Digiprise.PMS.Infrastructure` | In-memory store, repositories, JWT, password hasher |
| **API** | `Digiprise.PMS.API` | Controllers, SignalR Hub, middleware pipeline |

---

## API Reference

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/auth/login?tenantId=1` | Login, returns JWT tokens |
| POST | `/api/v1/auth/register` | Register user + create tenant |
| POST | `/api/v1/auth/refresh` | Refresh access token |
| POST | `/api/v1/auth/logout` | Revoke refresh token |

**Login Example:**
```bash
curl -X POST http://localhost:5000/api/v1/auth/login?tenantId=1 \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@demo.digiprise.io","password":"Admin@123!"}'
```

### Projects
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/projects` | List all projects |
| POST | `/api/v1/projects` | Create project |
| GET | `/api/v1/projects/{id}` | Get project by ID |
| GET | `/api/v1/projects/key/{key}` | Get project by key (e.g. DEMO) |
| PUT | `/api/v1/projects/{id}` | Update project |
| POST | `/api/v1/projects/{id}/archive` | Archive project |
| DELETE | `/api/v1/projects/{id}` | Soft-delete project |
| POST | `/api/v1/projects/{id}/members` | Add member |
| DELETE | `/api/v1/projects/{id}/members/{userId}` | Remove member |
| GET | `/api/v1/projects/{id}/sprints` | List sprints |
| POST | `/api/v1/projects/{id}/sprints` | Create sprint |

### Issues
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/issues` | Create issue |
| GET | `/api/v1/issues/{id}` | Get issue with full detail |
| PUT | `/api/v1/issues/{id}` | Update issue |
| PATCH | `/api/v1/issues/{id}` | Partial update |
| DELETE | `/api/v1/issues/{id}` | Delete issue |
| POST | `/api/v1/issues/{id}/transitions` | Transition workflow status |
| POST | `/api/v1/issues/search` | IQL search |
| POST | `/api/v1/issues/bulk` | Bulk update (max 50) |
| GET | `/api/v1/issues/project/{id}` | Issues for project |
| GET | `/api/v1/issues/project/{id}/backlog` | Backlog (unassigned to sprint) |
| POST | `/api/v1/issues/{id}/comments` | Add comment |
| GET | `/api/v1/issues/{id}/comments` | List comments |

**IQL Search Example:**
```bash
curl -X POST http://localhost:5000/api/v1/issues/search \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"iql":"issuetype = Bug AND priority = Critical","maxResults":20,"startAt":0}'
```

**Create Issue Example:**
```bash
curl -X POST http://localhost:5000/api/v1/issues \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "projectKey": "DEMO",
    "issueType": "Story",
    "summary": "Implement OAuth2 login for mobile app",
    "priority": "Major",
    "storyPoints": 5,
    "labels": ["auth","mobile"]
  }'
```

### Sprints
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/sprints/{id}` | Get sprint |
| GET | `/api/v1/sprints/active/{boardId}` | Get active sprint |
| POST | `/api/v1/sprints/{id}/start` | Start sprint |
| POST | `/api/v1/sprints/{id}/close` | Close sprint |
| GET | `/api/v1/sprints/{id}/burndown` | Burndown chart data |

### Dashboard
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/dashboard/summary` | Summary stats |
| GET | `/api/v1/dashboard/assigned-to-me` | Issues assigned to current user |

### Notifications
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/notifications/unread` | Unread notifications |
| POST | `/api/v1/notifications/{id}/read` | Mark one read |
| POST | `/api/v1/notifications/read-all` | Mark all read |

### Health
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health/live` | Liveness probe |
| GET | `/health/ready` | Readiness probe |

---

## Real-Time (SignalR)

Connect to `/hubs/pms?access_token=YOUR_JWT` and use hub methods:

```javascript
// Join a board room to receive live card updates
connection.invoke("JoinBoard", boardId);

// Join a project room
connection.invoke("JoinProject", projectId);

// Listen for events
connection.on("IssueStatusChanged", (data) => { /* update UI */ });
connection.on("IssueAssigned", (data) => { /* update assignee */ });
connection.on("CommentAdded", (data) => { /* append comment */ });
connection.on("SprintStarted", (data) => { /* show sprint active */ });
connection.on("NotificationReceived", (data) => { /* show bell badge */ });
```

---

## Domain Model

### Issue Hierarchy
```
Epic (Level 1)
  └── Story (Level 2)
        └── Subtask (Level 3)
              └── Sub-Subtask (Level 4)
```

### Issue Types
`Epic` · `Story` · `Subtask` · `SubSubtask` · `Bug` · `Task`

### Priority Levels
`Blocker > Critical > Major > Minor > Trivial`

### Sprint Lifecycle
`Created → Active → Closed`

### Project Methodologies
`Scrum` (sprints + burndown) · `Kanban` (continuous flow) · `Hybrid`

---

## Security

- **JWT**: HS256-signed, 15-minute access token + 7-day refresh token rotation
- **RBAC**: Role-based authorization (`Admin`, `Developer`, etc.) enforced at controller level
- **Multi-tenant**: Every query scoped by `TenantId` — cross-tenant data access is impossible
- **Password hashing**: PBKDF2-SHA256, 100,000 iterations, unique per-user salt
- **Security headers**: `X-Content-Type-Options`, `X-Frame-Options`, `X-XSS-Protection`, `Referrer-Policy`
- **CORS**: Strict origin whitelist configured in `appsettings.json`

---

## Configuration

Edit `appsettings.json` in `Digiprise.PMS.API/`:

```json
{
  "Jwt": {
    "Secret": "CHANGE-THIS-IN-PRODUCTION-MINIMUM-32-CHARS",
    "Issuer": "Digiprise.PMS",
    "Audience": "Digiprise.PMS.Client"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000", "https://your-frontend.com"]
  }
}
```

---

## Upgrading to Production (EF Core + SQL Server)

The repository layer uses the `IRepository<T>` interface. To switch from in-memory to SQL Server:

1. Add NuGet packages (when available):
   ```bash
   dotnet add Digiprise.PMS.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
   ```

2. Re-add `PmsDbContext` (the full EF Core schema is included in the codebase as reference).

3. In `Program.cs`, swap:
   ```csharp
   // From: in-memory
   builder.Services.AddScoped<IProjectRepository, ProjectRepository>();

   // To: EF Core
   builder.Services.AddScoped<IProjectRepository, EfProjectRepository>();
   ```

4. Update `appsettings.json` with your SQL Server connection string.

---

## Running Tests

```bash
dotnet run --project Digiprise.PMS.Tests
```

Output:
```
🧪 Digiprise PMS — Domain Test Suite
────────────────────────────────────────────────────────────
  📦 ProjectEntityTests        ✅ 7/7
  📦 IssueEntityTests          ✅ 8/8
  📦 SprintEntityTests         ✅ 5/5
  📦 AttachmentEntityTests     ✅ 2/2
  📦 IssueLinkEntityTests      ✅ 2/2
  📦 TenantEntityTests         ✅ 2/2
  📦 PasswordHasherTests       ✅ 4/4
────────────────────────────────────────────────────────────
Results: 30 passed, 0 failed ✅
```

---

## Key Design Decisions

| Decision | Implementation |
|----------|----------------|
| Clean Architecture | 5 layers with strict dependency inversion |
| No external packages | Only `Microsoft.AspNetCore.App` framework reference |
| Custom JWT | Manual HS256 using `System.Security.Cryptography` |
| In-memory storage | `ConcurrentDictionary<int, T>` — swap-ready via IRepository |
| Domain purity | Entities enforce invariants; no anemic domain model |
| Real-time | ASP.NET Core SignalR hub with board/project/user groups |
| IQL Search | Simplified query parser translatable to full SQL |
| CQRS-ready | Service layer structured for MediatR Command/Query split |
