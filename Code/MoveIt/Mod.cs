// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

// #define EXPORT_EN_US
namespace MoveIt
{
    using Colossal;
    using Colossal.Localization;
    using Game;
    using Game.Net;
    using Game.SceneFlow;
    using Game.Tools;
    using MoveIt.Settings;
    using MoveIt.Systems;
    using MoveIt.Tool;
    using Newtonsoft.Json;
    using QCommonLib;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    
    /// <summary>
    /// Mod entry point.
    /// </summary>
    public class Mod : Game.Modding.IMod
    {
        public const string MOD_NAME = "Move It";
        public const string MOD_UI = "MoveIt";

#if IS_DEBUG
        public const bool IS_BETA = true;
        public static string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString(4);
#else
#if RELEASE
        public const bool IS_BETA = false;
        public static string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString(4);
#else
        public const bool IS_BETA = false;
        public static string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
#endif
#endif

        public static Settings.Settings Settings;

        public void OnLoad(UpdateSystem updateSystem)
        {
            if (IS_BETA) QLog.Init(true);

            Settings = new Settings.Settings(this);
            Settings.RegisterKeyBindings();
            Settings.RegisterInOptionsUI();
            QLog.Info($"{nameof(OnLoad)} Initalizing en-US localization.");
            Game.SceneFlow.GameManager.instance.localizationManager.AddSource("en-US", new Settings.LocaleEN(Settings));
            QLog.Info($"[{nameof(Mod)}] {nameof(OnLoad)} Initalizing localization for other languages.");
            LoadNonEnglishLocalizations();
#if DEBUG && EXPORT_EN_US
            QLog.Info($"{nameof(Mod)}.{nameof(OnLoad)} Exporting localization");
            var localeDict = new LocaleEN(Settings).ReadEntries(new List<IDictionaryEntryError>(), new Dictionary<string, int>()).ToDictionary(pair => pair.Key, pair => pair.Value);
            var str = JsonConvert.SerializeObject(localeDict, Newtonsoft.Json.Formatting.Indented);
            try
            {
                File.WriteAllText($"C:\\Users\\TJ\\source\\repos\\CS2-MoveIt\\UI\\src\\lang\\en-US.json", str);
            }
            catch (Exception ex)
            {
                QLog.Error(ex.ToString());
            }
            try
            {
                File.WriteAllText($"C:\\Users\\TJ\\source\\repos\\CS2-MoveIt\\Code\\MoveIt\\l10n\\en-US.json", str);
            }
            catch (Exception ex)
            {
                QLog.Error(ex.ToString());
            }
#endif


            Colossal.IO.AssetDatabase.AssetDatabase.global.LoadSettings(nameof(MoveIt), Settings, new Settings.Settings(this));

            updateSystem.UpdateAt<Tool.MoveItToolSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<MIT_InputSystem>(SystemUpdatePhase.PreTool);
            updateSystem.UpdateAt<MIT_PostToolSystem>(SystemUpdatePhase.PostTool);
            updateSystem.UpdateAt<Overlays.MIT_OverlaySystem>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAt<MIT_UISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<MIT_ToolTipSystem>(SystemUpdatePhase.UITooltip);
            updateSystem.UpdateAfter<ApplyMoveItSystem, ApplyNetSystem>(SystemUpdatePhase.ApplyTool);
            updateSystem.UpdateBefore<TempNodeSystem, ApplyNetSystem>(SystemUpdatePhase.ApplyTool);
            updateSystem.UpdateBefore<RemoveSaveInstanceSystem, ApplyPrefabsSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateBefore<TempNodeSystem, CompositionSelectSystem>(SystemUpdatePhase.Modification3);
        }

        public void OnDispose()
        {
            Tool.MoveItToolSystem.Log?.Info(nameof(OnDispose));
            if (Settings != null)
            {
                Settings.UnregisterInOptionsUI();
                Settings = null;
            }
        }

        private void LoadNonEnglishLocalizations()
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = thisAssembly.GetManifestResourceNames();

            try
            {
                QLog.Debug($"Reading localizations");

                foreach (string localeID in GameManager.instance.localizationManager.GetSupportedLocales())
                {
                    string resourceName = $"{thisAssembly.GetName().Name}.l10n.{localeID}.json";
                    if (resourceNames.Contains(resourceName))
                    {
                        QLog.Debug($"Found localization file {resourceName}");
                        try
                        {
                            QLog.Debug($"Reading embedded translation file {resourceName}");

                            // Read embedded file.
                            using StreamReader reader = new(thisAssembly.GetManifestResourceStream(resourceName));
                            {
                                string entireFile = reader.ReadToEnd();
                                Colossal.Json.Variant varient = Colossal.Json.JSON.Load(entireFile);
                                Dictionary<string, string> translations = varient.Make<Dictionary<string, string>>();
                                GameManager.instance.localizationManager.AddSource(localeID, new MemorySource(translations));
                            }
                        }
                        catch (Exception e)
                        {
                            // Don't let a single failure stop us.
                            QLog.Error(e, $"Exception reading localization from embedded file {resourceName}");
                        }
                    }
                    else
                    {
                        QLog.Debug($"Did not find localization file {resourceName}");
                    }
                }
            }
            catch (Exception e)
            {
                QLog.Error(e, "Exception reading embedded settings localization files");
            }
        }
    }
}
