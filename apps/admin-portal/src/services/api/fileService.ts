import { apiClient } from './client';

/**
 * File storage types
 */
export interface FileStorageDto {
  id: string;
  companyId: string;
  originalFilename: string;
  storedFilename: string;
  storagePath: string;
  storageProvider: string;
  fileSize: number;
  mimeType: string;
  checksum?: string;
  uploadedBy?: string;
  entityType?: string;
  entityId?: string;
  isDeleted: boolean;
  createdAt: string;
}

export interface FileUploadResponse {
  id: string;
  storagePath: string;
  originalFilename: string;
  fileSize: number;
  mimeType: string;
}

/**
 * File Service - handles file upload/download operations
 */
export class FileService {
  private readonly endpoint = 'files';

  /**
   * Upload a file
   */
  async upload(
    file: File,
    companyId: string,
    entityType?: string,
    entityId?: string
  ): Promise<FileUploadResponse> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('companyId', companyId);
    if (entityType) formData.append('entityType', entityType);
    if (entityId) formData.append('entityId', entityId);

    // Use the underlying axios instance for multipart form data
    const response = await fetch(`${import.meta.env.VITE_API_URL || 'http://localhost:5001/api'}/${this.endpoint}/upload`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('admin_access_token')}`,
      },
      body: formData,
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Upload failed');
    }

    return response.json();
  }

  /**
   * Get download URL for a file
   */
  getDownloadUrl(storagePath: string): string {
    const baseUrl = import.meta.env.VITE_API_URL || 'http://localhost:5001/api';
    const token = localStorage.getItem('admin_access_token');
    return `${baseUrl}/${this.endpoint}/download/${encodeURIComponent(storagePath)}?token=${token}`;
  }

  /**
   * Download a file
   */
  async download(storagePath: string): Promise<Blob> {
    const response = await fetch(this.getDownloadUrl(storagePath));
    if (!response.ok) {
      throw new Error('Download failed');
    }
    return response.blob();
  }

  /**
   * Get file metadata by ID
   */
  async getById(id: string): Promise<FileStorageDto> {
    return apiClient.get<FileStorageDto>(`${this.endpoint}/${id}`);
  }

  /**
   * Delete a file (soft delete)
   */
  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }
}

// Singleton instance
export const fileService = new FileService();
