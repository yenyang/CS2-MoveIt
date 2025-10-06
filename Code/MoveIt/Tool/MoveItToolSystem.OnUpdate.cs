using Game.Prefabs;
using MoveIt.Actions.Select;
using MoveIt.Input;
using MoveIt.Managers;
using MoveIt.Systems;
using QCommonLib;
using System;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.InputSystem;

namespace MoveIt.Tool
{
    public partial class MoveItToolSystem : Game.Tools.ObjectToolBaseSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            m_InputDeps = base.OnUpdate(inputDeps);
            EntityCommandBuffer buffer = m_ToolOutputBarrier.CreateCommandBuffer();
            //ClearDebugOverlays();

            // If tool is opened before OnUpdate runs, m_RaycastTerrain won't be set. Skip this frame.
            if (m_RaycastTerrain is null) return inputDeps;

            if (!m_HasFocus) return inputDeps;
            UpdateUIHasFocus();

            if (m_SelectionDirty && Selection is not null)
            {
                Selection.CalculateCenter();
                m_SelectionDirty = false;
            }

            m_PointerPos = m_RaycastTerrain.HitPosition;
            Hover.Process(m_ToolRaycastSystem);
            InputManager.Process();

            switch (MITState)
            {
                case MITStates.Default:
                    break;

                case MITStates.ToolActive:
                    ToolboxManager.Update();
                    break;

                case MITStates.ApplyButtonHeld:
                case MITStates.SecondaryButtonHeld:
                    Actions.Action.Phase = Actions.Phases.Do;
                    break;

                case MITStates.DrawingSelection:
                    if (m_Marquee is null)
                    {
                        Log.Warning($"Drawing selection but m_Marquee is null");
                        break;
                    }

                    if (!m_Marquee.CheckIfMoved(m_PointerPos))
                    {
                        break;
                    }

                    if (Queue.Current is not SelectMarqueeAction sma)
                    {
                        Log.Debug($"Update DrawingSelection but current action is {Queue.Current.Name}");
                        break;
                    }

                    UpdateMarqueeList(m_Marquee);
                    sma.AddMarqueeSelection(m_Marquee, true);
                    break;
            }

            Queue.FireAction(ref inputDeps, ref buffer);

            


            if (!UIHasFocus &&
               (MITState == MITStates.Default ||
                MITState == MITStates.ApplyButtonHeld ||
                MITState == MITStates.SecondaryButtonHeld) || 
               (Deleting &&
                MITState != MITStates.DrawingSelection))
            {
                if (m_InputSystem.MouseApply.WasReleasedThisFrame() ||
                   (!Deleting &&
                    !Copying && 
                    !m_InputSystem.MouseApply.IsPressed() &&
                   (m_InputSystem.MouseCancel.WasReleasedThisFrame() ||
                    MovementKeyReleased)))
                {
                    return Apply(inputDeps);
                }
                else if (m_InputSystem.MouseApply.IsPressed() ||
                         Copying ||
                         m_InputSystem.MouseCancel.IsPressed() ||
                         MovementKeyPressed)
                {
                    return Update(inputDeps);
                }
            }

            ;

            if (MITState == MITStates.Cancelling)
            {
                MITState = MITStates.Default;
            }

            return Clear(inputDeps);
        }

        internal PrefabInfo GetPrefabInfo(Entity e)
        {
            PrefabInfo result = new();
            if (e == Entity.Null)
            {
                throw new Exception("Tool.GetPrefab: Entity is null");
            }
            if (!EntityManager.HasComponent<PrefabRef>(e))
            {
                throw new Exception($"Tool.GetPrefab: Entity {e.D()} has no PrefabRef");
            }

            PrefabRef prefabRef = EntityManager.GetComponentData<PrefabRef>(e);
            result.m_Name = QCommon.GetPrefabName(EntityManager, e);
            result.m_Entity = prefabRef.m_Prefab;
            return result;
        }

    }
}
