import '@browserjs/../../../modules/jsinterop/src/Microsoft.JSInterop.JS/src/Microsoft.JSInterop';
import '@browserjs/GlobalExports';
import { OutOfProcessRenderBatch } from '@browserjs/Rendering/RenderBatch/OutOfProcessRenderBatch';
import { internalFunctions as uriHelperFunctions } from '@browserjs/Services/UriHelper';
import { renderBatch } from '@browserjs/Rendering/Renderer';
import { loadEmbeddedResourcesAsync } from '@browserjs/BootCommon';
import { decode } from 'base64-arraybuffer';
import * as electron from 'electron';

function boot() {
  // In the background, start loading the boot config and any embedded resources
  const embeddedResourcesPromise = fetchBootConfigAsync().then(bootConfig => {
    return loadEmbeddedResourcesAsync(bootConfig);
  });

  // Configure the mechanism for JS->.NET calls
  DotNet.attachDispatcher({
    beginInvokeDotNetFromJS: (callId, assemblyName, methodIdentifier, dotNetObjectId, argsJson) => {
      electron.ipcRenderer.send('BeginInvokeDotNetFromJS', [callId ? callId.toString() : null, assemblyName, methodIdentifier, dotNetObjectId || 0, argsJson]);
    }
  });

  // Wait until the .NET process says it is ready
  electron.ipcRenderer.once('blazor:init', async () => {
    // Ensure any embedded resources have been loaded before starting the app
    await embeddedResourcesPromise;

    // Confirm that the JS side is ready for the app to start
    electron.ipcRenderer.send('blazor:init', [
      uriHelperFunctions.getLocationHref().replace(/\/index\.html$/, ''),
      uriHelperFunctions.getBaseURI()]);
  });

  electron.ipcRenderer.on('JS.BeginInvokeJS', (_, asyncHandle, identifier, argsJson) => {
    DotNet.jsCallDispatcher.beginInvokeJSFromDotNet(asyncHandle, identifier, argsJson);
  });

  electron.ipcRenderer.on('JS.RenderBatch', (_, rendererId, batchBase64) => {
    const headerLength = 5; // see also: MessagePackBinaryBlockStream.HeaderLength
    var batchData = new Uint8Array(decode(batchBase64), headerLength);
    renderBatch(rendererId, new OutOfProcessRenderBatch(batchData));
  });

  electron.ipcRenderer.on('JS.Error', (_, message) => {
    console.error(message);
  });
}

boot();

async function fetchBootConfigAsync() {
  const sourceName = '/js/blazor.electron.js';
  const bootJsonUrl = document.querySelector(`script[src$="${sourceName}"]`)!
    .getAttribute('src')!
    .replace(sourceName, '/dist/_framework/blazor.boot.json');
  const bootConfigResponse = await fetch(bootJsonUrl, { method: 'Get', credentials: 'include' });
  return bootConfigResponse.json() as Promise<any>;
}
