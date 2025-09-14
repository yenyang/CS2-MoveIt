// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

#define BURST

using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game;
using Game.Citizens;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Prefabs.Modes;
using Game.Simulation;
using Game.Tools;
using MoveIt.Components;
using MoveIt.Selection;
using QCommonLib;
using System.Numerics;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Game.Input.UIBaseInputAction;

namespace MoveIt.Tool
{
    /// <summary>
    /// Contains Job structs to be used by Move It Tool.
    /// </summary>
    public partial class MoveItToolSystem : Game.Tools.ObjectToolBaseSystem
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
            public ComponentLookup<Game.Objects.Transform> m_TransformLookup;
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
            [ReadOnly]
            public ComponentLookup<Game.Objects.Tree> m_TreeLookup;
            public CreationFlags m_CreationFlags;
            public ControlPoint m_StartPoint;
            public ControlPoint m_EndPoint;
            public ControlPoint m_Centroid;
            public float m_RotationAboutCenter;
            public bool m_FollowTerrain;
            public TerrainHeightData m_TerrainHeightData;
            [ReadOnly]
            public BufferLookup<Game.Areas.SubArea> m_SubAreaLookup;
            [ReadOnly]
            public BufferLookup<Game.Areas.Node> m_AreasNodeLookup;
            [ReadOnly]
            public BufferLookup<Game.Net.SubNet> m_SubNetLookup;
            [ReadOnly]
            public BufferLookup<Game.Net.ConnectedEdge> m_ConnectedEdgeLookup;
            [ReadOnly]
            public ComponentLookup<MIT_Selected> m_SelectedLookup;

            private NativeList<Entity> m_NetworksProcessed;


            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                m_NetworksProcessed = new(entityNativeArray.Length, Allocator.TempJob);

