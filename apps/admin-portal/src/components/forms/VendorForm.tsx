import { useState, useEffect } from 'react';
import { Vendor, CreateVendorDto, UpdateVendorDto } from '@/services/api/types';
import { useCreateVendor, useUpdateVendor } from '@/features/vendors/hooks';
import { useCompanies } from '@/hooks/api/useCompanies';
import { cn } from '@/lib/utils';

// TDS Section options based on Indian Income Tax Act
const TDS_SECTIONS = [
  { value: '194C', label: '194C - Contractors' },
  { value: '194J', label: '194J - Professional/Technical Services' },
  { value: '194H', label: '194H - Commission/Brokerage' },
  { value: '194I', label: '194I - Rent' },
  { value: '194A', label: '194A - Interest (non-banking)' },
  { value: '194Q', label: '194Q - Purchase of Goods' },
  { value: '195', label: '195 - Payments to Non-Residents' },
];

const VENDOR_TYPES = [
  { value: 'registered', label: 'GST Registered' },
  { value: 'unregistered', label: 'Unregistered' },
  { value: 'composition', label: 'Composition Scheme' },
  { value: 'overseas', label: 'Overseas/Import' },
];

const MSME_CATEGORIES = [
  { value: 'micro', label: 'Micro Enterprise' },
  { value: 'small', label: 'Small Enterprise' },
  { value: 'medium', label: 'Medium Enterprise' },
];

interface VendorFormProps {
  vendor?: Vendor;
  onSuccess: () => void;
  onCancel: () => void;
}

