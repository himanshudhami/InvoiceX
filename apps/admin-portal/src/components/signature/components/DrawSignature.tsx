import React, { useRef, useEffect } from 'react'
import SignatureCanvas from 'react-signature-canvas'
import { Button } from '@/components/ui/button'
import { useSignature } from '@/contexts/SignatureContext'

const colors = [
  '#000000', // Black
  '#1f2937', // Gray-800  
  '#3b82f6', // Blue-500
  '#8b5cf6', // Violet-500
  '#10b981', // Emerald-500
  '#f59e0b', // Amber-500
  '#ef4444', // Red-500
]

export const DrawSignature: React.FC = () => {
  const signatureRef = useRef<SignatureCanvas>(null)
  const { signature, setSignature, selectedColor, setSelectedColor } = useSignature()

  const handleCanvasEnd = () => {
    if (signatureRef.current && !signatureRef.current.isEmpty()) {
      const dataURL = signatureRef.current.toDataURL()
      setSignature({
        type: 'drawn',
        data: dataURL,
        color: selectedColor
      })
    }
  }

  const handleClear = () => {
    if (signatureRef.current) {
      signatureRef.current.clear()
      setSignature({ type: null, data: null })
    }
  }

  // Restore signature if it exists and is of drawn type
  useEffect(() => {
    if (signature.type === 'drawn' && signature.data && signatureRef.current) {
      signatureRef.current.fromDataURL(signature.data)
    }
  }, [signature])

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3">
        <span className="text-sm font-medium">Pen Color:</span>
        <div className="flex gap-2">
          {colors.map((color) => (
            <button
              key={color}
              onClick={() => setSelectedColor(color)}
              className={`w-6 h-6 rounded-full border-2 transition-all ${
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
      
      <div className="border-2 border-dashed border-gray-300 rounded-lg p-4">
        <SignatureCanvas
          ref={signatureRef}
          velocityFilterWeight={1}
          minWidth={1.4}
          maxWidth={1.4}
          penColor={selectedColor}
          canvasProps={{
            style: {
              background: '#f9fafb',
              borderRadius: '8px',
              width: '100%',
              height: '240px',
            }
          }}
          onEnd={handleCanvasEnd}
        />
      </div>

      <div className="flex justify-center">
        <Button 
          variant="outline" 
          onClick={handleClear}
          disabled={!signatureRef.current || signatureRef.current.isEmpty()}
        >
          Clear Canvas
        </Button>
      </div>

      <div className="text-sm text-gray-500 text-center">
        Draw your signature above using your mouse or touch screen
      </div>
    </div>
  )
}