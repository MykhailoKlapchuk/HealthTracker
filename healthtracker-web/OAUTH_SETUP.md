# OAuth 2.0 Integration Guide

The HealthTracker app is ready for OAuth integration. This guide shows how to add Google and GitHub authentication.

## Option 1: Google Sign-In 🔵

### Step 1: Create Google OAuth Credentials

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing one
3. Enable **Google+ API**
4. Create OAuth 2.0 credentials:
   - Type: Web application
   - Authorized redirect URI: `http://localhost:3000` (dev) or your domain (prod)
5. Copy **Client ID** (you'll need it)

### Step 2: Install React Google Login

```bash
npm install @react-oauth/google
```

### Step 3: Update App.jsx

```jsx
import { GoogleOAuthProvider } from '@react-oauth/google';

function App() {
  return (
    <GoogleOAuthProvider clientId="YOUR_GOOGLE_CLIENT_ID_HERE">
      {/* rest of app */}
    </GoogleOAuthProvider>
  );
}
```

### Step 4: Update Login.jsx

Replace the Google button with:

```jsx
import { GoogleLogin } from '@react-oauth/google';

// Inside the oauth-section, replace the Google button with:
<GoogleLogin
  onSuccess={(credentialResponse) => {
    handleGoogleLogin(credentialResponse);
  }}
  onError={() => {
    setError('Google login failed');
  }}
/>

// Add handler function:
const handleGoogleLogin = async (credentialResponse) => {
  try {
    const token = credentialResponse.credential;
    // Send token to backend for verification
    const response = await axios.post(
      `${API_BASE_URL}/auth/google`,
      { token }
    );
    
    if (response.data.token) {
      // Extract email from JWT (decode if needed)
      onLogin(response.data.email, response.data.token);
    }
  } catch (err) {
    setError('Google authentication failed');
  }
};
```

### Step 5: Backend Google Verification

Add to `HealthTracker.Api/Program.cs`:

```csharp
// Install: Google.Apis.Auth NuGet package
// dotnet add package Google.Apis.Auth

app.MapPost("/auth/google", async (GoogleTokenRequest req, NpgsqlDataSource db) =>
{
    try
    {
        // Verify the Google token
        var payload = await GoogleJsonWebSignature.ValidateAsync(req.IdToken);
        
        var email = payload.Email.ToLower();
        var displayName = payload.Name;
        
        // Create or get user
        await using var cmd = db.CreateCommand(
            "SELECT \"Id\" FROM \"Users\" WHERE \"Email\" = $1");
        cmd.Parameters.AddWithValue(email);
        
        var userId = await cmd.ExecuteScalarAsync() as int?;
        
        if (userId == null)
        {
            // Create new user (no password for OAuth)
            await using var insertCmd = db.CreateCommand(
                "INSERT INTO \"Users\" (\"Email\", \"PasswordHash\") VALUES ($1, $2) RETURNING \"Id\"");
            insertCmd.Parameters.AddWithValue(email);
            insertCmd.Parameters.AddWithValue("oauth_google"); // Placeholder
            
            userId = (int)await insertCmd.ExecuteScalarAsync();
        }
        
        var token = GenerateToken(userId.Value, email);
        return Results.Ok(new { token, userId, email });
    }
    catch (Exception ex)
    {
        return Results.BadRequest("Invalid Google token");
    }
});

record GoogleTokenRequest(string IdToken);
```

---

## Option 2: GitHub Sign-In ⚫

### Step 1: Create GitHub OAuth App

1. Go to [GitHub Developer Settings](https://github.com/settings/developers)
2. Click **New OAuth App**
3. Fill in:
   - **Application name**: HealthTracker
   - **Homepage URL**: `http://localhost:3000` (dev)
   - **Authorization callback URL**: `http://localhost:3000/auth/callback`
4. Copy **Client ID** and **Client Secret** (keep secret safe!)

### Step 2: Install GitHub Login Package

```bash
npm install @octokit/auth-oauth-device
# Or use a simpler package:
npm install react-github-login
```

### Step 3: Update .env

```env
VITE_GITHUB_CLIENT_ID=your_github_client_id
VITE_GITHUB_REDIRECT_URI=http://localhost:3000
```

### Step 4: Create GitHub Login Component

Create `src/components/GitHubLogin.jsx`:

```jsx
import { useState } from 'react';

function GitHubLogin({ onLogin }) {
  const handleClick = () => {
    const clientId = import.meta.env.VITE_GITHUB_CLIENT_ID;
    const redirectUri = import.meta.env.VITE_GITHUB_REDIRECT_URI;
    const scope = 'user:email';
    
    const url = `https://github.com/login/oauth/authorize?client_id=${clientId}&redirect_uri=${redirectUri}&scope=${scope}`;
    
    window.location.href = url;
  };

  return (
    <button type="button" onClick={handleClick} className="oauth-btn github-btn">
      <span>⚫</span> Sign in with GitHub
    </button>
  );
}

export default GitHubLogin;
```

### Step 5: Create Callback Page

Create `src/pages/GitHubCallback.jsx`:

```jsx
import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';

