# GoalFlow – Infrastructure Layer Service Map

## 🎯 Purpose

The **Infrastructure layer** wires up all **technology-facing services** that support the Application and Domain layers.  
It ensures the system can persist data, run background jobs, cache results, authenticate users, and log reminders — without leaking technology details into the business logic.

---

## 🗂 Registered Services Overview

### 1) Persistence (EF Core DbContext)
- **Service:** `GoalFlowDbContext`
- **Registered as:** `Scoped`
- **Responsibilities:**
  - Provides EF Core persistence for **Goals**, **ProgressLogs**, **Reminders**, and **RefreshTokens**.
  - Includes ASP.NET Core **Identity tables**.
- **Where used:** Command/Query handlers that require entity tracking (e.g., CreateGoal, UpdateGoal, CreateReminder).
- 📂 Code: [`GoalFlowDbContext`](./Persistence/GoalFlowDbContext.cs)

---

### 2) MediatR
- **Service:** `IMediator` (from MediatR)
- **Registered as:** All handlers from the `GoalFlow.Infrastructure` assembly.
- **Responsibilities:**
  - Dispatches `IRequest<T>` commands/queries to handlers.
  - Ensures Infrastructure responds only to Application contracts.
- **Where used:** Every Application request → handled in Infrastructure.

---

### 3) SQL (Dapper Connection Factory)
- **Service:** `ISqlConnectionFactory` → `SqlConnectionFactory`
- **Registered as:** `Singleton`
- **Responsibilities:**
  - Creates raw `SqlConnection` instances.
  - Enables high-performance queries/commands using **Dapper**.
- **Where used:** `GetGoalsHandler` and other read-heavy queries.
📂 Code: [`SqlConnectionFactory`](./Sql/SqlConnectionFactory.cs)
---

### 4) Redis Caching
- **Service:** `IConnectionMultiplexer` (StackExchange.Redis)  
- **Service:** `IGoalsCache` → `GoalsCache`  
- **Registered as:** `Singleton`  
- **Responsibilities:**
  - Redis multiplexer shared across app.
  - Tag-based caching for user goal lists (build key, cache, invalidate).
- **Where used:** `GetGoalsHandler` (read-through caching), `UpdateGoalHandler` & `DeleteGoalHandler` (invalidate user caches).
Code: [`GoalsCache`](./Caching/GoalsCache.cs)
---

### 5) Background Jobs (Hangfire)
- **Service:** Hangfire server (not in Test environment)  
- **Registered as:** Hosted background service  
- **Responsibilities:**
  - Provides persistent job scheduling.
  - Stores jobs in SQL Server (via `Hangfire.SqlServer`).
  - Runs `ReminderProcessor` on schedule.
- **Where used:** Reminder delivery and other recurring tasks.
Code: [`ReminderProcessor`](./Reminders/ReminderProcessor.cs)
---

### 6) Reminder Processor
- **Service:** `ReminderProcessor`  
- **Registered as:** `Scoped`  
- **Responsibilities:**
  - Queries active reminders that are due.
  - Logs (or sends) notifications.
  - Uses `NCrontab` to compute next run times.
- **Where used:** Triggered by Hangfire / hosted background jobs.

---

### 7) ASP.NET Core Identity
- **Service:** `IdentityCore<IdentityUser>` with EF stores  
- **Registered as:** Default DI registrations  
- **Responsibilities:**
  - User authentication & authorization.
  - Stores refresh tokens, password hashes, claims.
  - Provides SignInManager & token providers.
- **Where used:** Login, registration, refresh token flows.

---

### 8) Dependency Injection (Service Registration)
- Central entry point: [`DependencyInjection`](./DependencyInjection.cs)
- Registers:
  - DbContext
  - MediatR handlers
  - Dapper factory
  - Redis + GoalsCache
  - Hangfire (if not Test)
  - ReminderProcessor
  - IdentityCore
  
 ---

## 🔑 Key Design Principles

- **Dependency Inversion:** Application layer defines interfaces, Infrastructure implements them.  
- **Replaceable Tech:** Switch out EF Core, Redis, or Hangfire without touching business logic.  
- **Resilience:** Redis failures don’t break app; Hangfire disabled in Test env.  
- **Consistency:** `Result<T>` returned from handlers → predictable API responses.  

---

## 📊 Visual Map (High-Level)
+-------------------+          +-------------------+
|   Application     |          |     Domain        |
|  (Commands/Query) | <------> |  (Entities/Rules) |
+-------------------+          +-------------------+
          |                               
          v
+---------------------------------------------------+
|                 Infrastructure                     |
|                                                   |
|  EF Core (DbContext)   Dapper (SQL)   Redis Cache |
|  Hangfire Jobs         ReminderProcessor          |
|  Identity/Auth         Logging                    |
+---------------------------------------------------+

