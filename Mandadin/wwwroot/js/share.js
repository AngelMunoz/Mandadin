const channel = new BroadcastChannel("share-target");


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
 * @returns {Promise<boolean>}
 */
export async function ShareContent(title, text, url = undefined) {
    if (!navigator.share) {
        return Promise.reject('Share API not available');
    }
    try {
        await navigator.share({ title, text, url });
        return true;
    } catch (error) {
        console.warn(error);
        return false;
    }
}

channel.onmessage = function(event) {
    const isAllowed =
        ["SEND_IMPORT_DATA"].includes(event.data.type);

    if (event.data && !isAllowed || (!event.data && !event.data.data)) {
        return;
    }
    const data = event.data.data;
    if (data && data.text) {
        sessionStorage.setItem("import", JSON.stringify({
            title: data.title,
            text: data.text,
            url: ""
        }));
    }
};

const delay = time => new Promise(resolve => setTimeout(() => resolve(), time));

export async function ImportShareData() {
    channel.postMessage({ type: "GET_IMPORT_DATA" });
    await delay(500);
    try {
        return JSON.parse(sessionStorage.getItem("import"));
    } catch (e) {
        console.error(e);
        return { text: '', title: '', url: '' };
    }
}