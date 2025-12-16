import React, { createContext, useContext, useState, ReactNode } from 'react'

export interface SignatureData {
  type: 'drawn' | 'typed' | 'uploaded' | null
  data: string | null
  name?: string
  font?: string
  color?: string
}

interface SignatureContextType {
  signature: SignatureData
  setSignature: React.Dispatch<React.SetStateAction<SignatureData>>
  clearSignature: () => void
  selectedColor: string
  setSelectedColor: (color: string) => void
}

const SignatureContext = createContext<SignatureContextType | undefined>(undefined)

export const useSignature = () => {
  const context = useContext(SignatureContext)
  if (!context) {
    throw new Error('useSignature must be used within a SignatureProvider')
  }
  return context
}

interface SignatureProviderProps {
  children: ReactNode
  initialSignature?: SignatureData
}

export const SignatureProvider: React.FC<SignatureProviderProps> = ({ 
  children, 
  initialSignature 
}) => {
  const [signature, setSignature] = useState<SignatureData>(
    initialSignature || { type: null, data: null }
  )
  const [selectedColor, setSelectedColor] = useState(
    initialSignature?.color || '#000000'
  )

  const clearSignature = () => {
    setSignature({ type: null, data: null })
  }

  return (
    <SignatureContext.Provider
      value={{
        signature,
        setSignature,
        clearSignature,
        selectedColor,
        setSelectedColor,
      }}
    >
      {children}
    </SignatureContext.Provider>
  )
}