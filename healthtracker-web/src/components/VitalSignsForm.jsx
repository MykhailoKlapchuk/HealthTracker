import { useState } from 'react';
import '../styles/VitalSignsForm.css';

function VitalSignsForm({ onVitalAdded, onCancel }) {
  const [formData, setFormData] = useState({
    heartRate: '',
    temperatureCelsius: '',
    oxygenSaturation: '',
    notes: '',
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      // Validate at least one vital sign is provided
      if (
        !formData.heartRate &&
        !formData.temperatureCelsius &&
        !formData.oxygenSaturation
      ) {
        setError('Please enter at least one vital sign');
        setLoading(false);
        return;
      }

      const token = localStorage.getItem('token');
      const payload = {
        heartRate: formData.heartRate ? parseInt(formData.heartRate) : null,
        temperatureCelsius: formData.temperatureCelsius
          ? parseFloat(formData.temperatureCelsius)
          : null,
        oxygenSaturation: formData.oxygenSaturation
          ? parseInt(formData.oxygenSaturation)
          : null,
        notes: formData.notes || null,
      };

      const response = await fetch('http://localhost:5000/vitals', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`,
        },
        body: JSON.stringify(payload),
      });

      if (response.ok) {
        setFormData({
          heartRate: '',
          temperatureCelsius: '',
          oxygenSaturation: '',
          notes: '',
        });
        onVitalAdded();
      } else {
        setError('Failed to save vital signs');
      }
    } catch (err) {
      setError(err.message || 'An error occurred');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="form-container">
      <form onSubmit={handleSubmit} className="vitals-form">
        <h3>Add Vital Signs</h3>

        <div className="form-row">
          <div className="form-group">
            <label htmlFor="heartRate">Heart Rate (BPM)</label>
            <input
              id="heartRate"
              type="number"
              name="heartRate"
              value={formData.heartRate}
              onChange={handleChange}
              placeholder="e.g., 72"
              min="0"
              max="300"
            />
          </div>

          <div className="form-group">
            <label htmlFor="temperatureCelsius">Temperature (°C)</label>
            <input
              id="temperatureCelsius"
              type="number"
              name="temperatureCelsius"
              value={formData.temperatureCelsius}
              onChange={handleChange}
              placeholder="e.g., 36.5"
              min="35"
              max="42"
              step="0.1"
            />
          </div>

          <div className="form-group">
            <label htmlFor="oxygenSaturation">Oxygen Saturation (%)</label>
            <input
              id="oxygenSaturation"
              type="number"
              name="oxygenSaturation"
              value={formData.oxygenSaturation}
              onChange={handleChange}
              placeholder="e.g., 98"
              min="0"
              max="100"
            />
          </div>
        </div>

        <div className="form-group full-width">
          <label htmlFor="notes">Notes (optional)</label>
          <textarea
            id="notes"
            name="notes"
            value={formData.notes}
            onChange={handleChange}
            placeholder="Add any notes about your health..."
            rows="3"
          />
        </div>

        {error && <div className="error-message">{error}</div>}

        <div className="form-actions">
          <button type="submit" disabled={loading} className="submit-btn">
            {loading ? 'Saving...' : 'Save Vital Signs'}
          </button>
          <button type="button" onClick={onCancel} className="cancel-btn">
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
}

export default VitalSignsForm;
