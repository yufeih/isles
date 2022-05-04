var buffer = new SharedArrayBuffer(1280 * 800 * 4)
var pixels = new Uint8Array(buffer)
pixels[0] = 255;
pixels[3] = 255;

window.pixels = pixels
