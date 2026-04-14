using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using TextBox = System.Windows.Controls.TextBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;
using OpenDash.DiscordChatOverlay.Models;
using OpenDash.DiscordChatOverlay.Services;
using OpenDash.OverlayCore.Settings;

namespace OpenDash.DiscordChatOverlay.Settings;

public class AliasSettingsCategory : ISettingsCategory
{
    private readonly AliasService        _aliasService;
    private readonly VoiceSessionService _voiceService;

    // Tracks live control state: (guildId+channelId) → list of (member, customNameBox, avatarCheckBox)
    private readonly List<(ChannelContext Context, ChannelMember Member, TextBox NameBox, CheckBox AvatarCheck)>
        _rows = new();

    private StackPanel? _contentPanel;

    public string CategoryName => "Aliases";
    public int SortOrder       => 40;

    public AliasSettingsCategory(AliasService aliasService, VoiceSessionService voiceService)
    {
        _aliasService = aliasService;
        _voiceService = voiceService;
    }

    public FrameworkElement CreateContent()
    {
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        _contentPanel = new StackPanel { Margin = new Thickness(16) };
        scroll.Content = _contentPanel;

        BuildContent();
        return scroll;
    }

    public void SaveValues()
    {
        foreach (var (ctx, member, nameBox, avatarCheck) in _rows)
        {
            var customName = string.IsNullOrWhiteSpace(nameBox.Text) ? null : nameBox.Text.Trim();
            if (customName != null && customName.Length > 100)
                customName = customName[..100];

            member.CustomDisplayName = customName;
            member.AvatarVisible     = avatarCheck.IsChecked ?? true;

            _aliasService.UpsertChannelMember(
                ctx.GuildId, ctx.ChannelId, member.UserId, member.LastKnownName);
        }

        // Sync custom names and AvatarVisible — UpsertChannelMember only touches LastKnownName;
        // write the updated member values directly then save.
        _aliasService.Save();

        // Refresh the overlay immediately so the new names appear without requiring a restart.
        _voiceService.RefreshDisplayNames();
    }

    public void LoadValues()
    {
        if (_contentPanel == null) return;

        _rows.Clear();
        _contentPanel.Children.Clear();
        BuildContent();
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private void BuildContent()
    {
        if (_contentPanel == null) return;

        var contexts = _aliasService.GetAllContexts();

        if (contexts.Count == 0)
        {
            _contentPanel.Children.Add(new TextBlock
            {
                Text         = "No voice channel contexts recorded yet.\nJoin a Discord voice channel to auto-populate this list.",
                TextWrapping = TextWrapping.Wrap,
                Foreground   = System.Windows.Media.Brushes.Gray,
                Margin       = new Thickness(0, 8, 0, 0)
            });
            return;
        }

        foreach (var ctx in contexts)
        {
            // ── Context header row ─────────────────────────────────────────

            var headerRow = new DockPanel { Margin = new Thickness(0, 16, 0, 4) };

            var headerLabel = new TextBlock
            {
                Text       = $"{ctx.GuildName} / {ctx.ChannelName}",
                FontSize   = 14,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(headerLabel, Dock.Left);
            headerRow.Children.Add(headerLabel);

            var deleteBtn = new Button
            {
                Content = "Delete Context",
                Margin  = new Thickness(8, 0, 0, 0),
                Padding = new Thickness(6, 2, 6, 2),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var capturedCtx = ctx;
            deleteBtn.Click += (_, _) => OnDeleteContext(capturedCtx);
            DockPanel.SetDock(deleteBtn, Dock.Right);
            headerRow.Children.Add(deleteBtn);

            _contentPanel.Children.Add(headerRow);

            // ── Column header row ──────────────────────────────────────────

            var colHeader = new Grid { Margin = new Thickness(0, 0, 0, 2) };
            colHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            colHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            colHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

            AddColHeaderText(colHeader, "Discord Name", 0);
            AddColHeaderText(colHeader, "Custom Display Name (optional)", 1);
            AddColHeaderText(colHeader, "Avatar", 2);

            _contentPanel.Children.Add(colHeader);

            // ── Member rows ────────────────────────────────────────────────

            if (ctx.Members.Count == 0)
            {
                _contentPanel.Children.Add(new TextBlock
                {
                    Text       = "  (no members recorded)",
                    Foreground = System.Windows.Media.Brushes.Gray,
                    Margin     = new Thickness(0, 0, 0, 4)
                });
                continue;
            }

            foreach (var member in ctx.Members)
            {
                var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

                // Discord name (read-only)
                var nameLabel = new TextBlock
                {
                    Text              = member.LastKnownName,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming      = TextTrimming.CharacterEllipsis
                };
                Grid.SetColumn(nameLabel, 0);
                row.Children.Add(nameLabel);

                // Custom display name TextBox
                var nameBox = new TextBox
                {
                    Text       = member.CustomDisplayName ?? string.Empty,
                    MaxLength  = 100,
                    Margin     = new Thickness(4, 0, 4, 0),
                    AcceptsReturn = false
                };
                Grid.SetColumn(nameBox, 1);
                row.Children.Add(nameBox);

                // Avatar visible CheckBox
                var avatarCheck = new CheckBox
                {
                    IsChecked           = member.AvatarVisible,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center
                };
                Grid.SetColumn(avatarCheck, 2);
                row.Children.Add(avatarCheck);

                _contentPanel.Children.Add(row);
                _rows.Add((ctx, member, nameBox, avatarCheck));
            }
        }

        // ── Global save button ─────────────────────────────────────────────

        var saveBtn = new Button
        {
            Content = "Save Aliases",
            Margin  = new Thickness(0, 20, 0, 0),
            Padding = new Thickness(12, 4, 12, 4),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        saveBtn.Click += (_, _) => SaveValues();
        _contentPanel.Children.Add(saveBtn);
    }

    private void OnDeleteContext(ChannelContext ctx)
    {
        var result = MessageBox.Show(
            $"Delete context \"{ctx.GuildName} / {ctx.ChannelName}\" and all its member aliases?\nThis cannot be undone.",
            "Delete Context",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        _aliasService.DeleteChannelContext(ctx.GuildId, ctx.ChannelId);
        LoadValues();
    }

    private static void AddColHeaderText(Grid grid, string text, int column)
    {
        var tb = new TextBlock
        {
            Text       = text,
            FontWeight = FontWeights.SemiBold,
            FontSize   = 12,
            Foreground = System.Windows.Media.Brushes.Gray
        };
        Grid.SetColumn(tb, column);
        grid.Children.Add(tb);
    }
}
