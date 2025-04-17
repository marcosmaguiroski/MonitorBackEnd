using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using System;
using Microsoft.Extensions.Logging;
using PhaserMonitor.Model;

namespace PhaserMonitor.Services;
public class PhaserConfigManager
{
    private readonly string _filePath;
    private readonly ILogger<PhaserConfigManager> _logger;

    public PhaserConfigManager(ILogger<PhaserConfigManager> logger)
    {
        _logger = logger;
        _filePath = Path.Combine(AppContext.BaseDirectory, "PhaserData.json");
        _logger.LogInformation($"Arquivo de configuração localizado em: {_filePath}");

        // Verifica se o arquivo existe, caso contrário, cria um arquivo vazio
        if (!File.Exists(_filePath))
        {
            _logger.LogInformation($"Arquivo de configuração não encontrado. Criando novo arquivo em: {_filePath}");
            File.WriteAllText(_filePath, string.Empty);
        }
    }

    public async Task<PhaserConfig?> ReadPhaserConfigurations(int number)
    {
        _logger.LogInformation($"Buscando configuração do Phaser {number}.");
        var allPhasers = await ReadAllPhaserConfigurations();
        return allPhasers.FirstOrDefault(p => p.Number == number);
    }

    public async Task<List<PhaserConfig>> ReadAllPhaserConfigurations()
    {
        _logger.LogInformation("Lendo todas as configurações dos Phasers.");

        if (!File.Exists(_filePath))
        {
            _logger.LogWarning($"Arquivo de configuração não encontrado: {_filePath}");
            return new List<PhaserConfig>();
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(_filePath);
            var phasers = lines.Select(ParseLine)
                              .Where(p => p != null)
                              .Select(p => {
                                  // Remove http:// ou https:// se existir
                                  p.IpAddress = p.IpAddress.Replace("http://", "").Replace("https://", "");
                                  return p;
                              })
                              .ToList();

            _logger.LogInformation($"Foram encontradas {phasers.Count} configurações de Phasers.");
            return phasers;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro ao ler o arquivo de configuração: {ex.Message}");
            return new List<PhaserConfig>();
        }
    }
    private PhaserConfig? ParseLine(string line)
    {
        var parts = line.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !int.TryParse(parts[0].Replace("Phaser", ""), out int number))
            return null;

        return new PhaserConfig { Number = number, IpAddress = parts[1].Trim() };
    }


    public async Task UpdatePhaserIp(int number, string newIp)
    {
        _logger.LogInformation($"Atualizando IP do Phaser {number} para {newIp}.");

        // Remove http:// ou https:// se existir
        newIp = newIp.Replace("http://", "").Replace("https://", "");

        if (!IsValidIpAddress(newIp))
        {
            _logger.LogWarning($"IP inválido: {newIp}");
            throw new ArgumentException("O IP fornecido é inválido.");
        }

        var phasers = await ReadAllPhaserConfigurations();
        var existing = phasers.FirstOrDefault(p => p.Number == number);

        if (existing != null)
        {
            existing.IpAddress = newIp;
        }
        else
        {
            phasers.Add(new PhaserConfig { Number = number, IpAddress = newIp });
        }

        var lines = phasers.Select(p => $"Phaser {p.Number}: {p.IpAddress}");
        await File.WriteAllLinesAsync(_filePath, lines);

        _logger.LogInformation($"IP do Phaser {number} atualizado com sucesso.");
    }

    public bool IsValidIpAddress(string ip)
    {
        // Remove http:// ou https:// se existir
        ip = ip.Replace("http://", "").Replace("https://", "");

        // Verifica se é um IP válido ou um hostname válido
        return Uri.CheckHostName(ip) != UriHostNameType.Unknown;
    }
}