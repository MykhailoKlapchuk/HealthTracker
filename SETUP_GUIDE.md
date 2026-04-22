# HealthTracker Setup Guide

Complete setup instructions for the HealthTracker application with .NET backend and React frontend.

## Project Structure

```
AspireTest/
├── HealthTracker.Api/              # .NET Backend API
├── HealthTracker.AppHost/          # Aspire orchestration
├── HealthTracker.ServiceDefaults/  # Shared service configuration
├── HealthTracker.Worker/           # Background worker
├── healthtracker-web/              # React Frontend
├── API_DOCUMENTATION.md            # API reference
└── SETUP_GUIDE.md                  # This file
```

## Prerequisites

### System Requirements
- Windows 11 or later (or WSL2 on Windows)
- .NET 10 SDK or later
- Node.js 16+ and npm
- PostgreSQL 13+ (or use Docker)
- Git

### Development Tools (Recommended)
- Visual Studio 2022 or VS Code
- Azure Data Studio or pgAdmin (for database management)
- Postman or Thunder Client (for API testing)

## Quick Start

### 1. Backend Setup

#### A. Start PostgreSQL

**Option 1: Using Docker**
```bash
docker run --name healthdb -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:15
```

**Option 2: Local PostgreSQL**
Make sure PostgreSQL service is running on default port 5432.

#### B. Start the API

```bash
cd HealthTracker.Api
dotnet run
```

The API will be available at: `http://localhost:5000`

You'll see output like:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

#### C. Verify API is Working

Open in browser or use curl:
```bash
curl http://localhost:5000/scalar/v1
```

This opens the Scalar API reference page showing all available endpoints.

### 2. Frontend Setup

#### A. Install Dependencies

```bash
cd healthtracker-web
npm install
```

#### B. Start Development Server

```bash
npm run dev
```

The app will open at: `http://localhost:3000`

#### C. Test the Application

1. Register a new account
2. Login with your credentials
3. Add vital signs (heart rate, temperature, etc.)
4. View history

---

## Database Setup

### Auto-Creation (Default)

Tables are automatically created on first API run:
- `Users` - User accounts
- `VitalSigns` - Vital signs records
- `HeartRateHistory` - Legacy heart rate data

### Manual Database Creation

If you need to manually set up the database:

```sql
-- Connect to healthdb database
psql -U postgres -d healthdb

-- Create Users table
CREATE TABLE "Users" (
  "Id"           SERIAL PRIMARY KEY,
  "Email"        VARCHAR(255) UNIQUE NOT NULL,
  "PasswordHash" VARCHAR(255) NOT NULL,
  "CreatedAt"    TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create VitalSigns table
CREATE TABLE "VitalSigns" (
  "Id"               SERIAL PRIMARY KEY,
  "UserId"           INTEGER NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
  "HeartRate"        INTEGER,
  "TemperatureCelsius" DECIMAL(5,2),
  "OxygenSaturation" INTEGER,
  "Timestamp"        TIMESTAMPTZ NOT NULL,
  "Notes"            TEXT
);

-- Create HeartRateHistory table (legacy)
CREATE TABLE "HeartRateHistory" (
  "Id"        SERIAL PRIMARY KEY,
  "Bpm"       INTEGER NOT NULL,
  "Timestamp" TIMESTAMPTZ NOT NULL,
  "State"     TEXT NOT NULL
);
```

### Database Connection String

Default: `postgresql://postgres:postgres@localhost:5432/healthdb`

To change, modify `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "healthdb": "postgresql://user:password@localhost:5432/healthdb"
  }
}
```

---

## Configuration

### Backend Configuration

#### Environment Settings

**Development** (appsettings.Development.json)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

**Production** (appsettings.json)
- Disable detailed error messages
- Enable HTTPS only
- Set CORS to specific domains
- Configure rate limiting

#### Database Settings

Update connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "healthdb": "postgresql://user:password@host:port/healthdb"
  }
}
```

### Frontend Configuration

#### Environment Variables

**.env file**
```
VITE_API_URL=http://localhost:5000
```

For production:
```
VITE_API_URL=https://api.yourdomain.com
```

#### Build Configuration

**Vite** (`vite.config.js`):
```javascript
export default defineConfig({
  server: {
    port: 3000,
    open: true
  },
  build: {
    outDir: 'dist'
  }
})
```

---

## Running the Full Application

### Development Mode

**Terminal 1: Start Backend**
```bash
cd HealthTracker.Api
dotnet run
```

**Terminal 2: Start Frontend**
```bash
cd healthtracker-web
npm run dev
```

**Result:**
- API running on: http://localhost:5000
- Frontend running on: http://localhost:3000

### Production Build

#### Backend
```bash
dotnet publish -c Release
# Run the published app
dotnet HealthTracker.Api.dll
```

#### Frontend
```bash
cd healthtracker-web
npm run build
# Deploy the 'dist' folder to your web server
```

---

## Testing

### API Testing

#### Using cURL

**Register:**
```bash
curl -X POST http://localhost:5000/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!"}'
```

**Login:**
```bash
curl -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!"}'
```

**Add Vital Signs:**
```bash
curl -X POST http://localhost:5000/vitals \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "heartRate": 75,
    "temperatureCelsius": 36.8,
    "oxygenSaturation": 99,
    "notes": "After 30 min walk"
  }'
