interface OAuthQueryParams {
    redirectUri: string;
    state: string;
    codeChallenge: string;
    username: string;
    cspk: string;
}
declare function useOAuthParams(): OAuthQueryParams;
export default useOAuthParams;
