import React, { useState } from 'react'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { PenTool, Type, Upload } from 'lucide-react'
import { useSignature } from '@/contexts/SignatureContext'
import { DrawSignature } from './components/DrawSignature'
import { TypedSignature } from './components/TypedSignature'
import { UploadSignature } from './components/UploadSignature'

interface SignatureModalProps {
  onSave?: (signature: { type: string; data: string; name?: string; font?: string; color?: string }) => void
  trigger?: React.ReactNode
  defaultOpen?: boolean
}

export const SignatureModal: React.FC<SignatureModalProps> = ({ 
  onSave, 
  trigger,
  defaultOpen = false 
}) => {
  const [isOpen, setIsOpen] = useState(defaultOpen)
  const [activeTab, setActiveTab] = useState<'draw' | 'type' | 'upload'>('draw')
  const { signature, clearSignature } = useSignature()

  const handleSave = () => {
    if (signature.data && signature.type && onSave) {
      onSave({
        type: signature.type,
        data: signature.data,
        name: signature.name,
        font: signature.font,
        color: signature.color
      })
    }
    setIsOpen(false)
  }

  const handleClear = () => {
    clearSignature()
  }

  const defaultTrigger = (
    <Button variant="outline" className="w-full h-20 border-dashed">
      {signature.data ? (
        <div className="flex flex-col items-center">
          {signature.type === 'drawn' || signature.type === 'uploaded' ? (
            <img 
              src={signature.data} 
              alt="Signature" 
              className="max-h-12 max-w-full object-contain"
            />
          ) : signature.type === 'typed' ? (
            <div 
              className="text-lg font-medium"
              style={{ 
                fontFamily: signature.font || 'cursive',
                color: signature.color || '#000000'
              }}
            >
              {signature.name || signature.data}
            </div>
          ) : null}
          <span className="text-xs text-gray-500 mt-1">Click to edit</span>
        </div>
      ) : (
        <div className="flex flex-col items-center text-gray-500">
          <PenTool className="h-6 w-6 mb-1" />
          <span className="text-sm">Add Signature</span>
        </div>
      )}
    </Button>
  )

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      <DialogTrigger asChild>
        {trigger || defaultTrigger}
      </DialogTrigger>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>Create Signature</DialogTitle>
        </DialogHeader>
        
        <Tabs value={activeTab} onValueChange={(value) => setActiveTab(value as any)}>
          <TabsList className="grid w-full grid-cols-3">
            <TabsTrigger value="draw" className="flex items-center gap-2">
              <PenTool className="h-4 w-4" />
              Draw
            </TabsTrigger>
            <TabsTrigger value="type" className="flex items-center gap-2">
              <Type className="h-4 w-4" />
              Type
            </TabsTrigger>
            <TabsTrigger value="upload" className="flex items-center gap-2">
              <Upload className="h-4 w-4" />
              Upload
            </TabsTrigger>
          </TabsList>

          <TabsContent value="draw" className="mt-6">
            <DrawSignature />
          </TabsContent>

          <TabsContent value="type" className="mt-6">
            <TypedSignature />
          </TabsContent>

          <TabsContent value="upload" className="mt-6">
            <UploadSignature />
          </TabsContent>
        </Tabs>

        <div className="flex justify-end gap-2 mt-6">
          <Button variant="outline" onClick={handleClear}>
            Clear
          </Button>
          <Button 
            onClick={handleSave}
            disabled={!signature.data}
          >
            Save Signature
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}