```

#### Using Postman

1. Import the API endpoints
2. Set up variables:
   - `base_url`: http://localhost:5000
   - `token`: Save from login response
3. Test each endpoint

### Frontend Testing

#### Manual Testing Checklist

- [ ] User registration with new email
- [ ] User login with correct credentials
- [ ] Login failure with wrong password
- [ ] Add vital signs with all fields
- [ ] Add vital signs with partial fields
- [ ] View vital signs history
- [ ] Logout
- [ ] Login persistence (refresh page)

#### Automated Testing (Optional)

Set up with Cypress or Playwright:
```bash
npm install --save-dev cypress
npx cypress open
```

---

## Troubleshooting

### Backend Issues

#### API won't start
```bash
# Check .NET SDK version
dotnet --version

# Restore dependencies
dotnet restore

# Check for port conflicts
netstat -ano | findstr :5000
```

#### Database connection error
```
"Unable to connect to PostgreSQL"
```

**Fix:**
1. Verify PostgreSQL is running
2. Check connection string in `appsettings.json`
3. Ensure database exists:
   ```bash
   psql -U postgres -c "CREATE DATABASE healthdb;"
   ```

#### Port already in use
```bash
# Find process using port 5000
netstat -ano | findstr :5000

# Kill the process (replace PID with actual process ID)
taskkill /PID <PID> /F
```

### Frontend Issues

#### npm install fails
```bash
# Clear cache and retry
npm cache clean --force
npm install
```

#### Port 3000 already in use
The Vite dev server will use the next available port. Check terminal output.

#### API connection errors
- Check `VITE_API_URL` in `.env`
- Ensure backend is running
- Check CORS settings
- Open browser console for detailed errors

### Common Issues

| Issue | Solution |
|-------|----------|
| "Cannot find module" | Run `npm install` |
| Database errors | Check PostgreSQL is running |
| CORS error | Verify API CORS configuration |
| Login not working | Check token is being saved in localStorage |
| 401 Unauthorized | Token may be expired or invalid |

---

## Security Checklist

### Development
- [ ] Using HTTP (okay for local dev)
- [ ] Default database password (okay for local)
- [ ] CORS allows all origins (okay for local dev)

### Production
- [ ] [ ] Enable HTTPS/TLS
- [ ] [ ] Change default database password
- [ ] [ ] Restrict CORS to specific domains
- [ ] [ ] Implement proper JWT signing
- [ ] [ ] Use bcrypt/Argon2 for password hashing
- [ ] [ ] Enable HTTPS-only cookies
- [ ] [ ] Implement rate limiting
- [ ] [ ] Set up logging and monitoring
- [ ] [ ] Use environment variables for secrets
- [ ] [ ] Enable database encryption

---

## Performance Optimization

### Backend
```csharp
// Enable response compression
app.UseResponseCompression();

// Enable response caching
app.UseResponseCaching();
```

### Frontend
```javascript
// Vite already optimizes with:
// - Code splitting
// - Tree shaking
// - Minification
// - Source map exclusion in production
```

---

## Deployment

### Local Network Testing

**Make API accessible from frontend on different machine:**

Update firewall rules and configure API to listen on all interfaces:

```json
{
  "Urls": "http://0.0.0.0:5000"
}
```

### Docker Deployment

**Build API Docker image:**
```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:10

WORKDIR /app
COPY bin/Release/net10.0/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "HealthTracker.Api.dll"]
```

**Build frontend Docker image:**
```dockerfile
FROM node:18 AS build
WORKDIR /app
COPY . .
RUN npm install && npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
```

---

## Next Steps

1. **OAuth Integration**: Implement Google/GitHub authentication
2. **Data Visualization**: Add charts for vital signs trends
3. **Mobile App**: Build React Native version
4. **CI/CD**: Set up GitHub Actions for automated testing
5. **Monitoring**: Add Application Insights or similar
6. **Analytics**: Track user engagement and health trends

---

## Support & Documentation

- **API Docs**: See `API_DOCUMENTATION.md`
- **Frontend README**: See `healthtracker-web/README.md`
- **Backend Code**: Check `HealthTracker.Api/Program.cs`

## Useful Commands

```bash
# Backend
dotnet run                    # Run API
dotnet watch run             # Auto-reload on changes
dotnet build                 # Build project
dotnet test                  # Run tests
dotnet publish -c Release    # Build for production

# Frontend
npm run dev                   # Start dev server
npm run build                 # Build for production
npm run preview              # Preview production build
npm install                  # Install dependencies
npm update                   # Update dependencies
```

---

Happy coding! 🚀
