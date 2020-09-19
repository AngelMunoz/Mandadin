import { SwitchTheme, GetTheme } from '/js/theme.js'
import { CopyTextToClipboard, ReadTextFromClipboard } from '/js/clipboard.js'
import { CanShare, ShareContent } from '/js/share.js'
import {
  FindNotes,
  CreateNote,
  UpdateNote,
  FindNote,
  DeleteNote
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
      DeleteNote
    }
  };
  console.info(window.Mandadin);

  // window.Mandadin.Database.SaveNote("Alv2")
  //     .then(result => {
  //         console.log(result);
  //         return window.Mandadin.Database.FindNotes();
  //     });

})(window)