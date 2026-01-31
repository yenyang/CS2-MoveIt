using MoveIt.Moveables;
using MoveIt.Selection;
using MoveIt.Tool;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Jobs;

namespace MoveIt.Actions.Select
{
    internal class SelectAction : SelectBase
    {
        public override string Name => "SelectAction";

        /// <summary>
        /// Was the player in Manipulation Mode when action was created, including quick select (holding Alt)?
        /// </summary>
        internal readonly bool m_IsManipulating;

        private readonly bool _IsForChild;

        public SelectAction() : base()
        {
            _IsAppend = false;
            _IsForChild = false;
        }

        /// <summary>
        /// Constructor for SelectAction
        /// </summary>
        /// <param name="isManipulating">Is the player in Manipulation mode at the time of creation?</param>
        /// <param name="append">Should this be added to existing selection, or should new one be made?</param>
        /// <param name="isForChild">Is the object being added a manipulatable child?</param>
        internal SelectAction(bool isManipulating, bool append, bool isForChild = false) : base()
        {
            m_IsManipulating = isManipulating;
            _IsAppend = append;
            _IsForChild = isForChild;
        }

        public override void Do(ref JobHandle jobHandle, ref EntityCommandBuffer buffer)
        {
            base.Do(ref jobHandle, ref buffer);

            _MIT.Selection = m_IsManipulating ? new SelectionManip(_MIT.Selection) : new SelectionNormal(_MIT.Selection);

            if (!_IsAppend &&
               (_MIT.m_Workflow[(int)Workflow.DeselectAll] != WorkflowProgression.NotStarted ||
               (_MIT.m_Workflow[(int)Workflow.SelectingIndividual] != WorkflowProgression.NotStarted &&
                !_MIT.Hovered.IsSelected)))
            {
                if (m_IsManipulating && _IsForChild)
                {
                    // New Manip selection, selecting child object so keep existing parents
                    HashSet<MVDefinition> toRemove = new();
                    toRemove = _MIT.Selection.Definitions.Where(mvd => _MIT.Moveables.GetOrCreate<Moveable>(mvd).IsManipChild).ToHashSet();
                    _MIT.Selection.Remove(toRemove, false);
                    Deselect(toRemove);
                }
                else
                {
                    // New selection, wipe everything
                    _MIT.Selection.Clear(); // I think was causing problem with moving single selection if conditional wasn't added.  
                    _MIT.Moveables.Refresh();                 
                }
            }

            Phase = Phases.Complete;
        }

        public void AddHovered(bool append)
        {
            if (_MIT.Hovered.IsManipulatable != _MIT.IsManipulating) return;
            _MIT.Selection.ProcessAdd(_MIT.Hovered.Definition, append);
            _MIT.SetWorkflowInProgess(Tool.Workflow.SelectingIndividual);
        }
    }
}
