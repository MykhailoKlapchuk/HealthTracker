import '../styles/VitalSignsHistory.css';

function VitalSignsHistory({ vitals }) {
  const formatDate = (timestamp) => {
    return new Date(timestamp).toLocaleString();
  };

  return (
    <div className="history-container">
      <table className="history-table">
        <thead>
          <tr>
            <th>Date & Time</th>
            <th>Heart Rate (BPM)</th>
            <th>Temperature (°C)</th>
            <th>Oxygen Sat. (%)</th>
            <th>Notes</th>
          </tr>
        </thead>
        <tbody>
          {vitals.map((vital) => (
            <tr key={vital.id}>
              <td className="timestamp">{formatDate(vital.timestamp)}</td>
              <td className={vital.heartRate ? '' : 'empty'}>
                {vital.heartRate ? vital.heartRate : '—'}
              </td>
              <td className={vital.temperatureCelsius ? '' : 'empty'}>
                {vital.temperatureCelsius ? vital.temperatureCelsius : '—'}
              </td>
              <td className={vital.oxygenSaturation ? '' : 'empty'}>
                {vital.oxygenSaturation ? vital.oxygenSaturation : '—'}
              </td>
              <td className="notes">{vital.notes || '—'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default VitalSignsHistory;
