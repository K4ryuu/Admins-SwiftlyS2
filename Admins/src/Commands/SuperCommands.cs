using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Admins.Commands;

public partial class AdminCommands
{
    #region Player Management Commands
    [Command("hp", permission: "admins.commands.hp")]
    public void Command_HP(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            SendByPlayerOnly(context);
            return;
        }

        if (!ValidateArgsCount(context, 2, "hp", ["<player>", "<health>", "[armour]", "[helmet]"]))
        {
            return;
        }

        var players = FindTargetPlayers(context, context.Args[0]);
        if (players == null)
        {
            return;
        }

        if (!TryParseInt(context, context.Args[1], "health", 0, 100, out var health))
        {
            return;
        }

        var armour = 0;
        if (context.Args.Length >= 3 && !TryParseInt(context, context.Args[2], "armour", 0, 100, out armour))
        {
            return;
        }

        var helmet = false;
        if (context.Args.Length >= 4 && !TryParseBool(context, context.Args[3], "helmet", out helmet))
        {
            return;
        }

        foreach (var player in players)
        {
            ApplyHealthAndArmor(player, health, armour, helmet);
        }

        NotifyHealthChanged(players, context.Sender!, health, armour, helmet);
    }

    [Command("giveitem", permission: "admins.commands.giveitem")]
    public void Command_GiveItem(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            SendByPlayerOnly(context);
            return;
        }

        if (!ValidateArgsCount(context, 2, "giveitem", ["<player>", "<item_name>"]))
        {
            return;
        }

        var players = FindTargetPlayers(context, context.Args[0]);
        if (players == null)
        {
            return;
        }

        var itemName = context.Args[1];
        foreach (var player in players)
        {
            GiveItemToPlayer(player, itemName);
        }

        NotifyItemGiven(players, context.Sender!, itemName);
    }

    [Command("givemoney", permission: "admins.commands.givemoney")]
    public void Command_GiveMoney(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            SendByPlayerOnly(context);
            return;
        }

        if (!ValidateArgsCount(context, 2, "givemoney", ["<player>", "<amount>"]))
        {
            return;
        }

        var players = FindTargetPlayers(context, context.Args[0]);
        if (players == null)
        {
            return;
        }

        if (!TryParseInt(context, context.Args[1], "amount", 1, 16000, out var amount))
        {
            return;
        }

        foreach (var player in players)
        {
            ModifyPlayerMoney(player, amount, isAdditive: true);
        }

        NotifyMoneyChanged(players, context.Sender!, amount, "command.givemoney_success");
    }

    [Command("setmoney", permission: "admins.commands.setmoney")]
    public void Command_SetMoney(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            SendByPlayerOnly(context);
            return;
        }

        if (!ValidateArgsCount(context, 2, "setmoney", ["<player>", "<amount>"]))
        {
            return;
        }

        var players = FindTargetPlayers(context, context.Args[0]);
        if (players == null)
        {
            return;
        }

        if (!TryParseInt(context, context.Args[1], "amount", 0, 16000, out var amount))
        {
            return;
        }

        foreach (var player in players)
        {
            ModifyPlayerMoney(player, amount, isAdditive: false);
        }

        NotifyMoneyChanged(players, context.Sender!, amount, "command.setmoney_success");
    }

    [Command("melee", permission: "admins.commands.melee")]
    public void Command_Melee(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            SendByPlayerOnly(context);
            return;
        }

        if (!ValidateArgsCount(context, 1, "melee", ["<player>"]))
        {
            return;
        }

        var players = FindTargetPlayers(context, context.Args[0]);
        if (players == null)
        {
            return;
        }

        foreach (var player in players)
        {
            StripAndGiveKnife(player);
        }

        NotifyPlayersAction(players, context.Sender!, "command.melee_success");
    }

    [Command("disarm", permission: "admins.commands.disarm")]
    public void Command_Disarm(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            SendByPlayerOnly(context);
            return;
        }

        if (!ValidateArgsCount(context, 1, "disarm", ["<player>"]))
        {
            return;
        }

        var players = FindTargetPlayers(context, context.Args[0]);
        if (players == null)
        {
            return;
        }

        foreach (var player in players)
        {
            RemoveAllItems(player);
        }

        NotifyPlayersAction(players, context.Sender!, "command.disarm_success");
    }

    [Command("restartround", permission: "admins.commands.restartround")]
    [CommandAlias("rr")]
    public void Command_RestartRound(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            SendByPlayerOnly(context);
            return;
        }

        if (!ValidateArgsCount(context, 1, "restartround", ["<delay>"]))
        {
            return;
        }

        if (!TryParseFloat(context, context.Args[0], "delay", 0, 300, out var delay))
        {
            return;
        }

        var gameRules = Core.EntitySystem.GetGameRules();
        if (gameRules != null && gameRules.IsValid)
        {
            gameRules.TerminateRound(RoundEndReason.RoundDraw, delay);
        }

        var adminName = context.Sender!.Controller.PlayerName;
        SendMessageToPlayers(Core.PlayerManager.GetAllPlayers(), context.Sender!, (p, localizer) =>
        {
            return (localizer["command.restartround", Admins.Config.CurrentValue.Prefix, adminName, delay], MessageType.Chat);
        });
    }

    [Command("freeze", permission: "admins.commands.freeze")]
    public void Command_Freeze(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            SendByPlayerOnly(context);
            return;
        }

        if (!ValidateArgsCount(context, 1, "freeze", ["<player>"]))
        {
            return;
        }

        var players = FindTargetPlayers(context, context.Args[0]);
        if (players == null)
        {
            return;
        }

        foreach (var player in players)
        {
            SetPlayerMoveType(player, MoveType_t.MOVETYPE_INVALID);
        }

        NotifyPlayersAction(players, context.Sender!, "command.freeze_success");
    }

    [Command("unfreeze", permission: "admins.commands.unfreeze")]
    public void Command_Unfreeze(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            SendByPlayerOnly(context);
            return;
        }

        if (!ValidateArgsCount(context, 1, "unfreeze", ["<player>"]))
        {
            return;
        }

        var players = FindTargetPlayers(context, context.Args[0]);
        if (players == null)
        {
            return;
        }

        foreach (var player in players)
        {
            SetPlayerMoveType(player, MoveType_t.MOVETYPE_WALK);
        }

        NotifyPlayersAction(players, context.Sender!, "command.unfreeze_success");
    }

    [Command("noclip", permission: "admins.commands.noclip")]
    public void Command_Noclip(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            SendByPlayerOnly(context);
            return;
        }

        var pawn = context.Sender!.PlayerPawn;
        var localizer = GetPlayerLocalizer(context);

        if (!IsValidAlivePawn(pawn))
        {
            context.Reply(localizer["command.noclip_no_pawn", Admins.Config.CurrentValue.Prefix]);
            return;
        }

        if (pawn!.MoveType == MoveType_t.MOVETYPE_NOCLIP)
        {
            SetPlayerMoveType(context.Sender!, MoveType_t.MOVETYPE_WALK);
            context.Reply(localizer["command.noclip_disabled", Admins.Config.CurrentValue.Prefix]);
        }
        else
        {
            SetPlayerMoveType(context.Sender!, MoveType_t.MOVETYPE_NOCLIP);
            context.Reply(localizer["command.noclip_enabled", Admins.Config.CurrentValue.Prefix]);
        }
    }

    [Command("setspeed", permission: "admins.commands.setspeed")]
    public void Command_SetSpeed(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            SendByPlayerOnly(context);
            return;
        }

        if (!ValidateArgsCount(context, 1, "setspeed", ["<speed_multiplier>"]))
        {
            return;
        }

        if (!TryParseFloat(context, context.Args[0], "speed_multiplier", 0.1f, 10.0f, out var speedMultiplier))
        {
            return;
        }

        var pawn = context.Sender!.PlayerPawn;
        var localizer = GetPlayerLocalizer(context);

        if (!IsValidAlivePawn(pawn))
        {
            context.Reply(localizer["command.setspeed_no_pawn", Admins.Config.CurrentValue.Prefix]);
            return;
        }

        pawn!.VelocityModifier = speedMultiplier;
        pawn.VelocityModifierUpdated();

        context.Reply(localizer["command.setspeed_success", Admins.Config.CurrentValue.Prefix, speedMultiplier]);
    }

    [Command("setgravity", permission: "admins.commands.setgravity")]
    public void Command_SetGravity(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            SendByPlayerOnly(context);
            return;
        }

        if (!ValidateArgsCount(context, 1, "setgravity", ["<gravity_multiplier>"]))
        {
            return;
        }

        if (!TryParseFloat(context, context.Args[0], "gravity_multiplier", 0.1f, 10.0f, out var gravityMultiplier))
        {
            return;
        }

        var pawn = context.Sender!.PlayerPawn;
        var localizer = GetPlayerLocalizer(context);

        if (!IsValidAlivePawn(pawn))
        {
            context.Reply(localizer["command.setgravity_no_pawn", Admins.Config.CurrentValue.Prefix]);
            return;
        }

        pawn!.GravityScale = gravityMultiplier;
        pawn.GravityScaleUpdated();

        context.Reply(localizer["command.setgravity_success", Admins.Config.CurrentValue.Prefix, gravityMultiplier]);
    }

    [Command("slay", permission: "admins.commands.slay")]
    public void Command_Slay(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            SendByPlayerOnly(context);
            return;
        }

        if (!ValidateArgsCount(context, 1, "slay", ["<player>"]))
        {
            return;
        }

        var players = FindTargetPlayers(context, context.Args[0]);
        if (players == null)
        {
            return;
        }

        foreach (var player in players)
        {
            SlayPlayer(player);
        }

        NotifyPlayersAction(players, context.Sender!, "command.slay_success");
    }

    [Command("slap", permission: "admins.commands.slap")]
    public void Command_Slap(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            SendByPlayerOnly(context);
            return;
        }

        if (!ValidateArgsCount(context, 1, "slap", ["<player>", "[damage]"]))
        {
            return;
        }

        var players = FindTargetPlayers(context, context.Args[0]);
        if (players == null)
        {
            return;
        }

        var damage = 0;
        if (context.Args.Length >= 2 && !TryParseInt(context, context.Args[1], "damage", 0, 100, out damage))
        {
            return;
        }

        foreach (var player in players)
        {
            ApplySlap(player, damage);
        }

        NotifyPlayersAction(players, context.Sender!, "command.slap_success");
    }

    [Command("rename", permission: "admins.commands.rename")]
    public void Command_Rename(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            SendByPlayerOnly(context);
            return;
        }

        if (!ValidateArgsCount(context, 2, "rename", ["<player>", "<new_name>"]))
        {
            return;
        }

        var players = FindTargetPlayers(context, context.Args[0]);
        if (players == null)
        {
            return;
        }

        var oldNames = new Dictionary<IPlayer, string>();
        var newName = context.Args[1];

        foreach (var player in players)
        {
            if (player.Controller != null && player.Controller.IsValid)
            {
                oldNames[player] = player.Controller.PlayerName;
                player.Controller.PlayerName = newName;
                player.Controller.PlayerNameUpdated();
            }
        }

        NotifyRename(players, context.Sender!, oldNames, newName);
    }

    [Command("csay", permission: "admins.commands.csay")]
    public void Command_CSay(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            SendByPlayerOnly(context);
            return;
        }

        if (!ValidateArgsCount(context, 1, "csay", ["<message>"]))
        {
            return;
        }

        var message = string.Join(" ", context.Args);
        var adminName = context.Sender!.Controller.PlayerName;
        Core.PlayerManager.SendCenter($"{adminName}: {message}");
    }

    [Command("rcon", permission: "admins.commands.rcon")]
    public void Command_Rcon(ICommandContext context)
    {
        if (!ValidateArgsCount(context, 1, "rcon", ["<command>"]))
        {
            return;
        }

        var rconCommand = string.Join(" ", context.Args);
        Core.Engine.ExecuteCommand(rconCommand);
    }

    [Command("map", permission: "admins.commands.map")]
    [CommandAlias("changelevel")]
    [CommandAlias("changemap")]
    public void Command_Map(ICommandContext context)
    {
        if (!ValidateArgsCount(context, 1, "map", ["<map_name>"]))
        {
            return;
        }

        var mapName = context.Args[0];
        if (!int.TryParse(mapName, out var _))
        {
            Core.Engine.ExecuteCommand($"changelevel {mapName}");
        }
        else
        {
            Core.Engine.ExecuteCommand($"host_workshop_map {mapName}");
        }
    }

    #endregion

    #region Helper Methods

    private void ApplyHealthAndArmor(IPlayer player, int health, int armour, bool helmet)
    {
        var pawn = player.PlayerPawn;
        if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
        {
            return;
        }

        if (health <= 0)
        {
            pawn.CommitSuicide(false, false);
        }
        else
        {
            pawn.Health = health;
            pawn.HealthUpdated();
        }

        var itemServices = pawn.ItemServices;
        var weaponServices = pawn.WeaponServices;
        if (itemServices != null && itemServices.IsValid && weaponServices != null && weaponServices.IsValid)
        {
            if (helmet)
            {
                itemServices.GiveItem("item_assaultsuit");
            }
            else
            {
                var weapons = weaponServices.MyValidWeapons;
                foreach (var weapon in weapons)
                {
                    if (weapon.AttributeManager.Item.ItemDefinitionIndex == 51)
                    {
                        weaponServices.RemoveWeapon(weapon);
                        break;
                    }
                }
            }
        }

        pawn.ArmorValue = armour;
        pawn.ArmorValueUpdated();
    }

    private void NotifyHealthChanged(List<IPlayer> players, IPlayer sender, int health, int armour, bool helmet)
    {
        var adminName = sender.Controller.PlayerName;

        SendMessageToPlayers(players, sender, (p, localizer) =>
        {
            var playerName = GetPlayerName(p);
            return (localizer[
                "command.hp_success",
                Admins.Config.CurrentValue.Prefix,
                adminName,
                playerName,
                health,
                armour,
                helmet
            ], MessageType.Chat);
        });
    }

    private void GiveItemToPlayer(IPlayer player, string itemName)
    {
        var pawn = player.PlayerPawn;
        if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
        {
            return;
        }

        var itemServices = pawn.ItemServices;
        if (itemServices != null && itemServices.IsValid)
        {
            itemServices.GiveItem(itemName);
        }
    }

    private void NotifyItemGiven(List<IPlayer> players, IPlayer sender, string itemName)
    {
        var adminName = sender.Controller.PlayerName;

        SendMessageToPlayers(players, sender, (p, localizer) =>
        {
            var playerName = GetPlayerName(p);
            return (localizer[
                "command.giveitem_success",
                Admins.Config.CurrentValue.Prefix,
                adminName,
                itemName,
                playerName
            ], MessageType.Chat);
        });
    }

    private void ModifyPlayerMoney(IPlayer player, int amount, bool isAdditive)
    {
        var moneyServices = player.Controller.InGameMoneyServices;
        if (moneyServices != null && moneyServices.IsValid)
        {
            if (isAdditive)
            {
                moneyServices.Account += amount;
            }
            else
            {
                moneyServices.Account = amount;
            }
            moneyServices.AccountUpdated();
        }
    }

    private void NotifyMoneyChanged(List<IPlayer> players, IPlayer sender, int amount, string messageKey)
    {
        var adminName = sender.Controller.PlayerName;

        SendMessageToPlayers(players, sender, (p, localizer) =>
        {
            var playerName = GetPlayerName(p);
            return (localizer[
                messageKey,
                Admins.Config.CurrentValue.Prefix,
                adminName,
                amount,
                playerName
            ], MessageType.Chat);
        });
    }

    private void StripAndGiveKnife(IPlayer player)
    {
        var pawn = player.PlayerPawn;
        if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
        {
            return;
        }

        var itemServices = pawn.ItemServices;
        if (itemServices != null && itemServices.IsValid)
        {
            itemServices.RemoveItems();
            itemServices.GiveItem("weapon_knife");
        }
    }

    private void RemoveAllItems(IPlayer player)
    {
        var pawn = player.PlayerPawn;
        if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
        {
            return;
        }

        var itemServices = pawn.ItemServices;
        if (itemServices != null && itemServices.IsValid)
        {
            itemServices.RemoveItems();
        }
    }

    private void SetPlayerMoveType(IPlayer player, MoveType_t moveType)
    {
        var pawn = player.PlayerPawn;
        if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
        {
            return;
        }

        pawn.ActualMoveType = moveType;
        pawn.MoveType = moveType;
        pawn.MoveTypeUpdated();
    }

    private bool IsValidAlivePawn(CCSPlayerPawn? pawn)
    {
        return pawn != null && pawn!.IsValid && pawn!.LifeState == (byte)LifeState_t.LIFE_ALIVE;
    }

    private void SlayPlayer(IPlayer player)
    {
        var pawn = player.PlayerPawn;
        if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
        {
            return;
        }

        pawn.CommitSuicide(false, false);
    }

    private void ApplySlap(IPlayer player, int damage)
    {
        var pawn = player.PlayerPawn;
        if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
        {
            return;
        }

        pawn.Health = Math.Max(pawn.Health - damage, 0);
        pawn.HealthUpdated();

        if (pawn.Health == 0)
        {
            pawn.CommitSuicide(false, false);
        }
        else
        {
            pawn.Velocity.X.Value += (float)Random.Shared.NextInt64(50, 230) * (Random.Shared.NextDouble() < 0.5 ? -1 : 1);
            pawn.Velocity.Y.Value += (float)Random.Shared.NextInt64(50, 230) * (Random.Shared.NextDouble() < 0.5 ? -1 : 1);
            pawn.Velocity.Z.Value += Random.Shared.NextInt64(100, 300);
            pawn.VelocityUpdated();
        }
    }

    private void NotifyPlayersAction(List<IPlayer> players, IPlayer sender, string messageKey)
    {
        var adminName = sender.Controller.PlayerName;

        SendMessageToPlayers(players, sender, (p, localizer) =>
        {
            var playerName = GetPlayerName(p);
            return (localizer[
                messageKey,
                Admins.Config.CurrentValue.Prefix,
                adminName,
                playerName
            ], MessageType.Chat);
        });
    }

    private void NotifyRename(List<IPlayer> players, IPlayer sender, Dictionary<IPlayer, string> oldNames, string newName)
    {
        var adminName = sender.Controller.PlayerName;

        SendMessageToPlayers(players, sender, (p, localizer) =>
        {
            var oldName = oldNames.ContainsKey(p) ? oldNames[p] : "Unknown";
            return (localizer[
                "command.rename_success",
                Admins.Config.CurrentValue.Prefix,
                adminName,
                oldName,
                newName
            ], MessageType.Chat);
        });
    }

    #endregion
}