# Alignd — Real-time Sprint Planning Poker

A real-time planning poker application built with C# Clean Architecture and Angular 20.

## Tech Stack

**Backend**
- .NET 9 — C# Clean Architecture (SharedKernel → Domain → Application → Infrastructure → API)
- PostgreSQL + EF Core 9 with snake_case conventions
- SignalR for real-time room events
- JWT authentication (participant tokens + admin tokens)
- Result pattern (no exceptions for control flow)

**Frontend** *(coming soon)*
- Angular 20 standalone components, screaming architecture
- Tailwind CSS + Space Grotesk + Inter fonts
- `@microsoft/signalr` for hub connection

---

## Getting Started

### Prerequisites
- .NET 9 SDK
- PostgreSQL 15+

### 1. Configure the database

Update `src/Alignd.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=alignd;Username=postgres;Password=yourpassword"
  },
  "Jwt": {
    "Secret": "your-secret-key-at-least-32-characters-long"
  }
}
```

### 2. Run migrations

```bash
dotnet ef migrations add InitialSchema \
  --project src/Alignd.Infrastructure \
  --startup-project src/Alignd.API \
  --output-dir Persistence/Migrations

dotnet ef database update \
  --project src/Alignd.Infrastructure \
  --startup-project src/Alignd.API
```

### 3. Run the API

```bash
cd src/Alignd.API
dotnet run
```

API runs on `https://localhost:7000` — Swagger at `/swagger`.

---

## Project Structure

```
src/
├── Alignd.SharedKernel/      Result<T>, ResultCode, ResultError
├── Alignd.Domain/            Entities, Enums, ValueObjects
├── Alignd.Application/       Use case handlers, Repository interfaces
├── Alignd.Infrastructure/    EF Core, Repositories, SignalR, Auth, Profanity
└── Alignd.API/               Controllers, VotingHub, Middleware, Program.cs
```

## SignalR Event Contract

### Client → Server (hub methods)
| Method | Parameters |
|--------|-----------|
| `JoinRoomAsync` | `roomCode` |
| `StartRoundAsync` | `roomCode`, `freeTitle?` |
| `CastVoteAsync` | `roomCode`, `value` |
| `EndRoundAsync` | `roomCode` |
| `StartNextRoundAsync` | `roomCode` |
| `ClaimAdminAsync` | `roomCode` |
| `SetWatcherAsync` | `roomCode`, `isWatcher` |

### Server → Client (events)
| Event | Payload |
|-------|---------|
| `user-joined` | `{ participantId, username, role }` |
| `user-left` | `{ participantId, username }` |
| `round-started` | `{ roundId, taskTitle?, freeTitle?, votingType }` |
| `vote-cast` | `{ participantId }` — value hidden until reveal |
| `round-ended` | `{ roundId, votes[], topVotes[] }` |
| `task-completed` | `{ taskId, nextTaskId? }` |
| `room-reset` | `{ newRoundId, suggestedTitle? }` |
| `admin-changed` | `{ newAdminId, username }` |
| `room-finished` | `{}` — triggers easter egg |
| `error` | `{ code, message }` |

## Vote Values

| Type | Values |
|------|--------|
| Fibonacci | `1 2 3 5 8 13 21 ? ☕` |
| Shirt size | `XS S M L XL XXL ? ☕` |

## Profanity Filter

Replace `src/Alignd.Infrastructure/Profanity/profanity.txt` with the full
multilingual word list from [LDNOOBW](https://github.com/LDNOOBW/List-of-Dirty-Naughty-Obscene-and-Otherwise-Bad-Words).
The file is embedded as a resource at build time — no runtime file path needed.
