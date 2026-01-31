using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ResourceEngagementTrackingSystem.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using QRCoder;

namespace ResourceEngagementTrackingSystem.Infrastructure.Services;

public class TwoFactorService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public TwoFactorService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<(
        string qrCodeUri,
        string qrCodeImage,
        string manualEntryKey
    )> GenerateQrCodeDataAsync(ApplicationUser user, string issuer = "AuthMicroservice")
    {
        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        var email = user.Email ?? user.UserName;
        var qrCodeUri =
            $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={key}&issuer={Uri.EscapeDataString(issuer)}";

        // Generate QR Code Image
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(qrCodeUri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var pngBytes = qrCode.GetGraphic(20);
        var qrCodeImage = Convert.ToBase64String(pngBytes);

        // Format manual entry key
        var formattedKey = string.Join(
                " ",
                key.ToCharArray()
                    .Select((c, i) => i % 4 == 0 && i > 0 ? " " + c : c.ToString())
                    .ToArray()
            )
            .ToLower();

        return (qrCodeUri, $"data:image/png;base64,{qrCodeImage}", formattedKey);
    }

    public async Task<string> GenerateQrCodeUriAsync(
        ApplicationUser user,
        string issuer = "AuthMicroservice"
    )
    {
        var (qrCodeUri, _, _) = await GenerateQrCodeDataAsync(user, issuer);
        return qrCodeUri;
    }

    public async Task<string> GetManualEntryKeyAsync(ApplicationUser user)
    {
        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
        }
        return FormatKey(key!);
    }

    public async Task<bool> VerifyTwoFactorCodeAsync(ApplicationUser user, string code)
    {
        var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            code
        );
        return is2faTokenValid;
    }

    private string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        int currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.Substring(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }
        if (currentPosition < unformattedKey.Length)
        {
            result.Append(unformattedKey.Substring(currentPosition));
        }

        return result.ToString().ToLowerInvariant();
    }
}
