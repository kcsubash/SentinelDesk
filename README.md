# SentinelDesk

A real-time security monitoring dashboard for detecting suspicious login activity. Built as a personal project using .NET microservices and Angular, to practice microservices architecture, event-driven design, and cloud deployment.

## Architecture Plan

Two separate .NET microservices that communicate only through RabbitMQ, not direct API calls.

- **Detection Service** — generates/analyzes login events and runs them through detection rules (impossible travel, brute force, etc)
- When a rule fires, it publishes a `SuspiciousActivityDetected` message to **RabbitMQ** (via MassTransit)
- **Core API Service** — consumes that message, stores it as an alert, handles auth (JWT/RBAC), and exposes a REST API
- Core API pushes new alerts to the **Angular Dashboard** in real time over **SignalR**
- Core API also uses **Redis** for rate limiting
- Detection and Core API never call each other directly — the only thing that crosses between them is the RabbitMQ message, so each service can be built, deployed, and scaled independently

## Tech Stack

- Backend: ASP.NET Core (minimal APIs), C#
- Messaging: RabbitMQ + MassTransit
- Database: Azure SQL
- Cache / rate limiting: Redis
- Real-time updates: SignalR
- Auth: JWT + role-based access control
- Frontend: Angular
- Containers: Docker
- CI/CD: GitHub Actions
- Cloud: Azure