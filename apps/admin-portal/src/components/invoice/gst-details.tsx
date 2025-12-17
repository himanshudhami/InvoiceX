import { useInvoiceForm } from './form-context'

// Indian state codes for place of supply
const INDIAN_STATES = [
  { code: '01', name: 'Jammu & Kashmir' },
  { code: '02', name: 'Himachal Pradesh' },
  { code: '03', name: 'Punjab' },
  { code: '04', name: 'Chandigarh' },
  { code: '05', name: 'Uttarakhand' },
  { code: '06', name: 'Haryana' },
  { code: '07', name: 'Delhi' },
  { code: '08', name: 'Rajasthan' },
  { code: '09', name: 'Uttar Pradesh' },
  { code: '10', name: 'Bihar' },
  { code: '11', name: 'Sikkim' },
  { code: '12', name: 'Arunachal Pradesh' },
  { code: '13', name: 'Nagaland' },
  { code: '14', name: 'Manipur' },
  { code: '15', name: 'Mizoram' },
  { code: '16', name: 'Tripura' },
  { code: '17', name: 'Meghalaya' },
  { code: '18', name: 'Assam' },
  { code: '19', name: 'West Bengal' },
  { code: '20', name: 'Jharkhand' },
  { code: '21', name: 'Odisha' },
  { code: '22', name: 'Chhattisgarh' },
  { code: '23', name: 'Madhya Pradesh' },
  { code: '24', name: 'Gujarat' },
  { code: '26', name: 'Dadra & Nagar Haveli and Daman & Diu' },
  { code: '27', name: 'Maharashtra' },
  { code: '29', name: 'Karnataka' },
  { code: '30', name: 'Goa' },
  { code: '31', name: 'Lakshadweep' },
  { code: '32', name: 'Kerala' },
  { code: '33', name: 'Tamil Nadu' },
  { code: '34', name: 'Puducherry' },
  { code: '35', name: 'Andaman & Nicobar Islands' },
  { code: '36', name: 'Telangana' },
  { code: '37', name: 'Andhra Pradesh' },
  { code: '38', name: 'Ladakh' },
]

const INVOICE_TYPES = [
  { value: 'export', label: 'Export Invoice' },
  { value: 'domestic_b2b', label: 'Domestic B2B' },
  { value: 'domestic_b2c', label: 'Domestic B2C' },
  { value: 'sez', label: 'SEZ Supply' },
  { value: 'deemed_export', label: 'Deemed Export' },
]

export const GstDetails = () => {
  const { formData, updateField, companies } = useInvoiceForm()

  // Get company's state code for determining supply type
  const selectedCompany = companies.find(c => c.id === formData.companyId)
  const companyStateCode = selectedCompany?.gstStateCode || ''

  const handleInvoiceTypeChange = (type: string) => {
    updateField('invoiceType', type)

    // Auto-set supply type based on invoice type
    if (type === 'export' || type === 'sez') {
      updateField('supplyType', 'export')
      updateField('placeOfSupply', 'export')
    } else {
      // For domestic, determine based on place of supply
      updateField('supplyType', formData.placeOfSupply === companyStateCode ? 'intra_state' : 'inter_state')
    }
  }

  const handlePlaceOfSupplyChange = (stateCode: string) => {
    updateField('placeOfSupply', stateCode)

    // Auto-determine supply type
    if (formData.invoiceType === 'export' || formData.invoiceType === 'sez') {
      updateField('supplyType', 'export')
    } else if (stateCode === companyStateCode) {
      updateField('supplyType', 'intra_state')
    } else {
      updateField('supplyType', 'inter_state')
    }
  }

  // Don't show GST details for export invoices
  const showGstFields = formData.invoiceType !== 'export'

  return (
    <div className="space-y-4">
      <h3 className="text-lg font-medium text-gray-900">GST Classification</h3>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {/* Invoice Type */}
        <div>
          <label htmlFor="invoiceType" className="block text-sm font-medium text-gray-700 mb-1">
            Invoice Type
          </label>
          <select
            id="invoiceType"
            value={formData.invoiceType}
            onChange={(e) => handleInvoiceTypeChange(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          >
            {INVOICE_TYPES.map((type) => (
              <option key={type.value} value={type.value}>
                {type.label}
              </option>
            ))}
          </select>
        </div>

        {/* Place of Supply - only for domestic invoices */}
        {showGstFields && (
          <div>
            <label htmlFor="placeOfSupply" className="block text-sm font-medium text-gray-700 mb-1">
              Place of Supply
            </label>
            <select
              id="placeOfSupply"
              value={formData.placeOfSupply || ''}
              onChange={(e) => handlePlaceOfSupplyChange(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">Select State</option>
              {INDIAN_STATES.map((state) => (
                <option key={state.code} value={state.code}>
                  {state.code} - {state.name}
                </option>
              ))}
            </select>
          </div>
        )}

        {/* Supply Type Indicator */}
        {showGstFields && formData.supplyType && (
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Tax Type
            </label>
            <div className="px-3 py-2 border border-gray-200 rounded-md bg-gray-50">
              <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                formData.supplyType === 'intra_state'
                  ? 'bg-green-100 text-green-800'
                  : 'bg-blue-100 text-blue-800'
              }`}>
                {formData.supplyType === 'intra_state' ? 'CGST + SGST' : 'IGST'}
              </span>
              <p className="text-xs text-gray-500 mt-1">
                {formData.supplyType === 'intra_state' ? 'Intra-state supply' : 'Inter-state supply'}
              </p>
            </div>
          </div>
        )}
      </div>

      {/* Reverse Charge - only for domestic */}
      {showGstFields && (
        <div className="flex items-center">
          <input
            type="checkbox"
            id="reverseCharge"
            checked={formData.reverseCharge}
            onChange={(e) => updateField('reverseCharge', e.target.checked)}
            className="h-4 w-4 text-primary focus:ring-ring border-gray-300 rounded"
          />
          <label htmlFor="reverseCharge" className="ml-2 block text-sm text-gray-700">
            Reverse Charge Mechanism (RCM) applicable
          </label>
        </div>
      )}
    </div>
  )
}
