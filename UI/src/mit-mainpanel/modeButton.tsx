
import locale from "../lang/en-US.json";
import { useLocalization } from "cs2/l10n";
import styles from "./panel.module.scss";
import classNames from "classnames";
import { Button, Tooltip } from "cs2/ui";
import { DescriptionTooltip } from "./buttonRow";
import { VanillaComponentResolver } from "classes/VanillaComponentResolver";
import { ButtonPressed } from "bindings";
import { ButtonState } from "./panelState";
import { uilStandard } from "./toolboxFoldout";

export const uilColored =                          "coui://uil/Colored/";
export const uilDark =                          "coui://uil/Dark/";

export interface ModeButtonProps 
{
    Id : string,
    DisabledSrc: string,
    OffSrc: string,
    ActiveSrc: string,
}

export const ModeButton = (props: { data : ModeButtonProps, state : ButtonState}) => 
{
        // translation handling. Translates using locale keys that are defined in C# or fallback string from en-US.json.
    const { translate } = useLocalization();

    return (
        <div className={styles.buttonContainer}>
            <Tooltip tooltip={DescriptionTooltip(translate(`MoveIt.TOOLTIP_TITLE[${props.data.Id}]`),translate(`MoveIt.TOOLTIP_DESCRIPTION[${props.data.Id}]`)) }>
                <Button
                    disabled={false}
                    className={classNames(styles.button)}
                    id={props.data.Id}
                    src={!props.state.IsEnabled ? props.data.DisabledSrc : props.state.IsActive ? props.data.ActiveSrc : props.data.OffSrc}
                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                    onSelect={() => ButtonPressed("toprow", props.data.Id)}
                    variant="icon"
                >
                </Button>
            </Tooltip>
        </div>
    );
}