using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace QuestsInWorld;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool MSQIconEnabled { get; set; } = true;
    public bool GathererIconsEnabled { get; set; } = true;
    public bool TreasureCofferIconsEnabled { get; set; } = true;
    public bool PartyMemberIconsEnabled { get; set; } = true;
    public bool SummoningBellIconsEnabled { get; set; } = true;
    public bool MarketboardIconsEnabled { get; set; } = true;
    public bool AetheryteIconsEnabled { get; set; } = true;
    public bool AetherCurrentIconsEnabled { get; set; } = true;
    public bool EventObjectIconsEnabled { get; set; } = true;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
