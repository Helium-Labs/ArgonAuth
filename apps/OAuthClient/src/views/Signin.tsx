import React from 'react'
import { Link } from 'react-router-dom'
import { routes } from '../main'
import toast from 'react-hot-toast'
import { keychain } from '../lib'
import useOAuthParams from '../hooks/oauthParams'
import { Spinner } from '../components'
const Home = (): JSX.Element => {
  const { redirectUri, state, codeChallenge, username, cspk } =
    useOAuthParams()
  const [formUsername, setUsername] = React.useState<string>('')
  const [isLoading, setIsLoading] = React.useState<boolean>(false)
  React.useEffect(() => {
    if (username != null) {
      setUsername(username)
    }
  }, [username])

  const handleSignin = (e: React.FormEvent<HTMLFormElement>): void => {
    e.preventDefault()
    console.log('handleSignin', formUsername)
    if (
      redirectUri == null ||
      state == null ||
      codeChallenge == null ||
      cspk == null
    ) {
      toast.error('Query Params are invalid.')
      return
    }
    setIsLoading(true)
    keychain
      .signIn({
        username: formUsername,
        redirectUri,
        state,
        codeChallenge,
        cspk
      })
      .then(() => {
        setIsLoading(false)
        toast.success('Sign in successful')
        toast.success('Attempting sign')
      })
      .catch((e) => {
        setIsLoading(false)
        toast.error('Error Occurred')
        console.error(e)
      })
  }
  return (
    <main className="h-screen flex flex-col justify-center items-center">
      <div className="mx-auto max-w-[360px] w-full px-4 py-16 lg:px-8 bg-white rounded-lg sm:border">
        <div className="mx-auto max-w-lg text-center">
          <h1 className="text-2xl font-bold sm:text-3xl">Sign-In</h1>
          <p className="mt-4 text-gray-500">Sign into your account.</p>
        </div>
        <form
          action=""
          className="mx-auto mt-8 space-y-4 w-full max-w-md"
          onSubmit={handleSignin}
        >
          <div>
            <label htmlFor="email" className="sr-only">
              Email
            </label>
            <div className="relative">
              <input
                type="email"
                className="w-full rounded-lg border-gray-200 p-4 pe-12 text-sm shadow-sm"
                placeholder="Enter email"
                value={formUsername}
                onChange={(e) => {
                  setUsername(e.target.value)
                }}
              />
              <span className="absolute inset-y-0 end-0 grid place-content-center px-4">
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  className="h-4 w-4 text-gray-400"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M16 12a4 4 0 10-8 0 4 4 0 008 0zm0 0v1.5a2.5 2.5 0 005 0V12a9 9 0 10-9 9m4.5-1.206a8.959 8.959 0 01-4.5 1.207"
                  />
                </svg>
              </span>
            </div>
          </div>
          <div className="flex flex-col items-center justify-between gap-5">
            <button
              type="submit"
              className="inline-block rounded-lg bg-blue-500 px-5 py-3 text-sm font-medium text-white flex transition-all hover:bg-blue-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
            >
              {isLoading && <Spinner />}
              Sign in
            </button>
            <p className="text-sm text-gray-500">
              No account?{' '}
              <Link
                to={routes.signup}
                className="text-indigo-400 focus:outline-none focus:underline focus:text-indigo-500"
              >
                Sign in
              </Link>
            </p>
          </div>
        </form>
      </div>
    </main>
  )
}

export default Home
