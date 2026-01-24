// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

using Game.Common;
using Game.Prefabs;
using Game.Tools;
using MoveIt.Components;
using MoveIt.Input;
using MoveIt.Managers;
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
                m_CreationFlags = GetCreationFlags(),
                m_EdgeLookup = SystemAPI.GetComponentLookup<Game.Net.Edge>(isReadOnly: true),
                m_NetElevationLookup = SystemAPI.GetComponentLookup<Game.Net.Elevation>(isReadOnly: true),
                m_StartPoint = new ControlPoint() { m_Position = m_ClickPositionAbs },
                m_EndPoint = GetEndPosition(),
                m_Centroid = new ControlPoint() { m_Position = _Selection.Center },
                m_FollowTerrain = m_FollowingTerrain,
                m_TerrainHeightData = m_TerrainSystem.GetHeightData(false),
                m_TreeLookup = SystemAPI.GetComponentLookup<Game.Objects.Tree>(isReadOnly: true),
                m_AreasNodeLookup = SystemAPI.GetBufferLookup<Game.Areas.Node>(isReadOnly: true),
                m_SubAreaLookup = SystemAPI.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true),
                m_SubNetLookup = SystemAPI.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true),
                m_RotationAboutCenter = m_RotationAboutCenter,
                m_ConnectedEdgeLookup = SystemAPI.GetBufferLookup<Game.Net.ConnectedEdge>(isReadOnly: true),
                m_SelectedLookup = SystemAPI.GetComponentLookup<MIT_Selected>(isReadOnly: true),
                m_NodeLookup = SystemAPI.GetComponentLookup<Game.Net.Node>(isReadOnly: true),
                m_VerticalDisplacement = m_VerticalDisplacement,
            };
            inputDeps = createDefinitionJob.Schedule(m_MIT_SelectedQuery, inputDeps);
            
            m_TerrainSystem.AddCPUHeightReader(inputDeps);
            m_Barrier.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        private JobHandle Update(JobHandle inputDeps)
        {            
            if (m_Workflow[(int)Workflow.Rotate] == WorkflowProgression.InProgress)
            {
                float newRotation = (QCommon.MouseScreenPosition.x - m_MouseStartX) / (float)(Screen.height * 1.5f) * RotationDirection * 4f;
                if (!Mathf.Approximately(newRotation, m_PreviousRotation))
                {
                    m_RotationAboutCenter += newRotation - m_PreviousRotation;
                    m_PreviousRotation = newRotation;
                }
                else
                {
                    applyMode = ApplyMode.None;
                    return inputDeps;
                }
            }
            else if (InputManager.ProcessKeyMovement(out float3 direction, out _) && 
               (m_Workflow[(int)Workflow.Elevate] == WorkflowProgression.InProgress ||
                m_Workflow[(int)Workflow.Lower] == WorkflowProgression.InProgress))
            {
                m_VerticalDisplacement += direction.y;
            }
            else if (m_Workflow[(int)Workflow.Move] == WorkflowProgression.InProgress ||
                     m_Workflow[(int)Workflow.Copy] == WorkflowProgression.InProgress)
            {
                if (Mathf.Approximately(m_LastRaycastPoint.x, m_PreviousRaycastPoint.x) &&
                    Mathf.Approximately(m_LastRaycastPoint.y, m_PreviousRaycastPoint.y) &&
                    Mathf.Approximately(m_LastRaycastPoint.z, m_PreviousRaycastPoint.z))
                {
                    applyMode = ApplyMode.None;
                    return inputDeps;
                }
                else
                {
                    m_PreviousRaycastPoint = m_LastRaycastPoint;
                }
            }

            applyMode = ApplyMode.Clear;
            inputDeps = UpdateDefinitions(inputDeps);
            return inputDeps;       
        }


        private JobHandle Clear(JobHandle inputDeps)
        {
            applyMode = ApplyMode.Clear;
            inputDeps = DestroyDefinitions(m_DefinitionGroup, m_Barrier, inputDeps);

            if (Copying)
            {
                StartWorkflow(Workflow.Copy);
            }

            return inputDeps;
        }

        private JobHandle Apply(JobHandle inputDeps)
        {
            ApplyMode newApplyMode = ApplyMode.Clear;
            foreach (Workflow workflow in m_ShouldUpdateWorkflows)
            {
                if (m_Workflow[(int)workflow] == WorkflowProgression.Complete)
                {
                    newApplyMode = ApplyMode.Apply;
                }
            }

            applyMode = newApplyMode;

            if (Deleting)
            {
                Deleting = false;
                Selection.Clear();
            }

            if (!Copying)
            {
                m_RotationAboutCenter = 0f;
                m_PreviousRotation = 0f;
                m_ResetVerticalDisplacement = true;
            }

            m_SelectionDirty = true;
            Selection.CalculateCenter();

            for (int i = 0; i < m_Workflow.Length; i++)
            {
                if (i != (int)Workflow.Copy)
                {
                    ResetWorkflow((Workflow)i);
                }
                else if (Copying &&
                         m_Workflow[(int)Workflow.Copy] == WorkflowProgression.Complete)
                {
                    StartWorkflow(Workflow.Copy);
                }
            }

            return inputDeps;
        }

        private ControlPoint GetEndPosition()
        {
            if (m_Workflow[(int)Workflow.Move] == WorkflowProgression.InProgress ||
                m_Workflow[(int)Workflow.Copy] == WorkflowProgression.InProgress)
            {
                return new ControlPoint() { m_Position = m_PointerPos };
            }

            return new ControlPoint() { m_Position = m_ClickPositionAbs };
        }

        private CreationFlags GetCreationFlags()
        {
            if (Deleting)
            {
                return CreationFlags.Delete;
            }

            if (Copying)
            {
                return 0;
            }

            return CreationFlags.Relocate;
        }

    }
}
