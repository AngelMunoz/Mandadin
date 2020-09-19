

/**
 * @returns {Promise<boolean>}
 */
export function CanShare() {
    if (navigator.canShare) {
        return window.navigator.canShare({
            title: '',
            text: '',
            url: ''
        });
    }
    return Promise.resolve(false);
}

/**
 * 
 * @param {string} title 
 * @param {string} text 
 * @param {string?} url 
 * @returns {Promise<void>}
 */
export function ShareContent(title, text, url = undefined) {
    if (navigator.share) {
        return navigator.share({ title, text, url });
    }
    return Promise.reject(new Error("Share API not available"));
}