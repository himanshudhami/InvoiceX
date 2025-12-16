import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App.tsx'
import './globals.css'
import * as serviceWorker from './serviceWorker.ts'
import { registerNotoSansFont, registerNunitoFont } from './utils/pdfFonts'

// Register PDF fonts once at app startup before any PDF components are loaded
// This prevents BindingError when components reload during HMR
registerNotoSansFont()
registerNunitoFont()

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
)

serviceWorker.register()
