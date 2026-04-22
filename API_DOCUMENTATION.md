# HealthTracker API Documentation

## Overview

The HealthTracker API is a .NET Aspire backend that provides user authentication and vital signs tracking functionality. The API uses JWT-style tokens for authentication and PostgreSQL for data persistence.

## Base URL

```
http://localhost:5000
```

## Authentication

All protected endpoints require an `Authorization` header with a Bearer token:

```
Authorization: Bearer <token>
```

Tokens are obtained from the `/auth/login` or `/auth/register` endpoints.

---

## Endpoints

### Authentication Endpoints

#### Register User
Create a new user account.

**Request:**
```http
POST /auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "securepassword123"
}
```

**Response (201):**
```json
{
  "message": "User created successfully",
  "userId": 1
}
```

**Error Response (400):**
```json
"Email already exists"
```

---

#### Login
Authenticate an existing user and receive a token.

**Request:**
```http
POST /auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "securepassword123"
}
```

**Response (200):**
```json
{
  "token": "base64encodedtoken",
  "userId": 1,
  "email": "user@example.com"
}
```

**Error Response (401):**
```
Unauthorized
```

---

### Vital Signs Endpoints

#### Add Vital Signs
Record user vital signs (heart rate, temperature, oxygen saturation).

**Request:**
```http
POST /vitals
Authorization: Bearer <token>
Content-Type: application/json

{
  "heartRate": 72,
  "temperatureCelsius": 36.5,
  "oxygenSaturation": 98,
  "notes": "Feeling good"
}
```

**Notes:**
- All vital sign fields are optional (at least one must be provided)
- `heartRate`: integer, 0-300 BPM
- `temperatureCelsius`: decimal, 35-42°C
- `oxygenSaturation`: integer, 0-100%
- `notes`: optional text up to 1000 characters

**Response (200):**
```json
{
  "id": 42,
  "timestamp": "2024-04-22T11:23:45.123Z"
}
```

**Error Response (401):**
```
Unauthorized
```

---

#### Get Vital Signs History
Retrieve user's vital signs history (50 most recent records).

**Request:**
```http
GET /vitals
Authorization: Bearer <token>
```

**Response (200):**
```json
[
  {
    "id": 42,
    "heartRate": 72,
    "temperatureCelsius": 36.5,
    "oxygenSaturation": 98,
    "timestamp": "2024-04-22T11:23:45.123Z",
    "notes": "Feeling good"
  },
  {
    "id": 41,
    "heartRate": null,
    "temperatureCelsius": 37.2,
    "oxygenSaturation": 97,
    "timestamp": "2024-04-22T10:15:30.456Z",
    "notes": null
  }
]
```

**Error Response (401):**
```
Unauthorized
```

---

## Database Schema

### Users Table
```sql
CREATE TABLE "Users" (
  "Id"           SERIAL PRIMARY KEY,
  "Email"        VARCHAR(255) UNIQUE NOT NULL,
  "PasswordHash" VARCHAR(255) NOT NULL,
  "CreatedAt"    TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

### VitalSigns Table
```sql
CREATE TABLE "VitalSigns" (
  "Id"               SERIAL PRIMARY KEY,
  "UserId"           INTEGER NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
  "HeartRate"        INTEGER,
  "TemperatureCelsius" DECIMAL(5,2),
  "OxygenSaturation" INTEGER,
  "Timestamp"        TIMESTAMPTZ NOT NULL,
  "Notes"            TEXT
);
```

### HeartRateHistory Table (Legacy)
```sql
CREATE TABLE "HeartRateHistory" (
  "Id"        SERIAL PRIMARY KEY,
  "Bpm"       INTEGER NOT NULL,
  "Timestamp" TIMESTAMPTZ NOT NULL,
  "State"     TEXT NOT NULL
);
```

---

## Legacy Endpoints

The API maintains backward compatibility with the original heart rate endpoints:

### Add Heart Rate (Legacy)
```http
POST /heartrate
Content-Type: application/json

{
  "bpm": 72
}
```

### Get Heart Rate History (Legacy)
```http
GET /heartrate
```

---

## Error Handling

### Common Error Codes

| Status | Description |
|--------|-------------|
| 400 | Bad Request - Invalid input |
| 401 | Unauthorized - Missing or invalid token |
| 500 | Internal Server Error |

### Error Response Format
```json
{
  "error": "Error message describing what went wrong"
}
```

---

## Authentication Flow

### User Registration and Login

1. **Register**: User creates account with email and password
   - POST `/auth/register` → Returns `userId`

2. **Login**: User authenticates with email and password
   - POST `/auth/login` → Returns `token`

3. **Store Token**: Client stores token in `localStorage`

4. **Use Token**: Include token in all subsequent requests
   - Header: `Authorization: Bearer {token}`

5. **Logout**: Client deletes token from `localStorage`

---

## Rate Limiting

Currently no rate limiting is implemented. For production deployment, consider implementing:
- Per-IP rate limits
- Per-user rate limits
- DDoS protection

---

## CORS Configuration

The API is configured to accept requests from all origins in development:
```csharp
policy.AllowAnyOrigin()
      .AllowAnyMethod()
      .AllowAnyHeader();
```

**⚠️ For production**, restrict to specific origins:
```csharp
policy.WithOrigins("https://yourdomain.com")
      .AllowAnyMethod()
      .AllowAnyHeader();
```

---

## Future Enhancements

### OAuth 2.0 Support
- Google Sign-In
- GitHub Sign-In
- Microsoft Sign-In

### Data Export
- Export vital signs to CSV
- Export to PDF reports

### Analytics
- Vital signs trends
- Health statistics
- Data visualization endpoints

### Device Integration
- Wearable device sync
- Apple HealthKit integration
- Google Fit integration

---

## Development

### Running the API

```bash
dotnet run --project HealthTracker.Api
```

### Database Migrations

Tables are automatically created on API startup if they don't exist.

### Testing

Use the provided `.http` files or Swagger UI (development only):
- Navigate to: `http://localhost:5000/scalar/v1`

---

## Security Considerations

⚠️ **Current Implementation Limitations**:

1. **Token Format**: Uses simple base64 encoding
   - **Production Fix**: Use proper JWT with HS256 or RS256 signing

2. **Password Storage**: Uses SHA256 hashing without salt
   - **Production Fix**: Use bcrypt, Argon2, or scrypt with salt

3. **HTTPS**: Not enforced in development
   - **Production Fix**: Always use HTTPS/TLS

4. **Token Expiration**: Not currently validated
   - **Production Fix**: Implement token expiration and refresh tokens

5. **CORS**: Allows all origins
   - **Production Fix**: Restrict to specific domains

---

## Examples

### Complete User Journey

1. **Sign Up**
```bash
curl -X POST http://localhost:5000/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"pass123"}'
```

2. **Sign In**
```bash
curl -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"pass123"}'
# Response includes: token, userId, email
```

3. **Add Vital Signs**
```bash
curl -X POST http://localhost:5000/vitals \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "heartRate": 72,
    "temperatureCelsius": 36.5,
    "oxygenSaturation": 98,
    "notes": "After exercise"
  }'
```

4. **Get History**
```bash
curl -X GET http://localhost:5000/vitals \
  -H "Authorization: Bearer <token>"
```

---

## Support

For issues or questions about the API, please check the main project README.
