import React from 'react'
import './index.css'
import { RouterProvider, createBrowserRouter } from 'react-router-dom'
import Signin from './views/Signin'
import Signup from './views/Signup'
import { Toaster } from 'react-hot-toast'
import { createRoot } from 'react-dom/client'
import EmailCode from './views/EmailCode'

export const routes = {
  signin: '/',
  signup: '/signup',
  emailCode: '/email-code'
}

const router = createBrowserRouter([
  {
    path: routes.signin,
    element: <Signin />
  },
  {
    path: routes.signup,
    element: <Signup />
  },
  {
    path: routes.emailCode,
    element: <EmailCode />
  }
])

const container = document.getElementById('root')
if (!container) throw new Error('No root element')
const root = createRoot(container) // createRoot(container!) if you use TypeScript
root.render(
  <React.StrictMode>
    <RouterProvider router={router} />
    <Toaster />
  </React.StrictMode>
)
