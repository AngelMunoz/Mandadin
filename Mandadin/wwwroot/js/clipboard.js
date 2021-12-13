
const clipboard = navigator.clipboard;


/**
 * 
 * @param {string} text 
 * @returns {Promise<bool>}
 */
export async function CopyTextToClipboard(text) {
    if (!clipboard) return Promise.reject("Clipboard API not available");
    try {
        await clipboard.writeText(text);
        return true;
    } catch (error) {
        console.warn(error);
        return false;
    }
}

/**
 * * @returns {Promise<string>}
 */
export function ReadTextFromClipboard() {
    if (!clipboard) return Promise.reject("Clipboard API not available");
    return clipboard.readText();
}