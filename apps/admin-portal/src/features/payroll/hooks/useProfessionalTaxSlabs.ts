import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import { payrollService } from '@/services/api/payrollService'
import type {
  CreateProfessionalTaxSlabDto,
  UpdateProfessionalTaxSlabDto,
} from '@/features/payroll/types/payroll'
import { payrollKeys } from './payrollKeys'

// Note: useProfessionalTaxSlabs and useProfessionalTaxSlabForIncome are exported from useTaxConfiguration.ts
// This file exports additional hooks for CRUD operations and new queries

/**
 * Hook to fetch a single professional tax slab by ID
 */
export const useProfessionalTaxSlab = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: payrollKeys.professionalTaxSlab(id),
    queryFn: () => payrollService.getProfessionalTaxSlabById(id),
    enabled: enabled && !!id,
    staleTime: 10 * 60 * 1000,
    gcTime: 30 * 60 * 1000,
  })
}

/**
 * Hook to fetch all distinct states that have PT slabs configured
 */
export const useDistinctPtStates = () => {
  return useQuery({
    queryKey: payrollKeys.distinctPtStates(),
    queryFn: () => payrollService.getDistinctPtStates(),
    staleTime: 30 * 60 * 1000, // 30 minutes - this changes rarely
    gcTime: 60 * 60 * 1000, // 1 hour
  })
}

/**
 * Hook to fetch all Indian states
 */
export const useIndianStates = () => {
  return useQuery({
    queryKey: payrollKeys.indianStates(),
    queryFn: () => payrollService.getIndianStates(),
    staleTime: 24 * 60 * 60 * 1000, // 24 hours - static data
    gcTime: 24 * 60 * 60 * 1000,
  })
}

/**
 * Hook to fetch states that do NOT levy Professional Tax
 */
export const useNoPtStates = () => {
  return useQuery({
    queryKey: payrollKeys.noPtStates(),
    queryFn: () => payrollService.getNoPtStates(),
    staleTime: 24 * 60 * 60 * 1000, // 24 hours - static data
    gcTime: 24 * 60 * 60 * 1000,
  })
}

/**
 * Hook to create a new professional tax slab
 */
export const useCreateProfessionalTaxSlab = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateProfessionalTaxSlabDto) =>
      payrollService.createProfessionalTaxSlab(data),
    onSuccess: (_, variables) => {
      // Invalidate all PT slab queries
      queryClient.invalidateQueries({ queryKey: payrollKeys.professionalTaxSlabs() })
      queryClient.invalidateQueries({ queryKey: payrollKeys.professionalTaxSlabs(variables.state) })
      queryClient.invalidateQueries({ queryKey: payrollKeys.distinctPtStates() })
      toast.success('Professional Tax slab created successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to create Professional Tax slab')
    },
  })
}

/**
 * Hook to update an existing professional tax slab
 */
export const useUpdateProfessionalTaxSlab = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateProfessionalTaxSlabDto }) =>
      payrollService.updateProfessionalTaxSlab(id, data),
    onSuccess: (_, { id, data }) => {
      // Invalidate all PT slab queries
      queryClient.invalidateQueries({ queryKey: payrollKeys.professionalTaxSlab(id) })
      queryClient.invalidateQueries({ queryKey: payrollKeys.professionalTaxSlabs() })
      queryClient.invalidateQueries({ queryKey: payrollKeys.professionalTaxSlabs(data.state) })
      toast.success('Professional Tax slab updated successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to update Professional Tax slab')
    },
  })
}

/**
 * Hook to delete a professional tax slab
 */
export const useDeleteProfessionalTaxSlab = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => payrollService.deleteProfessionalTaxSlab(id),
    onSuccess: (_, id) => {
      // Invalidate all PT slab queries
      queryClient.invalidateQueries({ queryKey: payrollKeys.professionalTaxSlabs() })
      queryClient.removeQueries({ queryKey: payrollKeys.professionalTaxSlab(id) })
      queryClient.invalidateQueries({ queryKey: payrollKeys.distinctPtStates() })
      toast.success('Professional Tax slab deleted successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to delete Professional Tax slab')
    },
  })
}

/**
 * Hook to bulk create professional tax slabs
 */
export const useBulkCreateProfessionalTaxSlabs = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateProfessionalTaxSlabDto[]) =>
      payrollService.bulkCreateProfessionalTaxSlabs(data),
    onSuccess: () => {
      // Invalidate all PT slab queries
      queryClient.invalidateQueries({ queryKey: payrollKeys.professionalTaxSlabs() })
      queryClient.invalidateQueries({ queryKey: payrollKeys.distinctPtStates() })
      toast.success('Professional Tax slabs imported successfully')
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to import Professional Tax slabs')
    },
  })
}
