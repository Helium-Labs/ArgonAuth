import React, { useEffect, useState } from 'react'
import './index.css'
import { Navigate, RouterProvider, createBrowserRouter } from 'react-router-dom'
import Signin from './views/Signin'
import Signup from './views/Signup'
import { Toaster } from 'react-hot-toast'
import { createRoot } from 'react-dom/client'
import Profile from './views/Profile'
import { keychain } from './lib'
export const routes = {
  signin: '/',
  signup: '/signup',
  profile: '/profile'
}

const isAuthenticated = async (): Promise<boolean> => {
  const isValid = await keychain.tokenIsValid(keychain.jwt)
  return isValid
}

const ProtectedRoute = ({ children }: { children: JSX.Element }): JSX.Element | null => {
  const [isAuthChecked, setIsAuthChecked] = useState(false)
  const [isAuth, setIsAuth] = useState(false)

  useEffect(() => {
    const checkAuth = async (): Promise<void> => {
      const authenticated = await isAuthenticated()
      setIsAuth(authenticated)
      setIsAuthChecked(true)
    }

    checkAuth().then(() => {}).catch(() => {})
  }, [])

  if (!isAuthChecked) {
    // Render null or a loading indicator while checking authentication
    return null // or <LoadingIndicator /> if you have a loading component
  }

  // Redirect to signin if not authenticated, else render the children
  return isAuth ? children : <Navigate to={routes.signin} replace />
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
    path: routes.profile,
    element: (
      <ProtectedRoute>
        <Profile />
      </ProtectedRoute>
    )
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
