using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace PhaserMonitor.Model
{
    // Modelos
    public class PhaserAPIModel
    {
        public string IpAddress { get; set; }
    }

    public class PhaserConfig
    {
        public int Number { get; set; }
        public string IpAddress { get; set; }
    }

    public class PhaserStatus
    {
        public int PhaserNumber { get; set; }
        public string IpAddress { get; set; }
        public HttpStatusCode? StatusCode { get; set; }
        public string Data { get; set; }
        public string Error { get; set; }
        public DateTime Timestamp { get; set; }
    }
    public class SystemInfo
    {
        public string MacAddress { get; set; } = string.Empty;
        public string SoftwareVersion { get; set; } = string.Empty;

        public string LocalDateTime { get; set; } = string.Empty ;
        public string IpAddress { get; set; } = string.Empty ;
        public string NetworkMask { get; set; } = string.Empty;
        public string Gateway { get; set; } = string.Empty;
        public string PrimaryDns { get; set; } = string.Empty;
        public string SecondaryDns { get; set; } = string.Empty;
        public bool DHCP { get; set; }
    }
    public class PhaserNetworkConfigurationModel
    {
        public bool? DHCP { get; set; }
        public string? IPAddress { get; set; }
        public string? NetMask { get; set; }
        public string? Gateway { get; set; }
        public string? DNSPrimary { get; set; }
        public string? DNSSecondary { get; set; }
        public bool Equals(PhaserNetworkConfigurationModel other)
        {
            if (other == null)
                return false;

            return (this.DHCP == other.DHCP) &&
                (this.IPAddress == other.IPAddress || (this.IPAddress == null && other.IPAddress == "0.0.0.0") || (this.IPAddress == "0.0.0.0" && other.IPAddress == null)) &&
                (this.NetMask == other.NetMask || (this.NetMask == null && other.NetMask == "0.0.0.0") || (this.NetMask == "0.0.0.0" && other.NetMask == null)) &&
                (this.Gateway == other.Gateway || (this.Gateway == null && other.Gateway == "0.0.0.0") || (this.Gateway == "0.0.0.0" && other.Gateway == null));
        }
    }
}
