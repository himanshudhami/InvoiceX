// Common validation utilities and rules

export interface ValidationRule {
  required?: boolean;
  minLength?: number;
  maxLength?: number;
  min?: number;
  max?: number;
  pattern?: RegExp;
  custom?: (value: any) => string | null;
}

export interface ValidationResult {
  isValid: boolean;
  errors: Record<string, string>;
}

export interface FieldValidation {
  [fieldName: string]: ValidationRule;
}

/**
 * Validate a single field against rules
 */
export function validateField(value: any, rules: ValidationRule): string | null {
  if (rules.required && (value === undefined || value === null || value === '')) {
    return 'This field is required';
  }

  if (value === undefined || value === null || value === '') {
    return null; // Skip other validations if empty and not required
  }

  if (rules.minLength && typeof value === 'string' && value.length < rules.minLength) {
    return `Must be at least ${rules.minLength} characters`;
  }

  if (rules.maxLength && typeof value === 'string' && value.length > rules.maxLength) {
    return `Must be no more than ${rules.maxLength} characters`;
  }

  if (rules.min !== undefined && typeof value === 'number' && value < rules.min) {
    return `Must be at least ${rules.min}`;
  }

  if (rules.max !== undefined && typeof value === 'number' && value > rules.max) {
    return `Must be no more than ${rules.max}`;
  }

  if (rules.pattern && typeof value === 'string' && !rules.pattern.test(value)) {
    return 'Invalid format';
  }

  if (rules.custom) {
    return rules.custom(value);
  }

  return null;
}

/**
 * Validate multiple fields
 */
export function validateFields(data: Record<string, any>, validationRules: FieldValidation): ValidationResult {
  const errors: Record<string, string> = {};
  let isValid = true;

  for (const [fieldName, rules] of Object.entries(validationRules)) {
    const error = validateField(data[fieldName], rules);
    if (error) {
      errors[fieldName] = error;
      isValid = false;
    }
  }

  return { isValid, errors };
}

// Common validation rules
export const commonValidationRules = {
  required: { required: true },
  email: {
    required: true,
    pattern: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
    custom: (value: string) => {
      if (value && !value.includes('@')) {
        return 'Please enter a valid email address';
      }
      return null;
    }
  },
  phone: {
    pattern: /^[\+]?[1-9][\d]{0,15}$/,
    custom: (value: string) => {
      if (value && value.length < 10) {
        return 'Phone number must be at least 10 digits';
      }
      return null;
    }
  },
  positiveNumber: {
    min: 0,
    custom: (value: number) => {
      if (value < 0) {
        return 'Must be a positive number';
      }
      return null;
    }
  },
  percentage: {
    min: 0,
    max: 100,
    custom: (value: number) => {
      if (value < 0 || value > 100) {
        return 'Must be between 0 and 100';
      }
      return null;
    }
  }
};

// Specific validation schemas for different forms
export const customerValidationSchema: FieldValidation = {
  name: commonValidationRules.required,
  email: commonValidationRules.email,
  phone: commonValidationRules.phone,
  creditLimit: commonValidationRules.positiveNumber,
  paymentTerms: commonValidationRules.positiveNumber
};

export const productValidationSchema: FieldValidation = {
  name: commonValidationRules.required,
  unitPrice: commonValidationRules.positiveNumber,
  taxRate: commonValidationRules.percentage
};

export const companyValidationSchema: FieldValidation = {
  name: commonValidationRules.required,
  email: commonValidationRules.email,
  phone: commonValidationRules.phone
};

export const taxRateValidationSchema: FieldValidation = {
  name: commonValidationRules.required,
  rate: commonValidationRules.percentage
};

export const invoiceValidationSchema: FieldValidation = {
  customerId: commonValidationRules.required,
  invoiceNumber: commonValidationRules.required,
  invoiceDate: commonValidationRules.required,
  dueDate: commonValidationRules.required,
  subtotal: commonValidationRules.positiveNumber,
  totalAmount: commonValidationRules.positiveNumber
};

export const quoteValidationSchema: FieldValidation = {
  partyId: commonValidationRules.required,
  quoteNumber: commonValidationRules.required,
  quoteDate: commonValidationRules.required,
  validUntil: commonValidationRules.required,
  subtotal: commonValidationRules.positiveNumber,
  totalAmount: commonValidationRules.positiveNumber
};
