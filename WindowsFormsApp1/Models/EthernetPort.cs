using System;

namespace WindowsFormsApp1.Models
{
    public enum MacGroup
    {
        MAC0_Direct = 0,
        MAC1_Switch = 1
    }

    public enum PortStatus
    {
        Disconnected,
        Connected,
        Transmitting,
        Receiving,
        Error
    }

    public class EthernetPort
    {
        public int PortId { get; set; }
        public string PortName { get; set; }
        public MacGroup MacGroup { get; set; }
        public string IpAddress { get; set; }
        public string MacAddress { get; set; }
        public PortStatus Status { get; set; }
        public bool IsActive { get; set; }

        // Statistics counters
        public long TxPackets { get; set; }
        public long RxPackets { get; set; }
        public long TxBytes { get; set; }
        public long RxBytes { get; set; }
        public long TxErrors { get; set; }
        public long RxErrors { get; set; }

        // Ping metrics
        public double PingLatencyMs { get; set; }
        public bool PingSuccess { get; set; }

        // Speed
        public int LinkSpeedMbps { get; set; }

        public EthernetPort(int portId, MacGroup mac, string ip, string macAddr, string name)
        {
            PortId = portId;
            MacGroup = mac;
            IpAddress = ip;
            MacAddress = macAddr;
            PortName = name;
            Status = PortStatus.Disconnected;
            IsActive = false;
            LinkSpeedMbps = 1000;
            TxPackets = 0;
            RxPackets = 0;
            TxBytes = 0;
            RxBytes = 0;
            TxErrors = 0;
            RxErrors = 0;
            PingLatencyMs = -1;
            PingSuccess = false;
        }

        public string MacGroupLabel =>
            MacGroup == MacGroup.MAC0_Direct ? "MAC0 · Direct" : "MAC1 · Switch";

        public string StatusText => Status.ToString();
    }
}
