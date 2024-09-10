import { FidoApi } from '@gradian/keychain-auth-server-client'

const basePath = 'https://localhost:5001'

export const Fido2Client =
 new FidoApi({
   basePath
 })
