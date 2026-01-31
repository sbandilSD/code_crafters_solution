namespace ResourceEngagementTrackingSystem.Application.DTOs;

public class TwoFactorSetupDto
{
    public string QrCodeUri { get; set; } = string.Empty;
    public string QrCodeImage { get; set; } = string.Empty;
    public string ManualEntryKey { get; set; } = string.Empty;
}