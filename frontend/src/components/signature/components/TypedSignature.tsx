import React, { useState, useEffect } from 'react'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Button } from '@/components/ui/button'
import { useSignature } from '@/contexts/SignatureContext'

const signatureFonts = [
  { name: 'Cursive', value: 'cursive', example: 'Your Signature' },
  { name: 'Dancing Script', value: '"Dancing Script", cursive', example: 'Your Signature' },
  { name: 'Great Vibes', value: '"Great Vibes", cursive', example: 'Your Signature' },
  { name: 'Pacifico', value: '"Pacifico", cursive', example: 'Your Signature' },
  { name: 'Satisfy', value: '"Satisfy", cursive', example: 'Your Signature' },
  { name: 'Caveat', value: '"Caveat", cursive', example: 'Your Signature' },
]

const colors = [
  '#000000', // Black
  '#1f2937', // Gray-800  
  '#3b82f6', // Blue-500
  '#8b5cf6', // Violet-500
  '#10b981', // Emerald-500
  '#f59e0b', // Amber-500
  '#ef4444', // Red-500
]

export const TypedSignature: React.FC = () => {
  const [typedText, setTypedText] = useState('')
  const [selectedFont, setSelectedFont] = useState(signatureFonts[0].value)
  const { signature, setSignature, selectedColor, setSelectedColor } = useSignature()

  // Calculate dynamic font size based on text length
  const getFontSize = (text: string): number => {
    const baseSize = 48
    const minSize = 24
    const maxLength = 20
    
    if (text.length <= 5) return baseSize
    if (text.length >= maxLength) return minSize
    
    const reduction = (text.length - 5) * ((baseSize - minSize) / (maxLength - 5))
    return Math.max(minSize, baseSize - reduction)
  }

  const handleTextChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const text = e.target.value
    setTypedText(text)
    
    if (text.trim()) {
      setSignature({
        type: 'typed',
        data: text,
        name: text,
        font: selectedFont,
        color: selectedColor
      })
    } else {
      setSignature({ type: null, data: null })
    }
  }

  const handleFontChange = (font: string) => {
    setSelectedFont(font)
    if (typedText.trim()) {
      setSignature({
        type: 'typed',
        data: typedText,
        name: typedText,
        font: font,
        color: selectedColor
      })
    }
  }

  const handleColorChange = (color: string) => {
    setSelectedColor(color)
    if (typedText.trim()) {
      setSignature({
        type: 'typed',
        data: typedText,
        name: typedText,
        font: selectedFont,
        color: color
      })
    }
  }

  const handleClear = () => {
    setTypedText('')
    setSignature({ type: null, data: null })
  }

  // Load existing typed signature
  useEffect(() => {
    if (signature.type === 'typed' && (signature.name || signature.data)) {
      setTypedText(signature.name || signature.data || '')
      if (signature.font) setSelectedFont(signature.font)
      if (signature.color) setSelectedColor(signature.color)
    }
  }, [signature, setSelectedColor])

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <Label htmlFor="signature-text">Your Signature</Label>
        <Input
          id="signature-text"
          type="text"
          placeholder="Type your signature here..."
          value={typedText}
          onChange={handleTextChange}
          className="text-lg"
        />
      </div>

      <div className="space-y-3">
        <Label>Font Style</Label>
        <div className="grid grid-cols-2 gap-3">
          {signatureFonts.map((font) => (
            <button
              key={font.value}
              onClick={() => handleFontChange(font.value)}
              className={`p-3 border rounded-lg text-left transition-all ${
                selectedFont === font.value
                  ? 'border-primary bg-primary/5'
                  : 'border-gray-200 hover:border-gray-300'
              }`}
            >
              <div className="text-sm font-medium">{font.name}</div>
              <div 
                className="text-lg mt-1"
                style={{ fontFamily: font.value }}
              >
                {font.example}
              </div>
            </button>
          ))}
        </div>
      </div>

      <div className="space-y-3">
        <Label>Color</Label>
        <div className="flex gap-2">
          {colors.map((color) => (
            <button
              key={color}
              onClick={() => handleColorChange(color)}
              className={`w-8 h-8 rounded-full border-2 transition-all ${
                selectedColor === color 
                  ? 'border-gray-400 scale-110' 
                  : 'border-gray-200 hover:border-gray-300'
              }`}
              style={{ backgroundColor: color }}
              title={`Select ${color}`}
            />
          ))}
        </div>
      </div>

      {typedText && (
        <div className="space-y-3">
          <Label>Preview</Label>
          <div className="border rounded-lg p-6 bg-gray-50 text-center min-h-[120px] flex items-center justify-center">
            <div
              style={{
                fontFamily: selectedFont,
                fontSize: `${getFontSize(typedText)}px`,
                color: selectedColor,
                lineHeight: 1.2
              }}
            >
              {typedText}
            </div>
          </div>
        </div>
      )}

      <div className="flex justify-center">
        <Button 
          variant="outline" 
          onClick={handleClear}
          disabled={!typedText}
        >
          Clear Text
        </Button>
      </div>

      <div className="text-sm text-gray-500 text-center">
        Type your signature and customize the font style and color
      </div>
    </div>
  )
}