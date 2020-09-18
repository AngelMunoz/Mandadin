import {
    FindNotes,
    SaveNote,
    FindNote,
} from '/js/database.js';

(function(window) {
    window.Mandadin = window.Mandadin || {
        Theme: {
            /**
             * interacts with the HTML Element to switch classes
             * @param {'Light' | 'Dark'} theme
             */
            SwitchTheme(theme) {
                const html = document.querySelector('html');
                switch (theme) {
                    case 'Dark':
                        if (html.classList.contains('dark')) { return; }
                        html.classList.add('dark');
                        break;

                    case 'Light':
                        if (!html.classList.contains('dark')) { return; }
                        html.classList.remove('dark');
                        break;
                }
            }
        },
        ShareAPI: {

        },
        ClipboardAPI: {

        },
        Database: {
            FindNotes,
            SaveNote,
            FindNote
        }
    };
    console.info(window.Mandadin);

    // window.Mandadin.Database.SaveNote("Alv2")
    //     .then(result => {
    //         console.log(result);
    //         return window.Mandadin.Database.FindNotes();
    //     });

})(window)