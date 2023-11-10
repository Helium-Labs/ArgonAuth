## Setup

Use yarn.

```sh
# Install pnpm with your preferred method: https://pnpm.io/installation.
npm i -g pnpm

# Install all dependencies.
pnpm i
```

## Docs

See the following blog posts:

- [How to set up a TypeScript monorepo and make Go to definition work](https://medium.com/@NiGhTTraX/how-to-set-up-a-typescript-monorepo-with-lerna-c6acda7d4559)
- [Making TypeScript monorepos play nice with other tools](https://medium.com/@NiGhTTraX/making-typescript-monorepos-play-nice-with-other-tools-a8d197fdc680)

If you're looking for the project references solution checkout the [`project-references`](https://github.com/NiGhTTraX/ts-monorepo/tree/project-references) branch.

## Packages vs apps

This repo contains two types of workspaces:

- `packages`: meant to be published to npm and installed,
- `apps`: meant to be executed.

A good example to illustrate the difference is `create-react-app`: you wouldn't publish an app like this to npm, you would run it, more specifically you would build the JS bundle and then deploy that somewhere.

For packages, you don't want to bundle all the monorepo dependencies, and instead publish them individually. That's why packages have a separate build `tsconfig.json` that resolves monorepo dependencies to `node_modules`.

## Integrations

TLDR, depending on the build process you need to make minor adjustments to give the process awareness that it's in a workspaces monorepo, and to deal with the workspaces dependencies accordingly.

### Rollup

Must use `external: Object.keys(pkg.dependencies)`, otherwise rollup would inline our monorepo dependencies.

```
/* eslint-disable import/no-extraneous-dependencies */
import typescript from "@rollup/plugin-typescript";
import { defineConfig } from "rollup";
import pkg from "./package.json";

export default defineConfig({
  input: "src/index.tsx",
  // Without this, rollup would inline our monorepo dependencies.
  external: Object.keys(pkg.dependencies),
  plugins: [
    typescript({
      tsconfig: "./tsconfig.build.json",
    }),
  ],
  output: [{ dir: "./dist", format: "cjs", sourcemap: true }],
});
```

### ts-node

Use [tsconfig-paths](https://www.npmjs.com/package/tsconfig-paths) to resolve the path aliases at runtime:

```json
{
  "scripts": {
    "start": "ts-node -r tsconfig-paths/register src/index.ts"
  }
}
```

See the full example [here](apps/ts-node).

### Babel

Use [babel-plugin-module-resolver](https://www.npmjs.com/package/babel-plugin-module-resolver) to resolve the path aliases:

```js
module.exports = {
  presets: [
    ["@babel/preset-env", { targets: { node: "current" } }],
    "@babel/preset-typescript",
  ],

  plugins: [
    [
      "module-resolver",
      {
        alias: {
          "^@nighttrax/(.+)": "../\\1/src",
        },
      },
    ],
  ],
};
```

See the full example [here](apps/jest-babel).

### jest

If you use [ts-jest](https://github.com/kulshekhar/ts-jest) then you can use its `pathsToModuleNameMapper` helper: 

```js
const { pathsToModuleNameMapper } = require("ts-jest");
const { compilerOptions } = require("../../tsconfig.json");

module.exports = {
  preset: "ts-jest",

  moduleNameMapper: pathsToModuleNameMapper(compilerOptions.paths, {
    // This has to match the baseUrl defined in tsconfig.json.
    prefix: "<rootDir>/../../",
  }),
};
```

See the full example [here](apps/jest-tsjest).

### create-react-app

Use [craco](https://www.npmjs.com/package/@craco/craco) or [react-app-rewired](https://www.npmjs.com/package/react-app-rewired) to extend CRA's webpack config and apply the [tsconfig-paths-webpack-plugin](https://www.npmjs.com/package/tsconfig-paths-webpack-plugin):

```js
const TsconfigPathsPlugin = require("tsconfig-paths-webpack-plugin");

module.exports = (config) => {
  // Remove the ModuleScopePlugin which throws when we
  // try to import something outside of src/.
  config.resolve.plugins.pop();

  // Resolve the path aliases.
  config.resolve.plugins.push(new TsconfigPathsPlugin());

  // Let Babel compile outside of src/.
  const oneOfRule = config.module.rules.find((rule) => rule.oneOf);
    const tsRule = oneOfRule.oneOf.find((rule) =>
      rule.test.toString().includes("ts|tsx")
    );
  tsRule.include = undefined;
  tsRule.exclude = /node_modules/;

  return config;
};
```

See the full example [here](apps/cra). For tests, see the [jest example](#jest).

### NextJS

Extend Next's webpack config to enable compiling packages from the monorepo:

```js
module.exports = {
  webpack: (config) => {
    // Let Babel compile outside of src/.
    const tsRule = config.module.rules.find(
      (rule) => rule.test && rule.test.toString().includes("tsx|ts")
    );
    tsRule.include = undefined;
    tsRule.exclude = /node_modules/;

    return config;
  },
};
```

See the full example [here](apps/nextjs).

### Storybook

[Extend Storybook's webpack config](https://storybook.js.org/docs/react/builders/webpack#typescript-module-resolution) and apply the [tsconfig-paths-webpack-plugin](https://www.npmjs.com/package/tsconfig-paths-webpack-plugin):

```js
const TsconfigPathsPlugin = require('tsconfig-paths-webpack-plugin');

module.exports = {
  webpackFinal: async (config) => {
    config.resolve.plugins = [
      ...(config.resolve.plugins || []),
      new TsconfigPathsPlugin({
        extensions: config.resolve.extensions,
      }),
    ];
    return config;
  },
};
```

See the full example [here](apps/storybook).
