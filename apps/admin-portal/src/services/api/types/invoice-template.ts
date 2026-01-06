// Invoice Template types

export interface InvoiceTemplate {
  id: string;
  companyId?: string;
  name: string;
  templateData: string;
  templateKey?: string;
  previewUrl?: string;
  configSchema?: any;
  isDefault?: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateInvoiceTemplateDto {
  companyId?: string;
  name: string;
  templateData: string;
  isDefault?: boolean;
}

export interface UpdateInvoiceTemplateDto extends CreateInvoiceTemplateDto {}
