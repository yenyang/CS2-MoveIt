import { ReactNode, useState } from "react";
import classNames from "classnames";
import { Button, Tooltip } from "cs2/ui";
import { VanillaComponentResolver } from "classes/VanillaComponentResolver";
import { ButtonState, TopRowButtonStates } from "mit-mainpanel/panelState";
import { ButtonPressed } from "bindings";
import locale from "../lang/en-US.json";
import mod from "../../mod.json";

import followTerrainActiveSrc from "../img/NetworkGround_Active.svg";
import followTerrainOffSrc from "../img/NetworkGround_Off.svg";
import copyActiveSrc from "../img/RectangleCopy_Active.svg";
import copyOffSrc from "../img/RectangleCopy_Off.svg";
import deleteOffSrc from "../img/Trash_Off.svg";
import deleteDisabledSrc from "../img/Trash_Disabled.svg";
import copyDisabledSrc from "../img/RectangleCopy_Disabled.svg";
import followTerrainDisabledSrc from "../img/NetworkGround_Disabled.svg"


import styles from "./panel.module.scss";
// Ugly code to force these images to build
import ic0 from "../img/icon_Undo_Off.svg";
import ic1 from "../img/icon_Single_Off.svg";
import ic2 from "../img/icon_Marquee_Off.svg";
import ic3 from "../img/icon_Manipulation_Off.svg";
import ic4 from "../img/icon_Redo_Off.svg";
import ic5 from "../img/icon_Undo_Disabled.svg";
import ic6 from "../img/icon_Single_Active.svg";
import ic7 from "../img/icon_Marquee_Active.svg";
import ic8 from "../img/icon_Manipulation_Active.svg";
import ic9 from "../img/icon_Redo_Disabled.svg";
import icA from "../img/icon_FoldoutOpen.svg";
import icB from "../img/icon_FoldoutClose.svg";
import icC from "../img/icon_PopoutOpen.svg";
import icD from "../img/icon_PopoutClose.svg";
import { getModule } from "cs2/modding";
import { useLocalization } from "cs2/l10n";
import { ModeButton } from "./modeButton";
import { trigger } from "cs2/api";

