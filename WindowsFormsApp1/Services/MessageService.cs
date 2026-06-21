using System;
using System.Collections.Generic;
using System.Timers;
using WindowsFormsApp1.Models;

namespace WindowsFormsApp1.Services
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public PortMessage Message { get; set; }
    }

    public class MessageService : IDisposable
    {
        private readonly Queue<PortMessage> _txQueue = new Queue<PortMessage>();
        private readonly System.Timers.Timer _deliveryTimer;
        private bool _disposed;

        // 1 ms delay for TX→RX loopback simulation
        public const double DefaultDelayMs = 1.0;

        public event EventHandler<MessageReceivedEventArgs> MessageDelivered;

        public MessageService()
        {
            _deliveryTimer = new System.Timers.Timer(DefaultDelayMs);
            _deliveryTimer.Elapsed += OnDeliveryTimerElapsed;
            _deliveryTimer.AutoReset = true;
            _deliveryTimer.Start();
        }

        public void SendMessage(int portId, string portName, string data, double delayMs = DefaultDelayMs)
        {
            var msg = new PortMessage(portId, portName, MessageDirection.TX, data, delayMs);
            lock (_txQueue)
            {
                _txQueue.Enqueue(msg);
            }
        }

        public void BroadcastMessage(EthernetPort[] ports, string data, double delayMs = DefaultDelayMs)
        {
            foreach (var port in ports)
            {
                if (port.IsActive)
                {
                    SendMessage(port.PortId, port.PortName, data, delayMs);
                }
            }
        }

        private void OnDeliveryTimerElapsed(object sender, ElapsedEventArgs e)
        {
            PortMessage msg = null;
            lock (_txQueue)
            {
                if (_txQueue.Count > 0)
                    msg = _txQueue.Dequeue();
            }

            if (msg != null)
            {
                // Create corresponding RX message after delay
                var rxMsg = new PortMessage(msg.PortId, msg.PortName, MessageDirection.RX, msg.Data, msg.DelayMs);
                MessageDelivered?.Invoke(this, new MessageReceivedEventArgs { Message = rxMsg });
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _deliveryTimer?.Stop();
                _deliveryTimer?.Dispose();
                _disposed = true;
            }
        }
    }
}
