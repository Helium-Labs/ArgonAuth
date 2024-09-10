import React from 'react'

export const Spinner = (): JSX.Element => {
  return (
        <svg
                className="mr-3 h-5 w-5 text-white animate-spin -ml-1"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
              >
                <circle
                  className="opacity-30"
                  cx="12"
                  cy="12"
                  r="9"
                  stroke="currentColor"
                  strokeWidth="3"
                ></circle>
                <path
                  className="opacity-80"
                  fill="currentColor"
                  d="M5 12a7 7 0 017-7V1C6.373 1 1 6.373 1 12h4zm2 4.291A6.962 6.962 0 015 12H1c0 2.842 1.135 5.424 2.5 7.438l3.5-3.147z"
                ></path>
              </svg>
  )
}
