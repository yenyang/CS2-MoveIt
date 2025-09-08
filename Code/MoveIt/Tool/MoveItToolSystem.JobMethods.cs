// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

using Game.Common;
using Game.Prefabs;
using Game.Tools;
using QCommonLib;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Tool
{
    public partial class MoveItToolSystem : ObjectToolBaseSystem
    {
        private JobHandle UpdateDefinitions(JobHandle inputDeps)
        {
            JobHandle jobHandle = DestroyDefinitions(m_DefinitionGroup, m_Barrier, inputDeps);
            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();
            QLog.Debug($"{nameof(MoveItToolSystem)}:{nameof(UpdateDefinitions)} m_RotationAboutCenter {m_RotationAboutCenter}");

            CreateDefinitionJob createDefinitionJob = new CreateDefinitionJob()
            {
                buffer = buffer,
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_TransformLookup = SystemAPI.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true),
                m_PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true),
                m_OwnerLookup = SystemAPI.GetComponentLookup<Owner>(isReadOnly: true),
                m_CurveLookup = SystemAPI.GetComponentLookup<Game.Net.Curve>(isReadOnly: true),
                m_EditorContainterLookup = SystemAPI.GetComponentLookup<Game.Tools.EditorContainer>(isReadOnly: true),
                m_PseudoRandomSeedLookup = SystemAPI.GetComponentLookup<Game.Common.PseudoRandomSeed>(isReadOnly: true),
                m_AttachedLookup = SystemAPI.GetComponentLookup<Game.Objects.Attached>(isReadOnly: true),
                m_CreationFlags = m_CreationFlags,
                m_EdgeLookup = SystemAPI.GetComponentLookup<Game.Net.Edge>(isReadOnly: true),
                m_NetElevationLookup = SystemAPI.GetComponentLookup<Game.Net.Elevation>(isReadOnly: true),
                m_StartPoint = m_StartPoint,
                m_EndPoint = m_LastRaycastPoint,
                m_Centroid = new ControlPoint() { m_Position = _Selection.Center },
                m_FollowTerrain = m_FollowingTerrain,
                m_TerrainHeightData = m_TerrainSystem.GetHeightData(false),
                m_TreeLookup = SystemAPI.GetComponentLookup<Game.Objects.Tree>(isReadOnly: true),
                m_AreasNodeLookup = SystemAPI.GetBufferLookup<Game.Areas.Node>(isReadOnly: true),
                m_SubAreaLookup = SystemAPI.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true),
                m_SubNetLookup = SystemAPI.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true),
                m_RotationAboutCenter = m_RotationAboutCenter,
            };
            inputDeps = createDefinitionJob.Schedule(m_MIT_SelectedQuery, inputDeps);
            m_TerrainSystem.AddCPUHeightReader(inputDeps);
            m_Barrier.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        private JobHandle Update(JobHandle inputDeps)
        {
            if (GetRaycastResult(out ControlPoint controlPoint, out bool forceUpdate) ||
                Deleting)
            {
                if (m_InputSystem.MouseApply.WasPressedThisFrame() ||
                    m_InputSystem.MouseCancel.WasPressedThisFrame())
                {
                    applyMode = ApplyMode.Clear;
                    m_StartPoint = controlPoint;
                    m_LastRaycastPoint = controlPoint;
                    return UpdateDefinitions(inputDeps);
                }

                if (!Deleting &&
                    m_LastRaycastPoint.Equals(controlPoint) &&
                    !forceUpdate)
                {
                    applyMode = ApplyMode.None;
                    return inputDeps;
                }

                if (m_InputSystem.MouseCancel.IsPressed())
                {
                    applyMode = ApplyMode.Clear;
                    float mouseTravel = QCommon.MouseScreenPosition.x - m_MouseStartX;
                    m_RotationAboutCenter = mouseTravel / (float)(Screen.height * 1.5f) * RotationDirection;
                    return UpdateDefinitions(inputDeps);
                }
                
                applyMode = ApplyMode.Clear;
                m_LastRaycastPoint = controlPoint;
                return UpdateDefinitions(inputDeps);
            }
            if (m_LastRaycastPoint.Equals(default) &&
                !forceUpdate )
            {
                applyMode = ApplyMode.None;
                return inputDeps;
            }
            
            applyMode = ApplyMode.Clear;
            m_StartPoint = default;
            m_LastRaycastPoint = default;
            m_RotationAboutCenter = 0f;
            return Clear(inputDeps);            
        }


        private JobHandle Clear(JobHandle inputDeps)
        {
            applyMode = ApplyMode.Clear;
            inputDeps = DestroyDefinitions(m_DefinitionGroup, m_Barrier, inputDeps);
            return inputDeps;
        }

        private JobHandle Apply(JobHandle inputDeps)
        {
            applyMode = ApplyMode.Apply;
            if (Deleting)
            {
                Deleting = false;
            }
            return inputDeps;
        }

    }
}
