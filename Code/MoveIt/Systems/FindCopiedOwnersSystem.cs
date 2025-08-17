// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

#define BURST

using Game.Areas;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using MoveIt.Components;
using QCommonLib;
using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace MoveIt.Systems
{
    /// <summary>
    /// A system to find owners for temp subareas that have been copied. I don't know why this is necessary. 
    /// </summary>
    internal partial class FindCopiedOwnersSystem : MIT_System
    {
        private EntityQuery m_TempQuery;
        private EntityQuery m_OwnerDefinitionQuery;
        private ModificationBarrier2 m_Barrier;
        private ToolSystem m_ToolSystem;
        private EntityQuery m_TempSubAreaInstanceQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_Barrier = World.GetOrCreateSystemManaged<ModificationBarrier2>();

            m_ToolSystem.EventToolChanged += OnToolChanged;

            m_TempQuery = SystemAPI.QueryBuilder()
               .WithAll<Temp, Game.Objects.Transform, Game.Areas.SubArea, PrefabRef>()
               .WithNone<Deleted, Overridden>()
               .Build();

            m_OwnerDefinitionQuery = SystemAPI.QueryBuilder()
                .WithAll<CreationDefinition, OwnerDefinition, Game.Areas.Node>()
                .WithNone<Deleted, Temp, Overridden>()
                .Build();

            m_TempSubAreaInstanceQuery = SystemAPI.QueryBuilder()
                .WithAll<Temp, Game.Areas.Area, PrefabRef, Game.Areas.Node, Owner>()
                .WithNone<Deleted, Overridden>()
                .Build();

            RequireForUpdate(m_TempQuery);
            RequireForUpdate(m_OwnerDefinitionQuery);
            RequireForUpdate(m_TempSubAreaInstanceQuery);

            QLog.Info($"{nameof(FindCopiedOwnersSystem)}.{nameof(OnCreate)}");
            Enabled = false;
        }

        protected override void OnUpdate()
        {
            NativeArray<Entity> ownerDefinitionEntities = m_OwnerDefinitionQuery.ToEntityArray(Allocator.TempJob);
            NativeArray<Entity> tempOwnersOfSubAreas = m_TempQuery.ToEntityArray(Allocator.TempJob);

            FindCopiedOwnersJob findCopiedOwnersJob = new FindCopiedOwnersJob()
            {
                m_AreaNodeLookup = SystemAPI.GetBufferLookup<Game.Areas.Node>(isReadOnly: true),
                m_AreasNodeType = SystemAPI.GetBufferTypeHandle<Game.Areas.Node>(isReadOnly: true),
                m_EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                m_OwnerDefinitionEntities = ownerDefinitionEntities,
                m_OwnerDefinitionLookup = SystemAPI.GetComponentLookup<Game.Tools.OwnerDefinition>(isReadOnly: true),
                m_OwnerEntites = tempOwnersOfSubAreas,
                m_PrefabRefLookup = SystemAPI.GetComponentLookup<Game.Prefabs.PrefabRef>(isReadOnly: true),
                m_PrefabRefType = SystemAPI.GetComponentTypeHandle<Game.Prefabs.PrefabRef>(isReadOnly: true),
                m_TransformLookup = SystemAPI.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true),
                buffer = m_Barrier.CreateCommandBuffer(),
            };
            JobHandle jobHandle = findCopiedOwnersJob.Schedule(m_TempQuery, Dependency);
            m_Barrier.AddJobHandleForProducer(jobHandle);
            ownerDefinitionEntities.Dispose(jobHandle);
            tempOwnersOfSubAreas.Dispose(jobHandle);
            Dependency = jobHandle;
        }

        private void OnToolChanged(ToolBaseSystem tool)
        {
            if (tool == _MIT &&
                _MIT.Copying)
            {
                Enabled = true;
                return;
            }

            Enabled = false;
        }

#if BURST
        [BurstCompile]
