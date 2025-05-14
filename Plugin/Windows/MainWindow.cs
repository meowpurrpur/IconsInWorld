using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Collections.Generic;

namespace QuestsInWorld.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private Plugin Plugin;
        private Configuration Configuration;

        private readonly List<CheckboxSetting> CheckboxSettings;

        public MainWindow(Plugin plugin)
            : base("Icons In World##IconsInWorld", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(650, 350),
                MaximumSize = new Vector2(650, 350)
            };

            Plugin = plugin;
            Configuration = plugin.Configuration;

            CheckboxSettings = new List<CheckboxSetting>
            {
                new CheckboxSetting("Active Quest Icons Enabled", () => Configuration.MSQIconEnabled, Value => Configuration.MSQIconEnabled = Value),
                new CheckboxSetting("Gatherer Icons Enabled", () => Configuration.GathererIconsEnabled, Value => Configuration.GathererIconsEnabled = Value),
                new CheckboxSetting("Treasure Coffer Icons Enabled", () => Configuration.TreasureCofferIconsEnabled, Value => Configuration.TreasureCofferIconsEnabled = Value),
                new CheckboxSetting("Party Member Icons Enabled", () => Configuration.PartyMemberIconsEnabled, Value => Configuration.PartyMemberIconsEnabled = Value),
                new CheckboxSetting("Summoning Bell Icons Enabled", () => Configuration.SummoningBellIconsEnabled, Value => Configuration.SummoningBellIconsEnabled = Value),
                new CheckboxSetting("Market Board Icons Enabled", () => Configuration.MarketboardIconsEnabled, Value => Configuration.MarketboardIconsEnabled = Value),
                new CheckboxSetting("Aetheryte Icons Enabled", () => Configuration.AetheryteIconsEnabled, Value => Configuration.AetheryteIconsEnabled = Value),
                new CheckboxSetting("Aether Current Icons Enabled", () => Configuration.AetherCurrentIconsEnabled, Value => Configuration.AetherCurrentIconsEnabled = Value)
            };
        }

        public void Dispose() { }

        public override void Draw()
        {
            foreach (var Setting in CheckboxSettings)
            {
                bool Value = Setting.Get();
                if (ImGui.Checkbox(Setting.Label, ref Value))
                {
                    Setting.Set(Value);
                    Configuration.Save();
                }
            }
        }
    }

    public class CheckboxSetting
    {
        public string Label { get; }
        private Func<bool> Getter { get; }
        private Action<bool> Setter { get; }

        public CheckboxSetting(string label, Func<bool> getter, Action<bool> setter)
        {
            Label = label;
            Getter = getter;
            Setter = setter;
        }

        public bool Get() => Getter();
        public void Set(bool value) => Setter(value);
    }
}
