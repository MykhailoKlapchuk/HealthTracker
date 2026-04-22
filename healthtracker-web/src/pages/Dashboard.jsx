import { useState, useEffect } from 'react';
import VitalSignsForm from '../components/VitalSignsForm';
import VitalSignsHistory from '../components/VitalSignsHistory';
import '../styles/Dashboard.css';

function Dashboard({ user, onLogout }) {
  const [vitals, setVitals] = useState([]);
  const [loading, setLoading] = useState(false);
  const [showForm, setShowForm] = useState(false);

  const fetchVitals = async () => {
    setLoading(true);
    try {
      const token = localStorage.getItem('token');
      const response = await fetch('https://localhost:7020/vitals', {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (response.ok) {
        const data = await response.json();
        setVitals(data);
      }
    } catch (err) {
      console.error('Error fetching vitals:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchVitals();
  }, []);

  const handleVitalAdded = () => {
    fetchVitals();
    setShowForm(false);
  };

  const getLatestVitals = () => {
    return vitals.length > 0 ? vitals[0] : null;
  };

  const latest = getLatestVitals();

  return (
    <div className="dashboard">
      <header className="dashboard-header">
        <div className="header-content">
          <h1>HealthTracker</h1>
          <div className="user-section">
            <span className="user-email">{user.email}</span>
            <button onClick={onLogout} className="logout-btn">
              Logout
            </button>
          </div>
        </div>
      </header>

      <main className="dashboard-content">
        <section className="vital-cards">
          <h2>Latest Vital Signs</h2>
          {latest ? (
            <div className="cards-grid">
              {latest.heartRate !== null && (
                <div className={`vital-card heart-rate${
                  latest.warnings?.heartRateHigh || latest.warnings?.heartRateLow ? ' warning' : ''
                }`}>
                  <div className="vital-icon">❤️</div>
                  <div className="vital-value">{latest.heartRate}</div>
                  <div className="vital-label">BPM</div>
                  {latest.warnings?.heartRateHigh && (
                    <div className="vital-warning-label">High BPM</div>
                  )}
                  {latest.warnings?.heartRateLow && (
                    <div className="vital-warning-label">Low BPM</div>
                  )}
                </div>
              )}
              {latest.temperatureCelsius !== null && (
                <div className={`vital-card temperature${
                  latest.warnings?.temperatureHigh || latest.warnings?.temperatureLow ? ' warning' : ''
                }`}>
                  <div className="vital-icon">🌡️</div>
                  <div className="vital-value">{latest.temperatureCelsius}</div>
                  <div className="vital-label">°C</div>
                  {latest.warnings?.temperatureHigh && (
                    <div className="vital-warning-label">High Temp</div>
                  )}
                  {latest.warnings?.temperatureLow && (
                    <div className="vital-warning-label">Low Temp</div>
                  )}
                </div>
              )}
              {latest.oxygenSaturation !== null && (
                <div className={`vital-card oxygen${latest.warnings?.oxygenLow ? ' warning' : ''}`}>
                  <div className="vital-icon">💨</div>
                  <div className="vital-value">{latest.oxygenSaturation}</div>
                  <div className="vital-label">SpO₂ %</div>
                  {latest.warnings?.oxygenLow && (
                    <div className="vital-warning-label">Low SpO₂</div>
                  )}
                </div>
              )}
              {!latest.heartRate && !latest.temperatureCelsius && !latest.oxygenSaturation && (
                <div className="no-data">No vital signs recorded yet</div>
              )}
            </div>
          ) : (
            <div className="no-data">No vital signs recorded yet</div>
          )}
        </section>

        <section className="actions-section">
          {!showForm && (
            <button onClick={() => setShowForm(true)} className="add-btn">
              + Add Vital Signs
            </button>
          )}

          {showForm && (
            <VitalSignsForm onVitalAdded={handleVitalAdded} onCancel={() => setShowForm(false)} />
          )}
        </section>

        <section className="history-section">
          <h2>Vital Signs History</h2>
          {loading ? (
            <div className="loading">Loading...</div>
          ) : vitals.length > 0 ? (
            <VitalSignsHistory vitals={vitals} />
          ) : (
            <div className="no-data">No vital signs history</div>
          )}
        </section>
      </main>
    </div>
  );
}

export default Dashboard;
