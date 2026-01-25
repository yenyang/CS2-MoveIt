// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

#define BURST

using Game.Common;
using Game.Tools;
using QCommonLib;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace MoveIt.Systems
{
    /// <summary>
    /// A system to apply move it transformations to originals from temps. Only sometimes necessary such as object movement, node elevation/lowering.
    /// </summary>
    internal partial class ApplyMoveItSystem : MIT_System
    {
        private EntityQuery m_TempObjectQuery;
        private ToolOutputBarrier m_Barrier;
        private ToolSystem m_ToolSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_Barrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();

            m_ToolSystem.EventToolChanged += OnToolChanged;

            m_TempObjectQuery = SystemAPI.QueryBuilder()
               .WithAll<Temp, Game.Objects.Transform>()
               .WithNone<Deleted, Game.Common.Overridden>()
               .Build();

            RequireForUpdate(m_TempObjectQuery);

            QLog.Info($"{nameof(ApplyMoveItSystem)}.{nameof(OnCreate)}");
            Enabled = false;
        }

        protected override void OnUpdate()
        {
            if (!m_TempObjectQuery.IsEmptyIgnoreFilter)
            {
                ChangeOriginalObjectTransformsJob changeOriginalObjectTransformsJob = new ChangeOriginalObjectTransformsJob()
                {
                    m_TempType = SystemAPI.GetComponentTypeHandle<Temp>(),
                    m_TransformLookup = SystemAPI.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true),
                    buffer = m_Barrier.CreateCommandBuffer().AsParallelWriter(),
                    m_TransformType = SystemAPI.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true),
                };

                JobHandle jobHandle = changeOriginalObjectTransformsJob.ScheduleParallel(m_TempObjectQuery, Dependency);
                m_Barrier.AddJobHandleForProducer(jobHandle);
                Dependency = jobHandle;
            }
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
        private struct ChangeOriginalObjectTransformsJob : IJobChunk
        {
            [ReadOnly]
            public ComponentTypeHandle<Temp> m_TempType;
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> m_TransformLookup;
            [ReadOnly]
            public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

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
