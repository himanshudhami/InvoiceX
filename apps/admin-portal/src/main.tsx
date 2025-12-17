import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import App from './App.tsx'
import { AuthProvider } from './contexts/AuthContext'
import './globals.css'
import * as serviceWorker from './serviceWorker.ts'
import { registerNotoSansFont, registerNunitoFont } from './utils/pdfFonts'

// Register PDF fonts once at app startup before any PDF components are loaded
// This prevents BindingError when components reload during HMR
registerNotoSansFont()
registerNunitoFont()

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <App />
      </AuthProvider>
    </BrowserRouter>
  </React.StrictMode>,
)

serviceWorker.register()
