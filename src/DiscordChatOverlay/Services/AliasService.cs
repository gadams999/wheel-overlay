using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using OpenDash.DiscordChatOverlay.Models;
using OpenDash.OverlayCore.Services;

namespace OpenDash.DiscordChatOverlay.Services;

/// <summary>
/// Manages per-channel member aliases stored in %APPDATA%\DiscordChatOverlay\aliases.json.
/// </summary>
public class AliasService
{
    private static readonly string Default_aliasesPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DiscordChatOverlay",
        "aliases.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _aliasesPath;
    private List<ChannelContext> _contexts = new();

    /// <summary>Creates an AliasService using the default %APPDATA% path.</summary>
    public AliasService() : this(Default_aliasesPath) { }

    /// <summary>Creates an AliasService using a custom path. Intended for testing.</summary>
    internal AliasService(string aliasesPath)
    {
        _aliasesPath = aliasesPath;
    }

    /// <summary>
    /// Loads aliases.json. Skips malformed entries and logs each skip.
    /// Returns empty list on missing file or unrecoverable parse error.
    /// </summary>
    public void Load()
    {
        _contexts = new List<ChannelContext>();

        if (!File.Exists(_aliasesPath))
            return;

        try
        {
            var json = File.ReadAllText(_aliasesPath);
            var raw = JsonSerializer.Deserialize<List<ChannelContext>>(json, JsonOptions);
            if (raw == null) return;

            foreach (var ctx in raw)
            {
                if (!IsValidSnowflake(ctx.GuildId) || !IsValidSnowflake(ctx.ChannelId))
                {
                    LogService.Error(
                        $"AliasService.Load: Skipping ChannelContext with invalid GuildId='{ctx.GuildId}' or ChannelId='{ctx.ChannelId}'.");
                    continue;
                }

                var validMembers = new List<ChannelMember>();
                foreach (var member in ctx.Members ?? new List<ChannelMember>())
                {
                    if (!IsValidSnowflake(member.UserId) ||
                        (string.IsNullOrEmpty(member.LastKnownName) && member.CustomDisplayName == null))
                    {
                        LogService.Error(
                            $"AliasService.Load: Skipping ChannelMember with UserId='{member.UserId}' in context {ctx.GuildId}/{ctx.ChannelId}.");
                        continue;
                    }
                    validMembers.Add(member);
                }

                ctx.Members = validMembers;
                _contexts.Add(ctx);
            }
        }
        catch (Exception ex)
        {
            LogService.Error("AliasService.Load: Failed to parse aliases.json; starting with empty alias list.", ex);
            _contexts = new List<ChannelContext>();
        }
    }

    /// <summary>Saves aliases.json atomically via temp-file rename.</summary>
    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_aliasesPath)!;
            Directory.CreateDirectory(dir);

            var tmp = _aliasesPath + ".tmp";
            var json = JsonSerializer.Serialize(_contexts, JsonOptions);
            File.WriteAllText(tmp, json);
            File.Move(tmp, _aliasesPath, overwrite: true);
        }
        catch (Exception ex)
        {
            LogService.Error("AliasService.Save: Failed to write aliases.json.", ex);
        }
    }

    /// <summary>Creates or updates a ChannelContext entry and saves.</summary>
    public void UpsertChannelContext(string guildId, string guildName, string channelId, string channelName)
    {
        var ctx = _contexts.FirstOrDefault(c => c.GuildId == guildId && c.ChannelId == channelId);
        if (ctx == null)
        {
            ctx = new ChannelContext { GuildId = guildId, ChannelId = channelId };
            _contexts.Add(ctx);
        }

        ctx.GuildName = guildName;
        ctx.ChannelName = channelName;
        Save();
    }

    /// <summary>
    /// Adds or updates a ChannelMember entry. Always updates LastKnownName when changed.
    /// Preserves existing CustomDisplayName and AvatarVisible. Saves after update.
    /// </summary>
    public void UpsertChannelMember(string guildId, string channelId, string userId, string discordDisplayName)
    {
        var ctx = _contexts.FirstOrDefault(c => c.GuildId == guildId && c.ChannelId == channelId);
        if (ctx == null)
        {
            ctx = new ChannelContext { GuildId = guildId, ChannelId = channelId };
            _contexts.Add(ctx);
        }

        var member = ctx.Members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
        {
            member = new ChannelMember { UserId = userId };
            ctx.Members.Add(member);
        }

        if (member.LastKnownName != discordDisplayName)
            member.LastKnownName = discordDisplayName;

        Save();
    }

    /// <summary>Removes the context and all its members. Saves after removal.</summary>
    public void DeleteChannelContext(string guildId, string channelId)
    {
        _contexts.RemoveAll(c => c.GuildId == guildId && c.ChannelId == channelId);
        Save();
    }

    /// <summary>
    /// Resolves the display name for a participant:
    /// CustomDisplayName ?? LastKnownName ?? rawDiscordName
    /// </summary>
    public string Resolve(string userId, string guildId, string channelId, string rawDiscordName = "")
    {
        var ctx = _contexts.FirstOrDefault(c => c.GuildId == guildId && c.ChannelId == channelId);
        var member = ctx?.Members.FirstOrDefault(m => m.UserId == userId);

        return member?.CustomDisplayName
            ?? (string.IsNullOrEmpty(member?.LastKnownName) ? null : member.LastKnownName)
            ?? rawDiscordName;
    }

    /// <summary>Returns the ChannelContext for the given guild+channel, or null if not found.</summary>
    public ChannelContext? GetContext(string guildId, string channelId) =>
        _contexts.FirstOrDefault(c => c.GuildId == guildId && c.ChannelId == channelId);

    /// <summary>Returns all loaded ChannelContext records.</summary>
    public IReadOnlyList<ChannelContext> GetAllContexts() => _contexts.AsReadOnly();

    // --- helpers ---

    private static bool IsValidSnowflake(string? id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return ulong.TryParse(id, out _);
    }
}
