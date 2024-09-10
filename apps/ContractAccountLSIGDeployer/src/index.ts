import { OpenAPIRouter } from '@cloudflare/itty-router-openapi'
import { GetWebauthnLSIG } from './handler'
import { createCors } from 'itty-cors'

const { preflight, corsify } = createCors({
  methods: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'OPTIONS'],
  origins: ['*'],
  maxAge: 3600
})

const router = OpenAPIRouter()
  .all('*', preflight) // handle CORS preflight/OPTIONS requests
  .get('/api/webauthnlsig/:credpk/:origin/', GetWebauthnLSIG)
  .all('*', () => new Response('Not Found.', { status: 404 }))

// CF ES6 module syntax
export default {
  fetch: async (request: any, ...extra: any[]) => {
    return await router
      .handle(request, ...extra)
      .catch(err => {
        return new Response(err.message, { status: 500 })
      })
      .then(corsify)
  }
}
