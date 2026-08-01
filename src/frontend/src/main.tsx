import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App'
import './styles.css'

declare global {
  interface Window {
    __APP_CONFIG__?: {
      apiUrl?: string
    }
  }
}

const runtimeApiUrl = window.__APP_CONFIG__?.apiUrl?.trim() ?? ''

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App apiBaseUrl={runtimeApiUrl} />
  </React.StrictMode>,
)
