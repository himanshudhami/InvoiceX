import { useState, useEffect } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import { Trash2, Building2, Settings as SettingsIcon, PenTool } from 'lucide-react'
import { SignatureProvider } from '@/contexts/SignatureContext'
import { SignatureModal } from '@/components/signature/SignatureModal'
import { useCompanies, useUpdateCompany } from '@/hooks/api/useCompanies'
import { useTaxRates, useCreateTaxRate, useUpdateTaxRate, useDeleteTaxRate } from '@/hooks/api/useTaxRates'
import { useInvoiceTemplates, useCreateInvoiceTemplate, useUpdateInvoiceTemplate, useDeleteInvoiceTemplate } from '@/hooks/api/useInvoiceTemplates'
import { Company } from '@/services/api/types'
import toast from 'react-hot-toast'

const Settings = () => {
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('')
  
  // Payment instructions form state
  const [paymentInstructionsForm, setPaymentInstructionsForm] = useState('')

  // Tax rate form state  
  const [taxRateForm, setTaxRateForm] = useState({
    name: '',
    rate: '',
    isDefault: false,
    isActive: true
  })

  // Invoice template form state
  const [templateForm, setTemplateForm] = useState({
    name: '',
    templateData: '',
    isDefault: false
  })

  // Signature state
  const [currentSignature, setCurrentSignature] = useState<{
    type: string | null
    data: string | null
    name?: string
    font?: string
    color?: string
  }>({ type: null, data: null })

  // API hooks
  const { data: companies, isLoading: companiesLoading } = useCompanies()
  const { data: taxRates, isLoading: taxRatesLoading } = useTaxRates()
  const { data: templates, isLoading: templatesLoading } = useInvoiceTemplates()

  const updateCompany = useUpdateCompany()
  const createTaxRate = useCreateTaxRate()
  const updateTaxRate = useUpdateTaxRate()
  const deleteTaxRate = useDeleteTaxRate()
  const createTemplate = useCreateInvoiceTemplate()
  const updateTemplate = useUpdateInvoiceTemplate()
  const deleteTemplate = useDeleteInvoiceTemplate()

  // Get selected company
  const selectedCompany = companies?.find(c => c.id === selectedCompanyId)

  // Initialize with first company if available
  useEffect(() => {
    if (companies && companies.length > 0 && !selectedCompanyId) {
      setSelectedCompanyId(companies[0].id)
    }
  }, [companies, selectedCompanyId])

  // Update form when company changes
  useEffect(() => {
    // Find the company directly using the ID to ensure we have the latest data
    const company = companies?.find(c => c.id === selectedCompanyId)
    
    if (company) {
      setPaymentInstructionsForm(company.paymentInstructions || '')
      // Load existing signature if available
      if (company.signatureType && company.signatureData) {
        setCurrentSignature({
          type: company.signatureType,
          data: company.signatureData,
          name: company.signatureName,
          font: company.signatureFont,
          color: company.signatureColor
        })
      } else {
        setCurrentSignature({ type: null, data: null })
      }
    } else {
      // Reset when no company is selected
      setPaymentInstructionsForm('')
      setCurrentSignature({ type: null, data: null })
    }
  }, [selectedCompanyId, companies]) // Depend on both selectedCompanyId and companies

  // Filter data by selected company
  const companyTaxRates = taxRates?.filter(rate => rate.companyId === selectedCompanyId) || []
  const companyTemplates = templates?.filter(template => template.companyId === selectedCompanyId) || []

  const handleSavePaymentInstructions = async () => {
    if (!selectedCompany) {
      toast.error('Please select a company first')
      return
    }

    try {
      await updateCompany.mutateAsync({
        id: selectedCompany.id,
        data: {
          ...selectedCompany,
          paymentInstructions: paymentInstructionsForm
        }
      })
      toast.success('Payment instructions saved successfully')
    } catch (error) {
      toast.error('Failed to save payment instructions')
    }
  }

  const handleSaveSignature = async (signature: { type: string; data: string; name?: string; font?: string; color?: string }) => {
    if (!selectedCompany) {
      toast.error('Please select a company first')
      return
    }

    try {
      await updateCompany.mutateAsync({
        id: selectedCompany.id,
        data: {
          ...selectedCompany,
          signatureType: signature.type,
          signatureData: signature.data,
          signatureName: signature.name || null,
          signatureFont: signature.font || null,
          signatureColor: signature.color || null
        }
      })
      setCurrentSignature(signature)
      toast.success('Signature saved successfully')
    } catch (error) {
      toast.error('Failed to save signature')
    }
  }

  const handleClearSignature = async () => {
    if (!selectedCompany) {
      toast.error('Please select a company first')
      return
    }

    try {
      await updateCompany.mutateAsync({
        id: selectedCompany.id,
        data: {
          ...selectedCompany,
          signatureType: null,
          signatureData: null,
          signatureName: null,
          signatureFont: null,
          signatureColor: null
        }
      })
      setCurrentSignature({ type: null, data: null })
      toast.success('Signature cleared successfully')
    } catch (error) {
      toast.error('Failed to clear signature')
    }
  }

  const handleSaveTaxRate = async () => {
    if (!taxRateForm.name || !taxRateForm.rate) {
      toast.error('Please fill in all required fields')
      return
    }

    if (!selectedCompanyId) {
      toast.error('Please select a company first')
      return
    }

    try {
      await createTaxRate.mutateAsync({
        ...taxRateForm,
        rate: parseFloat(taxRateForm.rate),
        companyId: selectedCompanyId
      })
      setTaxRateForm({ name: '', rate: '', isDefault: false, isActive: true })
      toast.success('Tax rate created successfully')
    } catch (error) {
      toast.error('Failed to save tax rate')
    }
  }

  const handleDeleteTaxRate = async (id: string) => {
    try {
      await deleteTaxRate.mutateAsync(id)
      toast.success('Tax rate deleted successfully')
    } catch (error) {
      toast.error('Failed to delete tax rate')
    }
  }

  const handleSaveTemplate = async () => {
    if (!templateForm.name || !templateForm.templateData) {
      toast.error('Please fill in all required fields')
      return
    }

    if (!selectedCompanyId) {
      toast.error('Please select a company first')
      return
    }

    try {
      await createTemplate.mutateAsync({
        ...templateForm,
        companyId: selectedCompanyId
      })
      setTemplateForm({ name: '', templateData: '', isDefault: false })
      toast.success('Invoice template created successfully')
    } catch (error) {
      toast.error('Failed to save template')
    }
  }

  const handleDeleteTemplate = async (id: string) => {
    try {
      await deleteTemplate.mutateAsync(id)
      toast.success('Template deleted successfully')
    } catch (error) {
      toast.error('Failed to delete template')
    }
  }

  if (companiesLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    )
  }

  if (!companies || companies.length === 0) {
    return (
      <div className="text-center py-12">
        <Building2 className="mx-auto h-12 w-12 text-gray-400" />
        <h3 className="mt-2 text-sm font-medium text-gray-900">No companies found</h3>
        <p className="mt-1 text-sm text-gray-500">
          You need to create a company first before configuring settings.
        </p>
        <div className="mt-6">
          <Button onClick={() => window.location.href = '/companies'}>
            <Building2 className="h-4 w-4 mr-2" />
            Manage Companies
          </Button>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Settings</h1>
          <p className="text-gray-600 mt-2">Configure company-specific settings</p>
        </div>
        <Button 
          variant="outline"
          onClick={() => window.location.href = '/companies'}
        >
          <Building2 className="h-4 w-4 mr-2" />
          Manage Companies
        </Button>
      </div>

      {/* Company Selector */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <SettingsIcon className="h-5 w-5" />
            Select Company
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center gap-4">
            <Label htmlFor="company-select" className="text-sm font-medium">
              Configure settings for:
            </Label>
            <select
              id="company-select"
              value={selectedCompanyId}
              onChange={(e) => setSelectedCompanyId(e.target.value)}
              className="flex h-10 w-full max-w-sm rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
            >
              {companies.map((company) => (
                <option key={company.id} value={company.id}>
                  {company.name}
                </option>
              ))}
            </select>
          </div>
          {selectedCompany && (
            <div className="mt-4 p-4 bg-muted/50 rounded-lg">
              <div className="flex items-center gap-2 text-sm">
                <Building2 className="h-4 w-4" />
                <span className="font-medium">{selectedCompany.name}</span>
                {selectedCompany.email && (
                  <span className="text-muted-foreground">• {selectedCompany.email}</span>
                )}
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {selectedCompany && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Company Signature */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <PenTool className="h-5 w-5" />
                Company Signature
              </CardTitle>
              <p className="text-sm text-gray-600">
                Digital signature for {selectedCompany.name}'s invoices and documents
              </p>
            </CardHeader>
            <CardContent>
              {selectedCompanyId && (
                <div className="space-y-4">
                  <SignatureProvider 
                    key={`${selectedCompanyId}-${currentSignature.type}-${currentSignature.data}`}
                    initialSignature={{
                      type: currentSignature.type as any,
                      data: currentSignature.data,
                      name: currentSignature.name,
                      font: currentSignature.font,
                      color: currentSignature.color
                    }}
                  >
                    <SignatureModal 
                      onSave={handleSaveSignature}
                    />
                  </SignatureProvider>
                  {currentSignature.data && (
                    <Button 
                      variant="outline" 
                      size="sm" 
                      onClick={handleClearSignature}
                      className="w-full text-red-600 hover:text-red-700"
                    >
                      Clear Signature
                    </Button>
                  )}
                </div>
              )}
            </CardContent>
          </Card>

          {/* Payment Instructions */}
          <Card>
            <CardHeader>
              <CardTitle>Payment Instructions</CardTitle>
              <p className="text-sm text-gray-600">
                Default payment instructions for {selectedCompany.name}'s invoices
              </p>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <Label htmlFor="payment-instructions">Instructions</Label>
                <textarea
                  id="payment-instructions"
                  rows={6}
                  value={paymentInstructionsForm}
                  onChange={(e) => setPaymentInstructionsForm(e.target.value)}
                  className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                  placeholder="Enter payment instructions (bank details, payment terms, etc.)"
                />
              </div>
              <Button 
                onClick={handleSavePaymentInstructions}
                disabled={updateCompany.isPending}
              >
                {updateCompany.isPending ? 'Saving...' : 'Save Instructions'}
              </Button>
            </CardContent>
          </Card>

          {/* Tax Rates */}
          <Card>
            <CardHeader>
              <CardTitle>Tax Rates</CardTitle>
              <p className="text-sm text-gray-600">
                Manage tax rates for {selectedCompany.name}
              </p>
            </CardHeader>
            <CardContent className="space-y-4">
              {taxRatesLoading ? (
                <div className="text-gray-500">Loading tax rates...</div>
              ) : (
                <>
                  {companyTaxRates.length > 0 && (
                    <div className="space-y-2">
                      <Label className="text-sm font-medium">Current Tax Rates</Label>
                      {companyTaxRates.map((rate) => (
                        <div key={rate.id} className="flex items-center justify-between p-2 border rounded">
                          <div>
                            <span className="font-medium">{rate.name}</span>
                            <span className="text-gray-500 ml-2">{rate.rate}%</span>
                            {rate.isDefault && <Badge variant="secondary" className="ml-2">Default</Badge>}
                          </div>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleDeleteTaxRate(rate.id)}
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      ))}
                    </div>
                  )}

                  <div className="border-t pt-4">
                    <Label className="text-sm font-medium">Add New Tax Rate</Label>
                    <div className="grid grid-cols-2 gap-2 mt-2">
                      <Input
                        placeholder="Tax name"
                        value={taxRateForm.name}
                        onChange={(e) => setTaxRateForm({ ...taxRateForm, name: e.target.value })}
                      />
                      <Input
                        placeholder="Rate %"
                        type="number"
                        step="0.01"
                        value={taxRateForm.rate}
                        onChange={(e) => setTaxRateForm({ ...taxRateForm, rate: e.target.value })}
                      />
                    </div>
                    <div className="flex items-center mt-2">
                      <input
                        type="checkbox"
                        id="default-tax"
                        checked={taxRateForm.isDefault}
                        onChange={(e) => setTaxRateForm({ ...taxRateForm, isDefault: e.target.checked })}
                        className="h-4 w-4 text-primary border-gray-300 rounded"
                      />
                      <Label htmlFor="default-tax" className="ml-2 text-sm">Set as default</Label>
                    </div>
                    <Button 
                      className="w-full mt-3" 
                      onClick={handleSaveTaxRate}
                      disabled={createTaxRate.isPending}
                    >
                      {createTaxRate.isPending ? 'Adding...' : 'Add Tax Rate'}
                    </Button>
                  </div>
                </>
              )}
            </CardContent>
          </Card>

          {/* Invoice Templates */}
          <Card className="lg:col-span-2">
            <CardHeader>
              <CardTitle>Invoice Templates</CardTitle>
              <p className="text-sm text-gray-600">
                Manage invoice templates for {selectedCompany.name}
              </p>
            </CardHeader>
            <CardContent className="space-y-4">
              {templatesLoading ? (
                <div className="text-gray-500">Loading templates...</div>
              ) : (
                <>
                  {companyTemplates.length > 0 && (
                    <div className="space-y-2">
                      <Label className="text-sm font-medium">Current Templates</Label>
                      {companyTemplates.map((template) => (
                        <div key={template.id} className="flex items-center justify-between p-3 border rounded">
                          <div>
                            <span className="font-medium">{template.name}</span>
                            {template.isDefault && <Badge variant="secondary" className="ml-2">Default</Badge>}
                          </div>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleDeleteTemplate(template.id)}
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      ))}
                    </div>
                  )}

                  <div className="border-t pt-4">
                    <Label className="text-sm font-medium">Add New Template</Label>
                    <div className="space-y-2 mt-2">
                      <Input
                        placeholder="Template name"
                        value={templateForm.name}
                        onChange={(e) => setTemplateForm({ ...templateForm, name: e.target.value })}
                      />
                      <textarea
                        placeholder="Template data (JSON format)"
                        rows={4}
                        value={templateForm.templateData}
                        onChange={(e) => setTemplateForm({ ...templateForm, templateData: e.target.value })}
                        className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                      />
                      <div className="flex items-center">
                        <input
                          type="checkbox"
                          id="default-template"
                          checked={templateForm.isDefault}
                          onChange={(e) => setTemplateForm({ ...templateForm, isDefault: e.target.checked })}
                          className="h-4 w-4 text-primary border-gray-300 rounded"
                        />
                        <Label htmlFor="default-template" className="ml-2 text-sm">Set as default</Label>
                      </div>
                    </div>
                    <Button 
                      className="w-full mt-3" 
                      onClick={handleSaveTemplate}
                      disabled={createTemplate.isPending}
                    >
                      {createTemplate.isPending ? 'Adding...' : 'Add Template'}
                    </Button>
                  </div>
                </>
              )}
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  )
}

export default Settings