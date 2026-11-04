using Colossal.Mathematics;
using Game.Net;
using MoveIt.Moveables;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace MoveIt.Actions.Transform
{
#if USE_BURST
    [BurstCompile]
#endif
    internal struct NeighboursJob : IJobFor
    {
        public NativeList<Neighbour> m_Neighbours;
        public ComponentLookup<Curve> gnCurve;

        public void Execute(int index)
        {
            Neighbour neighbour = m_Neighbours[index];
            RefRW<Curve> component = gnCurve.GetRefRW(neighbour.m_Entity);
            Bezier4x3 curve = component.ValueRO.m_Bezier;

            component.ValueRW.m_Bezier = Reshape(neighbour.m_InitialCurve, curve.a, curve.d);
        }

        /// <summary>
        /// Fit the old curve to the new ends, keeping its shape
        /// </summary>
        internal static Bezier4x3 Reshape(Bezier4x3 initial, float3 newStart, float3 newEnd)
        {
            float2 oldChord = initial.d.xz - initial.a.xz;
            float2 newChord = newEnd.xz - newStart.xz;
            float lengthSq = math.lengthsq(oldChord);

            // Ends under 10 cm apart seen from above, so just move each handle with its end
            if (lengthSq < 0.01f || math.lengthsq(newChord) < 0.01f)
            {
                return new(
                    newStart,
                    initial.b + newStart - initial.a,
                    initial.c + newEnd - initial.d,
                    newEnd);
            }

            // How much the road turns and stretches seen from above
            float2 turn = new float2(
                math.dot(newChord, oldChord),
                oldChord.x * newChord.y - oldChord.y * newChord.x) / lengthSq;
            float rise = (newEnd.y - newStart.y) - (initial.d.y - initial.a.y);

            // The handles take a third and two thirds of the height change, like the road tool
            return new(
                newStart,
                newStart + MoveHandle(initial.b - initial.a, turn, rise / 3f),
                newStart + MoveHandle(initial.c - initial.a, turn, rise * 2f / 3f),
                newEnd);
        }

        private static float3 MoveHandle(float3 offset, float2 turn, float rise)
        {
            return new(
                turn.x * offset.x - turn.y * offset.z,
                offset.y + rise,
                turn.y * offset.x + turn.x * offset.z);
        }
    }
}
