export function HasOverlayControls() {
    if ('windowControlsOverlay' in navigator) { return true; }
    return false;
}