// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

// #define BURST

using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using MoveIt.Components;
using MoveIt.Selection;
using QCommonLib;
using System.Numerics;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace MoveIt.Tool
{
    /// <summary>
    /// Contains Job structs to be used by Move It Tool.
    /// </summary>
    public partial class MIT : Game.Tools.ObjectToolBaseSystem
    {

#if BURST
        [BurstCompile]
#endif
        /// <summary>
        /// Creates definitions for Entities from query.
        /// </summary>
        private struct CreateDefinitionJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> m_TransformData;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefLookup;
            public EntityCommandBuffer buffer;
            [ReadOnly]
            public ComponentLookup<Owner> m_OwnerLookup;
            [ReadOnly]
            public ComponentLookup<Game.Net.Curve> m_CurveLookup;
            [ReadOnly]
            public ComponentLookup<Game.Tools.EditorContainer> m_EditorContainterLookup;
            [ReadOnly]
            public ComponentLookup<Game.Common.PseudoRandomSeed> m_PseudoRandomSeedLookup;
            [ReadOnly]
            public ComponentLookup<Game.Objects.Attached> m_AttachedLookup;
            [ReadOnly]
            public ComponentLookup<Game.Net.Elevation> m_NetElevationLookup;
            [ReadOnly]
            public ComponentLookup<Game.Net.Edge> m_EdgeLookup;
            public CreationFlags m_CreationFlags;
            public ControlPoint m_StartPoint;
            public ControlPoint m_EndPoint;


            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity e = buffer.CreateEntity();
                    CreationDefinition creationDefinition = new()
                    {
                        m_Flags = m_CreationFlags,
                    };

                    if ((m_CreationFlags & CreationFlags.Relocate) == CreationFlags.Relocate ||
                        (m_CreationFlags & CreationFlags.Delete) == CreationFlags.Delete)
                    {
                        creationDefinition.m_Original = entityNativeArray[i];

                        // Not sure if this should be used for create or not.
                        if (m_AttachedLookup.HasComponent(entityNativeArray[i]))
                        {
                            creationDefinition.m_Attached = m_AttachedLookup[entityNativeArray[i]].m_Parent;
                        }
                    }

                    if (m_PrefabRefLookup.HasComponent(entityNativeArray[i]))
                    {
                        if (m_OwnerLookup.TryGetComponent(entityNativeArray[i], out Owner owner) &&
                            m_EditorContainterLookup.HasComponent(owner.m_Owner) &&
                            m_PrefabRefLookup.HasComponent(owner.m_Owner))
                        {
                            creationDefinition.m_Prefab = m_PrefabRefLookup[owner.m_Owner];
                            creationDefinition.m_SubPrefab = m_PrefabRefLookup[entityNativeArray[i]];
                        }
                        else
                        {
                            creationDefinition.m_Prefab = m_PrefabRefLookup[entityNativeArray[i]];
                        }
                    }

                    if (m_PseudoRandomSeedLookup.HasComponent(entityNativeArray[i]))
                    {
                        creationDefinition.m_RandomSeed = m_PseudoRandomSeedLookup[entityNativeArray[i]].m_Seed;
                    }

                    buffer.AddComponent(e, default(Updated));
                    if (m_TransformData.HasComponent(entityNativeArray[i]))
                    {
                        Game.Objects.Transform transform = m_TransformData[entityNativeArray[i]];
                        ObjectDefinition objectDefinition = new()
                        {
                            m_Position = transform.m_Position,
                            m_Rotation = transform.m_Rotation,
                            m_ParentMesh = -1,
                            m_Probability = 100,
                            m_PrefabSubIndex = -1,
                        };

                        if (m_OwnerLookup.TryGetComponent(entityNativeArray[i], out Owner owner) &&
                            m_TransformData.TryGetComponent(entityNativeArray[i], out Game.Objects.Transform subobjectTransform) &&
                            m_TransformData.TryGetComponent(owner.m_Owner, out Game.Objects.Transform ownerTransform))
                        {
                            Game.Objects.Transform inverseParentTransform = ObjectUtils.InverseTransform(ownerTransform);
                            Game.Objects.Transform localTransform = ObjectUtils.WorldToLocal(inverseParentTransform, subobjectTransform);

                            objectDefinition.m_LocalRotation = localTransform.m_Rotation;
                            objectDefinition.m_LocalPosition = localTransform.m_Position;
                        }

                        if ((m_CreationFlags & CreationFlags.Relocate) == CreationFlags.Relocate ||
                            m_CreationFlags == 0)
                        {
                            objectDefinition.m_Position.x += m_EndPoint.m_Position.x - m_StartPoint.m_Position.x;
                            objectDefinition.m_Position.z += m_EndPoint.m_Position.z - m_StartPoint.m_Position.z;
                        }

                        buffer.AddComponent(e, objectDefinition);
                    }

                    if (m_CurveLookup.TryGetComponent(entityNativeArray[i], out Game.Net.Curve curve))
                    {
                        NetCourse netCourse = new NetCourse()
                        {
                            m_Curve = curve.m_Bezier,
                            m_Elevation = default,
                            m_EndPosition = new CoursePos()
                            {
                                m_Entity = Entity.Null,
                                m_Elevation = default,
                                m_Flags = 0,
                                m_ParentMesh = -1,
                                m_Position = curve.m_Bezier.d,
                                m_SplitPosition = 0,
                                m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(curve.m_Bezier)),
                                m_CourseDelta = 1,
                            },
                            m_FixedIndex = -1,
                            m_Length = curve.m_Length,
                            m_StartPosition = new CoursePos()
                            {
                                m_Entity = Entity.Null,
                                m_Elevation = default,
                                m_Flags = 0,
                                m_ParentMesh = -1,
                                m_Position = curve.m_Bezier.a,
                                m_SplitPosition = 0,
                                m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(curve.m_Bezier)),
                                m_CourseDelta = 0,
                            },
                        };

                        if (m_EdgeLookup.TryGetComponent(entityNativeArray[i], out Game.Net.Edge edge))
                        {
                            netCourse.m_StartPosition.m_Entity = edge.m_Start;
                            netCourse.m_EndPosition.m_Entity = edge.m_End;

                            if (m_NetElevationLookup.TryGetComponent(edge.m_Start, out Game.Net.Elevation startElevation))
                            {
                                netCourse.m_StartPosition.m_Elevation = startElevation.m_Elevation;
                            }

                            if (m_NetElevationLookup.TryGetComponent(edge.m_Start, out Game.Net.Elevation endElevation))
                            {
                                netCourse.m_EndPosition.m_Elevation = endElevation.m_Elevation;
                            }
                        }

                        

                        buffer.AddComponent(e, netCourse);
                    }

                    buffer.AddComponent(e, creationDefinition);
                }
            }
        }

    }
}
