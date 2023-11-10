import { OpenAPIRouter } from '@cloudflare/itty-router-openapi'
import { GetWebauthnLSIG } from './handler'

const router = OpenAPIRouter()
router.get('/api/webauthnlsig/:credpk/', GetWebauthnLSIG)
router.all('*', () => new Response('Not Found.', { status: 404 }))

export default {
  fetch: router.handle
}
