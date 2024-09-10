interface OAuthQueryParams {
  redirectUri: string
  state: string
  codeChallenge: string
  username: string
  cspk: string
};

function useOAuthParams (): OAuthQueryParams {
  const searchParams = new URLSearchParams(window.location.search)

  const redirectUri = searchParams.get('redirect_uri')
  const state = searchParams.get('state')
  const codeChallenge = searchParams.get('code_challenge')
  const username = searchParams.get('username')
  const cspk = searchParams.get('cspk')

  if (redirectUri === null || state === null || codeChallenge === null || username === null || cspk === null) {
    // throw new Error('Not in OAuth flow.')
    // make them all ''
    return { redirectUri: '', state: '', codeChallenge: '', username: '', cspk: '' }
  }

  return { redirectUri, state, codeChallenge, username, cspk }
}

export default useOAuthParams
