
import { VanillaComponentResolver } from "classes/VanillaComponentResolver";
import { bindValue, trigger, useValue } from "cs2/api";
import { Portal, Panel, PanelSection, FormattedParagraphs, Button, Tooltip } from "cs2/ui";
import { MIT_RebindExistingMsg$, MIT_ShowRebindConfirm$ } from "bindings"
import icon from "../img/MoveIt_Active.svg";
import stylesMain from "../mit-mainpanel/panel.module.scss";
import styles from "./rebindConfirm.module.scss";
import mod from "../../mod.json";
import locale from "../lang/en-US.json";
import { useLocalization } from "cs2/l10n";

function ButtonPressed(doRebind : boolean)
{
    trigger(mod.id, "MIT_ShowRebindConfirm", doRebind);
}

export const MIT_RebindConfirm = () =>
{
    const showRebindConfirm = useValue(MIT_ShowRebindConfirm$);
    const rebindExistingMsg = useValue(MIT_RebindExistingMsg$);

    // translation handling. Translates using locale keys that are defined in C# or fallback string here.
    const { translate } = useLocalization();

    if (!showRebindConfirm) return null;

    return (
        <>
            <Portal>
                <Panel
                    draggable
                    className={styles.panel}
                    header={(
                        <div className={styles.header}>
                            <img src={icon} className={stylesMain.headerIcon} />
                            <span className={stylesMain.headerText}>{translate("MoveIt.SECTION_TITLE[UseMKey]", locale["MoveIt.SECTION_TITLE[UseMKey]"])}</span>
                        </div>
                    )}>
                    <PanelSection className={styles.section}>
                        <FormattedParagraphs>
                            {rebindExistingMsg}
                        </FormattedParagraphs>
                    {/* </PanelSection>
                    <PanelSection className={styles.section}> */}
                        <div className={styles.buttonRow}>
                            <div className={styles.buttonContainer}>
                            <Tooltip tooltip={translate("MoveIt.TOOLTIP_DESCRIPTION[YesUseMKey]", locale["MoveIt.TOOLTIP_DESCRIPTION[YesUseMKey]"])}>
                            <Button
                                className={styles.button}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                onSelect={() => ButtonPressed(true)}
                                variant="primary">{translate("MoveIt.TEXT_LABEL[Yes]", locale["MoveIt.TEXT_LABEL[Yes]"])}</Button>
                            </Tooltip>
                            </div>
                            <div className={styles.buttonContainer}>
                            <Tooltip tooltip={translate("MoveIt.TOOLTIP_DESCRIPTION[NoUseMKey]", locale["MoveIt.TOOLTIP_DESCRIPTION[NoUseMKey]"])}>
                            <Button
                                className={styles.button}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                onSelect={() => ButtonPressed(false)}
                                variant="primary">{translate("MoveIt.TEXT_LABEL[No]", locale["MoveIt.TEXT_LABEL[No]"])}</Button>
                            </Tooltip>
                            </div>
                        </div>
                    </PanelSection>
                </Panel>
            </Portal>
        </>
    )
}