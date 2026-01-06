// Statutory Compliance Services - Indian Payroll
export { form16Service, Form16Service } from './form16Service';
export { form24QFilingService, Form24QFilingService } from './form24QFilingService';
export { tdsChallanService, TdsChallanService } from './tdsChallanService';
export { pfEcrService, PfEcrService } from './pfEcrService';
export { esiReturnService, EsiReturnService } from './esiReturnService';

// Re-export types from Form 24Q Filing service
export type {
  Form24QFiling,
  Form24QFilingSummary,
  Form24QFilingStatistics,
  Form24QPreviewData,
  Form24QValidationResult,
  Form24QFilingFilterParams,
  CreateForm24QFilingRequest,
  RecordAcknowledgementRequest,
  RejectFilingRequest,
  QuarterStatus,
} from './form24QFilingService';
