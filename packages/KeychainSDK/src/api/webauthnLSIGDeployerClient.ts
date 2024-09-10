import WebauthnLSIGApi from '@gradian/lsig-deployer-client'

const WebauthnLSIGApiBasePath = 'http://localhost:8787'

export const webauthnLSIGClient = new WebauthnLSIGApi({
  basePath: WebauthnLSIGApiBasePath
})