export const ButtonRowTop = (props: {topRowState : TopRowButtonStates}) => {
    const classes = classNames({
        [styles.row]: true,
        [styles.buttonRow]: true,
    });

    // translation handling. Translates using locale keys that are defined in C# or fallback string from en-US.json.
    const { translate } = useLocalization();

    // Ugly code to force these images to build
    var x = ic0; x = ic1; x = ic2; x = ic3; x = ic4; x = ic5; x = ic6; 
    x = ic7; x = ic8; x = ic9; x = icA; x = icB; x = icC; x = icD;

    return (
        <>
            <div className={classes}>
                {ButtonRowButton(TopButtonsData[0], props.topRowState.ButtonUndo, DescriptionTooltip( translate("MoveIt.TOOLTIP_TITLE[Undo]", locale["MoveIt.TOOLTIP_TITLE[Undo]"]), translate("MoveIt.TOOLTIP_DESCRIPTION[Undo]", locale["MoveIt.TOOLTIP_DESCRIPTION[Undo]"]) ))}
                <div className={styles.separator}></div>
                {ButtonRowButton(TopButtonsData[1], props.topRowState.ButtonSingle, DescriptionTooltip( translate("MoveIt.TOOLTIP_TITLE[Single]", locale["MoveIt.TOOLTIP_TITLE[Single]"]), translate("MoveIt.TOOLTIP_DESCRIPTION[Single]", locale["MoveIt.TOOLTIP_DESCRIPTION[Single]"]) ))}
                <div className={styles.separator}></div>
                {ButtonRowButton(TopButtonsData[2], props.topRowState.ButtonMarquee, DescriptionTooltip( translate("MoveIt.TOOLTIP_TITLE[Marquee]", locale["MoveIt.TOOLTIP_TITLE[Marquee]"]), translate("MoveIt.TOOLTIP_DESCRIPTION[Marquee]", locale["MoveIt.TOOLTIP_DESCRIPTION[Marquee]"]) ))}
                <div className={styles.separator}></div>
                {ButtonRowButton(TopButtonsData[3], props.topRowState.ButtonManipulation, DescriptionTooltip( translate("MoveIt.TOOLTIP_TITLE[Manipulation]", locale["MoveIt.TOOLTIP_TITLE[Manipulation]"]), translate("MoveIt.TOOLTIP_DESCRIPTION[Manipulation]", locale["MoveIt.TOOLTIP_DESCRIPTION[Manipulation]"]) ))}
                <div className={styles.separator}></div>
                <ModeButton data={{Id: "FollowTerrain", OffSrc: followTerrainOffSrc, DisabledSrc: followTerrainDisabledSrc, ActiveSrc: followTerrainActiveSrc}} state={props.topRowState.ButtonFollowTerrain}></ModeButton>
                <span className={styles.separator}></span>
                <ModeButton data={{Id: "Copy", OffSrc: copyOffSrc, DisabledSrc: copyDisabledSrc, ActiveSrc: copyActiveSrc}} state={props.topRowState.ButtonCopy}></ModeButton>
                <span className={styles.separator}></span>
                <ModeButton data={{Id: "Delete", OffSrc: deleteOffSrc, DisabledSrc: deleteDisabledSrc, ActiveSrc: deleteOffSrc, OnMouseEnter:() => trigger(mod.id, "DeleteMouseEnter"), OnMouseLeave: () => trigger(mod.id, "DeleteMouseLeave")}} state={props.topRowState.ButtonDelete} ></ModeButton>
                <span className={styles.separator}></span>
                {ButtonRowButton(TopButtonsData[4], props.topRowState.ButtonRedo, DescriptionTooltip( translate("MoveIt.TOOLTIP_TITLE[Redo]", locale["MoveIt.TOOLTIP_TITLE[Redo]"]), translate("MoveIt.TOOLTIP_DESCRIPTION[Redo]", locale["MoveIt.TOOLTIP_DESCRIPTION[Redo]"]) ))}
            </div>
        </>
    );
}



export const descriptionToolTipStyle = getModule("game-ui/common/tooltip/description-tooltip/description-tooltip.module.scss", "classes");


// This is working, but it's possible a better solution is possible.
export function DescriptionTooltip(tooltipTitle: string | null, tooltipDescription: string | null) : JSX.Element {
    return (
        <>
            <div className={descriptionToolTipStyle.title}>{tooltipTitle}</div>
            <div className={descriptionToolTipStyle.content}>{tooltipDescription}</div>
        </>
    );
}

function ButtonRowButton(data : ButtonData, state : ButtonState, tooltip: ReactNode)
{   
    
    return (
        <div className={styles.buttonContainer}>
        <Tooltip tooltip={tooltip}>
        <Button
            disabled={!state.IsEnabled}
            className={styles.button}
            src={data.GetIconPath(state)}
            id={data.Id}
            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
            onSelect={() => ButtonPressed(data.Section, data.Id)}
            variant="icon" />
        </Tooltip>
        </div>
    );
}

class ButtonData
{
    Section : string;
    Id : string;
    Icon : string;

    constructor(section : string, id : string, icon : string)
    {
        this.Section = section;
        this.Id = id;
        this.Icon = icon;
    }

    public GetIconPath(state : ButtonState) : string
    {
        if (this.Icon === null || this.Icon === "") return "";
        let postfix = !state.IsEnabled ? "Disabled" : state.IsActive ? "Active" : "Off";
    
        return `coui://ui-mods/images/icon_${this.Icon}_${postfix}.svg`;
    }
}

const TopButtonsData : ButtonData[] = [
    new ButtonData("toprow",    "undo",          "Undo"),
    new ButtonData("toprow",    "single",        "Single"),
    new ButtonData("toprow",    "marquee",       "Marquee"),
    new ButtonData("toprow",    "manipulation",  "Manipulation"),
    new ButtonData("toprow",    "redo",          "Redo"),
];

