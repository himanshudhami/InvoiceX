import { useMutation } from '@tanstack/react-query';
import { fileService, FileUploadResponse } from '@/services/api/fileService';
import toast from 'react-hot-toast';

interface UploadParams {
  file: File;
  companyId: string;
  entityType?: string;
  entityId?: string;
}

/**
 * Hook for uploading files
 */
export const useFileUpload = () => {
  return useMutation({
    mutationFn: ({ file, companyId, entityType, entityId }: UploadParams) =>
      fileService.upload(file, companyId, entityType, entityId),
    onSuccess: (data: FileUploadResponse) => {
      toast.success(`File "${data.originalFilename}" uploaded successfully`);
    },
    onError: (error: any) => {
      const message = error?.message || 'Failed to upload file';
      toast.error(message);
    },
  });
};

/**
 * Hook for deleting files
 */
export const useFileDelete = () => {
  return useMutation({
    mutationFn: (id: string) => fileService.delete(id),
    onSuccess: () => {
      toast.success('File deleted successfully');
    },
    onError: (error: any) => {
      const message = error?.message || 'Failed to delete file';
      toast.error(message);
    },
  });
};

/**
 * Helper to get download URL
 */
export const useGetDownloadUrl = () => {
  return (storagePath: string) => fileService.getDownloadUrl(storagePath);
};
