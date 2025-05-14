using System.Collections.Generic;
using System;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using Lumina.Excel;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using System.Threading.Tasks;
using Serilog;
using Lumina.Extensions;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace QuestsInWorld
{
    internal static class SheetManager
    {
        public static ExcelSheet<Quest> QuestSheet { get; } = Plugin.DataManager.GetExcelSheet<Quest>();
    }

    internal class AllQuestManager
    {
        public static List<GameQuest> GameQuests { get; private set; } = new List<GameQuest>();
        public static List<GameQuest> SharedCache { get; private set; } = new List<GameQuest>();
        private static byte LastStep = 0;
        private static uint LastID = 0;
        public static void Initialize()
        {
            Plugin.ClientState.Login += OnLogin;
            Plugin.ClientState.Logout += OnLogout;
            if (GameQuests.Count == 0)
            {
                System.Threading.Tasks.Task.Run(OnLogin);
            }
            Plugin.Framework.Update += OnFrameworkUpdate;
        }
        public static void Dispose()
        {
            Plugin.ClientState.Login -= OnLogin;
            Plugin.ClientState.Logout -= OnLogout;
            Plugin.Framework.Update -= OnFrameworkUpdate;
        }

        public static void LoadQuests()
        {
            var ActiveQuest = GetActiveQuest();
            GameQuests.Clear();

            foreach (var QuestEntry in SheetManager.QuestSheet)
            {
                if (QuestEntry.Id == "" || QuestEntry.RowId == 0)
                {
                    continue;
                }

                if (QuestManager.GetQuestSequence(QuestEntry.RowId) > 0)
                {
                    GameQuests.Add(new GameQuest(QuestEntry.RowId));
                }
            }

            if (ActiveQuest != null && !GameQuests.Any(QuestEntry => QuestEntry.ID == ActiveQuest.ID))
            {
                Log.Debug($"Active quest {ActiveQuest.ID} was not found in GameQuests, assuming completed.");
                return;
            }
            else if (ActiveQuest != null)
            {
                SetActiveFlag(ActiveQuest.ID);
            }
        }

        public static GameQuest? GetActiveQuest()
        {
            return GameQuests.FirstOrDefault(Data => Data.Active);
        }

        public static GameQuest GetQuestById(uint ID)
        {
            if (SharedCache.FirstOrDefault(QuestEntry => QuestEntry.ID == ID) is GameQuest QuestData)
            {
                return QuestData;
            }
            else
            {
                var NewQuest = new GameQuest(ID);
                SharedCache.Add(NewQuest);
                return NewQuest;
            }
        }

        public static void SetActiveFlag(uint ID)
        {
            Log.Debug($"Setting active quest to {ID}");
            var QuestData = GameQuests.FirstOrDefault(QuestEntry => QuestEntry.ID == ID);
            if (QuestData != null)
            {
                QuestData.Active = true;
                foreach (var QuestEntry in GameQuests)
                {
                    if (QuestEntry.ID != ID)
                    {
                        QuestEntry.Active = false;
                    }
                }
            }
        }

        private static void OnLogin()
        {
            LoadQuests();
        }

        private static void OnLogout(int code, int _)
        {
            GameQuests.Clear();
            SharedCache.Clear();
        }

        private static void OnFrameworkUpdate(IFramework Framework)
        {
            if (Plugin.ClientState.LocalContentId == 0) return;
            var ActiveQuest = GetActiveQuest();
            if (ActiveQuest != null && ActiveQuest.ID == LastID)
            {
                var step = QuestManager.GetQuestSequence(ActiveQuest.ID);
                if (step != LastStep)
                {
                    if (LastStep == 0xFF && step == 0)
                    {
                        Log.Debug($"Quest {ActiveQuest.ID} was completed");

                        if (ActiveQuest.Data.PreviousQuest.TryGetFirst(out var prevQuest))
                        {
                            if (prevQuest.RowId == 0 || !prevQuest.IsValid)
                            {
                                Log.Debug("No previous quest in chain");
                                GameQuests.Remove(ActiveQuest);
                                LoadQuests();
                                return;
                            }
                            Log.Debug($"Previous quest in chain was {prevQuest.RowId}");
                            var NextQuest = SheetManager.QuestSheet.FirstOrDefault(qu => prevQuest.Value.RowId == ActiveQuest.ID);
                            if (NextQuest.RowId != 0)
                            {
                                Log.Debug($"Next quest in chain is {NextQuest.RowId}");
                                GameQuests.Add(new GameQuest(NextQuest.RowId));
                                SetActiveFlag(NextQuest.RowId);
                            }
                            else
                            {
                                Log.Debug("No next quest in chain");
                                GameQuests.Remove(ActiveQuest);
                                LoadQuests();
                                return;
                            }
                        }
                    }
                    else
                    {
                        LastStep = step;
                        Log.Debug($"Quest step changed to {step}");
                    }
                }
            }
            else if (ActiveQuest != null)
            {
                LastID = ActiveQuest.ID;
                LastStep = QuestManager.GetQuestSequence(ActiveQuest.ID);
            }
        }

        public static uint GetMainQuestID()
        {
            unsafe
            {
                var ID = (uint)AgentScenarioTree.Instance()->Data->CurrentScenarioQuest;
                if (ID == 0) return 0;
                else ID += 0x10000U;

                return ID;
            }
        }

        public static ExcelSheet<QuestDialogue> GetTextSheet(Quest QuestObject)
        {
            var ID = QuestObject.Id.ToString();
            var Dir = ID.Substring(ID.Length - 5, 3);
            return Plugin.DataManager.GetExcelSheet<QuestDialogue>(name: $"quest/{Dir}/{ID}");
        }
    }

    internal class GameQuest
    {
        public Quest Data { get; init; }
        public uint ID;
        public string Name => Data.Name.ExtractText();
        public byte Step;
        public List<string> Steps { get; private set; } = [];
        public bool Active { get; set; } = false;

        public GameQuest(uint QuestID) {
            ID = QuestID;
            if (SheetManager.QuestSheet.TryGetRow(ID, out var QuestData)) { Data = QuestData; }
            Step = QuestManager.GetQuestSequence(ID);

            var SheetData = AllQuestManager.GetTextSheet(Data);
            for (uint i = 0; i < SheetData.Count; i++)
            {
                var Row = SheetData.GetRow(i);
                if (Row.Key.ExtractText().Contains("_TODO_") && Row.Value.ExtractText() != "")
                {
                    Steps.Add(Row.Value.ExtractText());
                }
            }
        }

        public Level GetLocationRaw(int TargetStep) {
            return Data.TodoParams.FirstOrDefault(Param => Param.ToDoCompleteSeq == TargetStep)
                            .ToDoLocation.FirstOrDefault(Location => Location is not { RowId: 0 }).Value;
        }

        public Vector3 GetObjectivePosition(int TargetStep)
        {
            Level RawLocation = GetLocationRaw(TargetStep);
            return Vector3.Create(RawLocation.X, RawLocation.Y, RawLocation.Z);
        }
    }
}
