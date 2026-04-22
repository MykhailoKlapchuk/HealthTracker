# HealthTracker Web App

A React-based vital signs tracking application with user authentication.

## Features

- **User Authentication**: Sign up and sign in with email/password
- **Vital Signs Tracking**: Record heart rate (BPM), body temperature (°C), and oxygen saturation (%)
- **History View**: See all recorded vital signs in a sortable table
- **OAuth Ready**: Pre-configured UI placeholders for Google and GitHub OAuth integration

## Setup

### Prerequisites
- Node.js 16+ 
- npm or yarn

### Installation

```bash
cd healthtracker-web
npm install
```

### Running the Development Server

```bash
npm run dev
```

The app will open at `http://localhost:3000`

### Building for Production

```bash
npm run build
```

## Authentication Options

### Current Implementation
The app uses **JWT tokens** with a simple backend authentication system. Tokens are stored in browser localStorage.

### OAuth 2.0 Integration (Coming Soon)
The login page has UI ready for OAuth integration with:
- **Google OAuth**
- **GitHub OAuth**

To integrate:

1. **Google OAuth Setup**:
   - Create an app at [Google Cloud Console](https://console.cloud.google.com/)
   - Get your Client ID
   - Install `@react-oauth/google` package
   - Use the `GoogleLogin` component in `Login.jsx`

2. **GitHub OAuth Setup**:
   - Create an app at [GitHub Developer Settings](https://github.com/settings/developers)
   - Get your Client ID and Client Secret
   - Implement GitHub OAuth flow using `react-oauth-components` or similar

## API Endpoints

### Authentication
- `POST /auth/register` - Register a new user
- `POST /auth/login` - Login user

### Vital Signs
- `POST /vitals` - Create a new vital sign record
- `GET /vitals` - Get user's vital sign history (50 most recent)

## Environment Variables

`.env` file configuration:
```
VITE_API_URL=http://localhost:5000
```

## Project Structure

```
healthtracker-web/
├── src/
│   ├── components/          # React components
│   │   ├── VitalSignsForm.jsx
│   │   └── VitalSignsHistory.jsx
│   ├── pages/              # Page components
│   │   ├── Login.jsx
│   │   └── Dashboard.jsx
│   ├── styles/             # CSS files
│   ├── App.jsx
│   └── main.jsx
├── vite.config.js
└── package.json
```

## Features to Add

- [ ] OAuth 2.0 social login
- [ ] Data visualization (charts/graphs)
- [ ] Export vital signs to CSV
- [ ] Health trends analysis
- [ ] Mobile app version
- [ ] Wearable device integration
- [ ] Push notifications for abnormal readings

## Security Notes

⚠️ **Current Authentication**: The simple JWT implementation uses base64 encoding. For production:
- Implement proper JWT signing with a secret key
- Use HTTPS only
- Store tokens in secure, HTTP-only cookies
- Add refresh token mechanism
- Implement CORS properly

## License

MIT
