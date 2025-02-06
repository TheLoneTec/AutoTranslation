using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoTranslation.Translators;
using UnityEngine;
using Verse;
using Verse.Noise;
using Verse.Sound;
using static HarmonyLib.Code;

namespace AutoTranslation
{
    public class Settings : ModSettings
    {
        public static bool AppendTranslationCompleteTag = false;
        public static string APIKey = string.Empty;
        public static string TranslatorName = "Google";
        public static bool ShowOriginal = false;
        public static HashSet<string> WhiteListModPackageIds = new HashSet<string>();

        public static string SelectedModel = string.Empty;
        public static string CustomPrompt = string.Empty;

        private static List<ModContentPack> AllMods => _allModsCached ?? (_allModsCached = LoadedModManager.RunningMods.ToList());
        private static List<ModContentPack> _allModsCached;
        private static Vector2 scrollbarVector = Vector2.zero;
        private static string TestText = "Hello, World!";
        private static string TestResultText = string.Empty;
        private static string SearchText = string.Empty;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref AppendTranslationCompleteTag, "AutoTranslation_AppendTranslationCompleteTag", false);
            Scribe_Values.Look(ref APIKey, "AutoTranslation_APIKey", string.Empty);
            Scribe_Values.Look(ref TranslatorName, "AutoTranslation_TranslatorName", "Google");
            Scribe_Values.Look(ref ShowOriginal, "AutoTranslation_ShowOriginal", false);
            Scribe_Values.Look(ref SelectedModel, "AutoTranslation_SelectedModel", string.Empty);
            Scribe_Values.Look(ref CustomPrompt, "AutoTranslation_CustomPrompt", string.Empty);
            Scribe_Collections.Look(ref WhiteListModPackageIds, "AutoTranslation_WhiteListModPackageIds", LookMode.Value);
            if (WhiteListModPackageIds == null) WhiteListModPackageIds = new HashSet<string>();
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            var ls = new Listing_Standard();
            ls.Begin(inRect);

            ls.CheckboxLabeled("AT_Setting_ShowOriginal".Translate(), ref ShowOriginal, "AT_Setting_ShowOriginal_Tooltip".Translate());

            var h = ls.CurHeight;
            ls.End();

            inRect.y += h;
            inRect.height -= h;
            inRect.width /= 2;
            h = DoSettingsWindowContentsLeft(inRect);

            inRect.x += inRect.width;
            h = Mathf.Max(h, DoSettingsWindowContentsRight(inRect));
            inRect.y += h;
            inRect.height -= h;
            inRect.x -= inRect.width;
            inRect.width *= 2;

            const float entryHeight = 22f;
            var cntEntry = AllMods.Count;

            var outRect = new Rect(0f, inRect.y + 20f, inRect.width - 10f, inRect.height - 20f);
            var listRect = new Rect(0f, 0f, outRect.width - 50f, entryHeight * cntEntry);
            var labelRect = new Rect(entryHeight + 10f, 0f, listRect.width - 40f, entryHeight);

            var descRect = new Rect(labelRect.x, outRect.y - 22f, labelRect.width, 22f);
            var toggleRect =
                new Rect(labelRect.x + labelRect.width - "AT_Setting_ToggleAll".Translate().GetWidthCached(),
                    outRect.y - 22f, "AT_Setting_ToggleAll".Translate().GetWidthCached(), 22f);
            var searchRect = new Rect(
                labelRect.x + labelRect.width - "AT_Setting_ToggleAll".Translate().GetWidthCached() - 150f,
                outRect.y - 22f, 150f, 22f);
            Widgets.Label(descRect, "AT_Setting_WhiteList".Translate());
            if (Widgets.ButtonText(toggleRect, "AT_Setting_ToggleAll".Translate()))
            {
                if (WhiteListModPackageIds.Count == AllMods.Count)
                {
                    WhiteListModPackageIds.Clear();
                    SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
                }
                else if (WhiteListModPackageIds.Count == 0)
                {
                    foreach (var mod in AllMods)
                    {
                        WhiteListModPackageIds.Add(mod.PackageId);
                    }
                    SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
                }
                else
                {
                    int threshold = AllMods.Count / 2;
                    if (WhiteListModPackageIds.Count < threshold)
                    {
                        foreach (var mod in AllMods)
                        {
                            WhiteListModPackageIds.Add(mod.PackageId);
                        }
                        SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
                    }
                    else
                    {
                        WhiteListModPackageIds.Clear();
                        SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
                    }
                }
                

            }
            SearchText = Widgets.TextField(searchRect, SearchText);

