export type Subscription = Record<string, Array<(data: any) => void>>;
export interface JsonRpcRequestPayload {
    jsonrpc: string;
    method: string;
    params?: any[];
    id: string | number;
}
export interface JsonRpcResponsePayload {
    jsonrpc: string;
    id: string | number;
    result?: any;
    error?: {
        code: number;
        message: string;
        data?: any;
    };
}
export type AnyFunction = (...args: any[]) => any;
export type AnyAsyncFunction = (...args: any[]) => Promise<any>;
export interface GenericChannelMessage {
    eventName: string;
    data?: any;
    responseEventName?: string;
}
/**
 * Publish/Subscribe communication relay between an iframe and its parent window, and vice versa, using the postMessage API.
 * Supports JSON-RPC styled communication with requests and responses.
 */
export default class IframeCommunicationRelay {
    private subscriptions;
    private messageQueue;
    private iframeReady;
    private readonly iframeID;
    private readonly iframe;
    private readonly allowedOrigin;
    /**
       * Constructor for the IframeCommunicationRelay class. Sets up a relay mechanism for communication between an
       * iframe and its parent window, and vice versa, using the postMessage API. The implementation also provides
       * support for JSON-RPC styled communication with requests and responses.
       * @param iframeID - ID of the iframe to send messages to (if from parent)
       * @param iframe - iframe to send messages to (if from parent)
       * @param allowedOrigin - origin to allow messages from
       * @returns IframeCommunicationRelay
       **/
    constructor({ iframeID, iframe, allowedOrigin }?: {
        iframeID?: string;
        iframe?: HTMLIFrameElement;
        allowedOrigin?: string;
    });
    /**
       * Checks if the current window is an iframe.
       * @returns {boolean} boolean indicating whether the current window is an iframe
       */
    private inIframe;
    /**
       * Callback when the iframe status event is called.
       * @param {any} data - data received from the other window
       * @returns {void}
       */
    private handleStatus;
    /**
       * Sends any queued messages to the iframe (once it is ready).
       * @returns {void}
       */
    private sendQueuedMessages;
    /**
       * Handle message event received from the other window sent via postMessage.
       * @param {MessageEvent} event - message event received from the other window
       * @returns {void}
       */
    private handleMessage;
    /**
       * Publishes a message to the iframe, or to the parent window if the current window is an iframe.
       * @param {string} eventName - name of the event to publish
       * @param {any} data - data to send with the event
       * @param {AnyFunction | undefined} onResponse - callback function to be called when a response is received
       * @returns void
       */
    publish(eventName: string, data?: any): void;
    /**
       * Subscribes to an event, and publishes a response to data.responseEventName once the callback has been executed.
       * @param {string} eventName - name of the event to subscribe to
       * @param {AnyFunction} callback - callback function to be executed when the event is received
       * @returns void
       */
    subscribeWithResponse(eventName: string, callback: AnyFunction): void;
    /**
       * Subscribes to an event and executes the callback when the event is received from the other window.
       * @param {string} eventName - name of the event to subscribe to
       * @param {AnyFunction} callback - callback function to be executed when the event is received
       * @returns void
       */
    subscribe(eventName: string, callback: AnyFunction): void;
    /**
       * Unsubscribe from an event, so that the specified callback is no longer executed when the event is received.
       * @param {string} eventName - name of the event to unsubscribe from
       * @param {AnyFunction} callback - callback function to be unsubscribed
       * @returns void
       */
    unsubscribe(eventName: string, callback: AnyFunction): void;
    /**
       * Unsubscribe all callbacks from an event.
       * @param {string} eventName - name of the event to unsubscribe from
       * @returns void
       */
    unsubscribeAll(eventName: string): void;
    /**
       * Defines JSON-RPC method. Listens for RPC event to be called, once received it executes the associated callback and
       * returns the result to the invoker via publication on the methods response event channel.
       * @param {string} jsonRpcMethod - name of the JSON-RPC method to subscribe to
       * @param {AnyAsyncFunction} callback - callback function to be executed when the method is received
       * @returns void
       */
    defineJSONRPCMethod(jsonRpcMethod: string, callback: AnyAsyncFunction): void;
    /**
       * Call JSON-RPC method, defined in the other window. Invokes the callback, onResponse, on the result of the method invocation.
       * @param {string} method - name of the JSON-RPC method to subscribe to
       * @param {AnyFunction} onResponse - callback function to be executed when the method is received
       * @param {any[]} params - parameters to be passed to the RPC method
       * @returns void
       */
    callJSONRPCMethod(method: string, onResponse?: AnyFunction | undefined, ...params: any[]): void;
    /**
       * Respond to the invoker of a JSON-RPC method call.
       * @param {string | number} id - id of the JSON-RPC request to respond to. Must match the id of the request being responded to.
       * @param {any} result - result of the JSON-RPC request
       * @param {any} error - error object, if an error occured while processing the request
       * @returns void
       */
    JSONRPCResponse(id: string | number, result: any, error?: any): void;
}
