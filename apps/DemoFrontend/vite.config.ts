import { defineConfig } from 'vite'
import reactRefresh from '@vitejs/plugin-react-refresh'
import { viteSingleFile } from 'vite-plugin-singlefile'
import { nodePolyfills } from 'vite-plugin-node-polyfills'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    reactRefresh(),
    nodePolyfills(),
    viteSingleFile()
  ],
  build: {
    assetsInlineLimit: 100000000,
    rollupOptions: {
      external: ['@noble/hashes/sha512']
    }
  }
})
