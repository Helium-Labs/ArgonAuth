import { defineConfig } from 'vite'
import reactRefresh from '@vitejs/plugin-react-refresh'
import { nodePolyfills } from 'vite-plugin-node-polyfills'
import react from '@vitejs/plugin-react' // Updated plugin for React
// import css
import 'tailwindcss'
import 'tailwindcss/defaultTheme'
import 'tailwindcss/colors'
// import scss

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    reactRefresh(),
    nodePolyfills(),
    react()
  ],
  css: {
    preprocessorOptions: {
      scss: {
        // SCSS options
      }
    },
    postcss: {
      plugins: [
        require('tailwindcss'), // Make sure TailwindCSS is loaded here
        require('autoprefixer')
      ]
    }
  },
  build: {
    lib: {
      entry: 'src/index.ts', // or 'src/index.tsx' for JSX primary exports
      name: 'AuthClient',
      formats: ['es', 'umd']
    },
    rollupOptions: {
      external: ['react', 'react-dom', '@noble/hashes/sha512'],
      output: {
        globals: {
          react: 'React',
          'react-dom': 'ReactDOM'
        }
      }
    }
  }
})
