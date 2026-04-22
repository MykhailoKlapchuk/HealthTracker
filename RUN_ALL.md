# How to Run the Complete HealthTracker Solution

## Prerequisites

- .NET 10 SDK installed
- Node.js 16+ installed
- PostgreSQL running (local or Docker)

---

## Step 1: Start PostgreSQL (If Not Running)

### Option A: Using Docker
```bash
docker run --name healthdb -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:15
```

### Option B: Using Local PostgreSQL
Make sure PostgreSQL service is running on port 5432.

---

## Step 2: Start Backend API

**Open Terminal 1 and run:**

```bash
cd HealthTracker.Api
dotnet run
```

**Wait for this message:**
```
Now listening on: http://localhost:5000
```

✅ API is ready at: http://localhost:5000

---

## Step 3: Start Frontend App

**Open Terminal 2 (keep Terminal 1 running) and run:**

```bash
cd healthtracker-web
npm run dev
```

**Wait for this message:**
```
VITE v5.x.x  ready in xxx ms

➜  Local:   http://localhost:3000/
```

✅ Frontend is ready at: http://localhost:3000

---

## Step 4: Open Your Browser

Navigate to: **http://localhost:3000**

---

## Complete Setup Example (Using 3 Terminal Windows)

```
┌─ Terminal 1: PostgreSQL ────────────────────┐
│ $ docker run --name healthdb ...            │
│ (running in background)                     │
└─────────────────────────────────────────────┘

┌─ Terminal 2: Backend API ───────────────────┐
│ $ cd HealthTracker.Api                      │
│ $ dotnet run                                │
│ Now listening on: http://localhost:5000     │
│ (Press Ctrl+C to stop)                      │
└─────────────────────────────────────────────┘

┌─ Terminal 3: Frontend App ──────────────────┐
│ $ cd healthtracker-web                      │
│ $ npm run dev                               │
│ ➜ Local:   http://localhost:3000/           │
│ (Press Ctrl+C to stop)                      │
└─────────────────────────────────────────────┘
```

---

## Using One Terminal (Alternative)

If you want to run both services in one terminal using `&`:

```bash
cd HealthTracker.Api && dotnet run &
cd ../healthtracker-web && npm run dev
```

To stop: Press `Ctrl+C` (stops frontend, then you may need to kill backend process)

---

## Verify Everything is Working

### Check Backend API
```bash
curl http://localhost:5000/scalar/v1
```
Should open Scalar API documentation page.

### Check Frontend
Open browser: http://localhost:3000
Should see HealthTracker login page.

### Test End-to-End
1. Register with email: `test@example.com` password: `test123`
2. Login with those credentials
3. Add vital signs (heart rate, temperature, etc.)
4. View dashboard with recorded data

---

## What's Running

```
┌──────────────────────────────────────────────────┐
│           HealthTracker Solution                 │
├──────────────────────────────────────────────────┤
│                                                  │
│  Frontend (React)                               │
│  http://localhost:3000                          │
│  ├─ Pages: Login, Dashboard                     │
│  ├─ Components: Forms, History, Cards           │
│  └─ CSS: Responsive design with gradients       │
│                                                  │
│       ↕ (HTTP Requests)                         │
│                                                  │
│  Backend (.NET Core)                            │
│  http://localhost:5000                          │
│  ├─ /auth/register (create user)                │
│  ├─ /auth/login (authenticate)                  │
│  ├─ /vitals (add vital signs)                   │
│  └─ /vitals (get vital signs history)           │
│                                                  │
│       ↕ (SQL Queries)                           │
│                                                  │
│  Database (PostgreSQL)                          │
│  localhost:5432                                 │
│  ├─ Users table                                 │
│  ├─ VitalSigns table                            │
│  └─ HeartRateHistory table (legacy)             │
│                                                  │
└──────────────────────────────────────────────────┘
```

---

## Troubleshooting

### Port Already in Use

**For Backend (5000):**
```bash
# Find process using port 5000
netstat -ano | findstr :5000

# Kill the process (replace PID)
taskkill /PID <PID> /F
```

**For Frontend (3000):**
```bash
# Find process using port 3000
netstat -ano | findstr :3000

# Kill the process
taskkill /PID <PID> /F
```

### PostgreSQL Connection Error

Make sure PostgreSQL is running:
```bash
# Docker version
docker ps | grep healthdb

# If not running, start it
docker start healthdb
```

### npm Modules Missing

```bash
cd healthtracker-web
npm install
npm run dev
```

### dotnet Build Errors

```bash
cd HealthTracker.Api
dotnet restore
dotnet build
dotnet run
```

---

## Stopping Services

### Stop All Services (Keep running for development)

**To gracefully stop:**
- Terminal 1 (Frontend): Press `Ctrl+C`
- Terminal 2 (Backend): Press `Ctrl+C`
- Terminal 3 (Database): Press `Ctrl+C` or `docker stop healthdb`

---

## Development Workflow

### While Developing

1. **Backend changes**: Changes auto-reload with `dotnet run`
2. **Frontend changes**: Changes auto-reload with `npm run dev` (hot reload)
3. **Database changes**: Will auto-create tables on first run

### Testing the API

Use Scalar API docs at: http://localhost:5000/scalar/v1
- Try endpoints directly in browser
- Test with authentication tokens
- See request/response examples

### Viewing Database

```bash
# Connect to PostgreSQL
psql -U postgres -d healthdb

# List tables
\dt

# View users
SELECT * FROM "Users";

# View vital signs
SELECT * FROM "VitalSigns";
```

---

## Summary

| Component | Command | URL | Terminal |
|-----------|---------|-----|----------|
| PostgreSQL | `docker run ...` | localhost:5432 | 1 |
| Backend API | `dotnet run` | localhost:5000 | 2 |
| Frontend | `npm run dev` | localhost:3000 | 3 |

**Start all 3 and you're ready to go!** 🚀

---

## Quick Reference Commands

```bash
# Clone/Setup (one time)
cd AspireTest
dotnet restore
cd healthtracker-web && npm install && cd ..

# Run everything (use 3 terminals)
Terminal 1: docker run --name healthdb -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:15
Terminal 2: cd HealthTracker.Api && dotnet run
Terminal 3: cd healthtracker-web && npm run dev

# Then visit: http://localhost:3000
```

---

**You're all set! Follow the steps above and the app will be running.** ✨
