using Admins.Core.Contract;
using Admins.Menu.Contract;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace Admins.Comms.Menus;

public class AdminMenu
{
    private ISwiftlyCore Core = null!;
    private IAdminMenuAPI? _adminMenuAPI;

    public AdminMenu(ISwiftlyCore core)
    {
        Core = core;

        core.Registrator.Register(this);
    }

    public void SetAdminMenuAPI(IAdminMenuAPI adminMenuAPI)
    {
        _adminMenuAPI = adminMenuAPI;
    }

    public string TranslateString(IPlayer player, string key)
    {
        var localizer = Core.Translation.GetPlayerLocalizer(player);
        return localizer[key];
    }

    public IMenuAPI BuildSanctionKindMenu(IPlayer player, bool online)
    {
        var menuBuilder = Core.MenusAPI.CreateBuilder();

        menuBuilder
            .Design.SetMenuTitle(TranslateString(player, "menu.comms.sanction.give.kind"))
            .Design.SetMenuFooterColor(_adminMenuAPI!.GetMenuColor())
            .Design.SetVisualGuideLineColor(_adminMenuAPI.GetMenuColor())
            .Design.SetNavigationMarkerColor(_adminMenuAPI.GetMenuColor());

        return menuBuilder.Build();
    }

    public IMenuAPI BuildSanctionGiveMenu(IPlayer player)
    {
        var menuBuilder = Core.MenusAPI.CreateBuilder();

        menuBuilder
            .Design.SetMenuTitle(TranslateString(player, "menu.comms.sanctions.give"))
            .Design.SetMenuFooterColor(_adminMenuAPI!.GetMenuColor())
            .Design.SetVisualGuideLineColor(_adminMenuAPI.GetMenuColor())
            .Design.SetNavigationMarkerColor(_adminMenuAPI.GetMenuColor())
            .AddOption(new SubmenuMenuOption(TranslateString(player, "menu.comms.sanctions.give.online"), () => BuildSanctionKindMenu(player, true)))
            .AddOption(new SubmenuMenuOption(TranslateString(player, "menu.comms.sanctions.give.offline"), () => BuildSanctionKindMenu(player, false)));

        return menuBuilder.Build();
    }

    public void LoadAdminMenu()
    {
        if (_adminMenuAPI == null) return;

        _adminMenuAPI.RegisterSubmenu("menu.comms.title", ["admins.menu.comms"], TranslateString, (player) =>
        {
            var menuBuilder = Core.MenusAPI.CreateBuilder();

            menuBuilder
                .Design.SetMenuTitle(TranslateString(player, "menu.comms.title"))
                .Design.SetMenuFooterColor(_adminMenuAPI.GetMenuColor())
                .Design.SetVisualGuideLineColor(_adminMenuAPI.GetMenuColor())
                .Design.SetNavigationMarkerColor(_adminMenuAPI.GetMenuColor())
                .AddOption(new SubmenuMenuOption(
                    TranslateString(player, "menu.comms.sanctions.give"),
                    () => BuildSanctionGiveMenu(player)
                ));

            return menuBuilder.Build();
        });
    }

    public void UnloadAdminMenu()
    {

    }
}