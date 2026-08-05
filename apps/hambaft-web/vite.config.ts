import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

const proxyTarget = process.env.VITE_API_ORIGIN ?? 'http://localhost:5297'
const devPort = Number(process.env.VITE_PORT ?? 5173)

export default defineConfig({
  plugins: [react()],
  server: {
    port: devPort,
    proxy: Object.fromEntries(
      ['/api', '/hubs', '/health', '/alive', '/ready', '/internal'].map((path) => [
        path,
        { target: proxyTarget, changeOrigin: true, ws: path === '/hubs' },
      ]),
    ),
  },
  build: {
    outDir: '../../src/Hambaft.Api/wwwroot',
    emptyOutDir: true,
    rollupOptions: { output: { manualChunks: { react: ['react', 'react-dom', 'react-router-dom'], query: ['@tanstack/react-query', 'zustand'], realtime: ['@microsoft/signalr'], motion: ['motion'], validation: ['zod'] } } },
  },
  test: {
    environment: 'jsdom',
    setupFiles: './src/tests/setup.ts',
    include: ['src/**/*.test.{ts,tsx}'],
    css: true,
  },
})
