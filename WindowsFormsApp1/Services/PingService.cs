using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace WindowsFormsApp1.Services
{
    public class PingResult
    {
        public int PortId { get; set; }
        public bool Success { get; set; }
        public double LatencyMs { get; set; }
        public string IpAddress { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class PingService
    {
        private const int DefaultTimeoutMs = 1000;
        private const int PingBufferSize = 32;

        public async Task<PingResult> PingPortAsync(int portId, string ipAddress)
        {
            var result = new PingResult
            {
                PortId = portId,
                IpAddress = ipAddress,
                Success = false,
                LatencyMs = -1
            };

            try
            {
                using (var ping = new Ping())
                {
                    byte[] buffer = new byte[PingBufferSize];
                    var options = new PingOptions(64, true);

                    var reply = await ping.SendPingAsync(ipAddress, DefaultTimeoutMs, buffer, options);

                    if (reply.Status == IPStatus.Success)
                    {
                        result.Success = true;
                        result.LatencyMs = reply.RoundtripTime;
                    }
                    else
                    {
                        result.ErrorMessage = reply.Status.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<PingResult[]> PingAllPortsAsync(Models.EthernetPort[] ports)
        {
            var tasks = new Task<PingResult>[ports.Length];
            for (int i = 0; i < ports.Length; i++)
            {
                tasks[i] = PingPortAsync(ports[i].PortId, ports[i].IpAddress);
            }
            return await Task.WhenAll(tasks);
        }
    }
}
