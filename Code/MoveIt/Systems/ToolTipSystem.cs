// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

using Game.Tools;
using Game.UI.Localization;
using Game.UI.Tooltip;
using MoveIt.Actions.Toolbox;
using MoveIt.Settings;
using System.Collections.Generic;

namespace MoveIt.Systems
{
    /// <summary>
    /// Tooltip system that shows Toolbox modes and MIT modes.
    /// </summary>
    public partial class MIT_ToolTipSystem : TooltipSystemBase
    {
        private const string kUILStandard = "coui://uil/Standard/";
        private const string kCOUIpath = "coui://ui-mods/images/icon_";
        private readonly Dictionary<string, string> ToolboxIconPaths = new Dictionary<string, string>()
        {
            { nameof(TerrainHeight),  kUILStandard + "NetworkGround.svg"},
            { nameof(ObjectHeight), kUILStandard + "NoHeightLimit.svg" },
            { nameof(RotateAtCenter), kUILStandard +  "RotateAroundLeft.svg" },
            { nameof(RotateInPlace), kUILStandard + "ArrowCircularLeft.svg"},
        };

        private readonly Dictionary<string, string> MITmodeIconPaths = new Dictionary<string, string>()
        {
            { LocaleEN.kManipulation, $"{kCOUIpath}{LocaleEN.kManipulation}_Active.svg" },
            { LocaleEN.kMarquee, $"{kCOUIpath}{LocaleEN.kMarquee}_Active.svg" },
            { LocaleEN.kSingle, $"{kCOUIpath}{LocaleEN.kSingle}_Active.svg" },
        };
        
        private ToolSystem _ToolSystem;
        private StringTooltip _Tooltip;
        private Tool.MIT _MIT;


        private float _TTL;
        private LocalizedString _Text;
        private string _PreviousMode;
        private string _PreviousToolboxMode;

        private float TTL
        {
            get {  return _TTL; }
            set { _TTL = (value > 0) ? UnityEngine.Time.time + value : 0f; }
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            _ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
            _MIT = base.World.GetOrCreateSystemManaged<Tool.MIT>();
            _Tooltip = new StringTooltip
            {
                path = "QTesting_Main",
            };
            _TTL = 0f;
            _Text = new LocalizedString();
            _PreviousMode = string.Empty;
            _PreviousToolboxMode = string.Empty;
        }

        protected override void OnUpdate()
        {
            UpdateForTTL();
            if (_ToolSystem.activeTool != _MIT)
            {
                return;
            }

            CheckModesTooltip();
            CheckToolboxMode();

            if (_Text.value is not null && _TTL > 0f) 
            {
                _Tooltip.value = _Text;
                AddMouseTooltip(_Tooltip);
            }
        }

        public MIT_ToolTipSystem()
        { }

        /// <summary>
        /// Reduces the time until the tooltip disappears.
        /// </summary>
        private void UpdateForTTL()
        {
            if (_TTL > 0f && UnityEngine.Time.time > _TTL)
            {
                _Tooltip.icon = string.Empty;
                _Text = new LocalizedString();
                _TTL = 0f;
            }
        }

        internal void CheckModesTooltip()
        {
            string mode;

            if (_MIT.m_IsManipulateMode)
            {
                mode = LocaleEN.kManipulation;
            }
            else
            {
                mode = _MIT.m_MarqueeSelect ? LocaleEN.kMarquee : LocaleEN.kSingle;
            }

            if (mode != _PreviousMode)
            {
                _PreviousMode = mode;
                _Text = LocalizedString.IdWithFallback(LocaleEN.TooltipTitleKey(mode), $"{mode} Mode");
                TTL = 1.25f;
                if (MITmodeIconPaths.ContainsKey(mode))
                {
                    _Tooltip.icon = MITmodeIconPaths[mode];
                }
            }
        }

        internal void CheckToolboxMode()
        {
            string toolboxMode = _MIT.ToolboxManager.ActiveToolName;

            if (toolboxMode != _PreviousToolboxMode)
            {
                _PreviousToolboxMode = toolboxMode;
                if (toolboxMode != string.Empty)
                {
                    _Text = LocalizedString.IdWithFallback(LocaleEN.TooltipDescriptionKey(toolboxMode), toolboxMode);
                    if (ToolboxIconPaths.ContainsKey(toolboxMode))
                    {
                        _Tooltip.icon = ToolboxIconPaths[toolboxMode];
                    }
                    TTL = 5f;
                }
            }
                
        }
    }
}
