using System;
using System.Threading;
using WindowsFormsApp1.Models;

namespace WindowsFormsApp1.Services
{
    public class PortStatsEventArgs : EventArgs
    {
        public EthernetPort[] Ports { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class EthernetMonitorService : IDisposable
    {
        private Timer _monitorTimer;
        private readonly EthernetPort[] _ports;
        private readonly Random _rng = new Random();
        private bool _isRunning;
        private bool _disposed;

        // 1 ms polling interval
        private const int PollIntervalMs = 1;

        public event EventHandler<PortStatsEventArgs> PortStatsUpdated;

        public bool IsRunning => _isRunning;

        public EthernetMonitorService(EthernetPort[] ports)
        {
            _ports = ports;
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            _monitorTimer = new Timer(OnTimerTick, null, 0, PollIntervalMs);
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;
            _monitorTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _monitorTimer?.Dispose();
            _monitorTimer = null;
        }

        private void OnTimerTick(object state)
        {
            if (!_isRunning) return;

            // Simulate hardware register reads from Zynq MAC0/MAC1
            // Replace with actual WMI/NDIS/socket calls for real hardware
            foreach (var port in _ports)
            {
                if (port.IsActive)
                {
                    SimulatePortActivity(port);
                }
            }

            PortStatsUpdated?.Invoke(this, new PortStatsEventArgs
            {
                Ports = _ports,
                Timestamp = DateTime.Now
            });
        }

        private void SimulatePortActivity(EthernetPort port)
        {
            // Simulate realistic Ethernet traffic bursts
            // MAC0 direct: higher single-stream throughput
            // MAC1 switch: distributed across ports 1-4
            double txRate = port.MacGroup == MacGroup.MAC0_Direct ? 15000.0 : 8000.0;
            double rxRate = port.MacGroup == MacGroup.MAC0_Direct ? 12000.0 : 7000.0;

            // Add jitter
            txRate += (_rng.NextDouble() - 0.5) * txRate * 0.2;
            rxRate += (_rng.NextDouble() - 0.5) * rxRate * 0.2;

            int txPkts = (int)(txRate / 1000.0 * PollIntervalMs);
            int rxPkts = (int)(rxRate / 1000.0 * PollIntervalMs);

            port.TxPackets += txPkts > 0 ? txPkts : 0;
            port.RxPackets += rxPkts > 0 ? rxPkts : 0;
            port.TxBytes += txPkts * (_rng.Next(64, 1500));
            port.RxBytes += rxPkts * (_rng.Next(64, 1500));

            // Occasional errors (0.01%)
            if (_rng.NextDouble() < 0.0001)
            {
                port.TxErrors++;
            }
            if (_rng.NextDouble() < 0.0001)
            {
                port.RxErrors++;
            }

            // Update status
            if (txPkts > 0 || rxPkts > 0)
            {
                port.Status = _rng.NextDouble() < 0.5
                    ? PortStatus.Transmitting
                    : PortStatus.Receiving;
            }
            else
            {
                port.Status = PortStatus.Connected;
            }
        }

        public void SetPortActive(int portId, bool active)
        {
            foreach (var port in _ports)
            {
                if (port.PortId == portId)
                {
                    port.IsActive = active;
                    port.Status = active ? PortStatus.Connected : PortStatus.Disconnected;

                    if (!active)
                    {
                        port.PingSuccess = false;
                        port.PingLatencyMs = -1;
                    }
                    break;
                }
            }
        }

        public void ResetPortStats(int portId)
        {
            foreach (var port in _ports)
            {
                if (port.PortId == portId)
                {
                    port.TxPackets = 0;
                    port.RxPackets = 0;
                    port.TxBytes = 0;
                    port.RxBytes = 0;
                    port.TxErrors = 0;
                    port.RxErrors = 0;
                    break;
                }
            }
        }

        public void ResetAllStats()
        {
            foreach (var port in _ports)
            {
                port.TxPackets = 0;
                port.RxPackets = 0;
                port.TxBytes = 0;
                port.RxBytes = 0;
                port.TxErrors = 0;
                port.RxErrors = 0;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _disposed = true;
            }
        }
    }
}
