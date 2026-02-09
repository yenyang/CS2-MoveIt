using Colossal.IO.AssetDatabase.Internal;
using MoveIt.Moveables;
using MoveIt.Selection;
using MoveIt.Tool;
using QCommonLib;
using System.Collections.Generic;
using System.Linq;

namespace MoveIt.Actions.Select
{
    internal abstract class SelectBase : Action
    {
        public override string Name => "SelectBase";

        protected bool _IsAppend;

        /// <summary>
        /// Calculate what needs deselected, what needs reselected, save to live selection
        /// </summary>
        public override void Undo()
        {
            List<MVDefinition> fromSelection = _SelectionState.Definitions;
            List<MVDefinition> toSelection = _MIT.Queue.PrevAction.GetSelectionStates();
            ProcessSelectionChange(fromSelection, toSelection);
            base.Undo();
        }

        /// <summary>
        /// Calculate what needs deselected, what needs reselected, save to live selection
        /// </summary>
        public override void Redo()
        {
            List<MVDefinition> fromSelection = _MIT.Queue.PrevAction.GetSelectionStates();
            List<MVDefinition> toSelection = _SelectionState.Definitions;
            ProcessSelectionChange(fromSelection, toSelection);
            base.Redo();
        }

        /// <summary>
        /// Switch the current selection when traversing action history, calling OnSelect/OnDeselect as needed
        /// </summary>
        protected void ProcessSelectionChange(List<MVDefinition> fromSelection, List<MVDefinition> toSelection)
        {
            IEnumerable<MVDefinition> deselected = fromSelection.Except(toSelection);
            IEnumerable<MVDefinition> reselected = toSelection.Except(fromSelection);

            SelectionState newSelectionStates = new(_MIT.m_IsManipulateMode, toSelection);

            MoveItToolSystem.Log.Debug($"SB-{Name}.ProcessSelectionChange {QCommon.GetCallerDebug()}" +
                $"\n FromSelection: {MoveItToolSystem.DebugDefinitions(fromSelection)}" +
                $"\n   ToSelection: {MoveItToolSystem.DebugDefinitions(toSelection)}" +
                $"\n      Deselect: {MoveItToolSystem.DebugDefinitions(deselected)}" +
                $"\n      Reselect: {MoveItToolSystem.DebugDefinitions(reselected)}" +
                $"\n         Final: {MoveItToolSystem.DebugDefinitions(newSelectionStates.Definitions)}");

            _MIT.Selection = m_IsManipulationMode ? 
                new SelectionManip(newSelectionStates) : 
                new SelectionNormal(newSelectionStates);
            _MIT.Selection.Refresh();

            deselected.ForEach(mvd => _MIT.Moveables.GetIfExists<Moveable>(mvd)?.OnDeselect());
            reselected.ForEach(mvd => _MIT.Moveables.GetOrCreate<Moveable>(mvd).OnSelect());
        }
    }
}
