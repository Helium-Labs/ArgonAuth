import { FidoApi } from './RelyingPartyClient'

const basePath = 'https://localhost:5001'

export const relyingPartyClient =
 new FidoApi({
   basePath
 })
