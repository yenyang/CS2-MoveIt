import { CheckboxData } from "mit-mainpanel/checkbox/cbData";
import { ReactNode } from "react";

// Main Foldout menu

export interface FoldoutData {
    Title: FOTitleData;
    Entries: FOMainEntryData[];
}

export interface FOTitleData
{
    Section : string;
    Id : string;
    LabelFallback : string | undefined;
    LabelLocaleKey: string;
    Tooltip?: ReactNode | string;
    Checkbox? : CheckboxData | undefined;
    CBWhenClosed? : boolean | undefined;
    CBWhenOpen? : boolean | undefined;
}

export interface FOEntryDataBase
{
    Section : string;
    Id : string;
    RawId : string;
    LabelFallback? : string | undefined;
    LabelLocaleKey : string;    
    Tooltip?: ReactNode | string;
    Checkbox? : CheckboxData | undefined;
}

export interface FOMainEntryData extends FOEntryDataBase
{
    Popout? : FOPopoutData;
}

// Popout submenu

export interface FOPopoutData
{
    Entries: FOPopoutEntryData[];
}

export interface FOPopoutEntryData extends FOEntryDataBase
{
    SubSection : string;
}
