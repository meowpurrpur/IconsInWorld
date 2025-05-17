using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Dalamud.Interface.Textures.TextureWraps;
using QuestsInWorld;
using ImGuiNET;

namespace QuestsInWorld
{
    internal class DrawHelper
    {
        public static void DrawTextOutlined(string Text, Vector2 TextPosition, float FontSize = 18f, uint OutlineColor = 4278190080)
        {
            var DrawList = ImGui.GetBackgroundDrawList();

            ImFontPtr Font = ImGui.GetFont();
            float DefaultFontSize = Font.FontSize;

            Vector2 BaseSize = ImGui.CalcTextSize(Text);
            Vector2 ScaledSize = BaseSize * (FontSize / DefaultFontSize);

            DrawList.AddText(Font, FontSize, TextPosition, ImGui.GetColorU32(ImGuiCol.Text), Text);
            Vector2[] Offsets = new Vector2[]
            {
                Vector2.Create(-1, -1),
                Vector2.Create(1, -1),
                Vector2.Create(-1, 1),
                Vector2.Create(1, 1)
            };

            foreach (var Offset in Offsets)
                DrawList.AddText(Font, FontSize, TextPosition + Offset, OutlineColor, Text);

            DrawList.AddText(Font, FontSize, TextPosition, ImGui.GetColorU32(ImGuiCol.Text), Text);
        }

        public static Vector2 DrawImage(string ImageName, Vector2 ImagePosition, Vector2 ImageSize)
        {
            var DrawList = ImGui.GetBackgroundDrawList();
            IDalamudTextureWrap Icon = Plugin.TextureProvider.GetFromFile(Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, ImageName)).GetWrapOrDefault();

            var ImageTopLeft = ImagePosition - ImageSize * 0.5f;
            DrawList.AddImage(Icon.ImGuiHandle, ImageTopLeft, ImageTopLeft + ImageSize);

            return ImageTopLeft;
        }


        public const string TooltipSeparator = "--SEP--";
        public static void AttachToolTip(string text) // https://github.com/Penumbra-Sync/client/blob/main/MareSynchronos/UI/UISharedService.cs#L139
        {
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
                if (text.Contains(TooltipSeparator, StringComparison.Ordinal))
                {
                    var splitText = text.Split(TooltipSeparator, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < splitText.Length; i++)
                    {
                        ImGui.TextUnformatted(splitText[i]);
                        if (i != splitText.Length - 1) ImGui.Separator();
                    }
                }
                else
                {
                    ImGui.TextUnformatted(text);
                }
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }
    }
}
