
const clipboard = navigator.clipboard;


/**
 * 
 * @param {string} text 
 * @returns {Promise<void>}
 */
export function CopyTextToClipboard(text) {
    if (!clipboard) return Promise.reject("Clipboard API not available");
    return clipboard.writeText(text);
}

/**
 * * @returns {Promise<string>}
 */
export function ReadTextFromClipboard() {
    if (!clipboard) return Promise.reject("Clipboard API not available");
    return clipboard.readText();
}