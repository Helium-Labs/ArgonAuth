import { useLocation } from 'react-router-dom'

interface OAuthQueryParams { redirectUri: string, state: string, codeChallenge: string, username: string, cspk: string };
function useOAuthParams (): OAuthQueryParams {
  const location = useLocation()
  const searchParams = new URLSearchParams(location.search)

  const redirectUri = searchParams.get('redirect_uri')
  // base64url
  const state = searchParams.get('state')
  // base64url
  const codeChallenge = searchParams.get('code_challenge')
  const username = searchParams.get('username')
  // encoded as base64url
  const cspk = searchParams.get('cspk')
  // If any of the parameters are null, then we are not in the OAuth flow and throw an error
  if (redirectUri === null || state === null || codeChallenge === null || username === null || cspk === null) {
    throw new Error('Not in OAuth flow.')
  }
  return { redirectUri, state, codeChallenge, username, cspk }
}

export default useOAuthParams
