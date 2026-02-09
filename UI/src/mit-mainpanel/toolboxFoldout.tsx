import { FOMainEntryData, FoldoutData, FOTitleData, FOPopoutData } from "./foldout/foData";
import { FoldoutState } from "./foldout/foState";
import locale from "../lang/en-US.json";
import { useLocalization } from "cs2/l10n";
import { Foldout } from "./foldout/foldout";


export const uilStandard =                          "coui://uil/Standard/";
const toolSrc =                 uilStandard + "Tools.svg";
const terrainHeightSrc = uilStandard + "NetworkGround.svg";
const objectHeightSrc = uilStandard + "NoHeightLimit.svg";
const rotateAtCenterSrc = uilStandard + "RotateAroundLeft.svg";
const rotateInPlaceSrc = uilStandard +"ArrowCircularLeft.svg";

export const ToolboxFoldout = (props: {state: FoldoutState}) : JSX.Element =>
{

    // translation handling. Translates using locale keys that are defined in C# or fallback string from en-US.json.
    const { translate } = useLocalization();

    const ToolboxTitle : FOTitleData = 
    {
        Section: "toolbox", 
        Id: "toolboxTitle",
        LabelLocaleKey: "MoveIt.TEXT_LABEL[Toolbox]", 
        LabelFallback: locale["MoveIt.TEXT_LABEL[Toolbox]"], 
        Tooltip: translate("MoveIt.TOOLTIP_DESCRIPTION[Toolbox]", locale["MoveIt.TOOLTIP_DESCRIPTION[Toolbox]"]),
        Icon: toolSrc,
    }

    const TerrainHeight : FOMainEntryData = 
    {
        Section: "toolbox",
        RawId: "terrainHeight",
        Id: "terrainHeightRow",
        LabelLocaleKey: "MoveIt.TEXT_LABEL[TerrainHeight]",
        LabelFallback: locale["MoveIt.TEXT_LABEL[TerrainHeight]"],
        Tooltip: translate("MoveIt.TOOLTIP_DESCRIPTION[TerrainHeight]", locale["MoveIt.TOOLTIP_DESCRIPTION[TerrainHeight]"]),
        Icon: terrainHeightSrc,
    }

    const ObjectHeight : FOMainEntryData = 
    {
        Section: "toolbox",
        RawId: "objectHeight",
        Id: "objectHeightRow",
        LabelLocaleKey: "MoveIt.TEXT_LABEL[ObjectHeight]",
        LabelFallback: locale["MoveIt.TEXT_LABEL[ObjectHeight]"],
        Tooltip: translate("MoveIt.TOOLTIP_DESCRIPTION[ObjectHeight]", locale["MoveIt.TOOLTIP_DESCRIPTION[ObjectHeight]"]),
        Icon: objectHeightSrc,
    }

    const RotateAtCenter : FOMainEntryData = 
    {
        Section: "toolbox",
        RawId: "rotateAtCenter",
        Id: "rotateAtCentreRow",
        LabelLocaleKey: "MoveIt.TEXT_LABEL[RotateAtCenter]",
        LabelFallback: locale["MoveIt.TEXT_LABEL[RotateAtCenter]"],
        Tooltip: translate("MoveIt.TOOLTIP_DESCRIPTION[RotateAtCenter]", locale["MoveIt.TOOLTIP_DESCRIPTION[RotateAtCenter]"]),
        Icon: rotateAtCenterSrc,
    }

    const RotateInPlace : FOMainEntryData = 
    {
        Section: "toolbox",
        RawId: "rotateInPlace",
        Id: "rotateInPlace",
        LabelLocaleKey: "MoveIt.TEXT_LABEL[RotateInPlace]",
        LabelFallback: locale["MoveIt.TEXT_LABEL[RotateInPlace]"],
        Tooltip: translate("MoveIt.TOOLTIP_DESCRIPTION[RotateInPlace]", locale["MoveIt.TOOLTIP_DESCRIPTION[RotateInPlace]"]),
        Icon: rotateInPlaceSrc,
    }

    const data : FoldoutData = 
    {
        Title: ToolboxTitle,
        Entries : [TerrainHeight, ObjectHeight, RotateAtCenter, RotateInPlace],
    }

    return (
        <>
            <Foldout
                data={data}
                state={props.state}
            ></Foldout>
        </>
    );
}