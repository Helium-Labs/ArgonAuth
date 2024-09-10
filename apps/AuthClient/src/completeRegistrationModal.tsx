import React from 'react'
import { createRoot } from 'react-dom/client'
import EmailCode, { type EmailCodeProps } from './views/EmailCode'
import { Toaster } from 'react-hot-toast'
const emailModal = 'email-code-modal-root'

export function displayEmailCodeModal ({ onEmailCodeSubmit, sendEmailCode }: EmailCodeProps): void {
  // Create a new div element to host the modal
  const modalRoot = document.createElement('div')
  modalRoot.setAttribute('id', emailModal)

  // Apply full-screen fixed styles
  Object.assign(modalRoot.style, {
    position: 'fixed', // Fixed position
    top: '0', // Top edge of the viewport
    left: '0', // Left edge of the viewport
    width: '100%', // Full width
    height: '100%', // Full height
    zIndex: '1000', // High z-index to be on top of other elements
    display: 'flex',
    flexDirection: 'column',
    justifyContent: 'center',
    alignItems: 'center',
    backdropFilter: 'blur(4px)',
    backgroundColor: 'rgba(255, 255, 255, 0.7)'
  })

  document.body.appendChild(modalRoot)

  const root = createRoot(modalRoot) // Create a root
  root.render(
    <React.StrictMode>
      <Toaster />
      <EmailCode
        onEmailCodeSubmit={onEmailCodeSubmit}
        sendEmailCode={sendEmailCode}
      />
    </React.StrictMode>
  )
}

export function teardownEmailCodeModal (): void {
  const modalRoot = document.getElementById(emailModal)
  if (modalRoot) {
    modalRoot.remove()
  }
}
