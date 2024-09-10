import { nodeResolve } from "@rollup/plugin-node-resolve";
import commonjs from "@rollup/plugin-commonjs";
import json from "@rollup/plugin-json";
import nodePolyfills from "rollup-plugin-polyfill-node";
import typescript from 'rollup-plugin-typescript2';

export default [
  // Browser config
  {
    input: "src/index.ts",
    output: [
      {
        file: "dist/bundle.mjs",
        format: "es",
        inlineDynamicImports: true,
      },
    ],
    plugins: [
      json(),
      commonjs(),
      nodePolyfills(),
      nodeResolve({
        preferBuiltins: true,
        browser: true,
        modulesOnly: false,
      }),
      typescript({
        tsconfig: "./tsconfig.esm.json",
        declaration: true,
        declarationDir: "dist"
      }),
    ],
    external: ["algosdk", "@noble/hashes/sha512", "@noble/ed25519", "@json-rpc-tools/utils"],
  },
  // Node config
  {
    input: "src/index.ts",
    output: [
      {
        dir: "dist/cjs",
        format: "cjs",
      },
    ],
    external: ["algosdk", "@noble/hashes/sha512", "@noble/ed25519", "@json-rpc-tools/utils"],
    plugins: [
      json(),
      commonjs(),
      nodeResolve({
        preferBuiltins: true,
      }),
      typescript({
        tsconfig: "./tsconfig.cjs.json",
        declaration: true,
        declarationDir: "dist"
      })
    ],
  },
];