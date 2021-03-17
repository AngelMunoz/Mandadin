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


const channel = new BroadcastChannel("share-target");
channel.onmessage = function (event) {
    const parseRow = row => {
        const split = row.split(' ] ');
        if (split.length !== 2)
            throw new Error("List is not well formed");
        const isDone = split[0].contains('x')
        const name =
            split[1].trim();
        return [isDone, name];
    };

    console.debug(event.data);
    if (event.data && event.data.text) {
        const rows = event.data.text.split('\n').map(parseRow);
        ImportList(event.data.title || "Sin Titulo", rows)
            .catch(console.error);
    }
};