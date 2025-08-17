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
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

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
                .WithAll<CreationDefinition, OwnerDefinition>()
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
            FindCopiedOwnersJob changeOriginalEntitiesJob = new FindCopiedOwnersJob()
            {
                m_TempType = SystemAPI.GetComponentTypeHandle<Temp>(),
                m_TransformLookup = SystemAPI.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true),
                buffer = m_Barrier.CreateCommandBuffer().AsParallelWriter(),
            };

            JobHandle jobHandle = changeOriginalEntitiesJob.ScheduleParallel(m_TempQuery, Dependency);
            m_Barrier.AddJobHandleForProducer(jobHandle);
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


            public EntityCommandBuffer.ParallelWriter buffer;

            /// <summary>
            /// Executes job which will change Transform MIT Selected temp entities.
            /// </summary>
            /// <param name="chunk">ArchteypeChunk of IJobChunk.</param>
            /// <param name="unfilteredChunkIndex">Use for EntityCommandBuffer.ParralelWriter.</param>
            /// <param name="useEnabledMask">Part of IJobChunk. Unsure what it does.</param>
            /// <param name="chunkEnabledMask">Part of IJobChunk. Not sure what it does.</param>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Temp> tempNativeArray = chunk.GetNativeArray(ref m_TempType);
                NativeArray<Game.Objects.Transform> transformNativeArray = chunk.GetNativeArray(ref m_TransformType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    if (m_TransformLookup.TryGetComponent(tempNativeArray[i].m_Original, out Game.Objects.Transform originalTransform))
                    {
                        buffer.SetComponent(unfilteredChunkIndex, tempNativeArray[i].m_Original, transformNativeArray[i]);
                    }
                }
            }
        }


    }
}