function GitHubCallback({ onLogin }) {
  const navigate = useNavigate();

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const code = params.get('code');

    if (code) {
      // Send code to backend
      fetch('http://localhost:5000/auth/github', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ code }),
      })
        .then(res => res.json())
        .then(data => {
          onLogin(data.email, data.token);
          navigate('/');
        })
        .catch(err => {
          console.error('GitHub auth failed:', err);
          navigate('/login');
        });
    }
  }, []);

  return <div>Authenticating...</div>;
}

export default GitHubCallback;
```

### Step 6: Backend GitHub Handler

Add to `HealthTracker.Api/Program.cs`:

```csharp
// Install: RestSharp NuGet package
// dotnet add package RestSharp

app.MapPost("/auth/github", async (GitHubCodeRequest req, IHttpClientFactory httpClient, NpgsqlDataSource db) =>
{
    try
    {
        var client = httpClient.CreateClient();
        
        // Exchange code for token
        var tokenRequest = new
        {
            client_id = "YOUR_GITHUB_CLIENT_ID",
            client_secret = "YOUR_GITHUB_CLIENT_SECRET",
            code = req.Code
        };

        var response = await client.PostAsJsonAsync(
            "https://github.com/login/oauth/access_token",
            tokenRequest);

        var tokenResponse = await response.Content.ReadAsAsync<GitHubTokenResponse>();
        
        // Get user info
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("token", tokenResponse.access_token);
        
        var userResponse = await client.GetAsync("https://api.github.com/user");
        var user = await userResponse.Content.ReadAsAsync<GitHubUser>();

        // Create or get user
        var email = user.email ?? $"{user.login}@github.local";
        
        // ... (same user creation logic as Google) ...
        
        var token = GenerateToken(userId.Value, email);
        return Results.Ok(new { token, userId, email });
    }
    catch (Exception ex)
    {
        return Results.BadRequest("GitHub authentication failed");
    }
});

record GitHubCodeRequest(string Code);
record GitHubTokenResponse(string access_token);
record GitHubUser(int id, string login, string email);
```

---

## Option 3: Simple OAuth Middleware (Frontend Only)

For quick testing, implement a simplified OAuth flow:

```jsx
// Create src/services/oauthService.js

export const googleLogin = async (clientId) => {
  // Redirect to Google auth
  const scope = 'openid profile email';
  const redirectUri = window.location.origin + '/auth/callback';
  
  const url = `https://accounts.google.com/o/oauth2/v2/auth?` +
    `client_id=${clientId}&` +
    `redirect_uri=${redirectUri}&` +
    `scope=${scope}&` +
    `response_type=code`;
  
  window.location.href = url;
};
```

---

## Environment Variables Needed

### .env.local (development)
```env
VITE_GOOGLE_CLIENT_ID=your_google_client_id
VITE_GITHUB_CLIENT_ID=your_github_client_id
VITE_API_URL=http://localhost:5000
```

### .env.production
```env
VITE_GOOGLE_CLIENT_ID=your_production_google_client_id
VITE_GITHUB_CLIENT_ID=your_production_github_client_id
VITE_API_URL=https://api.yourdomain.com
```

---

## Backend OAuth Configuration

Store credentials securely:

```csharp
// appsettings.json
{
  "OAuth": {
    "Google": {
      "ClientId": "...",
      "ClientSecret": "..."
    },
    "GitHub": {
      "ClientId": "...",
      "ClientSecret": "..."
    }
  }
}
```

Access in code:
```csharp
var googleClientId = configuration["OAuth:Google:ClientId"];
```

---

## Testing OAuth

1. **Google OAuth Test**:
   - Click "Sign in with Google" button
   - Authenticate with your Google account
   - Should redirect back with token

2. **GitHub OAuth Test**:
   - Click "Sign in with GitHub" button
   - Authenticate with your GitHub account
   - Should redirect back with token

3. **Verify Token**:
   ```bash
   curl -H "Authorization: Bearer <token>" http://localhost:5000/vitals
   ```

---

## Security Best Practices

✅ **Do's**:
- Store Client Secret only on backend
- Use HTTPS in production
- Validate tokens on backend
- Implement PKCE for SPAs
- Use state parameter to prevent CSRF
- Store refresh tokens securely (HttpOnly cookies)

❌ **Don'ts**:
- Expose Client Secret in frontend code
- Store sensitive tokens in localStorage
- Use HTTP in production
- Skip token validation
- Hardcode credentials

---

## Common Issues

### "Redirect URI mismatch"
- Update GitHub/Google console with correct URI
- Match exactly (protocol, domain, path)

### "Invalid Client ID"
- Check you're using correct credential (Client ID, not Secret)
- Verify environment variables are loaded

### "Token expired"
- Implement refresh token flow
- Get new token when expired

---

## Production Deployment

1. Get credentials for your production domain
2. Update environment variables
3. Enable HTTPS
4. Update redirect URIs
5. Test thoroughly before going live

---

## Resources

- [Google Sign-In Documentation](https://developers.google.com/identity/protocols)
- [GitHub OAuth Documentation](https://docs.github.com/en/developers/apps/building-oauth-apps)
- [@react-oauth/google](https://github.com/react-oauth/react-oauth.google)

---

Ready to add OAuth? Pick your provider and follow the steps above! 🚀
