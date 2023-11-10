function IframePage (): JSX.Element {
  return (
    <div style={{ height: '100vh', width: '100vw', overflow: 'hidden' }}>
      <iframe
        src="/wallet"
        style={{ height: '100%', width: '100%', border: 0 }}
        sandbox='allow-scripts'
      />
    </div>
  )
}

export default IframePage
