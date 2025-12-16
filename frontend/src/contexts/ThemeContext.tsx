import React, { createContext, useContext, useEffect, useState } from 'react'

type Theme = 'blue' | 'green' | 'violet' | 'orange' | 'red' | 'rose' | 'slate'

interface ThemeContextType {
  theme: Theme
  setTheme: (theme: Theme) => void
  themes: {
    name: Theme
    label: string
    color: string
  }[]
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined)

export const themes = [
  { name: 'blue' as const, label: 'Blue', color: 'hsl(221.2 83.2% 53.3%)' },
  { name: 'green' as const, label: 'Green', color: 'hsl(142.1 76.2% 36.3%)' },
  { name: 'violet' as const, label: 'Violet', color: 'hsl(262.1 83.3% 57.8%)' },
  { name: 'orange' as const, label: 'Orange', color: 'hsl(24.6 95% 53.1%)' },
  { name: 'red' as const, label: 'Red', color: 'hsl(0 84.2% 60.2%)' },
  { name: 'rose' as const, label: 'Rose', color: 'hsl(346.8 77.2% 49.8%)' },
  { name: 'slate' as const, label: 'Slate', color: 'hsl(215.4 16.3% 46.9%)' },
]

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const [theme, setTheme] = useState<Theme>(() => {
    const savedTheme = localStorage.getItem('invoice-theme') as Theme
    return savedTheme || 'blue'
  })

  useEffect(() => {
    const root = window.document.documentElement
    root.setAttribute('data-theme', theme)
    localStorage.setItem('invoice-theme', theme)
  }, [theme])

  return (
    <ThemeContext.Provider
      value={{
        theme,
        setTheme,
        themes,
      }}
    >
      {children}
    </ThemeContext.Provider>
  )
}

export function useTheme() {
  const context = useContext(ThemeContext)
  if (context === undefined) {
    throw new Error('useTheme must be used within a ThemeProvider')
  }
  return context
}