                for (int i = 0; i < chunk.Count; i++)
                {
                    // Selected Nodes. Each connected segment gets it's own entity.
                    if (m_ConnectedEdgeLookup.TryGetBuffer(entityNativeArray[i], out DynamicBuffer<ConnectedEdge> connectedEdges))
                    {
                        foreach (ConnectedEdge connectedEdge in connectedEdges)
                        {
                            if (m_CurveLookup.TryGetComponent(connectedEdge.m_Edge, out Game.Net.Curve connectedCurve) &&
                               !m_NetworksProcessed.Contains(connectedEdge.m_Edge))
                            {
                                Entity connectedEdgeDefinition = buffer.CreateEntity();
                                ProcessCreationDefinition(connectedEdge.m_Edge, connectedEdgeDefinition);

                                buffer.AddComponent(connectedEdgeDefinition, default(Updated));

                                ProcessCurve(connectedEdge.m_Edge, connectedEdgeDefinition, connectedCurve);
                            }
                        }

                        continue;
                    } else if (m_CurveLookup.HasComponent(entityNativeArray[i]) &&
                               m_NetworksProcessed.Contains(entityNativeArray[i]))
                    {
                        continue;
                    }


                    Entity e = buffer.CreateEntity();
                    ProcessCreationDefinition(entityNativeArray[i], e);
                    buffer.AddComponent(e, default(Updated));
                    Game.Objects.Transform transform = new Game.Objects.Transform();
                    if (m_TransformLookup.HasComponent(entityNativeArray[i]))
                    {
                        transform = m_TransformLookup[entityNativeArray[i]];
                        ObjectDefinition objectDefinition = new()
                        {
                            m_Position = transform.m_Position,
                            m_Rotation = transform.m_Rotation,
                            m_ParentMesh = -1,
                            m_Probability = 100,
                            m_PrefabSubIndex = -1,
                            m_Age = 1,
                            m_Intensity = 1,
                            m_Scale = new float3(1,1,1),
                        };

                        // Apply Rotation
                        if (m_RotationAboutCenter != 0 && m_CreationFlags != CreationFlags.Delete)
                        {
                            Game.Objects.Transform newWorldTransform = GetRotatedPosition(transform);
                            objectDefinition.m_Position = newWorldTransform.m_Position;
                            objectDefinition.m_Rotation = newWorldTransform.m_Rotation;
                        }

                        // Apply Translation
                        objectDefinition.m_Position = GetTranslatedXZPosition(objectDefinition.m_Position);

                        if (m_TreeLookup.TryGetComponent(entityNativeArray[i], out Tree tree))
                        {
                            objectDefinition.m_Age = GetTreeAge(tree);
                        }

                        if (m_FollowTerrain)
                        {
                            objectDefinition.m_Position = FollowTerrain(objectDefinition.m_Position, transform.m_Position);
                        }

                        if (m_OwnerLookup.HasComponent(entityNativeArray[i]) &&
                            m_TransformLookup.TryGetComponent(entityNativeArray[i], out Game.Objects.Transform subobjectTransform))
                        {
                            Game.Objects.Transform inverseParentTransform = ObjectUtils.InverseTransform(new Game.Objects.Transform(objectDefinition.m_Position, objectDefinition.m_Rotation));
                            Game.Objects.Transform localTransform = ObjectUtils.WorldToLocal(inverseParentTransform, subobjectTransform);

                            objectDefinition.m_LocalRotation = localTransform.m_Rotation;
                            objectDefinition.m_LocalPosition = localTransform.m_Position;
                        }
                        else
                        {
                            objectDefinition.m_LocalPosition = objectDefinition.m_Position;
                            objectDefinition.m_LocalRotation = objectDefinition.m_Rotation;
                        }

                        buffer.AddComponent(e, objectDefinition);
                    }

                    if (m_CurveLookup.TryGetComponent(entityNativeArray[i], out Game.Net.Curve curve))
                    {
                        ProcessCurve(entityNativeArray[i], e, curve);
                    }
                    
                    // SubAreas require their own CreationDefinition entity. The originals don't get hidden the way I would want them too. . .
                    if (m_SubAreaLookup.TryGetBuffer(entityNativeArray[i], out DynamicBuffer<Game.Areas.SubArea> subAreas) &&
                        subAreas.Length > 0)
                    {
                        for (int j = 0; j < subAreas.Length; j++)
                        {
                            if (subAreas[j].m_Area == Entity.Null)
                            {
                                continue;
                            }

                            Entity subAreaDefinition = buffer.CreateEntity();
                            CreationDefinition subAreaCreationDefinition = new()
                            {
                                m_Flags = CreationFlags.Hidden,
                            };

                            if ((m_CreationFlags & CreationFlags.Relocate) == CreationFlags.Relocate ||
                                (m_CreationFlags & CreationFlags.Delete) == CreationFlags.Delete)
                            {
                                subAreaCreationDefinition.m_Original = subAreas[j].m_Area;
                            }

                            if (m_PrefabRefLookup.HasComponent(subAreas[j].m_Area))
                            {
                                subAreaCreationDefinition.m_Prefab = m_PrefabRefLookup[subAreas[j].m_Area];
                            }
                            
                            if (m_PrefabRefLookup.HasComponent(entityNativeArray[i]) &&
                                m_TransformLookup.HasComponent(entityNativeArray[i]))
                            {
                                OwnerDefinition ownerDefinition = new OwnerDefinition()
                                {
                                    m_Prefab = m_PrefabRefLookup[entityNativeArray[i]],
                                    m_Position = transform.m_Position,
                                    m_Rotation = transform.m_Rotation,
                                };
                                buffer.AddComponent(subAreaDefinition, ownerDefinition);
                            }

                            if (m_AreasNodeLookup.TryGetBuffer(subAreas[j].m_Area, out DynamicBuffer<Game.Areas.Node> nodes) &&
                                nodes.Length > 0)
                            {
                                DynamicBuffer<Game.Areas.Node> newNodeBuffer = buffer.AddBuffer<Game.Areas.Node>(subAreaDefinition);
                                for (int k=0; k<nodes.Length; k++)
                                {
                                    Game.Areas.Node node = new Game.Areas.Node() { m_Position= nodes[k].m_Position, m_Elevation = nodes[k].m_Elevation };
                                    if ((m_CreationFlags & CreationFlags.Relocate) == CreationFlags.Relocate)
                                    {
                                        node.m_Position.x += m_EndPoint.m_Position.x - m_StartPoint.m_Position.x;
                                        node.m_Position.z += m_EndPoint.m_Position.z - m_StartPoint.m_Position.z;
                                    }
                                    else if (m_CreationFlags == 0)
                                    {
                                        node.m_Position.x += m_EndPoint.m_Position.x - m_Centroid.m_Position.x;
                                        node.m_Position.z += m_EndPoint.m_Position.z - m_Centroid.m_Position.z;
                                    }

                                    newNodeBuffer.Add(node);
                                }

                                // Creating new subarea requires additional node to show that it's a closed figure.
                                if (m_CreationFlags == 0)
                                {
                                    newNodeBuffer.Add(new Game.Areas.Node() { m_Position = newNodeBuffer[0].m_Position, m_Elevation = newNodeBuffer[0].m_Elevation });
                                }
                            }

                            buffer.AddComponent(subAreaDefinition, subAreaCreationDefinition);
                            buffer.AddComponent<Updated>(subAreaDefinition);
                        }
                    }
                }

