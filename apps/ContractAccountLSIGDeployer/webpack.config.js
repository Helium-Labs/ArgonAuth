import path from 'path'
import { fileURLToPath } from 'url'
import webpack from 'webpack'

const filename = fileURLToPath(import.meta.url)
const dirname = path.dirname(filename)

const mode = process.env.NODE_ENV ?? 'production'

export default {
  entry: './src/index.ts', // Adjust with your main entry file
  output: {
    filename: `worker.${mode}.js`,
    path: path.join(dirname, 'dist')
  },
  mode,
  resolve: {
    extensions: ['.ts', '.tsx', '.js'],
    plugins: []
  },
  module: {
    rules: [
      {
        test: /\.tsx?$/,
        use: {
          loader: 'ts-loader',
          options: {
            transpileOnly: true
          }
        },
        exclude: /node_modules|tests/ // Exclude tests folder and node_modules
      }
    ]
  },
  plugins: [
    new webpack.DefinePlugin({
      'process.env.NODE_ENV': JSON.stringify(mode)
    })
  ],
  optimization: {
    minimize: mode === 'production', // Enable minimization in production mode
    usedExports: true // Enable tree shaking
  }
}
