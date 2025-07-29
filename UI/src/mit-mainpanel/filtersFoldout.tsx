import { CheckboxData } from "./checkbox/cbData";
import { FOMainEntryData, FOTitleData, FoldoutData } from "./foldout/foData";
import { FoldoutState } from "./foldout/foState";
import locale from "../lang/en-US.json";
import { useLocalization } from "cs2/l10n";
import { Foldout } from "./foldout/foldout";

export const FiltersFoldout = (props: {state: FoldoutState}) : JSX.Element => {
    // translation handling. Translates using locale keys that are defined in C# or fallback string from en-US.json.
    const { translate } = useLocalization();

    const FilterTitle : FOTitleData = 
    {
        Section: "filters", 
        Id: "filtersAll",
        LabelLocaleKey: "MoveIt.TEXT_LABEL[Filters]", 
        LabelFallback: locale["MoveIt.TEXT_LABEL[Filters]"], 
        Tooltip: translate("MoveIt.TOOLTIP_DESCRIPTION[Filters]", locale["MoveIt.TOOLTIP_DESCRIPTION[Filters]"]),
        Checkbox: {Id : "filtersAll"}, 
        CBWhenClosed: false, 
        CBWhenOpen: true,
    }

    const Buildings : FOMainEntryData = 
    {
        Section: "filters",
        RawId: "buildings",
        Id: "buildingsRow",
        LabelLocaleKey: "MoveIt.TEXT_LABEL[Buildings]",
        LabelFallback: locale["MoveIt.TEXT_LABEL[Buildings]"],
        Tooltip: translate("MoveIt.TOOLTIP_DESCRIPTION[Buildings]", locale["MoveIt.TOOLTIP_DESCRIPTION[BuildingsFilter]"]),
        Checkbox: { Id: "buildings"},
    }


    const Plants : FOMainEntryData = 
    {
        Section: "filters",
        RawId: "plants",
        Id: "plantsRow",
        LabelLocaleKey: "MoveIt.TEXT_LABEL[Plants]",
        LabelFallback: locale["MoveIt.TEXT_LABEL[Plants]"],
        Tooltip: translate("MoveIt.TOOLTIP_DESCRIPTION[Plants]", locale["MoveIt.TOOLTIP_DESCRIPTION[PlantsFilter]"]),
        Checkbox: {Id: "plants"},
    }

    const Decals : FOMainEntryData = 
    {
        Section: "filters",
        RawId: "decals",
        Id: "decalsRow",
        LabelLocaleKey: "MoveIt.TEXT_LABEL[Decals]",
        LabelFallback: locale["MoveIt.TEXT_LABEL[Decals]"],
        Tooltip: translate("MoveIt.TOOLTIP_DESCRIPTION[Decals]", locale["MoveIt.TOOLTIP_DESCRIPTION[DecalsFilter]"]),
        Checkbox: {Id: "decals"},
    }

    const Props : FOMainEntryData = 
    {
        Section: "filters",
        RawId: "props",
        Id: "propsRow",
        LabelLocaleKey: "MoveIt.TEXT_LABEL[Props]",
        LabelFallback: locale["MoveIt.TEXT_LABEL[Props]"],
        Tooltip: translate("MoveIt.TOOLTIP_DESCRIPTION[Props]", locale["MoveIt.TOOLTIP_DESCRIPTION[PropsFilter]"]),
        Checkbox: {Id: "props"},
    }

    const Surfaces : FOMainEntryData = 
    {
        Section: "filters",
        RawId: "surfaces",
        Id: "surfacesRow",
        LabelLocaleKey: "MoveIt.TEXT_LABEL[Surfaces]",
        LabelFallback: locale["MoveIt.TEXT_LABEL[Surfaces]"],
        Tooltip: translate("MoveIt.TOOLTIP_DESCRIPTION[Surfaces]", locale["MoveIt.TOOLTIP_DESCRIPTION[SurfacesFilter]"]),
        Checkbox: {Id: "surfaces"},
    }

    const Nodes : FOMainEntryData = 
    {
        Section: "filters",
        RawId: "nodes",
        Id: "nodesRow",
        LabelLocaleKey: "MoveIt.TEXT_LABEL[Nodes]",
        LabelFallback: locale["MoveIt.TEXT_LABEL[Nodes]"],
        Tooltip: translate("MoveIt.TOOLTIP_DESCRIPTION[Nodes]", locale["MoveIt.TOOLTIP_DESCRIPTION[NodesFilter]"]),
        Checkbox: {Id: "nodes"},
    }

    const Segments : FOMainEntryData = 
    {
        Section: "filters",
        RawId: "segments",
        Id: "segmentsRow",
        LabelLocaleKey: "MoveIt.TEXT_LABEL[Segments]",
        LabelFallback: locale["MoveIt.TEXT_LABEL[Segments]"],
        Tooltip: translate("MoveIt.TOOLTIP_DESCRIPTION[Segments]", locale["MoveIt.TOOLTIP_DESCRIPTION[SegmentsFilter]"]),
        Checkbox: {Id: "segments"},
    }

    const NetLanes : FOMainEntryData = 
    {
        Section: "filters",
        RawId: "netlanes",
        Id: "netlanesRow",
        LabelLocaleKey: "MoveIt.TEXT_LABEL[NetLanes]",
        LabelFallback: locale["MoveIt.TEXT_LABEL[NetLanes]"],
        Tooltip: translate("MoveIt.TOOLTIP_DESCRIPTION[NetLanes]", locale["MoveIt.TOOLTIP_DESCRIPTION[NetLanesFilter]"]),
        Checkbox: {Id: "netlanes"},
    }

    const data : FoldoutData = 
    {
        Title: FilterTitle,
        Entries : [Buildings, Plants, Decals, Props, Surfaces, Nodes, Segments, NetLanes],
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