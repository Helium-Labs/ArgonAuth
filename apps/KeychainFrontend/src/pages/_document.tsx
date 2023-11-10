import { Html, Head, Main, NextScript } from 'next/document'

export default function Document (): JSX.Element {
  return (
    <Html lang="en">
      <Head>
        <meta charSet="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <link rel="icon" href="/favicon.ico" />
        <meta name="description" content="Gradian Keychain" />
        <meta name="og:title" content="Gradian Keychain" />
      </Head>
      <body>
        <Main />
        <NextScript />
      </body>
    </Html>
  )
}
