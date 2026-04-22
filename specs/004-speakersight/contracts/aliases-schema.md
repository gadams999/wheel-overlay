# Contract: aliases.json Schema

**File**: `%APPDATA%\SpeakerSight\aliases.json`
**Owned by**: `AliasService.cs` (`OpenDash.SpeakerSight.Services`)
**Serializer**: `System.Text.Json`; case-insensitive; unknown properties ignored

---

## Schema

```jsonc
// Root: array of ChannelContext objects
[
  {
    "GuildId": "123456789012345678",       // string — Discord guild snowflake ID (permanent key)
    "GuildName": "My Server",              // string — cached; updated on each observation
    "ChannelId": "987654321098765432",     // string — Discord channel snowflake ID (permanent key)
    "ChannelName": "General",             // string — cached; updated on each observation
    "Members": [
      {
        "UserId": "111222333444555666",    // string — Discord user snowflake ID (permanent key)
        "LastKnownName": "JohnDoe",       // string — auto-updated; never null
        "CustomDisplayName": "Johnny",    // string | null — user-set; null = not set
        "AvatarVisible": true             // bool — default true
      }
    ]
  }
]
```

## Example

```json
[
  {
    "GuildId": "856701234567890123",
    "GuildName": "Racing Crew",
    "ChannelId": "856709876543210987",
    "ChannelName": "Voice Chat",
    "Members": [
      {
        "UserId": "190320984123768832",
        "LastKnownName": "Speedy McFast",
        "CustomDisplayName": "Speedy",
        "AvatarVisible": true
      },
      {
        "UserId": "290430094234879943",
        "LastKnownName": "TurboGamer99",
        "CustomDisplayName": null,
        "AvatarVisible": false
      }
    ]
  }
]
```

## Validation Rules

`AliasService` applies these rules at load time. Invalid entries are skipped and logged via `LogService.Error()`. Valid entries continue to apply.

| Check | Failure action |
|-------|---------------|
| `GuildId` empty or not a uint64 string | Skip entire `ChannelContext`; log error |
| `ChannelId` empty or not a uint64 string | Skip entire `ChannelContext`; log error |
| `Members` null | Treat as empty list; continue |
| `UserId` empty or not a uint64 string | Skip that `ChannelMember` entry; log error |
| `LastKnownName` null or empty AND `CustomDisplayName` null | Skip that `ChannelMember` entry; log error |
| Root JSON not an array | Load as empty list; `LogService.Error()` |
| Malformed JSON | Load as empty list; `LogService.Error()` |
| Missing file | Load as empty list (normal first-run state) |

## Write Contract

`AliasService.Save()` writes atomically (temp file + rename, same as `AppSettings`). Called after:
- A new `ChannelContext` is created (user joins a new channel)
- A `ChannelMember` is added or updated
- A user saves custom name / avatar toggle changes in settings
- A `ChannelContext` is deleted by the user

## Key Invariants

1. (`GuildId`, `ChannelId`) is unique within the root array — no duplicate contexts.
2. `UserId` is unique within a `ChannelContext.Members` list.
3. `CustomDisplayName` overrides `LastKnownName` for display — never the reverse.
4. `LastKnownName` is always updated to the member's current Discord display name on observation, even when `CustomDisplayName` is set.
5. Deleting a `ChannelContext` removes it and **all** its `Members` from the file.
