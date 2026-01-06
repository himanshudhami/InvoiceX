// Statutory Compliance Hooks
export { statutoryKeys } from './statutoryKeys';

// Form 16 Hooks
export {
  useForm16List,
  useForm16,
  useForm16ByEmployee,
  useForm16Summary,
  useForm16Preview,
  useGenerateForm16,
  useGenerateForm16ForEmployee,
  useRegenerateForm16,
  useVerifyForm16,
  useIssueForm16,
  useCancelForm16,
  useGenerateForm16Pdf,
  useValidateForm16,
} from './useForm16';

// TDS Challan Hooks
export {
  useTdsChallanList,
  useTdsChallan,
  usePendingTdsChallans,
  useTdsChallanSummary,
  useTdsChallanPreview,
  useCreateTdsChallan,
  useRecordTdsPayment,
  useUpdateTdsCin,
  useGenerateTdsChallanFromPayroll,
} from './useTdsChallan';

// PF ECR Hooks
export {
  usePfEcrGenerate,
  usePfEcrPreview,
  usePfEcr,
  usePendingPfEcrs,
  useFiledPfEcrs,
  usePfEcrSummary,
  usePfReconciliation,
  useCreatePfEcrPayment,
  useRecordPfPayment,
  useUpdatePfTrrn,
  useGeneratePfEcrFile,
} from './usePfEcr';

// ESI Return Hooks
export {
  useEsiReturnGenerate,
  useEsiReturnPreview,
  useEsiReturn,
  usePendingEsiReturns,
  useFiledEsiReturns,
  useEsiReturnSummary,
  useEsiReconciliation,
  useCreateEsiReturnPayment,
  useRecordEsiPayment,
  useUpdateEsiChallanNumber,
  useGenerateEsiReturnFile,
} from './useEsiReturn';

// Form 24Q Filing Hooks
export {
  useForm24QFilingList,
  useForm24QFiling,
  useForm24QFilingByQuarter,
  useForm24QFilingStatistics,
  useForm24QFilingsByYear,
  usePendingForm24QFilings,
  useOverdueForm24QFilings,
  useForm24QPreview,
  useForm24QCorrections,
  useCreateForm24QFiling,
  useRefreshForm24QFiling,
  useValidateForm24QFiling,
  useGenerateForm24QFvu,
  useDownloadForm24QFvu,
  useSubmitForm24QFiling,
  useRecordForm24QAcknowledgement,
  useRejectForm24QFiling,
  useCreateForm24QCorrection,
  useDeleteForm24QFiling,
} from './useForm24QFiling';
