export function HasOverlayControls() {
    if ('windowControlsOverlay' in navigator && navigator['windowControlsOverlay'].visible) { return true; }
    return false;
}