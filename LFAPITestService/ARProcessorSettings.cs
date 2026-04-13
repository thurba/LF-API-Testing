
namespace LFAPITestService;

public class ARProcessorSettings
{
    public string MonitorFilePath { get; set; } = string.Empty;
    public string PassPhrase { get; set; } = string.Empty;
    public string PrivateKeyPath { get; set; } = string.Empty; // "/run/secrets/pgp_private_key" // for Docker secret path
}
