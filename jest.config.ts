import type { Config } from 'jest'

const config: Config = {
  projects: [
    {
      displayName: 'test',
      preset: 'ts-jest',
      testMatch: ['<rootDir>/**/*test.ts']
    }
  ]
}

export default config
