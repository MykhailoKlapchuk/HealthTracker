import { useState, useEffect } from 'react';
import api from './api/api';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import './styles/App.css';

function App() {
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const validateToken = async () => {
      try {
        const response = await api.get('/auth/validate');
        if (response.data.valid && response.data.email) {
          setUser({ email: response.data.email });
          setIsLoggedIn(true);
        }
      } catch (err) {
        // Not authenticated (401), network error, or other issue — stay logged out
        if (err.response?.status !== 401) {
          console.warn('Session validation error:', err.message);
        }
      } finally {
        setLoading(false);
      }
    };

    validateToken();
  }, []);

  const handleLogin = (email) => {
    setUser({ email });
    setIsLoggedIn(true);
  };

  const handleLogout = async () => {
    try {
      await api.post('/auth/logout');
    } catch (err) {
      console.error('Logout error:', err);
    } finally {
      setUser(null);
      setIsLoggedIn(false);
    }
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
