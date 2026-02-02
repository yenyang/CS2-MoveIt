// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

using Game.Tools;
using MoveIt.Managers;
using MoveIt.Searcher;
using MoveIt.Selection;
using MoveIt.Systems;
using QCommonLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace MoveIt.Tool
{
    public enum MITStates
    {
        Default,
        ApplyButtonHeld,
        SecondaryButtonHeld,
        DrawingSelection,
        ToolActive,
        Cancelling,
    }

    [Flags]
    public enum InteractionFlags
    {
        None                = 0,
        Hovering            = 1,
        Selected            = 2,
        Moving              = 4,
        Deselect            = 8,
        ToolHover           = 16,
        ToolParentHover     = 32,
        Static              = 64,
        ParentHovering      = 128,
        ParentSelected      = 256,
        ParentManipulating  = 512,
    }

    public enum Workflow
    {
        SelectingIndividual,
        DrawingMarquee,
        DeselectAll,
        Move,
        Rotate,
        Elevate,
        Lower,
        Copy,
        Delete,
        Undo,
        Redo,
        AlignObjectHeight,
        AlignRotateAtCenter,
        AlignRotateInPlace,
        AlignTerrainHeight,
        Manipulating,
    }

    public enum WorkflowProgression
    {
        NotStarted = 0,
        Starting = 1,
        InProgress = 2,
        Complete = 3,
    }

    public partial class MoveItToolSystem : ObjectToolBaseSystem
    {
        internal static MoveItToolSystem m_Instance;

        internal ControlPointManager ControlPointManager;
        internal InputManager InputManager;
        internal HoverManager Hover;
        internal HoverHolder Hovered => Hover.TopHovered;
        internal MoveablesManager Moveables;
        internal QueueManager Queue;
        internal ToolboxManager ToolboxManager;
        internal FilterManager Filtering;

        internal MIT_UISystem m_UISystem;
        internal MIT_PostToolSystem m_PostToolSystem;
        internal MIT_InputSystem m_InputSystem;
        internal Overlays.MIT_OverlaySystem m_OverlaySystem;
        internal Systems.MIT_ToolTipSystem m_ToolTipSystem;
        internal ToolOutputBarrier m_Barrier;
        internal float3 m_LastRaycastPoint;
        internal float m_RotationAboutCenter;
        internal float m_PreviousRotation;
        internal float3 m_PreviousRaycastPoint;
        internal bool m_ResetVerticalDisplacement;
        internal float m_VerticalDisplacement;
        internal WorkflowProgression[] m_Workflow;
        internal List<Workflow> m_ShouldClearWorkflows = new List<Workflow>() { Workflow.DrawingMarquee, Workflow.Undo, Workflow.Redo , Workflow.DeselectAll, Workflow.AlignObjectHeight, Workflow.AlignRotateAtCenter, Workflow.AlignRotateAtCenter, Workflow.AlignTerrainHeight};
        internal List<Workflow> m_ShouldUpdateWorkflows = new List<Workflow>() { Workflow.Move, Workflow.Rotate, Workflow.Elevate, Workflow.Lower, Workflow.Copy, Workflow.Delete, Workflow.Manipulating };
        private bool m_RegisteredWithAnarchy;

        internal Game.Common.RaycastSystem m_RaycastSystem;

        public static QLoggerCO Log => _Log;

        private static readonly QLoggerCO _Log = new(false, "", true, Mod.IS_BETA);


        private ToolBaseSystem _PreviousTool = null;

        /// <summary>
        /// Check if the UI, including Move It panel, has focus (raycast hits it)
        /// If hit, is set to 3 but immediately decremented each frame so first frames after leaving UI doesn't register
        /// </summary>
        internal bool UIHasFocus => _UIHasFocusStep != 0;
        private short _UIHasFocusStep;

        internal JobHandle m_InputDeps;

        internal EntityQuery m_TempQuery;
        internal EntityQuery m_ControlPointQuery;
        internal EntityQuery m_SurfacesQuery;
        internal EntityQuery m_MIT_SelectedQuery;
        internal EntityQuery m_MIT_SelectedControlPointQuery;
        private EntityQuery m_DefinitionGroup;

        // Options
        internal bool ShowDebugPanel        => Mod.Settings.ShowDebugPanel;
        internal bool HideMoveItIcon        => Mod.Settings.HideMoveItIcon;
        internal bool ExtraDebugLogging     => Mod.Settings.ExtraDebugLogging;
        internal int RotationDirection      => Mod.Settings.InvertRotation ? 1 : -1;

        /// <summary>
        /// Get the currently hovered entity, readonly. For other mods to access.
        /// Entity.Null if nothing hovered.
        /// </summary>
        public Entity HoveredEntity => Hover.Normal.Definition.m_Entity;

        /// <summary>
        /// Get a hashset of the currently selected entities, readonly. For other mods to access.
        /// Empty hashset if nothing selected.
        /// </summary>
        public HashSet<Entity> SelectedEntities => Selection.Definitions.Select(mvd => mvd.m_Entity).ToHashSet();

        /// <summary>
        /// Gets current depenencies for MIT.
        /// </summary>
        public JobHandle Dependencies
        {
            get { return Dependency; }
        }

        /// <summary>
        /// Raycaster which only hits terrain
        /// </summary>
        internal RaycastTerrain m_RaycastTerrain;

        /// <summary>
        /// Raycaster which only hits surfaces
        /// </summary>
        internal RaycastSurface m_RaycastSurface;

        internal struct PrefabInfo
        {
            internal string m_Name;
            internal Entity m_Entity;
        }

        public MITStates MITState           { get; set; }
        public ApplyMode BaseApplyMode      { get => applyMode; set => applyMode = value; }

        /// <summary>
        /// Is Low Sensitivity mode active?
        /// Slower mouse movement, no overlays
        /// </summary>
        internal bool UsingPrecisionMode => _IsLowSensitivity;
        private bool _IsLowSensitivity;

        /// <summary>
        /// Where the current drag started, relative to selection center
        /// </summary>
        internal float3 m_DragPointerOffsetFromSelection;
        /// <summary>
        /// World position of where the current drag started, absolute
        /// </summary>
        internal float3 m_ClickPositionAbs;
        /// <summary>
        /// Where sensitivity was last toggled, absolute
        /// </summary>
        internal float3 m_SensitivityTogglePosAbs;

        /// <summary>
        /// Screen position where rotation started, X-axis absolute
        /// </summary>
        internal float m_MouseStartX;
        /// <summary>
        /// The current mouse position on the terrain
        /// </summary>
        internal float3 m_PointerPos;
        /// <summary>
        /// Where sensitivity was last toggled, X-axis absolute
        /// </summary>
        internal float m_SensitivityTogglePosX;

        // Selection modes
        internal Input.Marquee m_Marquee;
        internal bool m_MarqueeSelect;
        internal bool UseMarquee => m_MarqueeSelect && !m_IsManipulateMode;

        /// <summary>
        /// Is Manipulation Mode active, including quick-selection (player holding Alt)?
        /// </summary>
        public bool IsManipulating => m_IsManipulateMode || (QKeyboard.Alt && MITState == MITStates.Default);
        /// <summary>
        /// Is Manipulation Mode active, NOT including quick-selection?
        /// </summary>
        internal bool m_IsManipulateMode;

        // Selections
        public SelectionBase Selection { get => _Selection; set => _Selection = value; }
        private SelectionBase _Selection = null;
        public bool m_SelectionDirty = true;

        internal const float TERRAIN_UPDATE_MARGIN = 16f;

        /// <summary>
        /// Gets a value indicating whether objects are following terrain or moving along a flat plane.
        /// </summary>
        internal bool m_FollowingTerrain;

        /// <summary>
        /// Gets or sets a value indicating whether MIT is copying.
        /// </summary>
        public bool Copying
        {
            get { return m_Workflow[(int)Workflow.Copy] != WorkflowProgression.NotStarted; }
            set 
            {
                if (m_Workflow[(int)Workflow.Copy] == WorkflowProgression.NotStarted && value)
                {
                    StartWorkflow(Workflow.Copy);
                }
                else if (!value)
                {
                    ResetWorkflow(Workflow.Copy);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether objects are selected and therefore can be copied or deleted.
        /// </summary>
        public bool CanCopyOrDelete
        {
            get { return !m_MIT_SelectedQuery.IsEmptyIgnoreFilter && !IsManipulating; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether MIT is deleting or preparing to delete.
        /// </summary>
        public bool Deleting
        {
            get { return m_Workflow[(int)Workflow.Delete] != WorkflowProgression.NotStarted; }
            set
            {
                if (m_Workflow[(int)Workflow.Delete] == WorkflowProgression.NotStarted && value)
                {
                    StartWorkflow(Workflow.Delete);
                }
                else if (!value)
                {
                    ResetWorkflow(Workflow.Delete);
                }
            }
        }
    }
}
