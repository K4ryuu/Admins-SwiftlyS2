using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Translation;
using TimeSpanParserUtil;

namespace Admins.Commands;

/// <summary>
/// Base class for admin commands, providing common utility methods.
/// </summary>
public partial class AdminCommands
{
    private ISwiftlyCore Core = null!;

    /// <summary>
    /// Initializes the command handler and registers it with the core.
    /// </summary>
    public void Init()
    {
        Core = Admins.SwiftlyCore;
        Core.Registrator.Register(this);
    }

    /// <summary>
    /// Sends command syntax help message to the command sender.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="cmdname">The command name.</param>
    /// <param name="arguments">Array of required arguments.</param>
    private void SendSyntax(ICommandContext context, string cmdname, string[] arguments)
    {
        var localizer = GetPlayerLocalizer(context);
        var syntax = localizer[
            "command.syntax",
            Admins.Config.CurrentValue.Prefix,
            context.Prefix,
            cmdname,
            string.Join(" ", arguments)
        ];
        context.Reply(syntax);
    }

    /// <summary>
    /// Gets the appropriate localizer for the command context.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <returns>Player-specific localizer if sent by player, otherwise server localizer.</returns>
    private ILocalizer GetPlayerLocalizer(ICommandContext context)
    {
        return context.IsSentByPlayer
            ? Core.Translation.GetPlayerLocalizer(context.Sender!)
            : Core.Localizer;
    }

    /// <summary>
    /// Sends a \"player only\" error message when command requires a player sender.
    /// </summary>
    /// <param name="context">The command context.</param>
    private void SendByPlayerOnly(ICommandContext context)
    {
        var localizer = GetPlayerLocalizer(context);
        context.Reply(localizer["command.player_only", Admins.Config.CurrentValue.Prefix]);
    }

    /// <summary>
    /// Sends a message to multiple players and optionally the command sender.
    /// </summary>
    /// <param name="players">Target players to receive the message.</param>
    /// <param name="sender">The command sender (excluded from player list).</param>
    /// <param name="messageBuilder">Function to build the message for each player.</param>
    private void SendMessageToPlayers(
        IEnumerable<IPlayer> players,
        IPlayer? sender,
        Func<IPlayer, ILocalizer, (string message, MessageType type)> messageBuilder)
    {
        foreach (var player in players)
        {
            var localizer = Core.Translation.GetPlayerLocalizer(player);
            var (message, type) = messageBuilder(player, localizer);

            // Send to target player
            player.SendMessage(type, message);

            // Also notify sender if different from target
            if (sender != null && sender != player)
            {
                sender.SendMessage(type, message);
            }
        }
    }

