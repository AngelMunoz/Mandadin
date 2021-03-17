

/**
 * @returns {Promise<boolean>}
 */
export function CanShare() {
    //@ts-ignore
    if (navigator.canShare) {
        //@ts-ignore
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
    return Promise.reject('Share API not available');
}
