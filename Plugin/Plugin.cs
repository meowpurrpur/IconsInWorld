using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using QuestsInWorld.Windows;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Text.ReadOnly;
using Lumina.Excel.Sheets;
using System.Numerics;
using System;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System.Linq;

namespace QuestsInWorld;

[Sheet("QuestDialogue")]
public readonly struct QuestDialogue(RawRow Row) : IExcelRow<QuestDialogue>
{
    public uint RowId => Row.RowId;
    public ReadOnlySeString Key => Row.ReadStringColumn(0);
    public ReadOnlySeString Value => Row.ReadStringColumn(1);

    static QuestDialogue IExcelRow<QuestDialogue>.Create(ExcelPage Page, uint Offset, uint Row) =>
        new(new RawRow(Page, Offset, Row));
}

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IPartyList PartyList { get; private set; } = null!;
    [PluginService] internal static IAetheryteList AetheryteList { get; private set; } = null!;

    public Configuration Configuration { get; init; }
    public readonly WindowSystem WindowSystem = new("Icons In World");
    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        AllQuestManager.Initialize();

        MainWindow = new MainWindow(this);
        WindowSystem.AddWindow(MainWindow);

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleMainUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        MainWindow.Dispose();
    }

    private unsafe void DrawUI()
    {
        var DrawList = ImGui.GetBackgroundDrawList();
        var CurrentMapID = DataManager.GetExcelSheet<Map>()?.GetRow(ClientState.MapId).Id.ToString();
        var CurrentTerritoryMapID = DataManager.GetExcelSheet<TerritoryType>()?.GetRow(ClientState.TerritoryType).Map.Value.Id.ToString();

        try
        {
            AllQuestManager.LoadQuests();

            foreach (var QuestEntry in AllQuestManager.GameQuests)
            {
                var RawLocation = QuestEntry.GetLocationRaw(QuestEntry.Step);
                if (RawLocation.Map.Value.Id.ToString() != CurrentMapID) continue;

                var OnScreen = GameGui.WorldToScreen(QuestEntry.GetObjectivePosition(QuestEntry.Step), out var ScreenLocation);
                if (!OnScreen || !Configuration.MSQIconEnabled) continue;

                var ImageTopLeft = DrawHelper.DrawImage("MSQ.png", ScreenLocation, new Vector2(84, 84));

                var Font = ImGui.GetFont();
                var FontSize = 18f;
                var TextSize = ImGui.CalcTextSize(QuestEntry.Name) * (FontSize / Font.FontSize);

                var TextPosition = Vector2.Create(
                    ScreenLocation.X - TextSize.X * 0.5f,
                    ImageTopLeft.Y - TextSize.Y - 4f
                );

                DrawHelper.DrawTextOutlined(QuestEntry.Name, TextPosition, FontSize);

            }
        }
        catch (Exception)
        {
            Log.Warning("Failed to draw quest icons");
        }

        try
        {
            if (Configuration.GathererIconsEnabled || Configuration.TreasureCofferIconsEnabled || Configuration.SummoningBellIconsEnabled || Configuration.MarketboardIconsEnabled || Configuration.AetheryteIconsEnabled)
            {
                var Job = ClientState.LocalPlayer.ClassJob.Value.Abbreviation.ExtractText();

                for (int Index = 0; Index < ObjectTable.Length; Index++)
                {
                    var GameObject = ObjectTable[Index];
                    if (GameObject == null || !GameObject.IsTargetable) continue;

                    var Name = GameObject.Name.ToString();
                    var Type = GameObject.ObjectKind;
                    if (!GameGui.WorldToScreen(GameObject.Position, out var ScreenLocation)) continue;

                    if (Configuration.TreasureCofferIconsEnabled && Name == "Treasure Coffer")
                        DrawHelper.DrawImage("Coffer.png", ScreenLocation, new Vector2(64, 64));

                    if (Configuration.SummoningBellIconsEnabled && Name == "Summoning Bell")
                        DrawHelper.DrawImage("SummoningBell.png", ScreenLocation, new Vector2(64, 64));

                    if (Configuration.MarketboardIconsEnabled && Name == "Market Board")
                        DrawHelper.DrawImage("MarketBoard.png", ScreenLocation, new Vector2(64, 64));

                    if (Configuration.AetheryteIconsEnabled && Name.Contains("Aethernet"))
                        DrawHelper.DrawImage("Aethernet.png", ScreenLocation, new Vector2(48, 48));

                    if(Configuration.AetheryteIconsEnabled && Type.ToString() == ObjectKind.Aetheryte.ToString() && !Name.Contains("Aethernet"))
                        DrawHelper.DrawImage("Aetheryte.png", ScreenLocation, new Vector2(48, 48));

                    if (Configuration.AetherCurrentIconsEnabled && Name == "Aether Current")
                        DrawHelper.DrawImage("AetherCurrent.png", ScreenLocation, new Vector2(32, 32));

                    if (!Configuration.GathererIconsEnabled) continue;

                    string Icon = Name switch
                    {
                        "Rocky Outcrop" when Job == "MIN" => "RockyOutcrop.png",
                        "Mineral Deposit" when Job == "MIN" => "MineralDeposit.png",
                        "Mature Tree" when Job == "BTN" => "MatureTree.png",
                        "Lush Vegetation Patch" when Job == "BTN" => "LushPatch.png",
                        _ => ""
                    };

                    if (!string.IsNullOrEmpty(Icon))
                        DrawHelper.DrawImage(Icon, ScreenLocation, new Vector2(64, 64));
                }
            }
        }
        catch (Exception)
        {
            Log.Warning("Failed to draw object icons");
        }

        try
        {
            if(Configuration.PartyMemberIconsEnabled)
            {
                for (int Index = 0; Index < PartyList.Length; Index++)
                {
                    var PartyMember = PartyList[Index];
                    if (PartyMember == null || PartyMember.ObjectId == ClientState.LocalPlayer.GameObjectId) continue;
                    if (PartyMember.Territory.Value.Map.Value.Id.ToString() != CurrentTerritoryMapID) continue;

                    GameObject* Character = (GameObject*)PartyMember.GameObject.Address;
                    FFXIVClientStructs.FFXIV.Common.Math.Vector3 Center = FFXIVClientStructs.FFXIV.Common.Math.Vector3.Zero;
                    Character->GetCenterPosition(&Center);

                    Vector3 RealPos = new Vector3(Center.X, Center.Y, Center.Z);
                    Vector3 Position = new Vector3(RealPos.X, RealPos.Y + (Character->Height * 1.3f), RealPos.Z);
                    if (!GameGui.WorldToScreen(Position, out var ScreenLocation)) continue;

                    var ImageTopLeft = DrawHelper.DrawImage("Dot2.png", ScreenLocation, new Vector2(32, 32));

                    var Font = ImGui.GetFont();
                    var FontSize = 20f;
                    var TextSize = ImGui.CalcTextSize(PartyMember.Name.ToString()) * (FontSize / Font.FontSize);

                    var TextPosition = Vector2.Create(
                        ScreenLocation.X - TextSize.X * 0.5f,
                        ImageTopLeft.Y - TextSize.Y - 4f
                    );

                    DrawHelper.DrawTextOutlined(PartyMember.Name.ToString(), TextPosition, FontSize);
                }
            }
        }
        catch (Exception)
        {
            Log.Warning("Failed to draw party icons");
        }

        WindowSystem.Draw();
    }

    public void ToggleMainUI() => MainWindow.Toggle();
}
