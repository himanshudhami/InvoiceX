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
      '@react-pdf/pdfkit',
      '@react-pdf/textkit',
      '@react-pdf/primitives',
      'yoga-layout',
    ],
  },
  optimizeDeps: {
    esbuildOptions: {
      target: 'esnext',
    },
    // Force react-pdf to be pre-bundled as ESM (prevents star export errors)
    include: [
      '@react-pdf/renderer',
      '@react-pdf/layout',
      '@react-pdf/render',
      '@react-pdf/font',
      '@react-pdf/stylesheet',
      '@react-pdf/pdfkit',
      '@react-pdf/textkit',
      '@react-pdf/primitives',
      'yoga-layout',
      'yoga-layout/load',
    ],
  },
  esbuild: {
    target: 'esnext',
  },
  build: {
    target: 'esnext',
    commonjsOptions: {
      include: [/node_modules/],
    },
  },
  server: {
    host: '0.0.0.0',
    port: 3000,
    allowedHosts: [
      'rcmr.xcdify.com',
      'employee.rcmr.xcdify.com',
      '192.168.86.250',
      '192.168.86.50',
      'localhost',
    ],
  },
  preview: {
    host: '0.0.0.0',
    port: 3000,
    allowedHosts: [
      'rcmr.xcdify.com',
      'employee.rcmr.xcdify.com',
      '192.168.86.250',
      '192.168.86.50',
      'localhost',
    ],
  },
})