export const VendorForm = ({ vendor, onSuccess, onCancel }: VendorFormProps) => {
  const [formData, setFormData] = useState<CreateVendorDto>({
    name: '',
    companyName: '',
    email: '',
    phone: '',
    addressLine1: '',
    addressLine2: '',
    city: '',
    state: '',
    zipCode: '',
    country: 'India',
    notes: '',
    creditLimit: 0,
    paymentTerms: 30,
    isActive: true,
    // GST Compliance
    vendorType: 'registered',
    gstin: '',
    gstStateCode: '',
    isGstRegistered: true,
    panNumber: '',
    // TDS
    tanNumber: '',
    defaultTdsSection: '',
    defaultTdsRate: 0,
    tdsApplicable: false,
    // MSME
    msmeRegistered: false,
    msmeRegistrationNumber: '',
    msmeCategory: '',
    // Bank
    bankAccountNumber: '',
    bankName: '',
    ifscCode: '',
    bankBranch: '',
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const createVendor = useCreateVendor();
  const updateVendor = useUpdateVendor();
  const { data: companies = [] } = useCompanies();

  const isEditing = !!vendor;
  const isLoading = createVendor.isPending || updateVendor.isPending;

  // Populate form with existing vendor data
  useEffect(() => {
    if (vendor) {
      setFormData({
        companyId: vendor.companyId,
        name: vendor.name || '',
        companyName: vendor.companyName || '',
        email: vendor.email || '',
        phone: vendor.phone || '',
        addressLine1: vendor.addressLine1 || '',
        addressLine2: vendor.addressLine2 || '',
        city: vendor.city || '',
        state: vendor.state || '',
        zipCode: vendor.zipCode || '',
        country: vendor.country || 'India',
        notes: vendor.notes || '',
        creditLimit: vendor.creditLimit || 0,
        paymentTerms: vendor.paymentTerms || 30,
        isActive: vendor.isActive ?? true,
        // GST
        vendorType: vendor.vendorType || 'registered',
        gstin: vendor.gstin || '',
        gstStateCode: vendor.gstStateCode || '',
        isGstRegistered: vendor.isGstRegistered ?? true,
        panNumber: vendor.panNumber || '',
        // TDS
        tanNumber: vendor.tanNumber || '',
        defaultTdsSection: vendor.defaultTdsSection || '',
        defaultTdsRate: vendor.defaultTdsRate || 0,
        tdsApplicable: vendor.tdsApplicable ?? false,
        // MSME
        msmeRegistered: vendor.msmeRegistered ?? false,
        msmeRegistrationNumber: vendor.msmeRegistrationNumber || '',
        msmeCategory: vendor.msmeCategory || '',
        // Bank
        bankAccountNumber: vendor.bankAccountNumber || '',
        bankName: vendor.bankName || '',
        ifscCode: vendor.ifscCode || '',
        bankBranch: vendor.bankBranch || '',
      });
    }
  }, [vendor]);

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.name?.trim()) {
      newErrors.name = 'Vendor name is required';
    }

    if (formData.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      newErrors.email = 'Please enter a valid email address';
    }

    // GSTIN validation
    if (formData.gstin && !/^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$/.test(formData.gstin)) {
      newErrors.gstin = 'Invalid GSTIN format (e.g., 27AAACP1234C1Z5)';
    }

    // PAN validation
    if (formData.panNumber && !/^[A-Z]{5}[0-9]{4}[A-Z]{1}$/.test(formData.panNumber)) {
      newErrors.panNumber = 'Invalid PAN format (e.g., AAACP1234C)';
    }

    // TAN validation (10 characters)
    if (formData.tanNumber && !/^[A-Z]{4}[0-9]{5}[A-Z]{1}$/.test(formData.tanNumber)) {
      newErrors.tanNumber = 'Invalid TAN format (e.g., MUMB12345A)';
    }

    // IFSC validation
    if (formData.ifscCode && !/^[A-Z]{4}0[A-Z0-9]{6}$/.test(formData.ifscCode)) {
      newErrors.ifscCode = 'Invalid IFSC format (e.g., SBIN0001234)';
    }

    // MSME validation
    if (formData.msmeRegistered && !formData.msmeRegistrationNumber) {
      newErrors.msmeRegistrationNumber = 'MSME registration number is required for registered vendors';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) return;

    try {
      if (isEditing && vendor) {
        await updateVendor.mutateAsync({ id: vendor.id, data: formData as UpdateVendorDto });
      } else {
        await createVendor.mutateAsync(formData);
      }
      onSuccess();
    } catch (error) {
      console.error('Form submission error:', error);
    }
  };

  const handleChange = (field: keyof CreateVendorDto, value: string | number | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }));
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Basic Information */}
      <div className="space-y-4">
        <h3 className="text-sm font-semibold text-gray-900 border-b pb-2">Basic Information</h3>

        <div>
          <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
            Vendor Name *
          </label>
          <input
            id="name"
            type="text"
            value={formData.name}
            onChange={(e) => handleChange('name', e.target.value)}
            className={cn(
              "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
              errors.name ? "border-red-500" : "border-gray-300"
            )}
            placeholder="Enter vendor name"
          />
          {errors.name && <p className="text-red-500 text-sm mt-1">{errors.name}</p>}
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label htmlFor="companyId" className="block text-sm font-medium text-gray-700 mb-1">
              Our Company
            </label>
            <select
              id="companyId"
              value={formData.companyId || ''}
              onChange={(e) => handleChange('companyId', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">Select a company</option>
              {companies.map((company) => (
                <option key={company.id} value={company.id}>
                  {company.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label htmlFor="companyName" className="block text-sm font-medium text-gray-700 mb-1">
              Vendor Company Name
            </label>
            <input
              id="companyName"
              type="text"
              value={formData.companyName}
              onChange={(e) => handleChange('companyName', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="Legal company name"
            />
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
              Email
            </label>
            <input
              id="email"
              type="email"
              value={formData.email}
              onChange={(e) => handleChange('email', e.target.value)}
              className={cn(
                "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
                errors.email ? "border-red-500" : "border-gray-300"
              )}
              placeholder="vendor@example.com"
            />
            {errors.email && <p className="text-red-500 text-sm mt-1">{errors.email}</p>}
          </div>
          <div>
            <label htmlFor="phone" className="block text-sm font-medium text-gray-700 mb-1">
              Phone
            </label>
            <input
              id="phone"
              type="tel"
              value={formData.phone}
              onChange={(e) => handleChange('phone', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="+91 98765 43210"
            />
          </div>
        </div>
      </div>

      {/* Address */}
      <div className="space-y-4">
        <h3 className="text-sm font-semibold text-gray-900 border-b pb-2">Address</h3>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label htmlFor="addressLine1" className="block text-sm font-medium text-gray-700 mb-1">
              Address Line 1
            </label>
            <input
              id="addressLine1"
              type="text"
              value={formData.addressLine1}
              onChange={(e) => handleChange('addressLine1', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="Street address"
            />
          </div>
          <div>
            <label htmlFor="addressLine2" className="block text-sm font-medium text-gray-700 mb-1">
              Address Line 2
            </label>
            <input
              id="addressLine2"
              type="text"
              value={formData.addressLine2}
              onChange={(e) => handleChange('addressLine2', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="Building, floor, etc."
            />
          </div>
        </div>

        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div>
            <label htmlFor="city" className="block text-sm font-medium text-gray-700 mb-1">
              City
            </label>
            <input
              id="city"
              type="text"
              value={formData.city}
              onChange={(e) => handleChange('city', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="Mumbai"
            />
          </div>
          <div>
            <label htmlFor="state" className="block text-sm font-medium text-gray-700 mb-1">
              State
            </label>
            <input
              id="state"
              type="text"
              value={formData.state}
              onChange={(e) => handleChange('state', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="Maharashtra"
            />
          </div>
          <div>
            <label htmlFor="zipCode" className="block text-sm font-medium text-gray-700 mb-1">
              PIN Code
            </label>
            <input
              id="zipCode"
              type="text"
              value={formData.zipCode}
              onChange={(e) => handleChange('zipCode', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="400001"
              maxLength={6}
            />
          </div>
          <div>
            <label htmlFor="country" className="block text-sm font-medium text-gray-700 mb-1">
              Country
            </label>
            <input
              id="country"
              type="text"
              value={formData.country}
              onChange={(e) => handleChange('country', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="India"
            />
          </div>
        </div>
      </div>

      {/* GST Compliance */}
      <div className="space-y-4">
        <h3 className="text-sm font-semibold text-gray-900 border-b pb-2">GST Compliance</h3>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label htmlFor="vendorType" className="block text-sm font-medium text-gray-700 mb-1">
              Vendor Type
            </label>
            <select
              id="vendorType"
              value={formData.vendorType}
              onChange={(e) => {
                handleChange('vendorType', e.target.value);
                handleChange('isGstRegistered', e.target.value === 'registered');
              }}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              {VENDOR_TYPES.map(type => (
                <option key={type.value} value={type.value}>{type.label}</option>
              ))}
            </select>
          </div>
          <div>
            <label htmlFor="gstin" className="block text-sm font-medium text-gray-700 mb-1">
              GSTIN
            </label>
            <input
              id="gstin"
              type="text"
              value={formData.gstin}
              onChange={(e) => handleChange('gstin', e.target.value.toUpperCase())}
              className={cn(
                "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
                errors.gstin ? "border-red-500" : "border-gray-300"
              )}
              placeholder="27AAACP1234C1Z5"
              maxLength={15}
            />
            {errors.gstin && <p className="text-red-500 text-sm mt-1">{errors.gstin}</p>}
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label htmlFor="panNumber" className="block text-sm font-medium text-gray-700 mb-1">
              PAN Number
            </label>
            <input
              id="panNumber"
              type="text"
              value={formData.panNumber}
              onChange={(e) => handleChange('panNumber', e.target.value.toUpperCase())}
              className={cn(
                "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
                errors.panNumber ? "border-red-500" : "border-gray-300"
              )}
              placeholder="AAACP1234C"
              maxLength={10}
            />
            {errors.panNumber && <p className="text-red-500 text-sm mt-1">{errors.panNumber}</p>}
          </div>
          <div>
            <label htmlFor="gstStateCode" className="block text-sm font-medium text-gray-700 mb-1">
              GST State Code
            </label>
            <input
              id="gstStateCode"
              type="text"
              value={formData.gstStateCode}
              onChange={(e) => handleChange('gstStateCode', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="27"
              maxLength={2}
            />
          </div>
        </div>
      </div>

      {/* TDS Settings */}
      <div className="space-y-4">
        <h3 className="text-sm font-semibold text-gray-900 border-b pb-2">TDS Settings</h3>

        <div className="flex items-center mb-4">
          <input
            id="tdsApplicable"
            type="checkbox"
            checked={formData.tdsApplicable}
            onChange={(e) => handleChange('tdsApplicable', e.target.checked)}
            className="h-4 w-4 text-primary focus:ring-ring border-gray-300 rounded"
          />
          <label htmlFor="tdsApplicable" className="ml-2 block text-sm text-gray-900">
            TDS Applicable on Payments
          </label>
        </div>

        {formData.tdsApplicable && (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label htmlFor="defaultTdsSection" className="block text-sm font-medium text-gray-700 mb-1">
                Default TDS Section
              </label>
              <select
                id="defaultTdsSection"
                value={formData.defaultTdsSection}
                onChange={(e) => handleChange('defaultTdsSection', e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              >
                <option value="">Select section</option>
                {TDS_SECTIONS.map(section => (
                  <option key={section.value} value={section.value}>{section.label}</option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="defaultTdsRate" className="block text-sm font-medium text-gray-700 mb-1">
                Default TDS Rate (%)
              </label>
              <input
                id="defaultTdsRate"
                type="number"
                step="0.01"
                min="0"
                max="100"
                value={formData.defaultTdsRate}
                onChange={(e) => handleChange('defaultTdsRate', parseFloat(e.target.value) || 0)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                placeholder="10.00"
              />
            </div>
            <div>
              <label htmlFor="tanNumber" className="block text-sm font-medium text-gray-700 mb-1">
                TAN Number
              </label>
              <input
                id="tanNumber"
                type="text"
                value={formData.tanNumber}
                onChange={(e) => handleChange('tanNumber', e.target.value.toUpperCase())}
                className={cn(
                  "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
                  errors.tanNumber ? "border-red-500" : "border-gray-300"
                )}
                placeholder="MUMB12345A"
                maxLength={10}
              />
              {errors.tanNumber && <p className="text-red-500 text-sm mt-1">{errors.tanNumber}</p>}
            </div>
          </div>
        )}
      </div>

      {/* MSME Compliance */}
      <div className="space-y-4">
        <h3 className="text-sm font-semibold text-gray-900 border-b pb-2">MSME Compliance</h3>

        <div className="flex items-center mb-4">
          <input
            id="msmeRegistered"
            type="checkbox"
            checked={formData.msmeRegistered}
            onChange={(e) => handleChange('msmeRegistered', e.target.checked)}
            className="h-4 w-4 text-primary focus:ring-ring border-gray-300 rounded"
          />
          <label htmlFor="msmeRegistered" className="ml-2 block text-sm text-gray-900">
            MSME Registered Vendor
          </label>
        </div>

        {formData.msmeRegistered && (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label htmlFor="msmeRegistrationNumber" className="block text-sm font-medium text-gray-700 mb-1">
                MSME Registration Number *
              </label>
              <input
                id="msmeRegistrationNumber"
                type="text"
                value={formData.msmeRegistrationNumber}
                onChange={(e) => handleChange('msmeRegistrationNumber', e.target.value.toUpperCase())}
                className={cn(
                  "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
                  errors.msmeRegistrationNumber ? "border-red-500" : "border-gray-300"
                )}
                placeholder="UDYAM-XX-00-0000000"
              />
              {errors.msmeRegistrationNumber && <p className="text-red-500 text-sm mt-1">{errors.msmeRegistrationNumber}</p>}
            </div>
            <div>
              <label htmlFor="msmeCategory" className="block text-sm font-medium text-gray-700 mb-1">
                MSME Category
              </label>
              <select
                id="msmeCategory"
                value={formData.msmeCategory}
                onChange={(e) => handleChange('msmeCategory', e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              >
                <option value="">Select category</option>
                {MSME_CATEGORIES.map(cat => (
                  <option key={cat.value} value={cat.value}>{cat.label}</option>
                ))}
              </select>
            </div>
          </div>
        )}
      </div>

      {/* Bank Details */}
      <div className="space-y-4">
        <h3 className="text-sm font-semibold text-gray-900 border-b pb-2">Bank Details</h3>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label htmlFor="bankName" className="block text-sm font-medium text-gray-700 mb-1">
              Bank Name
            </label>
            <input
              id="bankName"
              type="text"
              value={formData.bankName}
              onChange={(e) => handleChange('bankName', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="State Bank of India"
            />
          </div>
          <div>
            <label htmlFor="bankBranch" className="block text-sm font-medium text-gray-700 mb-1">
              Branch
            </label>
            <input
              id="bankBranch"
              type="text"
              value={formData.bankBranch}
              onChange={(e) => handleChange('bankBranch', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="Fort Branch, Mumbai"
            />
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label htmlFor="bankAccountNumber" className="block text-sm font-medium text-gray-700 mb-1">
              Account Number
            </label>
            <input
              id="bankAccountNumber"
              type="text"
              value={formData.bankAccountNumber}
              onChange={(e) => handleChange('bankAccountNumber', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="1234567890"
            />
          </div>
          <div>
            <label htmlFor="ifscCode" className="block text-sm font-medium text-gray-700 mb-1">
              IFSC Code
            </label>
            <input
              id="ifscCode"
              type="text"
              value={formData.ifscCode}
              onChange={(e) => handleChange('ifscCode', e.target.value.toUpperCase())}
              className={cn(
                "w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring",
                errors.ifscCode ? "border-red-500" : "border-gray-300"
              )}
              placeholder="SBIN0001234"
              maxLength={11}
            />
            {errors.ifscCode && <p className="text-red-500 text-sm mt-1">{errors.ifscCode}</p>}
          </div>
        </div>
      </div>

      {/* Payment Settings */}
      <div className="space-y-4">
        <h3 className="text-sm font-semibold text-gray-900 border-b pb-2">Payment Settings</h3>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label htmlFor="creditLimit" className="block text-sm font-medium text-gray-700 mb-1">
              Credit Limit
            </label>
            <input
              id="creditLimit"
              type="number"
              step="0.01"
              min="0"
              value={formData.creditLimit}
              onChange={(e) => handleChange('creditLimit', parseFloat(e.target.value) || 0)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="0.00"
            />
          </div>
          <div>
            <label htmlFor="paymentTerms" className="block text-sm font-medium text-gray-700 mb-1">
              Payment Terms (days)
            </label>
            <input
              id="paymentTerms"
              type="number"
              min="0"
              value={formData.paymentTerms}
              onChange={(e) => handleChange('paymentTerms', parseInt(e.target.value) || 0)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="30"
            />
          </div>
        </div>
      </div>

      {/* Notes */}
      <div>
        <label htmlFor="notes" className="block text-sm font-medium text-gray-700 mb-1">
          Notes
        </label>
        <textarea
          id="notes"
          rows={3}
          value={formData.notes}
          onChange={(e) => handleChange('notes', e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          placeholder="Additional notes about this vendor..."
        />
      </div>

      {/* Active Status */}
      <div className="flex items-center">
        <input
          id="isActive"
          type="checkbox"
          checked={formData.isActive}
          onChange={(e) => handleChange('isActive', e.target.checked)}
          className="h-4 w-4 text-primary focus:ring-ring border-gray-300 rounded"
        />
        <label htmlFor="isActive" className="ml-2 block text-sm text-gray-900">
          Vendor is active
        </label>
      </div>

      {/* Form Actions */}
      <div className="flex justify-end space-x-3 pt-4 border-t">
        <button
          type="button"
          onClick={onCancel}
          disabled={isLoading}
          className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:opacity-50"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={isLoading}
          className="px-4 py-2 text-sm font-medium text-primary-foreground bg-primary border border-transparent rounded-md hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:opacity-50"
        >
          {isLoading ? 'Saving...' : isEditing ? 'Update Vendor' : 'Create Vendor'}
        </button>
      </div>
    </form>
  );
};
