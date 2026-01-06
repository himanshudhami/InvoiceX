// Tax Rate types

export interface TaxRate {
  id: string;
  companyId?: string;
  name: string;
  rate: number;
  isDefault?: boolean;
  isActive?: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateTaxRateDto {
  companyId?: string;
  name: string;
  rate: number;
  isDefault?: boolean;
  isActive?: boolean;
}

export interface UpdateTaxRateDto extends CreateTaxRateDto {}
