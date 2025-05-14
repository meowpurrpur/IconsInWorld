using System;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Lumina.Excel;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel.Design;
using Serilog;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace QuestsInWorld.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;

    public MainWindow(Plugin plugin)
        : base("Icons In World##IconsInWorld", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(650, 350),
            MaximumSize = new Vector2(650, 350),
        };

        Plugin = plugin;
        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var DrawList = ImGui.GetBackgroundDrawList();

        var MSQIconEnabled = Configuration.MSQIconEnabled;
        if (ImGui.Checkbox("Active Quest Icons Enabled", ref MSQIconEnabled))
        {
            Configuration.MSQIconEnabled = MSQIconEnabled;
            Configuration.Save();
        }

        var GathererIconsEnabled = Configuration.GathererIconsEnabled;
        if (ImGui.Checkbox("Gatherer Icons Enabled", ref GathererIconsEnabled))
        {
            Configuration.GathererIconsEnabled = GathererIconsEnabled;
            Configuration.Save();
        }

        var TreasureCofferIconsEnabled = Configuration.TreasureCofferIconsEnabled;
        if (ImGui.Checkbox("Treasure Coffer Icons Enabled", ref TreasureCofferIconsEnabled))
        {
            Configuration.TreasureCofferIconsEnabled = TreasureCofferIconsEnabled;
            Configuration.Save();
        }

        var PartyMemberIconsEnabled = Configuration.PartyMemberIconsEnabled;
        if (ImGui.Checkbox("Party Member Icons Enabled", ref PartyMemberIconsEnabled))
        {
            Configuration.PartyMemberIconsEnabled = PartyMemberIconsEnabled;
            Configuration.Save();
        }

        var SummoningBellIconsEnabled = Configuration.SummoningBellIconsEnabled;
        if (ImGui.Checkbox("Summoning Bell Icons Enabled", ref SummoningBellIconsEnabled))
        {
            Configuration.SummoningBellIconsEnabled = SummoningBellIconsEnabled;
            Configuration.Save();
        }

        var MarketboardIconsEnabled = Configuration.MarketboardIconsEnabled;
        if (ImGui.Checkbox("Market Board Icons Enabled", ref MarketboardIconsEnabled))
        {
            Configuration.MarketboardIconsEnabled = MarketboardIconsEnabled;
            Configuration.Save();
        }

        var AetheryteIconsEnabled = Configuration.AetheryteIconsEnabled;
        if (ImGui.Checkbox("Aetheryte Icons Enabled", ref AetheryteIconsEnabled))
        {
            Configuration.AetheryteIconsEnabled = AetheryteIconsEnabled;
            Configuration.Save();
        }

        var AetherCurrentIconsEnabled = Configuration.AetherCurrentIconsEnabled;
        if (ImGui.Checkbox("Aether Current Icons Enabled", ref AetherCurrentIconsEnabled))
        {
            Configuration.AetherCurrentIconsEnabled = AetherCurrentIconsEnabled;
            Configuration.Save();
        }
    }
}

