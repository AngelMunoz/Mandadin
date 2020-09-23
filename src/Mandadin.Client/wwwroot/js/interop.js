import { SwitchTheme, GetTheme } from '/js/theme.js'
import { CopyTextToClipboard, ReadTextFromClipboard } from '/js/clipboard.js'
import { CanShare, ShareContent } from '/js/share.js'
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
} from '/js/database.js';


(function(window) {
  window.Mandadin = window.Mandadin || {
    Theme: {
      SwitchTheme,
      GetTheme
    },
    Share: {
      CanShare,
      ShareContent
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
})(window)
