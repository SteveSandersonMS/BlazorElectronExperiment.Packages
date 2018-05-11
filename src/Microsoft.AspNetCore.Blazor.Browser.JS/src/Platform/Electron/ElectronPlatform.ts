import { MethodHandle, System_Object, System_String, System_Array, Pointer, Platform } from '../Platform';
import { getRegisteredFunction } from '../../Interop/RegisteredFunction';
import { ipcRenderer } from 'electron';
import * as msgpack from 'msgpack-lite';

export const electronPlatform: Platform = {
  info: {
    name: 'electron',
    supportsDotNetInProcess: false,
  },

  start: function start(properties: Map<string, string>): Promise<void> {

    ipcRenderer.on('blazor:InvokeJSArray', (event, args) => {
      const functionName = args.shift();
      return invokeJSFromDotNet(functionName, args);
    });

    return new Promise(resolve => {
      console.log("waiting for blazor:init message");
      ipcRenderer.once("blazor:init", () => {
        console.log("got blazor:init message... ready!");
        ipcRenderer.send("blazor:init");
        resolve();
      });
    });
  },

  findMethod: function (assemblyName: string, namespace: string, className: string, methodName: string): MethodHandle {
    return { assemblyName, namespace, className, methodName } as ElectronMethodHandle;
  },

  callMethod: function callMethod(method: MethodHandle, target: System_Object, args: System_Object[]): System_Object {
    if (target != null) {
      throw new Error("Electron interop does not support passing an instance.");
    }

    const message: DotNetInvokeMessage = { 
      methodInfo: method as ElectronMethodHandle, 
      argsJsonArray: args.map(arg => JSON.stringify(arg)), 
    };
    return ipcRenderer.sendSync('blazor:CallDotNetFromJS', message) as System_Object;
  },

  toJavaScriptString: function toJavaScriptString(managedString: System_String) {
    return managedString as any as string;
  },

  toDotNetString: function toDotNetString(jsString: string): System_String {
    return jsString as any;
  },

  getArrayLength: function getArrayLength(array: System_Array<any>): number {
    if (array instanceof MessagePack_Object) {
      return array.data.length;
    }
  
    throw new Error('this only works with messagepack, sorry try again later.');
  },

  getArrayEntryPtr: function getArrayEntryPtr<TPtr extends Pointer>(array: System_Array<TPtr>, index: number, itemSize: number): TPtr {
    if (array instanceof MessagePack_Object) {
      let value = array.data[index];
      if (value === undefined) {
        throw new Error('value is undefined. this is not good.');
      }
      return new MessagePack_Object(value) as any;
    }
  
    throw new Error('this only works with messagepack, sorry try again later.');
  },

  getObjectFieldsBaseAddress: function getObjectFieldsBaseAddress(referenceTypedObject: System_Object): Pointer {
    return referenceTypedObject as any;
  },

  readInt32Field: readPrimitiveField,
  readFloatField: readPrimitiveField,
  readObjectField: readNonPrimitiveField,
  readStringField: readPrimitiveField,
  readStructField: readNonPrimitiveField
};

// Bypass normal type checking to add this extra function. It's only intended to be called from
// the JS code in Mono's driver.c. It's never intended to be called from TypeScript.
(electronPlatform as any).electronGetRegisteredFunction = getRegisteredFunction;

function readPrimitiveField(baseAddress: Pointer, fieldOffset?: number): any {
  if (baseAddress instanceof MessagePack_Object) {
    if (fieldOffset === undefined){
      return baseAddress.data;
    }

    let value = baseAddress.data[(fieldOffset || 0) / 4];
    if (value === undefined) {
      throw new Error('value is undefined. this is not good.');
    }
    return value;
  }

  throw new Error('this only works with messagepack, sorry try again later.');
}

function readNonPrimitiveField(baseAddress: Pointer, fieldOffset?: number): any {
  if (baseAddress instanceof MessagePack_Object) {
    if (fieldOffset === undefined){
      return baseAddress.data;
    }
    
    let value = baseAddress.data[(fieldOffset || 0) / 4];
    if (value === undefined) {
      throw new Error('value is undefined. this is not good.');
    }
    return new MessagePack_Object(value);
  }

  throw new Error('this only works with messagepack, sorry try again later.');
}

type SerializedArg = null | number | string | { kind: string };
type MarshalledArg = null | number | string | MessagePack_Object;

// using class so we can do instanceof
class MessagePack_Object implements System_Object {
  data: any[];
  System_Object__DO_NOT_IMPLEMENT: any;
  constructor(data: any[]) {
    this.data = data;
  }
}

function isMessagePackObject(obj: System_Object) {
  return (obj as any).hasOwnProperty('kind') && (obj as any).kind === 'messagepack';
}

function invokeJSFromDotNet(functionName: string, args: SerializedArg[]) {
  const functionInstance = getRegisteredFunction(functionName);
  var marshalled : MarshalledArg[] = args.map(a => {

    if (a === '__null__' || a === null) {
      return null;
    }

    if (typeof a === 'number') {
      return a;
    }

    if (typeof a === 'string') {
      return a;
    }

    if (a.hasOwnProperty('kind') && a.kind === 'msgpack') {
      var data = msgpack.decode((a as any).bytes as Uint8Array);
      return new MessagePack_Object(data as any[]);
    }

    throw new Error('Cannot figure out how to marshal value: ' + a);
  });
  return functionInstance.apply(null, marshalled);
}

// We don't actually have handles, it all goes over the IPC channel.
//
// Matches DotNetInvokeMethodInfo.cs
interface ElectronMethodHandle extends MethodHandle {
  assemblyName: string,
  namespace: string,
  className: string,
  methodName: string,
}

// Matches DotNetInvokeMessage.cs
interface DotNetInvokeMessage {
  methodInfo: ElectronMethodHandle,
  argsJsonArray: string[],
}

// Matches DotNetInvokeResponse.cs
interface DotNetInvokeResponse {
  resultJson: string;
  exception: string;
}
