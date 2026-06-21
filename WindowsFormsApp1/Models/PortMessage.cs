using System;

namespace WindowsFormsApp1.Models
{
    public enum MessageDirection
    {
        TX,
        RX
    }

    public class PortMessage
    {
        public DateTime Timestamp { get; set; }
        public MessageDirection Direction { get; set; }
        public int PortId { get; set; }
        public string PortName { get; set; }
        public string Data { get; set; }
        public double DelayMs { get; set; }
        public int DataLengthBytes { get; set; }

        public PortMessage(int portId, string portName, MessageDirection dir, string data, double delayMs = 1.0)
        {
            PortId = portId;
            PortName = portName;
            Direction = dir;
            Data = data;
            DelayMs = delayMs;
            Timestamp = DateTime.Now;
            DataLengthBytes = System.Text.Encoding.UTF8.GetByteCount(data);
        }

        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss.fff}] [{Direction}] Port {PortId} ({PortName}) | Delay: {DelayMs}ms | Len: {DataLengthBytes}B | {Data}";
        }
    }
}
