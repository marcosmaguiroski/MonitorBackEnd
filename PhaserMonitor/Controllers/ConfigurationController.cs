using Microsoft.AspNetCore.Mvc;
using PhaserMonitor.Model;
using PhaserMonitor.Helpers;
using Microsoft.Extensions.Logging;

namespace PhaserMonitor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NetworkController : ControllerBase
    {
        private readonly ILogger<NetworkController> _logger;

        public NetworkController(ILogger<NetworkController> logger)
        {
            _logger = logger;
        }

        [HttpGet("configuration")]
        public IActionResult GetNetworkConfiguration()
        {
            try
            {
                var config = LinuxHelpers.GetNetworkConfiguration();
                _logger.LogInformation("Configuração de rede recuperada com sucesso");
                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao obter configuração de rede: {ex.Message}");
                return StatusCode(500, "Erro interno ao obter configurações de rede");
            }
        }

        [HttpPut("configuration")]
        public IActionResult UpdateNetworkConfiguration([FromBody] PhaserNetworkConfigurationModel config)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Configuração de rede inválida recebida");
                    return BadRequest(ModelState);
                }

                LinuxHelpers.SetNetworkConfiguration(config);
                _logger.LogInformation("Configuração de rede atualizada com sucesso");
                return Ok(new { success = true, message = "Configuração atualizada" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Erro de validação: {ex.Message}");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao atualizar configuração: {ex.Message}");
                return StatusCode(500, "Erro interno ao atualizar configurações");
            }
        }
    }
}