    /// <summary>
    /// Validates that the command has the required number of arguments.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="requiredArgs">Number of required arguments.</param>
    /// <param name="cmdname">The command name.</param>
    /// <param name="arguments">Array of argument names for syntax help.</param>
    /// <returns>True if validation passes, false otherwise.</returns>
    private bool ValidateArgsCount(ICommandContext context, int requiredArgs, string cmdname, string[] arguments)
    {
        if (context.Args.Length < requiredArgs)
        {
            SendSyntax(context, cmdname, arguments);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Finds target players based on the target string.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="target">The target player identifier.</param>
    /// <returns>List of target players, or null if none found.</returns>
    private List<IPlayer>? FindTargetPlayers(ICommandContext context, string target)
    {
        var players = Core.PlayerManager.FindTargettedPlayers(
            context.Sender!,
            target,
            TargetSearchMode.IncludeSelf
        );

        if (players == null || !players.Any())
        {
            var localizer = GetPlayerLocalizer(context);
            context.Reply(localizer[
                "command.player_not_found",
                Admins.Config.CurrentValue.Prefix,
                target
            ]);
            return null;
        }

        return players.ToList();
    }

    /// <summary>
    /// Tries to parse a duration string into a TimeSpan.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="timeString">The time string to parse.</param>
    /// <param name="duration">The parsed duration.</param>
    /// <returns>True if parsing succeeds, false otherwise.</returns>
    private bool TryParseDuration(ICommandContext context, string timeString, out TimeSpan duration)
    {
        if (!TimeSpanParser.TryParse(timeString, out duration))
        {
            var localizer = GetPlayerLocalizer(context);
            context.Reply(localizer[
                "command.invalid_time_format",
                Admins.Config.CurrentValue.Prefix,
                timeString
            ]);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Tries to parse a SteamID64 string.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="steamIdString">The SteamID64 string to parse.</param>
    /// <param name="steamId64">The parsed SteamID64.</param>
    /// <returns>True if parsing succeeds, false otherwise.</returns>
    private bool TryParseSteamID(ICommandContext context, string steamIdString, out ulong steamId64)
    {
        if (!ulong.TryParse(steamIdString, out steamId64))
        {
            var localizer = GetPlayerLocalizer(context);
            context.Reply(localizer[
                "command.invalid_steamid",
                Admins.Config.CurrentValue.Prefix,
                steamIdString
            ]);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Gets the admin name from the command context.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <returns>Admin name or "Console" if sent by server.</returns>
    private string GetAdminName(ICommandContext context)
    {
        return context.IsSentByPlayer
            ? context.Sender!.Controller.PlayerName
            : "Console";
    }

    /// <summary>
    /// Calculates expiration timestamp from duration.
    /// </summary>
    /// <param name="duration">The duration.</param>
    /// <returns>Unix timestamp in milliseconds, or 0 for permanent.</returns>
    private long CalculateExpiresAt(TimeSpan duration)
    {
        return duration.TotalMilliseconds == 0
            ? 0
            : DateTimeOffset.UtcNow.Add(duration).ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Tries to parse an integer value with validation.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="value">The string value to parse.</param>
    /// <param name="paramName">The parameter name for error messages.</param>
    /// <param name="min">Minimum allowed value.</param>
    /// <param name="max">Maximum allowed value.</param>
    /// <param name="result">The parsed integer.</param>
    /// <returns>True if parsing and validation succeed.</returns>
    private bool TryParseInt(
        ICommandContext context,
        string value,
        string paramName,
        int min,
        int max,
        out int result)
    {
        var localizer = GetPlayerLocalizer(context);

        if (!int.TryParse(value, out result))
        {
            context.Reply(localizer[
                "command.invalid_value_range",
                Admins.Config.CurrentValue.Prefix,
                value,
                paramName,
                min.ToString(),
                max.ToString()
            ]);
            return false;
        }

        if (result < min || result > max)
        {
            context.Reply(localizer[
                "command.invalid_value_range",
                Admins.Config.CurrentValue.Prefix,
                value,
                paramName,
                min.ToString(),
                max.ToString()
            ]);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Tries to parse a float value with validation.
    /// </summary>
    private bool TryParseFloat(
        ICommandContext context,
        string value,
        string paramName,
        float min,
        float max,
        out float result)
    {
        var localizer = GetPlayerLocalizer(context);

        if (!float.TryParse(value, out result))
        {
            context.Reply(localizer[
                "command.invalid_value_range",
                Admins.Config.CurrentValue.Prefix,
                value,
                paramName,
                min.ToString("F1"),
                max.ToString("F1")
            ]);
            return false;
        }

        if (result < min || result > max)
        {
            context.Reply(localizer[
                "command.invalid_value_range",
                Admins.Config.CurrentValue.Prefix,
                value,
                paramName,
                min.ToString("F1"),
                max.ToString("F1")
            ]);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Tries to parse a boolean value.
    /// </summary>
    private bool TryParseBool(ICommandContext context, string value, string paramName, out bool result)
    {
        var localizer = GetPlayerLocalizer(context);

        if (!bool.TryParse(value, out result))
        {
            context.Reply(localizer[
                "command.invalid_value_range",
                Admins.Config.CurrentValue.Prefix,
                value,
                paramName,
                "false",
                "true"
            ]);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets a valid player name, defaulting to "Unknown" if controller is invalid.
    /// </summary>
    private string GetPlayerName(IPlayer player)
    {
        return player.Controller.IsValid ? player.Controller.PlayerName : "Unknown";
    }
}