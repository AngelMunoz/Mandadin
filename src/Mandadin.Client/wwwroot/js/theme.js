/**
 * @type {PouchDB.Database<{ theme: string }>}
 */
const themedb = new PouchDB('theme');

/**
 * 
 * @param {string} theme 
 * @returns {Promise<boolean>}
 */
export function SaveTheme(theme) {
  return themedb.get("theme")
    .then(doc => themedb.put({ ...doc, theme }))
    .then(({ ok }) => ok)
    .catch(({ status, ...error }) => {
      if (status === 404) {
        return themedb.put({ _id: "theme", theme }).then(({ ok }) => ok);
      }
      return false;
    });
}

/**
 * @returns {Promise<Theme>}
 */
export function GetTheme() {
  return themedb
    .get("theme")
    .then(({ theme }) => theme)
    .catch(err => {
      console.warn(err);
      return "None"
    });
}


/**
 * interacts with the HTML Element to switch classes
 * @param {Theme} theme
 * @return {Promise<boolean>}
 */
export function SwitchTheme(theme) {
  const html = document.querySelector('html');
  switch (theme) {
    case 'Dark':
      if (html.classList.contains('dark')) { return Promise.resolve(false); }
      html.classList.add('dark');
      return SaveTheme(theme);

    case 'Light':
      if (!html.classList.contains('dark')) { return Promise.resolve(false); }
      html.classList.remove('dark');
      return SaveTheme(theme);
  }
}