import foStyles from "./foldout.module.scss";
import classNames from "classnames";
import { Button, HTMLImageElement, Icon, Tooltip } from "cs2/ui";
import { LabelMouseUp } from "bindings";
import { FOTitleData, FOMainEntryData } from "./foData";
import { FoldoutState, FOMainEntryState } from "./foState";
import { VanillaComponentResolver } from "classes/VanillaComponentResolver";
import { BuildCheckbox } from "mit-mainpanel/checkbox/checkbox";
import { useLocalization } from "cs2/l10n";
import { getModule } from "cs2/modding";

const FoldoutOpenSrc = `coui://ui-mods/images/icon_FoldoutOpen.svg`;
const FoldoutClosedSrc = `coui://ui-mods/images/icon_FoldoutClose.svg`;

export const FOTitleRow = (props: {data: FOTitleData, state: FoldoutState}) : JSX.Element => {
    
    // translation handling. Translates using locale keys that are defined in C# or fallback string from en-US.json.
    const { translate } = useLocalization();

    var showCB = props.data.Checkbox !== undefined && props.state.Title.Checkbox !== undefined;
    if (showCB)
    {
        showCB = props.state.IsOpen ? (props.data.CBWhenOpen === true) : (props.data.CBWhenClosed === true);
    }
    const classes = classNames({
        [foStyles.rowGroup]: true,
        [foStyles.title]: true,
        [foStyles.titleOpen]: props.state.IsOpen,
        [foStyles.titleClosed]: !props.state.IsOpen,
    });
    const labelClasses = classNames({
        [foStyles.label]: true,
        [foStyles.rowGroup]: true,
    });

    return (
             <Tooltip tooltip={props.data.Tooltip}>
                <div className={classes}>
                    {showCB &&  
                    (
                        <>
                            <div className={foStyles.checkbox}>
                                {BuildCheckbox(true, props.data.Section, props.data.Checkbox, props.state.Title.Checkbox)}
                            </div>
                        </>
                    )}      
                    <Button
                        className={foStyles.icon}                
                        focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                        onMouseUpCapture={(e) => LabelMouseUp(props.data.Section, props.data.Id, e.button)}
                        variant="icon"
                        src={props.data.Icon}
                    >
                    </Button>           
                    <Button
                        className={foStyles.titleButton}
                        id={props.data.Id}
                        focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                        onMouseUpCapture={(e) => LabelMouseUp(props.data.Section, props.data.Id, e.button)}
                        variant="icon">
                                <>
                                    <div className={labelClasses}>
                                        {translate(props.data.LabelLocaleKey, props.data.LabelFallback)}
                                    </div>
                                    <img src={props.state.IsOpen ? FoldoutOpenSrc : FoldoutClosedSrc} className={foStyles.foldoutButton}></img>
                                </>
                        
                    </Button>
                </div>
            </Tooltip>
    );
}

export interface FOEntryRowProps 
{

}

export const FOEntryRow = (props: {data: FOMainEntryData, state: FOMainEntryState, isOpen : boolean}) : JSX.Element => {
      // translation handling. Translates using locale keys that are defined in C# or fallback string from en-US.json.
    const { translate } = useLocalization();
    const classes = classNames({
        [foStyles.row]: true,
        [foStyles.entry]: true,
        [foStyles.active]: props.state.IsActive,
        [foStyles.inactive]: !props.state.IsActive,
    });
    const showCB = props.data.Checkbox !== undefined && props.state.Checkbox !== undefined;
    const showPO = false;//data.Popout !== undefined && state.Popout !== undefined;
    const labelClasses = classNames({
        [foStyles.label]: true,
        [foStyles.labelCB]: showCB,
        [foStyles.labelPO]: showPO,
        [foStyles.rowGroup]: true,
    })

    return (
        <>
            {props.isOpen && (
                <Tooltip tooltip={props.data.Tooltip}>
                    <div className={classes}>
                        {showCB && (<div
                            className={foStyles.checkbox}
                            onMouseUp={(e) => LabelMouseUp(props.data.Section, props.data.RawId, e.button)}
                            >
                                {BuildCheckbox(false, props.data.Section, props.data.Checkbox, props.state.Checkbox)}
                            </div>
                        )}
                        
                        <Button
                            className={foStyles.icon}                
                            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                            onMouseUpCapture={(e) => LabelMouseUp(props.data.Section, props.data.RawId, e.button)}
                            variant="icon"
                            src={props.data.Icon}
                        >
                        </Button>
                        <div
                            className={labelClasses}
                            onMouseUp={(e) => LabelMouseUp(props.data.Section, props.data.RawId, e.button)}
                            >
                                <>
                                    {translate(props.data.LabelLocaleKey, props.data.LabelFallback)}          
                                </>
                        </div>
                    </div>
                </Tooltip>
            )}
        </>
    );
}
