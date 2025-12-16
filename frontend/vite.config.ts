import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'
import path from 'path'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
    // Prevent Vite from bundling multiple copies of react-pdf/yoga internals
    dedupe: [
      'react',
      'react-dom',
      '@react-pdf/renderer',
      '@react-pdf/layout',
      '@react-pdf/render',
      '@react-pdf/font',
      '@react-pdf/types',
      '@react-pdf/stylesheet',
      'yoga-layout',
    ],
  },
  optimizeDeps: {
    // Force react-pdf to be pre-bundled as ESM (prevents star export errors)
    include: [
      '@react-pdf/renderer',
    ],
  },
})