            Widgets.BeginScrollView(outRect, ref scrollbarVector, listRect, true);

            var filteredMods = AllMods.Where(m =>
                    string.IsNullOrEmpty(SearchText) || m.Name.ToLower().Contains(SearchText.ToLower()) ||
                    m.PackageId.ToLower().Contains(SearchText.ToLower()))
                .ToList();

            for (int i = 0; i < filteredMods.Count; i++)
            {
                var curMod = filteredMods[i];
                var entryRect = new Rect(0f, i * entryHeight, inRect.width - 60f, entryHeight);
                if (i % 2 == 0)
                {
                    Widgets.DrawLightHighlight(entryRect);
                }
                GUI.BeginGroup(entryRect);
#if RW14
#else
                Widgets.ButtonImage(new Rect(0f, 0f, entryHeight, entryHeight), curMod.ModMetaData?.Icon ?? BaseContent.BadTex);
#endif
                InjectionManager.MissingCountByPackageId.TryGetValue(curMod.ModMetaData?.PackageId ?? "", out var cnt);
                

                var tmp = !WhiteListModPackageIds.Contains(curMod.PackageId);
                var tmp2 = tmp;
                Widgets.CheckboxLabeled(labelRect, $"{curMod.Name}:::{curMod.PackageId}:::{cnt.Item1}+{cnt.Item2}", ref tmp);
                if (tmp != tmp2)
                {
                    if (!tmp)
                    {
                        WhiteListModPackageIds.Add(curMod.PackageId);
                        if (TranslatorManager._queue.Count == 0)
                        {
                            InjectionManager.UndoInjectMissingDefInjection(curMod);
                            InjectionManager.UndoInjectMissingKeyed(curMod);
                            ResetDefCaches();
                        }
                        else
                        {
                            Messages.Message("AT_Message_WhiteList_Failed".Translate(), MessageTypeDefOf.NegativeEvent);
                        }
                    }
                    else if (WhiteListModPackageIds.Remove(curMod.PackageId))
                    {
                        if (TranslatorManager._queue.Count == 0)
                        {
                            InjectionManager.InjectMissingDefInjection(curMod);
                            ResetDefCaches();
                        }
                        else
                        {
                            Messages.Message("AT_Message_WhiteList_Failed".Translate(), MessageTypeDefOf.NegativeEvent);
                        }
                    }
                }
                
                GUI.EndGroup();
            }

