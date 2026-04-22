using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Meziantou.Framework.Win32;
using OpenDash.OverlayCore.Services;

namespace OpenDash.SpeakerSight.Services;

public record TokenBundle(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiryUtc);

public class TokenStorageService
{
    private const string CredentialTarget = "SpeakerSight";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly HttpClient Http = new();

    // ── Read / Write / Delete ──────────────────────────────────────────────

    public TokenBundle? ReadToken()
    {
        try
        {
            var cred = CredentialManager.ReadCredential(CredentialTarget);
            if (cred == null) return null;

            var bundle = JsonSerializer.Deserialize<TokenBundle>(cred.Password!, JsonOptions);
            return bundle;
        }
        catch (Exception ex)
        {
            LogService.Error("TokenStorageService.ReadToken: Failed to read credential.", ex);
            return null;
        }
    }

    public void WriteToken(TokenBundle bundle)
    {
        try
        {
            var json = JsonSerializer.Serialize(bundle, JsonOptions);
            CredentialManager.WriteCredential(
                CredentialTarget,
                userName: "oauth2",
                secret: json,
                CredentialPersistence.LocalMachine);
        }
        catch (Exception ex)
        {
            LogService.Error("TokenStorageService.WriteToken: Failed to write credential.", ex);
        }
    }

    public void DeleteToken()
    {
        try
        {
            CredentialManager.DeleteCredential(CredentialTarget);
        }
        catch (Exception ex)
        {
            LogService.Error("TokenStorageService.DeleteToken: Failed to delete credential.", ex);
        }
    }

    // ── Expiry check ───────────────────────────────────────────────────────

    public bool IsTokenExpiredOrExpiringSoon(TokenBundle bundle) =>
        bundle.ExpiryUtc - DateTime.UtcNow < TimeSpan.FromHours(1);

    // ── PKCE helpers ───────────────────────────────────────────────────────

    public string GeneratePkceVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    public string GeneratePkceChallenge(string verifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Base64UrlEncode(hash);
    }

    // ── Token exchange ─────────────────────────────────────────────────────

    public async Task<TokenBundle> ExchangeCode(string code, string verifier, string clientId)
    {
        var form = new System.Collections.Generic.Dictionary<string, string>
        {
            ["grant_type"]    = "authorization_code",
            ["code"]          = code,
            ["code_verifier"] = verifier,
            ["client_id"]     = clientId,
        };

        return await PostTokenRequest(form);
    }

    public async Task<TokenBundle> RefreshToken(string refreshToken, string clientId)
    {
        var form = new System.Collections.Generic.Dictionary<string, string>
        {
            ["grant_type"]    = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"]     = clientId,
        };

        return await PostTokenRequest(form);
    }

    // ── Internals ──────────────────────────────────────────────────────────

    private static async Task<TokenBundle> PostTokenRequest(
        System.Collections.Generic.Dictionary<string, string> form)
    {
        try
        {
            var response = await Http.PostAsync(
                "https://discord.com/api/oauth2/token",
                new FormUrlEncodedContent(form));

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var accessToken  = root.GetProperty("access_token").GetString()!;
            var refreshToken = root.GetProperty("refresh_token").GetString()!;
            var expiresIn    = root.GetProperty("expires_in").GetInt32();

            return new TokenBundle(accessToken, refreshToken, DateTime.UtcNow.AddSeconds(expiresIn));
        }
        catch (Exception ex)
        {
            LogService.Error("TokenStorageService: Token HTTP request failed.", ex);
            throw;
        }
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
