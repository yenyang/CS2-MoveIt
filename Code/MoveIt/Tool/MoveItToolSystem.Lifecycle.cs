// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

using Colossal.Logging;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Simulation;
using Game.Tools;
using MoveIt.Components;
using MoveIt.Selection;
using Newtonsoft.Json;
using QCommonLib;
using System;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using Unity.Jobs;

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

            Filtering = new();

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

            m_MIT_SelectedControlPointQuery = SystemAPI.QueryBuilder()
                .WithAll<MIT_Selected, MIT_ControlPoint>()
                .WithNone<Game.Tools.Temp, Game.Common.Overridden, Game.Common.Deleted>()
                .Build();

            m_Workflow = new WorkflowProgression[(int)Workflow.Manipulating+1];

            for (int i = 0; i < m_Workflow.Length; i++)
            {
                m_Workflow[i] = WorkflowProgression.NotStarted;
            }
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

            MITState = MITStates.Default;
            Actions.Action.Phase = Actions.Phases.None;

            m_MarqueeSelect = false;
            m_IsManipulateMode = false;
            Selection ??= new SelectionNormal();

            m_OverlaySystem.DestroyAllEntities();
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            if (m_RegisteredWithAnarchy)
            {
                return;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                if (!assembly.GetName().FullName.Contains("Anarchy,"))
                {
                    continue;
                }


                QLog.Info($"{nameof(MoveItToolSystem)}.Lifecycle.{nameof(OnGameLoadingComplete)} Found Anarchy Assembly: {assembly.FullName}.");
                Type[] types = assembly.GetTypes();

                Type anarchyBridge = assembly.GetTypes().FirstOrDefault(x => x.FullName.Contains("Anarchy.Bridge.AnarchyBridge"));
                if (anarchyBridge is null)
                {
                    QLog.Info($"{nameof(MoveItToolSystem)}.Lifecycle.{nameof(OnGameLoadingComplete)} Couldn't locate Anarchy Bridge.");
                    continue;
                }

                QLog.Debug($"{nameof(MoveItToolSystem)}.Lifecycle.{nameof(OnGameLoadingComplete)} Located Anarchy Bridge.");
                
                MethodInfo addToolMethod = anarchyBridge.GetMethod("TryAddToolSystem", BindingFlags.Public | BindingFlags.Static);
                if (addToolMethod is null)
                {
                    QLog.Info($"{nameof(MoveItToolSystem)}.Lifecycle.{nameof(OnGameLoadingComplete)} Could not find method to add tool.");
                    break;
                }

                var results = addToolMethod.Invoke(null, new object[] { this });
                if (results is Boolean &&
                    (bool)results == true)
                {
                    m_RegisteredWithAnarchy = true;
                    QLog.Info($"{nameof(MoveItToolSystem)}.Lifecycle.{nameof(OnGameLoadingComplete)} Successfully registered with Anarchy!");
                } else
                { 
                    QLog.Info($"{nameof(MoveItToolSystem)}.Lifecycle.{nameof(OnGameLoadingComplete)} Failed to register with Anarchy.");
                }
            }
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
            Copying = false;
            Deleting = false;
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
