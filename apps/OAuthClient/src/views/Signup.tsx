import React from 'react'
import { routes } from '../main'
import { Link } from 'react-router-dom'
import { keychain } from '../lib'
import toast from 'react-hot-toast'
import { Spinner } from '../components'

const Signup = (): JSX.Element => {
  const [username, setUsername] = React.useState<string>('')
  const [isLoading, setIsLoading] = React.useState<boolean>(false)

  const handleSignup = (e: React.FormEvent<HTMLFormElement>): void => {
    e.preventDefault()
    setIsLoading(true)
    keychain.register(username).then(() => {
      toast.success('Sign in successful')
    }).catch(e => {
      toast.error('Error Occurred')
      console.error(e)
    }).finally(() => {
      setIsLoading(false)
    })
  }
  return (
    <main className="h-screen flex flex-col justify-center items-center">
      <div className="mx-auto max-w-[360px] w-full px-4 py-16 lg:px-8 bg-white rounded-lg sm:border">
        <div className="mx-auto max-w-lg text-center">
          <h1 className="text-2xl font-bold sm:text-3xl">Sign-Up</h1>
          <p className="mt-4 text-gray-500">Create an account.</p>
        </div>
        <form
          action=""
          className="mx-auto mt-8 space-y-4 w-full max-w-md"
          onSubmit={handleSignup}
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
              className="inline-block rounded-lg bg-blue-500 px-5 py-3 text-sm font-medium text-white flex"
            >
              {isLoading && <Spinner />}
              Sign up
            </button>
            <p className="text-sm text-gray-500">
              Have an account?{' '}
              <Link
                to={routes.signin}
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

export default Signup
