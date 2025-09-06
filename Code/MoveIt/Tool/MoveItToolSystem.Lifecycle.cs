// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Simulation;
using Game.Tools;
using MoveIt.Components;
using MoveIt.Selection;
using QCommonLib;
using Unity.Entities;

namespace MoveIt.Tool
{
    public partial class MoveItToolSystem : ObjectToolBaseSystem
    {
        // Runs on first load
        protected override void OnCreate()
        {
            Log.Info($"Tool.OnCreate");
            base.OnCreate();
            m_Instance = this;
            Enabled = false;

            m_OverlaySystem = World.GetOrCreateSystemManaged<Overlays.MIT_OverlaySystem>();
            m_UISystem = World.GetOrCreateSystemManaged<Systems.MIT_UISystem>();
            m_PostToolSystem = World.GetOrCreateSystemManaged<Systems.MIT_PostToolSystem>();
            m_InputSystem = World.GetOrCreateSystemManaged<Systems.MIT_InputSystem>();
            m_ToolTipSystem = World.GetOrCreateSystemManaged<Systems.MIT_ToolTipSystem>();
            m_Barrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_RaycastSystem = World.GetOrCreateSystemManaged<Game.Common.RaycastSystem>();

            QKeyboard.Init();

            m_DefinitionGroup = GetDefinitionQuery();

            m_TempQuery = SystemAPI.QueryBuilder()
                .WithAll<Temp>()
                .WithNone<Deleted, Game.Common.Overridden>()
                .Build();

            m_ControlPointQuery = SystemAPI.QueryBuilder()
                .WithAll<MIT_ControlPoint>()
                .Build();

            m_SurfacesQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Areas.Area, Game.Areas.Surface>()
                .WithNone<Game.Common.Owner>()
                .Build();

            m_MIT_SelectedQuery = SystemAPI.QueryBuilder()
                .WithAll<MIT_Selected>()
                .WithNone<Game.Tools.Temp, Game.Common.Overridden, Game.Common.Deleted, MIT_ControlPoint>()
                .Build();
        }

        // Runs on every load, after OnCreate
        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            Log.Info($"Tool.OnGamePreload(Purpose:{purpose}, GameMode:{mode})");
            base.OnGamePreload(purpose, mode);

            m_Instance = this;
            Enabled = false;

            m_InputSystem.Initialise(Mod.Settings);
            ControlPointManager = new();
            InputManager = new();
            Hover = new();
            Moveables = new();
            Queue = new();
            ToolboxManager = new();
            Filtering = new();

            MITState = MITStates.Default;
            Actions.Action.Phase = Actions.Phases.None;

            m_MarqueeSelect = false;
            m_IsManipulateMode = false;
            Selection ??= new SelectionNormal();

            m_CreationFlags = CreationFlags.Relocate;

            m_OverlaySystem.DestroyAllEntities();
        }

        protected override void OnStartRunning()
        {
            Log.IsDebug = ExtraDebugLogging;
            Log.Info("Tool.OnStartRunning()");
            base.OnStartRunning();

            m_ToolTipSystem.Enabled = true;
            m_InputSystem.OnToolEnable();
            m_PostToolSystem.Start();
            m_OverlaySystem.Start();
            InputManager.OnToolEnable();
            m_LastRaycastPoint = default;

            Moveables.Refresh();
            Selection.Refresh();
            m_SelectionDirty = true;
        }

        protected override void OnStopRunning()
        {
            Log.Info($"Tool.OnStopRunning");
            base.OnStopRunning();

            Hover.Clear();
            InputManager.OnToolDisable();
            m_OverlaySystem.End();
            m_PostToolSystem.End();
            m_InputSystem.OnToolDisable();
            m_ToolTipSystem.Enabled = false;
            // secondaryApplyAction.enabled = false;

            QLog.FlushBundle();
        }

        protected override void OnDestroy()
        {
            Log.Info($"Tool.OnDestroy() {(ControlPointManager is null ? "(CPM destroyed)" : "(CPM closing)")}");
            Log.Shutdown();
            base.OnDestroy();
        }

        public void RequestEnable()
        {
            if (m_ToolSystem.activeTool != this)
            {
                _PreviousTool = m_ToolSystem.activeTool;
                m_ToolSystem.selected = Entity.Null;
                m_ToolSystem.activeTool = this;
                applyMode = ApplyMode.Clear;

                _UIHasFocusStep = 0;
            }
        }

        public void RequestToggle()
        {
            if (m_ToolSystem.activeTool == this)
            {
                RequestDisable();
            }
            else
            {
                RequestEnable();
            }
        }

        private void RequestDisable()
        {
            m_ToolSystem.activeTool = _PreviousTool ?? m_DefaultToolSystem;
        }
    }
}
