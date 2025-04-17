using Microsoft.AspNetCore.Mvc;
using System.Net;
using PhaserMonitor.Model;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Reflection;
using PhaserMonitor.Services;

namespace PhaserMonitor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhaserController : ControllerBase
    {
        private readonly PhaserConfigManager _configManager;
        private readonly HttpClient _httpClient;
        private readonly ILogger<PhaserController> _logger;

        public PhaserController(
            IHttpClientFactory httpClientFactory,
            ILogger<PhaserController> logger,
            PhaserConfigManager configManager)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _configManager = configManager;
        }

        [HttpGet("{number}")]
        public async Task<IActionResult> GetPhaserStatus(int number)
        {
            try
            {
                var config = await _configManager.ReadPhaserConfigurations(number);
                if (config == null)
                    return NotFound($"Phaser {number} não encontrado.");

                // Compatibilidade: aceitar IPs com ou sem http://
                var formattedIp = config.IpAddress.Contains("://")
                    ? config.IpAddress
                    : $"http://{config.IpAddress}";

                var response = await _httpClient.GetAsync($"{formattedIp}/api/status");
                return Ok(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Erro de comunicação: {ex.Message}");
                return StatusCode(503, "Serviço Indisponível");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro crítico: {ex.Message}");
                return StatusCode(500, "Erro Interno");
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllPhasersConfigurations()
        {
            try
            {
                // 1. Lê as configurações do arquivo JSON
                var phasers = await _configManager.ReadAllPhaserConfigurations();

                // 2. Valida se existem Phasers configurados
                if (phasers == null || !phasers.Any())
                {
                    return NotFound("Nenhum Phaser configurado.");
                }

                // 3. Retorna os dados brutos (apenas Number e IpAddress)
                return Ok(phasers.Select(p => new
                {
                    PhaserNumber = p.Number,
                    IpAddress = p.IpAddress
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao ler configurações: {ex.Message}");
                return StatusCode(500, "Erro interno ao carregar configurações.");
            }
        }

        [HttpPut("{number}")]
        public async Task<IActionResult> PutPhaser(int number, [FromBody] PhaserAPIModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Dados inválidos.");

            try
            {
                string ipAddress = model.IpAddress.Trim();

                // Remove protocolo se existir
                ipAddress = ipAddress.Replace("http://", "").Replace("https://", "");

                if (!_configManager.IsValidIpAddress(ipAddress))
                    return BadRequest("Formato de IP inválido.");

                await _configManager.UpdatePhaserIp(number, ipAddress);

                return Ok(new
                {
                    success = true,
                    id = number,
                    newIp = ipAddress
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro crítico: {ex.Message}");
                return StatusCode(500, "Erro Interno");
            }
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class SystemInfoController : ControllerBase
    {
        private readonly ILogger<SystemInfoController> _logger;

        public SystemInfoController(ILogger<SystemInfoController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetSystemInfo()
        {
            try
            {
                var networkInfo = GetNetworkInterfaceInfo();
                var dhcpStatus = GetDhcpStatus(networkInfo.InterfaceName);

                var info = new SystemInfo
                {
                    MacAddress = networkInfo.MacAddress,
                    IpAddress = networkInfo.IpAddress,
                    NetworkMask = networkInfo.NetworkMask,
                    Gateway = networkInfo.Gateway,
                    PrimaryDns = networkInfo.PrimaryDns,
                    SecondaryDns = networkInfo.SecondaryDns,
                    DHCP = dhcpStatus,
                    SoftwareVersion = GetSoftwareVersion(),
                    LocalDateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
                };

                _logger.LogInformation("Informações do sistema recuperadas com sucesso.");
                return Ok(info);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao recuperar informações: {ex.Message}");
                return StatusCode(500, "Erro interno");
            }
        }

        private static (string InterfaceName, string MacAddress, string IpAddress, string NetworkMask,
                        string Gateway, string PrimaryDns, string SecondaryDns) GetNetworkInterfaceInfo()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    !ni.Description.Contains("virtual", StringComparison.OrdinalIgnoreCase))
                {
                    string macAddress = FormatMacAddress(ni.GetPhysicalAddress().ToString());

                    string ipAddress = "0.0.0.0";
                    string networkMask = "0.0.0.0";
                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            ipAddress = ua.Address.ToString();
                            networkMask = ua.IPv4Mask?.ToString() ?? "0.0.0.0";
                            break;
                        }
                    }

                    string gateway = "0.0.0.0";
                    foreach (var ga in ni.GetIPProperties().GatewayAddresses)
                    {
                        if (ga.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            gateway = ga.Address.ToString();
                            break;
                        }
                    }

                    string primaryDns = "0.0.0.0";
                    string secondaryDns = "0.0.0.0";
                    var dns = ni.GetIPProperties().DnsAddresses;
                    if (dns.Count > 0)
                    {
                        primaryDns = dns[0].ToString();
                        if (dns.Count > 1)
                            secondaryDns = dns[1].ToString();
                    }

                    return (ni.Name, macAddress, ipAddress, networkMask, gateway, primaryDns, secondaryDns);
                }
            }

            return ("", "00:00:00:00:00:00", "0.0.0.0", "0.0.0.0", "0.0.0.0", "0.0.0.0", "0.0.0.0");
        }

        private static bool GetDhcpStatus(string interfaceName)
        {
            if (string.IsNullOrWhiteSpace(interfaceName))
                return false;

            try
            {
                var dhcpcdConfPath = "/etc/dhcpcd.conf";

                if (System.IO.File.Exists(dhcpcdConfPath))
                {
                    var lines = System.IO.File.ReadAllLines(dhcpcdConfPath);
                    bool insideInterfaceBlock = false;

                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();

                        if (trimmed.StartsWith("interface ") && trimmed.Contains(interfaceName))
                        {
                            insideInterfaceBlock = true;
                            continue;
                        }

                        if (insideInterfaceBlock && trimmed.StartsWith("interface "))
                        {
                            break; // Novo bloco, fim do anterior
                        }

                        if (insideInterfaceBlock && trimmed.StartsWith("static "))
                        {
                            return false; // Interface com IP estático
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string FormatMacAddress(string mac)
        {
            return string.Join(":", Enumerable.Range(0, 6)
                .Select(i => mac.Substring(i * 2, 2)));
        }

        private static string GetSoftwareVersion()
        {
            return "1.0.0";
        }
    }
}