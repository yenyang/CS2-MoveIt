// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

#define BURST

using Game.Common;
using Game.Rendering;
using Game.Tools;
using MoveIt.Components;
using MoveIt.Systems;
using QCommonLib;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace MoveIt.Tool
{
    /// <summary>
    /// A system to apply move it transformations to originals from temps.
    /// </summary>
    internal partial class ApplyMoveItTransformationsSystem : MIT_System
    {
        private EntityQuery m_TempQuery;
        private ToolOutputBarrier m_Barrier;
        private ToolSystem m_ToolSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_Barrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();

            m_ToolSystem.EventToolChanged += OnToolChanged;

            m_TempQuery = SystemAPI.QueryBuilder()
               .WithAnyRW<Game.Objects.Transform, Game.Net.Curve>()
               .WithAll<Temp>()
               .WithNone<Deleted, Game.Common.Overridden>()
               .Build();

            RequireForUpdate(m_TempQuery);

            QLog.Info($"{nameof(ApplyMoveItTransformationsSystem)}.{nameof(OnCreate)}");
            Enabled = false;
        }

        protected override void OnUpdate()
        {
            ChangeOriginalEntitiesJob changeOriginalEntitiesJob = new ChangeOriginalEntitiesJob()
            {
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_TempType = SystemAPI.GetComponentTypeHandle<Temp>(),
                m_CurveLookup = SystemAPI.GetComponentLookup<Game.Net.Curve>(isReadOnly: true),
                m_TransformLookup = SystemAPI.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true),
                buffer = m_Barrier.CreateCommandBuffer(),
            };

            JobHandle jobHandle = changeOriginalEntitiesJob.Schedule(m_TempQuery, Dependency);
            m_Barrier.AddJobHandleForProducer(jobHandle);
            Dependency = jobHandle;
        }

        private void OnToolChanged(ToolBaseSystem tool)
        {
            if (tool == _MIT)
            {
                Enabled = true;
                return;
            }

            Enabled = false;
        }

#if BURST
        [BurstCompile]
#endif
        private struct ChangeOriginalEntitiesJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<Temp> m_TempType;
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> m_TransformLookup;
            [ReadOnly]
            public ComponentLookup<Game.Net.Curve> m_CurveLookup;
            public EntityCommandBuffer buffer;

            /// <summary>
            /// Executes job which will change Transform or curve for MIT Selected temp entities.
            /// </summary>
            /// <param name="chunk">ArchteypeChunk of IJobChunk.</param>
            /// <param name="unfilteredChunkIndex">Use for EntityCommandBuffer.ParralelWriter.</param>
            /// <param name="useEnabledMask">Part of IJobChunk. Unsure what it does.</param>
            /// <param name="chunkEnabledMask">Part of IJobChunk. Not sure what it does.</param>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<Temp> tempNativeArray = chunk.GetNativeArray(ref m_TempType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity tempEntity = entityNativeArray[i];
                    Entity originalEntity = tempNativeArray[i].m_Original;

                    if (!m_TransformLookup.TryGetComponent(originalEntity, out Game.Objects.Transform originalTransform) ||
                        !m_TransformLookup.TryGetComponent(tempEntity, out Game.Objects.Transform tempTransform))
                    {
                        continue;
                    }

                    buffer.SetComponent(originalEntity, tempTransform);
                }
            }
        }


    }
}