#endif
        private struct FindCopiedOwnersJob : IJobChunk
        {
            [ReadOnly]
            public NativeArray<Entity> m_OwnerEntites;
            [ReadOnly]
            public NativeArray<Entity> m_OwnerDefinitionEntities;
            [ReadOnly]
            public BufferLookup<Game.Areas.Node> m_AreaNodeLookup;
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> m_TransformLookup;
            [ReadOnly]
            public ComponentLookup<Game.Tools.OwnerDefinition> m_OwnerDefinitionLookup;
            [ReadOnly]
            public ComponentLookup<Game.Prefabs.PrefabRef> m_PrefabRefLookup;
            [ReadOnly]
            public EntityTypeHandle m_EntityTypeHandle;
            [ReadOnly]
            public BufferTypeHandle<Game.Areas.Node> m_AreasNodeType;
            [ReadOnly]
            public ComponentTypeHandle<Game.Prefabs.PrefabRef> m_PrefabRefType;

            public EntityCommandBuffer buffer;

            /// <summary>
            /// Executes job which will change Transform MIT Selected temp entities.
            /// </summary>
            /// <param name="chunk">ArchteypeChunk of IJobChunk.</param>
            /// <param name="unfilteredChunkIndex">Use for EntityCommandBuffer.ParralelWriter.</param>
            /// <param name="useEnabledMask">Part of IJobChunk. Unsure what it does.</param>
            /// <param name="chunkEnabledMask">Part of IJobChunk. Not sure what it does.</param>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityTypeHandle);
                BufferAccessor<Game.Areas.Node> areasNodeAccessor = chunk.GetBufferAccessor(ref m_AreasNodeType);
                NativeArray<PrefabRef> prefabRefNativeArray = chunk.GetNativeArray(ref m_PrefabRefType); 
                for (int i = 0; i < chunk.Count; i++)
                {
                    for (int j = 0; j < m_OwnerDefinitionEntities.Length; j++)
                    {

                        // Check if OwnerDefinition and Temp SubArea has the same prefab, If not continue.
                        if (!m_OwnerDefinitionLookup.TryGetComponent(m_OwnerDefinitionEntities[j], out OwnerDefinition ownerDefinition) ||
                            ownerDefinition.m_Prefab == Entity.Null ||
                            prefabRefNativeArray[i] == Entity.Null ||
                            ownerDefinition.m_Prefab != prefabRefNativeArray[i])
                        {
                            continue;
                        }

                        // Check if definition area nodes matches Temp area nodes. if not continue;
                        if (m_AreaNodeLookup.TryGetBuffer(m_OwnerDefinitionEntities[j], out DynamicBuffer<Game.Areas.Node> definitionNodes))
                        {
                            DynamicBuffer<Game.Areas.Node> tempNodes = areasNodeAccessor[i];

                            bool matches = true;
                            for (int k = 0; k < Math.Min(tempNodes.Length, definitionNodes.Length); k++)
                            {
                                if (!Mathf.Approximately(definitionNodes[k].m_Position.x, tempNodes[k].m_Position.x) ||
                                    !Mathf.Approximately(definitionNodes[k].m_Position.z, tempNodes[k].m_Position.z))
                                {
                                    matches = false; 
                                    break;
                                }
                            }

                            if (!matches)
                            {
                                continue;
                            }
                        }

                        // Find an Owner that matches transform location and prefab of OwnerDefinition.
                        for (int k = 0; k < m_OwnerEntites.Length; k++)
                        {
                            // Check prefab of Owner Definition and potential owner.
                            if (!m_PrefabRefLookup.TryGetComponent(m_OwnerEntites[k], out PrefabRef ownerPrefabRef) ||
                                ownerPrefabRef == Entity.Null ||
                                ownerPrefabRef != ownerDefinition.m_Prefab)
                            {
                                continue;
                            }

                            // Check transform location of potential owner against owner definition. if doesn't match continue.
                            if (!m_TransformLookup.TryGetComponent(m_OwnerEntites[k], out Game.Objects.Transform ownerTransform) ||
                                !Mathf.Approximately(ownerDefinition.m_Position.x, ownerTransform.m_Position.x) ||
                                !Mathf.Approximately(ownerDefinition.m_Position.y, ownerTransform.m_Position.y) ||
                                !Mathf.Approximately(ownerDefinition.m_Position.z, ownerTransform.m_Position.z) ||
                                !Mathf.Approximately(ownerDefinition.m_Rotation.value.x, ownerTransform.m_Rotation.value.x) ||
                                !Mathf.Approximately(ownerDefinition.m_Rotation.value.y, ownerTransform.m_Rotation.value.y) ||
                                !Mathf.Approximately(ownerDefinition.m_Rotation.value.z, ownerTransform.m_Rotation.value.z) ||
                                !Mathf.Approximately(ownerDefinition.m_Rotation.value.w, ownerTransform.m_Rotation.value.w))
                            {
                                continue;
                            }

                            buffer.SetComponent(entityNativeArray[i], new Owner() { m_Owner = m_OwnerEntites[k] });
                        }
                    }
                }
            }
        }


    }
}
