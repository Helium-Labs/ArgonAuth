/**
 * Publish/Subscribe communication relay between an iframe and its parent window, and vice versa, using the postMessage API.
 * Supports JSON-RPC styled communication with requests and responses.
 */
export default class IframeCommunicationRelay {
    subscriptions;
    messageQueue;
    iframeReady;
    iframeID;
    iframe;
    allowedOrigin = '*';
    /**
       * Constructor for the IframeCommunicationRelay class. Sets up a relay mechanism for communication between an
       * iframe and its parent window, and vice versa, using the postMessage API. The implementation also provides
       * support for JSON-RPC styled communication with requests and responses.
       * @param iframeID - ID of the iframe to send messages to (if from parent)
       * @param iframe - iframe to send messages to (if from parent)
       * @param allowedOrigin - origin to allow messages from
       * @returns IframeCommunicationRelay
       **/
    constructor({ iframeID, iframe, allowedOrigin } = {}) {
        this.subscriptions = {};
        this.messageQueue = [];
        this.iframeReady = false;
        this.iframeID = iframeID;
        this.iframe = iframe;
        this.subscribe('status', this.handleStatus);
        this.allowedOrigin = allowedOrigin ?? '*';
        // handle window event not existing in Node environment, e.g. Next SSR
        if (typeof window === 'undefined') {
            console.error('Window is undefined. Likely in a SSR environment.');
            return;
        }
        window.addEventListener('message', this.handleMessage, false);
    }
    /**
       * Checks if the current window is an iframe.
       * @returns {boolean} boolean indicating whether the current window is an iframe
       */
    inIframe() {
        try {
            return window.self !== window.top;
        }
        catch (e) {
            return true;
        }
    }
    /**
       * Callback when the iframe status event is called.
       * @param {any} data - data received from the other window
       * @returns {void}
       */
    handleStatus(data) {
        if (data === 'ready') {
            this.iframeReady = true;
            this.sendQueuedMessages();
        }
    }
    /**
       * Sends any queued messages to the iframe (once it is ready).
       * @returns {void}
       */
    sendQueuedMessages() {
        if (this.iframeReady) {
            this.messageQueue.forEach((message) => {
                this.publish(message.eventName, message.data);
            });
            this.messageQueue = [];
        }
    }
    /**
       * Handle message event received from the other window sent via postMessage.
       * @param {MessageEvent} event - message event received from the other window
       * @returns {void}
       */
    handleMessage(event) {
        if (this.allowedOrigin !== event.origin && this.allowedOrigin !== '*') {
            // Not the expected sender, reject the message
            return;
        }
        const { eventName, data } = event.data;
        if (this.subscriptions[eventName] !== undefined) {
            this.subscriptions[eventName].forEach((callback) => { callback(data); });
        }
        else if (data?.jsonrpc !== undefined && data.id !== undefined) {
            const responseEventName = `${data.id}Response`;
            if (this.subscriptions[responseEventName] !== undefined) {
                this.subscriptions[responseEventName].forEach((callback) => { callback(data); });
            }
        }
    }
    /**
       * Publishes a message to the iframe, or to the parent window if the current window is an iframe.
       * @param {string} eventName - name of the event to publish
       * @param {any} data - data to send with the event
       * @param {AnyFunction | undefined} onResponse - callback function to be called when a response is received
       * @returns void
       */
    publish(eventName, data = undefined) {
        if (typeof window === 'undefined')
            return;
        const message = { eventName, data };
        if (!this.inIframe()) {
            // we are the parent, and attempting to send a message to the iframe that the relay is attached to
            if (this.iframeID === undefined)
                throw new Error('iframeID must be provided');
            let iframe = this.iframe;
            if (!iframe) {
                iframe = document.getElementById(this.iframeID);
            }
            if (!iframe || !this.iframeReady) {
                // iframe not found or not ready, queue the message for sending once the iframe is ready
                // assumption that the reason for the iframe being undefined is that it hasn't loaded yet (not ready)
                this.messageQueue.push(message);
                return;
            }
            iframe.contentWindow?.postMessage(message, this.allowedOrigin);
        }
        else {
            // we're the iframe, and attempting to send a message to the parent
            window.parent.postMessage(message, this.allowedOrigin);
        }
    }
    /**
       * Subscribes to an event, and publishes a response to data.responseEventName once the callback has been executed.
       * @param {string} eventName - name of the event to subscribe to
       * @param {AnyFunction} callback - callback function to be executed when the event is received
       * @returns void
       */
    subscribeWithResponse(eventName, callback) {
        // publishes response to responseEventName once the response has been received
        this.subscribe(eventName, async (data) => {
            const response = await callback(data);
            this.publish(data.responseEventName, response);
        });
    }
    /**
       * Subscribes to an event and executes the callback when the event is received from the other window.
       * @param {string} eventName - name of the event to subscribe to
       * @param {AnyFunction} callback - callback function to be executed when the event is received
       * @returns void
       */
    subscribe(eventName, callback) {
        if (this.subscriptions[eventName] === undefined) {
            this.subscriptions[eventName] = [];
        }
        this.subscriptions[eventName].push(callback);
    }
    /**
       * Unsubscribe from an event, so that the specified callback is no longer executed when the event is received.
       * @param {string} eventName - name of the event to unsubscribe from
       * @param {AnyFunction} callback - callback function to be unsubscribed
       * @returns void
       */
    unsubscribe(eventName, callback) {
        if (this.subscriptions[eventName] !== undefined) {
            this.subscriptions[eventName] = this.subscriptions[eventName].filter((cb) => cb !== callback);
        }
    }
    /**
       * Unsubscribe all callbacks from an event.
       * @param {string} eventName - name of the event to unsubscribe from
       * @returns void
       */
    unsubscribeAll(eventName) {
        if (this.subscriptions[eventName] !== undefined) {
            this.subscriptions[eventName] = [];
            // eslint-disable-next-line @typescript-eslint/no-dynamic-delete
            delete this.subscriptions[eventName];
        }
    }
    /// /////////////////////////////////////////
    // JSON-RPC Extension
    /// /////////////////////////////////////////
    /**
       * Defines JSON-RPC method. Listens for RPC event to be called, once received it executes the associated callback and
       * returns the result to the invoker via publication on the methods response event channel.
       * @param {string} jsonRpcMethod - name of the JSON-RPC method to subscribe to
       * @param {AnyAsyncFunction} callback - callback function to be executed when the method is received
       * @returns void
       */
    defineJSONRPCMethod(jsonRpcMethod, callback) {
        this.subscribe(jsonRpcMethod, async (jsonRpcRequest) => {
            // Verify if it is a valid JSON-RPC request
            if (jsonRpcRequest?.jsonrpc === undefined ||
                jsonRpcRequest.jsonrpc !== '2.0' ||
                jsonRpcRequest.method === undefined ||
                jsonRpcRequest.method !== jsonRpcMethod ||
                jsonRpcRequest.id === undefined) {
                console.error('Invalid JSON-RPC request', jsonRpcRequest);
                this.JSONRPCResponse(jsonRpcRequest.id, undefined, {
                    code: -32600,
                    message: 'Invalid Request'
                });
                return;
            }
            try {
                // Process the JSON-RPC request with the provided callback function
                const requestArgs = jsonRpcRequest.params ?? [];
                // Equivalent to: const result = await callback(...requestArgs)
                const result = await callback.apply(null, requestArgs);
                // Send a JSON-RPC response back, by publishing on the same channel.
                this.JSONRPCResponse(jsonRpcRequest.id, result);
            }
            catch (error) {
                // If an error occured while processing, send a JSON-RPC error response back
                this.JSONRPCResponse(jsonRpcRequest.id, undefined, {
                    code: -32603,
                    message: error.message
                });
            }
        });
    }
    /**
       * Call JSON-RPC method, defined in the other window. Invokes the callback, onResponse, on the result of the method invocation.
       * @param {string} method - name of the JSON-RPC method to subscribe to
       * @param {AnyFunction} onResponse - callback function to be executed when the method is received
       * @param {any[]} params - parameters to be passed to the RPC method
       * @returns void
       */
    callJSONRPCMethod(method, onResponse = undefined, ...params) {
        const id = crypto.randomUUID !== undefined ? crypto.randomUUID() : Math.random().toString(36).substring(7);
        const payload = {
            jsonrpc: '2.0',
            method,
            params,
            id
        };
        this.publish(method, payload);
        const responseEventName = `${id}JSONRPCResponse`;
        if (!onResponse)
            return;
        // listen for response generated by the method invocation
        this.subscribe(responseEventName, (data) => {
            onResponse(data);
            // unsubscribe after the response has been received, as
            this.unsubscribeAll(responseEventName);
        });
    }
    /**
       * Respond to the invoker of a JSON-RPC method call.
       * @param {string | number} id - id of the JSON-RPC request to respond to. Must match the id of the request being responded to.
       * @param {any} result - result of the JSON-RPC request
       * @param {any} error - error object, if an error occured while processing the request
       * @returns void
       */
    JSONRPCResponse(id, result, error = undefined) {
        const payload = {
            jsonrpc: '2.0',
            id
        };
        if (error !== undefined) {
            payload.error = {
                code: error.code ?? -32603,
                message: error.message ?? 'Internal error',
                data: error.data
            };
        }
        else {
            payload.result = result;
        }
        const responseEventName = `${id}JSONRPCResponse`;
        this.publish(responseEventName, payload);
    }
}
