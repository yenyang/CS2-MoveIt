using MoveIt.Actions.Transform;
using MoveIt.Components;
using MoveIt.Overlays.Children;
using MoveIt.Tool;
using QCommonLib;
using Unity.Entities;

namespace MoveIt.Moveables
{
    public class MVManipSegment : MVSegment
    {
        public override bool IsManipulatable => true;

        public MVManipSegment(Entity e) : base(e, Identity.Segment)
        {
            m_CPDefinitions = new();
            for (short i = 0; i < CURVE_CPS; i++)
            {
                MVDefinition mvd = new(Identity.ControlPoint, Entity.Null, IsManipulatable, true, m_Entity, m_Identity, i);
                //QLog.Debug($"{i} MVMSeg.ctor1 cp:{mvd}");
                MVControlPoint cp = _MIT.ControlPointManager.GetOrCreateMoveable(mvd);
                m_CPDefinitions.Add(cp.Definition);
                //QLog.Debug($"{i} MVMSeg.ctor2 cp:{mvd}");
            }

            m_Overlay = new OverlayManipSegment(this);
            RefreshFromAbstract();
        }

        internal override void MoveIt(TransformBase action, State state, bool move, bool rotate)
        {
            MoveItToolSystem.Log.Error($"Attempted to move ManipulateSegment {m_Entity.D()}");
        }


        public override void OnSelect()
        {
            //MIT.Log.Debug($"{m_Entity.D()} {Name} OnSelect");
            m_Overlay.AddFlag(InteractionFlags.Selected);
            foreach (Moveable mv in GetChildMoveablesForOverlays<Moveable>())
            {
                mv.m_Overlay.AddFlag(IsManipulatable ? InteractionFlags.ParentManipulating : InteractionFlags.ParentSelected);
            }
        }
    }
}
