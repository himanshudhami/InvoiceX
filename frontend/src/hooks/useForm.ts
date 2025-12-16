import { useState, useCallback } from 'react';
import { validateFields, FieldValidation, ValidationResult } from '@/lib/validation';

export interface UseFormOptions<T> {
  initialData: T;
  validationSchema?: FieldValidation;
  onSubmit?: (data: T) => Promise<void> | void;
}

export interface UseFormReturn<T> {
  data: T;
  errors: Record<string, string>;
  isValid: boolean;
  isSubmitting: boolean;
  setData: (data: T) => void;
  updateField: (field: keyof T, value: any) => void;
  validate: () => ValidationResult;
  reset: () => void;
  submit: () => Promise<void>;
  clearErrors: () => void;
  setErrors: (errors: Record<string, string>) => void;
}

/**
 * Generic form hook that provides common form functionality
 */
export function useForm<T extends Record<string, any>>({
  initialData,
  validationSchema,
  onSubmit
}: UseFormOptions<T>): UseFormReturn<T> {
  const [data, setData] = useState<T>(initialData);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const validate = useCallback((): ValidationResult => {
    if (!validationSchema) {
      return { isValid: true, errors: {} };
    }
    return validateFields(data, validationSchema);
  }, [data, validationSchema]);

  const updateField = useCallback((field: keyof T, value: any) => {
    setData(prev => ({ ...prev, [field]: value }));

    // Clear error for this field when user starts typing
    if (errors[field as string]) {
      setErrors(prev => {
        const newErrors = { ...prev };
        delete newErrors[field as string];
        return newErrors;
      });
    }
  }, [errors]);

  const reset = useCallback(() => {
    setData(initialData);
    setErrors({});
    setIsSubmitting(false);
  }, [initialData]);

  const clearErrors = useCallback(() => {
    setErrors({});
  }, []);

  const submit = useCallback(async () => {
    if (!onSubmit) return;

    const validation = validate();
    if (!validation.isValid) {
      setErrors(validation.errors);
      return;
    }

    setIsSubmitting(true);
    try {
      await onSubmit(data);
    } finally {
      setIsSubmitting(false);
    }
  }, [data, validate, onSubmit]);

  return {
    data,
    errors,
    isValid: Object.keys(errors).length === 0,
    isSubmitting,
    setData,
    updateField,
    validate,
    reset,
    submit,
    clearErrors,
    setErrors
  };
}
