export const notifyError = (errorMessage: string): void => {
  window.dispatchEvent(new CustomEvent('message', {
    detail: JSON.stringify({
      title: 'Error',
      body: errorMessage,
      type: 'info'
    })
  }))
}
