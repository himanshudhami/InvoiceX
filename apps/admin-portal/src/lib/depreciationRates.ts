/**
 * Indian tax depreciation rates per Schedule II, Companies Act 2013
 * and Income Tax Act, 1961
 */

export interface DepreciationRate {
  category: string;
  rate: number; // Annual percentage rate
  description: string;
}

/**
 * Map of asset categories to Indian tax depreciation rates
 * These rates are used for tax-compliant reporting
 */
export const INDIAN_DEPRECIATION_RATES: Record<string, DepreciationRate> = {
  building: {
    category: 'Building',
    rate: 10, // RCC buildings: 10% (60 years useful life)
    description: 'Buildings (RCC)',
  },
  building_residential: {
    category: 'Building (Residential)',
    rate: 5,
    description: 'Residential Buildings',
  },
  plant_machinery: {
    category: 'Plant & Machinery',
    rate: 15,
    description: 'Plant & Machinery (General)',
  },
  office_equipment: {
    category: 'Office Equipment',
    rate: 10,
    description: 'Furniture, Fittings, Office Equipment',
  },
  computer: {
    category: 'Computer',
    rate: 40,
    description: 'Computers, Servers, Networks (3-6 years useful life)',
  },
  vehicle: {
    category: 'Vehicle',
    rate: 15,
    description: 'Cars (Other than used for hire)',
  },
  vehicle_commercial: {
    category: 'Vehicle (Commercial)',
    rate: 30,
    description: 'Commercial Vehicles',
  },
  furniture: {
    category: 'Furniture',
    rate: 10,
    description: 'Furniture and Fixtures',
  },
  machinery: {
    category: 'Machinery',
    rate: 15,
    description: 'General Machinery',
  },
};

/**
 * Get depreciation rate for an asset category
 * Falls back to 15% (general plant & machinery rate) if category not found
 */
export const getDepreciationRate = (category?: string): DepreciationRate => {
  if (!category) {
    return INDIAN_DEPRECIATION_RATES.plant_machinery;
  }

  const normalizedCategory = category.toLowerCase().replace(/[_\s-]/g, '_');

  // Try exact match first
  if (INDIAN_DEPRECIATION_RATES[normalizedCategory]) {
    return INDIAN_DEPRECIATION_RATES[normalizedCategory];
  }

  // Try partial matches
  if (normalizedCategory.includes('building')) {
    return normalizedCategory.includes('residential')
      ? INDIAN_DEPRECIATION_RATES.building_residential
      : INDIAN_DEPRECIATION_RATES.building;
  }

  if (normalizedCategory.includes('computer') || normalizedCategory.includes('server') || normalizedCategory.includes('laptop')) {
    return INDIAN_DEPRECIATION_RATES.computer;
  }

  if (normalizedCategory.includes('vehicle') || normalizedCategory.includes('car')) {
    return normalizedCategory.includes('commercial')
      ? INDIAN_DEPRECIATION_RATES.vehicle_commercial
      : INDIAN_DEPRECIATION_RATES.vehicle;
  }

  if (normalizedCategory.includes('furniture') || normalizedCategory.includes('fixture')) {
    return INDIAN_DEPRECIATION_RATES.furniture;
  }

  if (normalizedCategory.includes('office') || normalizedCategory.includes('equipment')) {
    return INDIAN_DEPRECIATION_RATES.office_equipment;
  }

  if (normalizedCategory.includes('machinery') || normalizedCategory.includes('plant')) {
    return INDIAN_DEPRECIATION_RATES.plant_machinery;
  }

  // Default fallback
  return INDIAN_DEPRECIATION_RATES.plant_machinery;
};

/**
 * Calculate monthly depreciation from annual rate
 */
export const calculateMonthlyDepreciation = (annualRate: number, assetValue: number): number => {
  return (assetValue * annualRate) / (100 * 12);
};

/**
 * Get all depreciation rate categories for display
 */
export const getAllDepreciationRates = (): DepreciationRate[] => {
  return Object.values(INDIAN_DEPRECIATION_RATES);
};




