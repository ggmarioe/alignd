# Alignd — Project Overview

Alignd is a planning poker web application. Players join a room by code, cast votes on work items, and the admin reveals results in real time via SignalR.

## Structure

- `frontend/` — Angular 20 standalone SPA (`ng serve` on :4200, proxies to :7000)
- `src/` — .NET 9 C# backend (Clean Architecture: Domain / Application / Infrastructure / API)
- `test/` — Backend NUnit test suite

## Key Technologies

**Frontend:** Angular 20, Signals, Tailwind CSS v4, Vitest, Angular Testing Library  
**Backend:** .NET 9, EF Core 9 + PostgreSQL, SignalR, JWT (participant + admin tokens)

## Agent Transparency
Whenever you delegate a task to a subagent or invoke a skill, explicitly announce it before doing so. Use this format:

> 🤖 **Delegating to agent:** `<agent-name>` — <brief reason why>

After the agent completes, summarize what it returned with:

> ✅ **Agent `<agent-name>` completed:** <one-line summary of result>

## Sub-Agent Routing

**Use @frontend-developer for:**
- Any work in `frontend/src/`
- Angular components, signals, templates, Tailwind styling
- Vitest / Angular Testing Library tests
- SignalR client integration (`VotingHub` events)

**Use @angular-architect for:**
- Routing strategy, lazy loading, DI tree design
- Performance budgets, change detection strategy
- Large-scale structural decisions

**Parallel dispatch** (run agents simultaneously):
- Frontend UI changes that don't touch API contracts
- Backend-only logic changes with no frontend dependency

**Sequential dispatch:**
- Backend adds/changes a SignalR event → frontend handler must follow
- API response shape changes → frontend service/model updates after

## Architectural Conventions

- **Result monad** on the backend — never throw for control flow; use `Result<T>`
- **Signals-first** on the frontend — local state uses `signal()`/`computed()`; avoid BehaviorSubjects in components
- **No NgRx** — not used in this project; do not introduce it
- **SignalR stubs** — `VotingRoom` currently holds placeholder signals; wire them up to hub events when implementing real-time features
- **Screaming architecture** for components: `atoms/` → `molecules/` → `organisms/` → page components in feature folders
