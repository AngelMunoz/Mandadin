import { SwitchTheme, GetTheme } from './theme.js';
import { CopyTextToClipboard, ReadTextFromClipboard } from './clipboard.js';
import { CanShare, ShareContent, ImportShareData } from './share.js';
import {
    FindNotes,
    CreateNote,
    UpdateNote,
    FindNote,
    DeleteNote,
    FindLists,
    CreateList,
    ImportList,
    ListNameExists,
    DeleteList,
    GetListItems,
    ListItemExists,
    CreateListItem,
    UpdateListItem,
    DeleteListItem,
    GetHideDone,
    SaveHideDone
} from './database.js';

import { HasOverlayControls } from './overlay-controls.js';


(function(window) {
    window.Mandadin = window.Mandadin || {
        Theme: {
            SetDocumentTitle(title) { document.title = title || "Mandadin 4" },
            SwitchTheme,
            GetTheme,
            HasOverlayControls
        },
        Share: {
            CanShare,
            ShareContent,
            ImportShareData
        },
        Clipboard: {
            CopyTextToClipboard,
            ReadTextFromClipboard
        },
        Database: {
            FindNotes,
            CreateNote,
            UpdateNote,
            FindNote,
            DeleteNote,
            FindLists,
            CreateList,
            ImportList,
            ListNameExists,
            DeleteList,
            GetListItems,
            ListItemExists,
            CreateListItem,
            UpdateListItem,
            DeleteListItem,
            GetHideDone,
            SaveHideDone
        }
    };
    console.info(window.Mandadin);
})(window);