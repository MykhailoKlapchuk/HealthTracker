import { useState, useEffect } from 'react';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import './styles/App.css';

function App() {
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Check if user is already logged in by validating the token
    const validateToken = async () => {
      const token = localStorage.getItem('token');
      const userEmail = localStorage.getItem('userEmail');

      if (token && userEmail) {
        try {
          // Validate token by checking if user exists
          const response = await fetch('https://localhost:7020/auth/validate', {
            headers: { 'Authorization': `Bearer ${token}` }
          });

          if (response.ok) {
            setUser({ email: userEmail });
            setIsLoggedIn(true);
          } else {
            // Token is invalid or user doesn't exist, clear it
            localStorage.removeItem('token');
            localStorage.removeItem('userEmail');
          }
        } catch (err) {
          // Can't reach API, clear token to be safe
          localStorage.removeItem('token');
          localStorage.removeItem('userEmail');
        }
      }
      setLoading(false);
    };

    validateToken();
  }, []);

  const handleLogin = (email, token) => {
    localStorage.setItem('token', token);
    localStorage.setItem('userEmail', email);
    setUser({ email });
    setIsLoggedIn(true);
  };

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('userEmail');
    setUser(null);
    setIsLoggedIn(false);
  };

  if (loading) {
    return <div className="loading">Loading...</div>;
  }

  return (
    <div className="app">
      {isLoggedIn ? (
        <Dashboard user={user} onLogout={handleLogout} />
      ) : (
        <Login onLogin={handleLogin} />
      )}
    </div>
  );
}

export default App;
