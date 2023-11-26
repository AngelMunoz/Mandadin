import { SwitchTheme, GetTheme } from "./theme.js";
import { CopyTextToClipboard, ReadTextFromClipboard } from "./clipboard.js";
import { CanShare, ShareContent, ImportShareData } from "./share.js";
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
  SaveHideDone,
} from "./database.js";

import { HasOverlayControls } from "./overlay-controls.js";

// Ensure we have the right theme
// when we load the script
GetTheme();

(function (window) {
  window.Mandadin = window.Mandadin || {
    Theme: {
      SwitchTheme,
      GetTheme,
      HasOverlayControls,
    },
    Share: {
      CanShare,
      ShareContent,
      ImportShareData,
    },
    Clipboard: {
      CopyTextToClipboard,
      ReadTextFromClipboard,
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
      SaveHideDone,
    },
    Elements: {
      GetValue(elt) {
        if (elt) {
          return elt.value;
        }
        return "";
      },
    },
  };
})(window);
