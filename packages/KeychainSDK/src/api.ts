import { FidoApi } from '@gradian/keychain-auth-server-client'
import WebauthnLSIGApi from '@gradian/lsig-deployer-client'

const basePath = process.env.REACT_APP_GRADIAN_AUTH_BASE_PATH ??
    process.env.GRADIAN_AUTH_BASE_PATH ??
    'https://localhost:5001'
export const relyingPartyClient = new FidoApi({
  basePath
})

const WebauthnLSIGApiBasePath = process.env.REACT_APP_GRADIAN_LSIG_DEPLOYER_BASE_PATH ?? process.env.GRADIAN_LSIG_DEPLOYER_BASE_PATH ?? 'https://localhost:8787'
export const webauthnLSIGClient = new WebauthnLSIGApi({
  basePath: WebauthnLSIGApiBasePath
})
