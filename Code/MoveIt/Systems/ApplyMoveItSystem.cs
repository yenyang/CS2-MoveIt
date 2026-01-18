// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

// #define BURST

using Game.Common;
using Game.Rendering;
using Game.Tools;
using MoveIt.Components;
using MoveIt.Systems;
using MoveIt.Tool;
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
        private EntityQuery m_TempNodeQuery;
        private EntityQuery m_TempCurveQuery;
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

            m_TempNodeQuery = SystemAPI.QueryBuilder()
               .WithAll<Temp, Game.Net.Node>()
               .WithNone<Deleted, Game.Common.Overridden>()
               .Build();

            m_TempCurveQuery = SystemAPI.QueryBuilder()
               .WithAll<Temp, Game.Net.Curve>()
               .WithNone<Deleted, Game.Common.Overridden>()
               .Build();

            RequireAnyForUpdate(new EntityQuery[] { m_TempNodeQuery, m_TempObjectQuery, m_TempCurveQuery });

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

            if (m_ToolSystem.activeTool == _MIT &&
                !_MIT.Copying &&
                !_MIT.Deleting)
            {
                if (!m_TempNodeQuery.IsEmptyIgnoreFilter)
                {
                    QLog.Debug($"{nameof(ApplyMoveItSystem)}.{nameof(OnUpdate)} Scheduling ChangeOriginalNetNodesJob.");
                    ChangeOriginalNetNodesJob changeOriginalNetNodesJob = new ChangeOriginalNetNodesJob()
                    {
                        m_TempType = SystemAPI.GetComponentTypeHandle<Temp>(),
                        m_NodeLookup = SystemAPI.GetComponentLookup<Game.Net.Node>(isReadOnly: true),
                        buffer = m_Barrier.CreateCommandBuffer(),
                        m_NodeTypeHandle = SystemAPI.GetComponentTypeHandle<Game.Net.Node>(isReadOnly: true),
                        m_VerticalDisplacement = _MIT.m_VerticalDisplacement,
                    };

                    JobHandle jobHandle = changeOriginalNetNodesJob.Schedule(m_TempNodeQuery, Dependency);
                    m_Barrier.AddJobHandleForProducer(jobHandle);
                    Dependency = jobHandle;
                }

                if (!m_TempCurveQuery.IsEmptyIgnoreFilter)
                {
                    QLog.Debug($"{nameof(ApplyMoveItSystem)}.{nameof(OnUpdate)} Scheduling ChangeOriginalNetCurveJob.");
                    ChangeOriginalNetCurveJob changeOriginalNetCurveJob = new ChangeOriginalNetCurveJob()
                    {
                        m_TempType = SystemAPI.GetComponentTypeHandle<Temp>(),
                        m_CurveLookup = SystemAPI.GetComponentLookup<Game.Net.Curve>(isReadOnly: true),
                        buffer = m_Barrier.CreateCommandBuffer(),
                        m_CurveType = SystemAPI.GetComponentTypeHandle<Game.Net.Curve>(isReadOnly: true),
                        m_VerticalDisplacement = _MIT.m_VerticalDisplacement,
                    };

                    JobHandle jobHandle = changeOriginalNetCurveJob.Schedule(m_TempCurveQuery, Dependency);
                    m_Barrier.AddJobHandleForProducer(jobHandle);
                    Dependency = jobHandle;
                }
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



#if BURST
        [BurstCompile]
#endif
        private struct ChangeOriginalNetNodesJob : IJobChunk
        {
            [ReadOnly]
            public ComponentTypeHandle<Temp> m_TempType;
            [ReadOnly]
            public ComponentLookup<Game.Net.Node> m_NodeLookup;
            [ReadOnly]
            public ComponentTypeHandle<Game.Net.Node> m_NodeTypeHandle;
            [ReadOnly]
            public float m_VerticalDisplacement;
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
                NativeArray<Temp> tempNativeArray = chunk.GetNativeArray(ref m_TempType);
                NativeArray<Game.Net.Node> nodeNativeArray = chunk.GetNativeArray(ref m_NodeTypeHandle);
                for (int i = 0; i < chunk.Count; i++)
                {
#if DEBUG

                    QLog.Debug($"{nameof(ChangeOriginalNetNodesJob)} nodeNativeArray[{i}].m_Position.y = {nodeNativeArray[i].m_Position.y} ");
#endif
                    if (m_NodeLookup.TryGetComponent(tempNativeArray[i].m_Original, out Game.Net.Node originalNode))
                    {
#if DEBUG

                        QLog.Debug($"{nameof(ChangeOriginalNetNodesJob)} originalNode.m_Position.y = {originalNode.m_Position.y} ");
#endif
                        originalNode.m_Position.y += m_VerticalDisplacement;
                        buffer.SetComponent(tempNativeArray[i].m_Original, nodeNativeArray[i]);
                    }
                }
            }
        }



#if BURST
        [BurstCompile]
#endif
        private struct ChangeOriginalNetCurveJob : IJobChunk
        {
            [ReadOnly]
            public ComponentTypeHandle<Temp> m_TempType;
            [ReadOnly]
            public ComponentLookup<Game.Net.Curve> m_CurveLookup;
            [ReadOnly]
            public ComponentTypeHandle<Game.Net.Curve> m_CurveType;
            [ReadOnly]
            public float m_VerticalDisplacement;
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
                NativeArray<Temp> tempNativeArray = chunk.GetNativeArray(ref m_TempType);
                NativeArray<Game.Net.Curve> curveNativeArray = chunk.GetNativeArray(ref m_CurveType);
                for (int i = 0; i < chunk.Count; i++)
                {
#if DEBUG

                    QLog.Debug($"{nameof(ChangeOriginalNetCurveJob)} curveNativeArray[{i}].m_Bezier.a.y = {curveNativeArray[i].m_Bezier.a.y} ");
                    QLog.Debug($"{nameof(ChangeOriginalNetCurveJob)} curveNativeArray[{i}].m_Bezier.b.y = {curveNativeArray[i].m_Bezier.b.y} ");
                    QLog.Debug($"{nameof(ChangeOriginalNetCurveJob)} curveNativeArray[{i}].m_Bezier.c.y = {curveNativeArray[i].m_Bezier.c.y} ");
                    QLog.Debug($"{nameof(ChangeOriginalNetCurveJob)} curveNativeArray[{i}].m_Bezier.d.y = {curveNativeArray[i].m_Bezier.d.y} ");
#endif
                    if (m_CurveLookup.TryGetComponent(tempNativeArray[i].m_Original, out Game.Net.Curve originalCurve))
                    {
#if DEBUG

                        QLog.Debug($"{nameof(ChangeOriginalNetCurveJob)} originalCurve.m_Bezier.a.y = {originalCurve.m_Bezier.a.y} ");
                        QLog.Debug($"{nameof(ChangeOriginalNetCurveJob)} originalCurve.m_Bezier.b.y = {originalCurve.m_Bezier.b.y} ");
                        QLog.Debug($"{nameof(ChangeOriginalNetCurveJob)} originalCurve.m_Bezier.c.y = {originalCurve.m_Bezier.c.y} ");
                        QLog.Debug($"{nameof(ChangeOriginalNetCurveJob)} originalCurve.m_Bezier.d.y = {originalCurve.m_Bezier.d.y} ");
#endif
                        originalCurve.m_Bezier.a.y += m_VerticalDisplacement;
                        originalCurve.m_Bezier.d.y += m_VerticalDisplacement;
                        buffer.SetComponent(tempNativeArray[i].m_Original, curveNativeArray[i]);
                    }
                }
            }
        }
    }
}