                m_NetworksProcessed.Dispose();
            }

            private float GetTreeAge(Tree tree)
            {
                if (tree.m_State == 0)
                {
                    return Mathf.Lerp(tree.m_Growth / 255f, 0f, ObjectUtils.TREE_AGE_PHASE_CHILD);
                }
                else if (tree.m_State == TreeState.Teen)
                {
                    return Mathf.Lerp(tree.m_Growth / 255f, ObjectUtils.TREE_AGE_PHASE_CHILD, ObjectUtils.TREE_AGE_PHASE_CHILD + ObjectUtils.TREE_AGE_PHASE_TEEN); ;
                }
                else if (tree.m_State == TreeState.Adult)
                {
                    return Mathf.Lerp(tree.m_Growth / 255f, ObjectUtils.TREE_AGE_PHASE_CHILD + ObjectUtils.TREE_AGE_PHASE_TEEN, ObjectUtils.TREE_AGE_PHASE_CHILD + ObjectUtils.TREE_AGE_PHASE_TEEN + ObjectUtils.TREE_AGE_PHASE_ADULT); ;
                } 
                else if (tree.m_State == TreeState.Elderly)
                {
                    return Mathf.Lerp(tree.m_Growth / 255f, ObjectUtils.TREE_AGE_PHASE_CHILD + ObjectUtils.TREE_AGE_PHASE_TEEN + ObjectUtils.TREE_AGE_PHASE_ADULT, ObjectUtils.TREE_AGE_PHASE_CHILD + ObjectUtils.TREE_AGE_PHASE_TEEN + ObjectUtils.TREE_AGE_PHASE_ADULT + ObjectUtils.TREE_AGE_PHASE_ELDERLY); ;
                }
                else
                {
                    return Mathf.Lerp(tree.m_Growth / 255f, ObjectUtils.TREE_AGE_PHASE_CHILD + ObjectUtils.TREE_AGE_PHASE_TEEN + ObjectUtils.TREE_AGE_PHASE_ADULT + ObjectUtils.TREE_AGE_PHASE_ELDERLY + 0.0001f, ObjectUtils.TREE_AGE_PHASE_CHILD + ObjectUtils.TREE_AGE_PHASE_TEEN + ObjectUtils.TREE_AGE_PHASE_ADULT + ObjectUtils.TREE_AGE_PHASE_ELDERLY + ObjectUtils.TREE_AGE_PHASE_DEAD); ;
                }
            }

            private void ProcessCurve(Entity originalInstance, Entity definitionEntity, Game.Net.Curve curve)
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

                if (m_EdgeLookup.TryGetComponent(originalInstance, out Game.Net.Edge edge))
                {
                    if (m_CreationFlags != 0)
                    {
                        netCourse.m_StartPosition.m_Entity = edge.m_Start;
                        netCourse.m_EndPosition.m_Entity = edge.m_End;
                    }
                    else
                    {
                        netCourse.m_StartPosition.m_Flags = CoursePosFlags.IsLeft | CoursePosFlags.IsRight | CoursePosFlags.FreeHeight;
                        netCourse.m_EndPosition.m_Flags = CoursePosFlags.IsLeft | CoursePosFlags.IsRight | CoursePosFlags.FreeHeight;
                    }

                    if (m_CreationFlags != CreationFlags.Delete)
                    {
                        if (m_SelectedLookup.HasComponent(edge.m_Start) || m_CreationFlags == 0)
                        {
                            if (m_RotationAboutCenter != 0)
                            {
                                Game.Objects.Transform rotatedPosition = GetRotatedPosition(new Game.Objects.Transform() { m_Position = netCourse.m_StartPosition.m_Position, m_Rotation = netCourse.m_StartPosition.m_Rotation });
                                netCourse.m_StartPosition.m_Position = rotatedPosition.m_Position;
                                netCourse.m_StartPosition.m_Rotation = rotatedPosition.m_Rotation;
                                netCourse.m_Curve.b = GetRotatedPosition(new Game.Objects.Transform() { m_Position = curve.m_Bezier.b, m_Rotation = quaternion.identity }).m_Position;
                            }

                            netCourse.m_StartPosition.m_Position = GetTranslatedXZPosition(netCourse.m_StartPosition.m_Position);
                            netCourse.m_Curve.b = GetTranslatedXZPosition(netCourse.m_Curve.b);

                            if (m_FollowTerrain)
                            {
                                netCourse.m_StartPosition.m_Position = FollowTerrain(netCourse.m_StartPosition.m_Position, curve.m_Bezier.a);
                                netCourse.m_Curve.b = FollowTerrain(netCourse.m_Curve.b, curve.m_Bezier.b);
                            }

                            netCourse.m_Curve.a = netCourse.m_StartPosition.m_Position;
                        }

                        if (m_SelectedLookup.HasComponent(edge.m_End) || m_CreationFlags == 0)
                        {
                            if (m_RotationAboutCenter != 0)
                            {
                                Game.Objects.Transform rotatedPosition = GetRotatedPosition(new Game.Objects.Transform() { m_Position = netCourse.m_EndPosition.m_Position, m_Rotation = netCourse.m_EndPosition.m_Rotation });
                                netCourse.m_EndPosition.m_Position = rotatedPosition.m_Position;
                                netCourse.m_EndPosition.m_Rotation = rotatedPosition.m_Rotation;
                                netCourse.m_Curve.c = GetRotatedPosition(new Game.Objects.Transform() { m_Position = curve.m_Bezier.c, m_Rotation = quaternion.identity }).m_Position;
                            }

                            netCourse.m_EndPosition.m_Position = GetTranslatedXZPosition(netCourse.m_EndPosition.m_Position);
                            netCourse.m_Curve.c = GetTranslatedXZPosition(netCourse.m_Curve.c);

                            if (m_FollowTerrain)
                            {
                                netCourse.m_EndPosition.m_Position = FollowTerrain(netCourse.m_EndPosition.m_Position, curve.m_Bezier.a);
                                netCourse.m_Curve.c = FollowTerrain(netCourse.m_Curve.c, curve.m_Bezier.c);
                            }

                            netCourse.m_Curve.d = netCourse.m_EndPosition.m_Position;
                        }
                    }
                }

                if (m_NetElevationLookup.TryGetComponent(edge.m_Start, out Game.Net.Elevation startElevation))
                {
                    netCourse.m_StartPosition.m_Elevation = startElevation.m_Elevation;
                }

                if (m_NetElevationLookup.TryGetComponent(edge.m_Start, out Game.Net.Elevation endElevation))
                {
                    netCourse.m_EndPosition.m_Elevation = endElevation.m_Elevation;
                }

                buffer.AddComponent(definitionEntity, netCourse);
                m_NetworksProcessed.Add(originalInstance);
            }

            private void ProcessCreationDefinition(Entity originalInstance, Entity definitionEntity)
            {
                CreationDefinition creationDefinition = new()
                {
                    m_Flags = m_CreationFlags,
                };

                if ((m_CreationFlags & CreationFlags.Relocate) == CreationFlags.Relocate ||
                    (m_CreationFlags & CreationFlags.Delete) == CreationFlags.Delete)
                {
                    creationDefinition.m_Original = originalInstance;

                    // Not sure if this should be used for create or not.
                    if (m_AttachedLookup.HasComponent(originalInstance))
                    {
                        creationDefinition.m_Attached = m_AttachedLookup[originalInstance].m_Parent;
                    }
                }

                if (m_PrefabRefLookup.HasComponent(originalInstance))
                {
                    if (m_OwnerLookup.TryGetComponent(originalInstance, out Owner owner) &&
                        m_EditorContainterLookup.HasComponent(owner.m_Owner) &&
                        m_PrefabRefLookup.HasComponent(owner.m_Owner))
                    {
                        creationDefinition.m_Prefab = m_PrefabRefLookup[owner.m_Owner];
                        creationDefinition.m_SubPrefab = m_PrefabRefLookup[originalInstance];
                    }
                    else
                    {
                        creationDefinition.m_Prefab = new PrefabRef(m_PrefabRefLookup[originalInstance].m_Prefab);
                    }
                }

                if (m_PseudoRandomSeedLookup.HasComponent(originalInstance))
                {
                    creationDefinition.m_RandomSeed = m_PseudoRandomSeedLookup[originalInstance].m_Seed;
                }
                
                if (m_CurveLookup.HasComponent(originalInstance) &&
                    m_CreationFlags == CreationFlags.Relocate)
                {
                    // highlight in this situation. Noticed some instability after adding this, but could be a coincidence. Might just be spamming tool too fast.
                    // Just select give highlight, but not overriden greyed out when moving. May affect other validation checks too.
                    creationDefinition.m_Flags = CreationFlags.Recreate | CreationFlags.Parent;
                }

                buffer.AddComponent(definitionEntity, creationDefinition);
            }

            private Game.Objects.Transform GetRotatedPosition(Game.Objects.Transform originalTransform)
            {
                Game.Objects.Transform parentTransform = new Game.Objects.Transform(m_Centroid.m_Position, quaternion.RotateY(m_RotationAboutCenter));
                Game.Objects.Transform localTransform = new Game.Objects.Transform() { m_Position = originalTransform.m_Position - m_Centroid.m_Position, m_Rotation = originalTransform.m_Rotation };
                return ObjectUtils.LocalToWorld(parentTransform, localTransform);
            }

            private float3 GetTranslatedXZPosition(float3 position)
            {
                if (m_CreationFlags == CreationFlags.Relocate)
                {
                    position.x += m_EndPoint.m_Position.x - m_StartPoint.m_Position.x;
                    position.z += m_EndPoint.m_Position.z - m_StartPoint.m_Position.z;
                }
                else if (m_CreationFlags == 0)
                {
                    position.x += m_EndPoint.m_Position.x - m_Centroid.m_Position.x;
                    position.z += m_EndPoint.m_Position.z - m_Centroid.m_Position.z;
                }

                return position;
            }

            private float3 FollowTerrain(float3 newPosition, float3 originalPosition)
            {
                newPosition.y += TerrainUtils.SampleHeight(ref m_TerrainHeightData, newPosition) - TerrainUtils.SampleHeight(ref m_TerrainHeightData, originalPosition);
                return newPosition;
            }
        }

    }
}
