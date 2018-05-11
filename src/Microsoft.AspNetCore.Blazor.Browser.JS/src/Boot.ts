import { platform } from './Environment';
import { getAssemblyNameFromUrl } from './Platform/DotNet';
import './Rendering/Renderer';
import './Services/Http';
import './Services/UriHelper';
import './GlobalExports';

async function boot() {
  // Read startup config from the <script> element that's importing this file
  const allScriptElems = document.getElementsByTagName('script');
  const thisScriptElem = (document.currentScript || allScriptElems[allScriptElems.length - 1]) as HTMLScriptElement;

  var properties = new Map<string, string>()
  for (let i = 0; i < thisScriptElem.attributes.length; i++) {
    let attribute = thisScriptElem.attributes[i];
    properties.set(attribute.name, attribute.value);
  }

  try {
    await platform.start(properties);
  } catch (ex) {
    throw new Error(`Failed to start platform. Reason: ${ex}`);
  }
}

boot();
