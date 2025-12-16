import React, { useRef, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Upload, X, Image } from 'lucide-react'
import { useSignature } from '@/contexts/SignatureContext'

export const UploadSignature: React.FC = () => {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [dragActive, setDragActive] = useState(false)
  const { signature, setSignature } = useSignature()

  const handleFileSelect = (file: File) => {
    if (file && file.type.startsWith('image/')) {
      const reader = new FileReader()
      reader.onload = (e) => {
        const result = e.target?.result as string
        setSignature({
          type: 'uploaded',
          data: result,
          name: file.name
        })
      }
      reader.readAsDataURL(file)
    }
  }

  const handleFileInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) {
      handleFileSelect(file)
    }
  }

  const handleDrag = (e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    if (e.type === 'dragenter' || e.type === 'dragover') {
      setDragActive(true)
    } else if (e.type === 'dragleave') {
      setDragActive(false)
    }
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setDragActive(false)

    const files = e.dataTransfer.files
    if (files?.[0]) {
      handleFileSelect(files[0])
    }
  }

  const handleUploadClick = () => {
    fileInputRef.current?.click()
  }

  const handleRemove = () => {
    setSignature({ type: null, data: null })
    if (fileInputRef.current) {
      fileInputRef.current.value = ''
    }
  }

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <Label>Upload Signature Image</Label>
        <p className="text-sm text-gray-600">
          Upload an image file of your handwritten signature. Supported formats: PNG, JPG, SVG
        </p>
      </div>

      <input
        ref={fileInputRef}
        type="file"
        accept="image/*"
        onChange={handleFileInputChange}
        className="hidden"
      />

      {signature.data && signature.type === 'uploaded' ? (
        <div className="space-y-4">
          <div className="border rounded-lg p-4 bg-gray-50">
            <div className="flex items-center justify-between mb-3">
              <Label>Uploaded Signature</Label>
              <Button
                variant="outline"
                size="sm"
                onClick={handleRemove}
                className="text-red-600 hover:text-red-700"
              >
                <X className="h-4 w-4 mr-1" />
                Remove
              </Button>
            </div>
            <div className="flex justify-center">
              <img
                src={signature.data}
                alt="Uploaded signature"
                className="max-h-32 max-w-full object-contain border rounded"
              />
            </div>
            {signature.name && (
              <p className="text-sm text-gray-500 text-center mt-2">
                {signature.name}
              </p>
            )}
          </div>
        </div>
      ) : (
        <div
          className={`border-2 border-dashed rounded-lg p-8 text-center cursor-pointer transition-colors ${
            dragActive 
              ? 'border-primary bg-primary/5' 
              : 'border-gray-300 hover:border-gray-400'
          }`}
          onDragEnter={handleDrag}
          onDragLeave={handleDrag}
          onDragOver={handleDrag}
          onDrop={handleDrop}
          onClick={handleUploadClick}
        >
          <div className="flex flex-col items-center space-y-3">
            <div className="p-3 bg-gray-100 rounded-full">
              {dragActive ? (
                <Upload className="h-8 w-8 text-primary" />
              ) : (
                <Image className="h-8 w-8 text-gray-400" />
              )}
            </div>
            <div>
              <p className="text-lg font-medium text-gray-900">
                {dragActive ? 'Drop your image here' : 'Upload signature image'}
              </p>
              <p className="text-sm text-gray-500">
                Drag and drop or click to select a file
              </p>
            </div>
            <Button variant="outline" type="button">
              <Upload className="h-4 w-4 mr-2" />
              Choose File
            </Button>
          </div>
        </div>
      )}

      <div className="text-sm text-gray-500 text-center">
        For best results, use a high-resolution image with a transparent or white background
      </div>
    </div>
  )
}