# Quick Start - HealthTracker

Get the app running in 5 minutes.

## Prerequisites

- .NET 10 SDK
- Node.js 16+
- PostgreSQL running

## Run in 3 Steps

### 1️⃣ Start Backend (Terminal 1)

```bash
cd HealthTracker.Api
dotnet run
```

Wait for: `Now listening on: http://localhost:5000`

### 2️⃣ Start Frontend (Terminal 2)

```bash
cd healthtracker-web
npm install  # Only first time
npm run dev
```

Browser will open to: `http://localhost:3000`

### 3️⃣ Test It Out

1. **Register**: Create account with any email/password
2. **Login**: Sign in with your credentials
3. **Add Vitals**: Click "+ Add Vital Signs"
   - Heart Rate: 72
   - Temperature: 36.5
   - Oxygen: 98
4. **View Dashboard**: See your vitals displayed
5. **Check History**: Scroll down for table

---

## Common Commands

```bash
# Backend
cd HealthTracker.Api && dotnet run      # Run API
dotnet watch run                        # Auto-reload

# Frontend
cd healthtracker-web
npm run dev                             # Dev server
npm run build                           # Build prod
npm run preview                         # Preview build
```

## URLs

- **Frontend**: http://localhost:3000
- **API**: http://localhost:5000
- **API Docs**: http://localhost:5000/scalar/v1

## First Time Setup Needed?

PostgreSQL not running?

```bash
# Docker
docker run --name healthdb -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:15

# Or install locally and start service
```

## API Test (Optional)

Login and get token:
```bash
curl -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"test123"}'
```

## Stuck?

- Check **SETUP_GUIDE.md** for detailed instructions
- See **API_DOCUMENTATION.md** for endpoint details
- Check **healthtracker-web/README.md** for frontend info

---

## What's Running

```
┌─────────────────────────────────────────┐
│   HealthTracker Application             │
├─────────────────────────────────────────┤
│                                         │
│  Frontend (React)                       │
│  ↓ http://localhost:3000                │
│                                         │
│  ↓↑ HTTP Requests                       │
│                                         │
│  Backend (.NET)                         │
│  ↓ http://localhost:5000                │
│                                         │
│  ↓↑ SQL Queries                         │
│                                         │
│  Database (PostgreSQL)                  │
│  ↓ localhost:5432                       │
│                                         │
└─────────────────────────────────────────┘
```

---

That's it! You're ready to track vital signs! 💪
