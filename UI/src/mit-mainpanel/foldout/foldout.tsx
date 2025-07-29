import foStyles from "./foldout.module.scss";
import { FoldoutData } from "./foData";
import { FoldoutState } from "./foState";
import { FOEntryRow, FOTitleRow } from "./foRow";

export const Foldout = (props : {data: FoldoutData, state: FoldoutState})  : JSX.Element => {
    return (  
        <div className={foStyles.container}>
            <FOTitleRow data={props.data.Title} state={props.state}></FOTitleRow>
            <div className={foStyles.entries}>
            {
                props.data.Entries.map((entry, index) => (
                    <FOEntryRow
                        data ={entry}
                        state ={props.state.Entries[index]}
                        isOpen={props.state.IsOpen}
                    ></FOEntryRow>
                ))
            }
            </div>
        </div>
    );
}