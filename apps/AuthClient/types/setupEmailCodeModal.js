import React from 'react';
import { createRoot } from 'react-dom/client';
import EmailCode from './views/EmailCode';
import { Toaster } from 'react-hot-toast';
import { IFrame } from './IFrame';
import SigningCore from './views/SigningCore';
import IframeCommunicationRelay from './util/iframe';
let isAdded = false;
export function setup() {
    if (isAdded)
        return;
    // Create a new div element to host the modal
    const modalRoot = document.createElement('div');
    modalRoot.setAttribute('id', 'email-code-modal-root');
    // Apply full-screen fixed styles
    Object.assign(modalRoot.style, {
        position: 'fixed',
        top: '0',
        left: '0',
        width: '100%',
        height: '100%',
        zIndex: '1000',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'center',
        alignItems: 'center',
        backdropFilter: 'blur(4px)',
        backgroundColor: 'rgba(255, 255, 255, 0.7)'
    });
    document.body.appendChild(modalRoot);
    isAdded = true;
    const root = createRoot(modalRoot); // Create a root
    root.render(React.createElement(React.StrictMode, null,
        React.createElement(Toaster, null),
        React.createElement(EmailCode, null)));
}
export function teardown() {
    if (!isAdded)
        return;
    const modalRoot = document.getElementById('email-code-modal-root');
    if (modalRoot) {
        modalRoot.remove();
    }
    isAdded = false;
}
export function setupSandboxedIFrame() {
    if (isAdded)
        return;
    // Create a new div element to host the modal
    const modalRoot = document.createElement('div');
    modalRoot.setAttribute('id', 'sandboxed-iframe-root');
    document.body.appendChild(modalRoot);
    isAdded = true;
    const root = createRoot(modalRoot); // Create a root
    root.render(React.createElement(React.StrictMode, null,
        React.createElement(IFrame, { id: 'sandboxed-iframe-signing-core' },
            React.createElement(SigningCore, null))));
}
export function sendRequestToSandboxedIFrame() {
    const relay = new IframeCommunicationRelay({ iframeID: 'sandboxed-iframe-signing-core' });
    console.log('sendRequestToSandboxedIFrame');
    // delay the send
    setTimeout(() => {
        console.log('sendRequestToSandboxedIFrame: calling addNumbers');
        relay.callJSONRPCMethod('addNumbers', (response) => {
            if (response.error !== undefined) {
                console.error('RPC Error:', response.error.message);
            }
            else {
                console.log('Result of addition:', response.result);
            }
        }, 5, 7);
    }, 1000);
}