            Widgets.EndScrollView();
        }

        public float DoSettingsWindowContentsLeft(Rect inRect)
        {
            var ls = new Listing_Standard();
            ls.Begin(inRect);

            ls.Label("AT_Setting".Translate());
            ls.Label("AT_Setting_Note".Translate());
            ls.GapLine();
            ls.Label("AT_Setting_SelectEngine".Translate());
            if (Widgets.ButtonText(ls.GetRect(28f), TranslatorName))
            {
                var list = TranslatorManager.translators.Select(t =>
                    new FloatMenuOption(
                        t.Name,
                        () =>
                        {
                            if (t is Translator_BaseTraditional tr && !tr.SupportsCurrentLanguage())
                            {
                                Messages.Message("AT_Message_LanguageNotSupported".Translate(), MessageTypeDefOf.NegativeEvent);
                            }
                            else
                            {
                                TranslatorName = t.Name;
                            }
                        })).ToList();

                Find.WindowStack.Add(new FloatMenu(list));
            }

            var targetTranslator = TranslatorManager.GetTranslator(TranslatorName);

            if (targetTranslator == null)
            {
                ls.Label("AT_Setting_NoTranslatorError".Translate());
                ls.End();
                return ls.CurHeight;
            }

            if (targetTranslator.RequiresKey)
            {
                ls.Label("AT_Setting_RequiresAPIKey".Translate(), tooltip: "AT_Setting_RequiresAPIKey_Tooltip".Translate());
                var textRect = ls.GetRect(Text.LineHeight);
                APIKey = Widgets.TextEntryLabeled(textRect, "API Key:", APIKey);
            }

            if (targetTranslator is Translator_BaseOnlineAIModel aiTranslator)
            {
                if (Widgets.ButtonText(ls.GetRect(28f), "AT_Setting_SelectModel".Translate() + (string.IsNullOrEmpty(SelectedModel) ? (string)"AT_Setting_SelectModelNone".Translate() : SelectedModel)))
                {
                    var list = aiTranslator.Models?.Select(m =>
                        new FloatMenuOption(
                            m,
                            () => { SelectedModel = m; })).ToList();

                    if (list == null)
                    {
                        list = new List<FloatMenuOption>
                        {
                            new FloatMenuOption("AT_Setting_NoModelFound".Translate(), () => { }, playSelectionSound: false),
                        };
                    }

                    Find.WindowStack.Add(new FloatMenu(list));
                }

                CustomPrompt = Widgets.TextEntryLabeled(ls.GetRect(Text.LineHeight * 3),
                    "AT_Setting_CustomPrompt".Translate(), CustomPrompt);
            }

            var entryRect = ls.GetRect(28f);

            var left = entryRect.LeftPart(0.33f);
            var mid = new Rect(entryRect.x + left.width, entryRect.y, left.width, entryRect.height);
            var right = new Rect(entryRect.x + left.width + mid.width, entryRect.y, left.width, entryRect.height);

            TestText = Widgets.TextField(left, TestText);

            if (Widgets.ButtonText(mid, "AT_Setting_TestTranslation".Translate()))
            {
                if (targetTranslator is Translator_BaseOnlineAIModel ait)
                {
                    ait.ResetSettings();
                }
                var s = targetTranslator.TryTranslate(TestText, out TestResultText);
                if (!s)
                {
                    Messages.Message("AT_Message_TestFailed".Translate(), MessageTypeDefOf.NegativeEvent);
                    Log.TryOpenLogWindow();
                }
            }

            Widgets.TextField(right, TestResultText);


            if (Prefs.DevMode)
            {
                ls.CheckboxLabeled("AT_Setting_Test".Translate(), ref AppendTranslationCompleteTag);
            }

            ls.End();

            return ls.CurHeight;
        }

        public float DoSettingsWindowContentsRight(Rect inRect)
        {
            var ls = new Listing_Standard();
            ls.Begin(inRect);

            ls.Label("AT_Setting_Misc".Translate());
            ls.GapLine();

            if (ls.ButtonText("AT_Setting_ResetDefCache".Translate()))
            {
                ResetDefCaches();
                Messages.Message("AT_Message_ResetDefCache".Translate(), MessageTypeDefOf.PositiveEvent);
            }

            ls.GapLine();
            string status;
            if (TranslatorManager._queue.Count > 0)
                status = "AT_Status1".Translate();
            else if (TranslatorManager.workCnt > 20) 
                status = "AT_Status2".Translate();
            else 
                status = "AT_Status3".Translate();
            ls.Label("AT_Setting_CurStatus".Translate() + status);
            ls.Label("AT_Setting_Cached".Translate() + $"{TranslatorManager.CachedTranslations.Count}");
            ls.Label("AT_Setting_NotYet".Translate() + $"{TranslatorManager._queue.Count}");

            if (ls.ButtonText("AT_Setting_ResetTranslationCache".Translate()))
            {
                TranslatorManager.CachedTranslations.Clear();
                TranslatorManager._cacheCount = 0;
                CacheFileTool.Export(nameof(TranslatorManager.CachedTranslations), new Dictionary<string, string>(TranslatorManager.CachedTranslations));

                Messages.Message("AT_Message_ResetTranslationCache".Translate(), MessageTypeDefOf.NeutralEvent);
            }

            if (ls.ButtonText("AT_Setting_RestartWork".Translate()))
            {
                var t = TranslatorManager.GetTranslator(TranslatorName);
                if (t is Translator_BaseOnlineAIModel aiTranslator)
                {
                    aiTranslator.ResetSettings();
                }
                if (t.Ready)
                {
                    TranslatorManager.ClearQueue();
                    TranslatorManager.CurrentTranslator = t;

                    InjectionManager.UndoInjectAll();
                    InjectionManager.ClearDefInjectedTranslations();
                    InjectionManager.ReverseTranslator.Clear();

                    InjectionManager.InjectAll();

                    Messages.Message("AT_Message_RestartWork".Translate(), MessageTypeDefOf.NeutralEvent);
                }
                else
                {
                    Messages.Message("AT_Message_RestartFailed".Translate(), MessageTypeDefOf.NegativeEvent);
                }
            }

            if (ls.ButtonText("AT_Setting_OpenDir".Translate()))
            {
                Application.OpenURL($"file://{CacheFileTool.CacheDirectory}");
            }

            ls.End();

            return ls.CurHeight;
        }

        private static void ResetDefCaches()
        {
            foreach (var defType in InjectionManager.defTypesTranslated)
            {
                GenGeneric.InvokeStaticMethodOnGenericType(typeof(DefDatabase<>), defType, "ClearCachedData");
            }
        }
    }
}
