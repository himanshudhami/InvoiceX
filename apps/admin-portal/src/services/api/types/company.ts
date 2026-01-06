// Company types

export interface Company {
  id: string;
  name: string;
  logoUrl?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  email?: string;
  phone?: string;
  website?: string;
  taxNumber?: string;
  paymentInstructions?: string;
  invoiceTemplateId?: string;
  signatureType?: string;
  signatureData?: string;
  signatureName?: string;
  signatureFont?: string;
  signatureColor?: string;
  createdAt?: string;
  updatedAt?: string;
  // Statutory identifiers for Indian compliance
  tanNumber?: string;            // Tax Account Number (for TDS)
  pfRegistrationNumber?: string; // PF Establishment Code
  esiRegistrationNumber?: string; // ESI Code
  // GST Compliance fields
  gstin?: string;                // GSTIN (15 characters)
  gstStateCode?: string;         // GST State Code (first 2 digits of GSTIN)
  panNumber?: string;            // PAN Number (10 characters)
  cinNumber?: string;            // CIN (Corporate Identity Number)
  gstRegistrationType?: string;  // 'regular' | 'composition' | 'unregistered' | 'overseas'
}

export interface CreateCompanyDto {
  name: string;
  logoUrl?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  email?: string;
  phone?: string;
  website?: string;
  taxNumber?: string;
  paymentInstructions?: string;
  signatureType?: string;
  signatureData?: string;
  signatureName?: string;
  signatureFont?: string;
  signatureColor?: string;
  // GST Compliance fields
  gstin?: string;
  gstStateCode?: string;
  panNumber?: string;
  cinNumber?: string;
  gstRegistrationType?: string;
}

export interface UpdateCompanyDto extends CreateCompanyDto {}
