// Expose an export called 'platform' of the interface type 'Platform',
// so that consumers can be agnostic about which implementation they use.
// Basic alternative to having an actual DI container.
import { Platform } from './Platform/Platform';
import { monoPlatform } from './Platform/Mono/MonoPlatform';
import { electronPlatform } from './Platform/Electron/ElectronPlatform';

let currentPlatform = monoPlatform;
if (window && (window as any).process && (window as any).process.type)
{
    currentPlatform = electronPlatform; 
}

export const platform: Platform = currentPlatform;
