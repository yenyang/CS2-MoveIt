using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Tools;
using MoveIt.Actions;
using MoveIt.Actions.Select;
using MoveIt.Actions.Transform;
using MoveIt.Overlays;
using MoveIt.Settings;
using QCommonLib;
using System;
using System.Reflection;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Tool
{
    public partial class MoveItToolSystem : ObjectToolBaseSystem
    {
        public override void InitializeRaycast()
        {
            base.InitializeRaycast();

            if (!m_TempQuery.IsEmptyIgnoreFilter)
            {
                m_ToolRaycastSystem.typeMask = TypeMask.Terrain;
            }
            else
            {
                m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground | CollisionMask.ExclusiveGround;
                m_ToolRaycastSystem.typeMask = TypeMask.StaticObjects | TypeMask.Lanes | TypeMask.Net | TypeMask.Areas | TypeMask.Terrain;
                m_ToolRaycastSystem.raycastFlags = RaycastFlags.Decals | RaycastFlags.Markers;
                m_ToolRaycastSystem.netLayerMask = Layer.Road | Layer.TrainTrack | Layer.TramTrack | Layer.SubwayTrack | Layer.Pathway | Layer.Fence | Layer.LaneEditor;
                m_ToolRaycastSystem.iconLayerMask = Game.Notifications.IconLayerMask.None;
            }
            m_RaycastTerrain = new RaycastTerrain(World);
            m_RaycastSurface = new RaycastSurface(World);
        }

        /// <summary>
        /// Handle when the pointer leaves the UI; wait a few frames before reactiving overlays and mouse buttons
        /// </summary>
        private void UpdateUIHasFocus()
        {
            bool hit = (m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable | RaycastFlags.UIDisable)) != 0;

            if (!hit && _UIHasFocusStep == 0) return;

            if (hit)
            {
                _UIHasFocusStep = 3;
            }
            else
            {
                // Focus left UI recently
                _UIHasFocusStep = (short)math.max(0, _UIHasFocusStep - 1);
            }
        }

        internal float GetTerrainHeight(float3 position)
        {
            m_TerrainSystem.AddCPUHeightReader(m_InputDeps);
            Game.Simulation.TerrainHeightData heightData = m_TerrainSystem.GetHeightData(false);
            return Game.Simulation.TerrainUtils.SampleHeight(ref heightData, position);
        }

        internal void QueueOverlayUpdate(Overlay overlay)
        {
            m_PostToolSystem.QueueOverlayUpdate(overlay);
        }

        internal void QueueOverlayUpdateDeferred(Overlay overlay)
        {
            m_PostToolSystem.QueueOverlayUpdateDeferred(overlay);
        }

        internal void ToggleSelectionMode() => SetSelectionMode(!m_MarqueeSelect);

        internal void SetSelectionMode(bool toMarquee)
        {
            m_MarqueeSelect = toMarquee;

            SetManipulationMode(false);
        }

        internal void ToggleManipulationMode() => SetManipulationMode(!m_IsManipulateMode);

        internal void SetManipulationMode(bool toManipulate)
        {
            if (m_IsManipulateMode == toManipulate) return;

            Queue.Push(new ModeSwitchAction());
            EntityCommandBuffer buffer = new EntityCommandBuffer();
            Queue.Do(ref m_InputDeps, ref buffer);
            buffer.Playback(EntityManager);
            buffer.Dispose();
        }



        internal void MoveStart()
        {
            //QLog.Debug($"MOVESTART OnPress:{Hover.TopPressed.E()}-Null:{Hover.TopPressed.IsNull} :: {Hover.Normal.OnPress.E()}/{Hover.Child.OnPress.E()} (sel:{Selection.Has(Hover.TopPressed)})\n{QCommon.GetStackTrace(3)}");
            if (MITState == MITStates.SecondaryButtonHeld) return;
            if (Selection.Has(Hover.TopPressed))
            {
                QLog.Debug($"{nameof(MoveStart)} Selection.Has(Hover.TopPressed)");
                StartWorkflow(Workflow.Move);
            }
            else
            {
                QLog.Debug($"{nameof(MoveStart)} Queue.Push(new SelectAction()");
                // Requires OnHold to have fired, causing a 250ms delay
                Queue.Push(new SelectAction());
                JobHandle jobHandle = Dependency;
                EntityCommandBuffer buffer = m_ToolOutputBarrier.CreateCommandBuffer();
                Queue.Do(ref jobHandle,  ref buffer);

                StartWorkflow(Workflow.Move);
            }
            MITState = MITStates.ApplyButtonHeld;
            TransformStart();
        }

        internal void RotationStart()
        {
            StartWorkflow(Workflow.Rotate);
            if (MITState == MITStates.ApplyButtonHeld) return;           
            MITState = MITStates.SecondaryButtonHeld;
        }

        private void TransformStart()
        {
        }

        internal void EndMove()
        {
            CompeleteWorkflow(Workflow.Move);
            TransformEnd();
        }

        internal void RotationEnd()
        {
            CompeleteWorkflow(Workflow.Rotate);
            TransformEnd();
        }

        private void TransformEnd()
        {
            MITState = MITStates.Default;
        }

        internal static float GetDistanceBetween2D(Moveables.Moveable a, Moveables.Moveable b)
        {
            float3 posA = a.Transform.m_Position;
            float3 posB = b.Transform.m_Position;
            return posA.DistanceXZ(posB);
        }

        internal void UpdateSensitivityMode()
        {
            if (QKeyboard.Control)
            {
                if (!_IsLowSensitivity)
                {
                    ProcessSensitivityMode(true);
                }
            }
            else
            {
                if (_IsLowSensitivity)
                {
                    ProcessSensitivityMode(false);
                }
            }
        }

        internal void ProcessSensitivityMode(bool enable)
        {
            if (Queue?.Current is null || !Queue.Current.m_CanUseLowSensitivity) return;
            
            if (enable)
            {
                m_SensitivityTogglePosAbs = m_PointerPos;
                m_SensitivityTogglePosX = QCommon.MouseScreenPosition.x; //UnityEngine.InputSystem.Mouse.current.position.x.ReadValue();
            }

            _IsLowSensitivity = enable;
        }

        internal void SetUpdateAreaField(Bounds3 bounds)
        {
            if (Mathf.Approximately(bounds.min.x, bounds.max.x) || Mathf.Approximately(bounds.min.z, bounds.max.z)) return;
            float4 area = new(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
            FieldInfo field = m_TerrainSystem.GetType().GetField("m_UpdateArea", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new Exception("Failed to find TerrainSystem.m_UpdateArea");
            field.SetValue(m_TerrainSystem, area);
        }

        internal void StartWorkflow(Workflow workflow)
        {
            if (m_Workflow[(int)workflow] != WorkflowProgression.Starting)
            {
                m_Workflow[(int)workflow] = WorkflowProgression.Starting;
                QLog.Debug($"{nameof(MoveItToolSystem)}.Methods | Starting workflow: {workflow}");
            }
        }

        internal void CompeleteWorkflow(Workflow workflow)
        {
            if (m_Workflow[(int)workflow] != WorkflowProgression.Complete)
            {
                m_Workflow[(int)workflow] = WorkflowProgression.Complete;
                QLog.Debug($"{nameof(MoveItToolSystem)}.Methods | Workflow complete: {workflow}");
            }
        }

        internal void SetWorkflowInProgess(Workflow workflow)
        {
            if (m_Workflow[(int)workflow] != WorkflowProgression.InProgress)
            {
                m_Workflow[(int)workflow] = WorkflowProgression.InProgress;
                QLog.Debug($"{nameof(MoveItToolSystem)}.Methods | Workflow In Progress: {workflow}");
            }
        }

        internal void ResetWorkflow(Workflow workflow)
        {
            if (m_Workflow[(int)workflow] != WorkflowProgression.NotStarted)
            {
                m_Workflow[(int)workflow] = WorkflowProgression.NotStarted;
                QLog.Debug($"{nameof(MoveItToolSystem)}.Methods | Workflow Reset: {workflow}");
            }
        }

        internal bool ShouldClear()
        {
            if (UIHasFocus &&
                !Deleting)
            {
                return true;
            }

            // Complete workflows means that it should apply so workflow gets reset.
            foreach (Workflow workflow in m_ShouldClearWorkflows)
            {
                if (m_Workflow[(int)workflow] == WorkflowProgression.Complete)
                {
                    return false;
                }
            }

            // These workflows still use Quboid's setup and they do not need to go through Update process.
            foreach (Workflow workflow in m_ShouldClearWorkflows)
            {
                if (m_Workflow[(int)workflow] != WorkflowProgression.NotStarted)
                {
                    return true;
                }
            }

            // These workflows use the new setup.
            foreach (Workflow workflow in m_ShouldUpdateWorkflows)
            {
                if (m_Workflow[(int)workflow] != WorkflowProgression.NotStarted)
                {
                    return false;
                }
            }

            return true;
        }

        internal bool ShouldApply()
        {
            if (Copying)
            {
                if (m_Workflow[(int)Workflow.Rotate] == WorkflowProgression.Complete)
                {
                    ResetWorkflow(Workflow.Rotate);
                    return false;
                }

                if (m_Workflow[(int)Workflow.Elevate] == WorkflowProgression.Complete)
                {
                    ResetWorkflow(Workflow.Elevate);
                    return false;
                }
                
                if (m_Workflow[(int)Workflow.Lower] == WorkflowProgression.Complete)
                {
                    ResetWorkflow(Workflow.Lower);
                    return false;
                }
            }
               
            
            for (int i = 0; i < m_Workflow.Length; i++)
            {
                if (m_Workflow[i] == WorkflowProgression.Complete)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool ShouldUpdate(bool checkShouldApply = false)
        {
            if (checkShouldApply && ShouldApply())
            {
                return false;
            }

            foreach(Workflow workflow in m_ShouldUpdateWorkflows)
            {
                if (m_Workflow[(int)workflow] == WorkflowProgression.Starting ||
                    m_Workflow[(int)workflow] == WorkflowProgression.InProgress)
                {
                    SetWorkflowInProgess(workflow);
                    return true;
                }
            }

            return false;
        }

        public override string toolID => "MoveItTool";
        public override Game.Prefabs.PrefabBase GetPrefab() => null;
        public override bool TrySetPrefab(Game.Prefabs.PrefabBase prefab) => false;
    }
}
