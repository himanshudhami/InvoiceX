import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { tagService } from '@/services/api/tags';
import type {
  Tag,
  CreateTagDto,
  UpdateTagDto,
  TagsFilterParams,
  ApplyTagsToTransactionDto,
  AutoAttributeRequest,
  TagGroup,
} from '@/services/api/types';
import { tagKeys } from './tagKeys';

/**
 * Fetch all tags for a company
 */
export const useTags = (companyId?: string) => {
  return useQuery({
    queryKey: tagKeys.list(companyId),
    queryFn: () => tagService.getAll(companyId),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Fetch paginated tags with filtering
 */
export const useTagsPaged = (params: TagsFilterParams = {}) => {
  return useQuery({
    queryKey: tagKeys.paged(params),
    queryFn: () => tagService.getPaged(params),
    keepPreviousData: true,
    staleTime: 30 * 1000,
  });
};

/**
 * Fetch single tag by ID
 */
export const useTag = (id: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: tagKeys.detail(id),
    queryFn: () => tagService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch tags by group
 */
export const useTagsByGroup = (companyId: string, tagGroup: TagGroup, enabled: boolean = true) => {
  return useQuery({
    queryKey: tagKeys.byGroup(companyId, tagGroup),
    queryFn: () => tagService.getByGroup(companyId, tagGroup),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch tag hierarchy (tree structure)
 */
export const useTagHierarchy = (companyId: string, tagGroup?: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: tagKeys.hierarchy(companyId, tagGroup),
    queryFn: () => tagService.getHierarchy(companyId, tagGroup),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Fetch tag summaries (for dropdowns)
 */
export const useTagSummaries = (companyId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: tagKeys.summaries(companyId),
    queryFn: () => tagService.getSummaries(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Create tag mutation
 */
export const useCreateTag = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateTagDto) => tagService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tagKeys.lists() });
      queryClient.invalidateQueries({ queryKey: tagKeys.groups() });
    },
    onError: (error) => {
      console.error('Failed to create tag:', error);
    },
  });
};

/**
 * Update tag mutation
 */
export const useUpdateTag = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateTagDto }) =>
      tagService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: tagKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: tagKeys.lists() });
      queryClient.invalidateQueries({ queryKey: tagKeys.groups() });
    },
    onError: (error) => {
      console.error('Failed to update tag:', error);
    },
  });
};

/**
 * Delete tag mutation
 */
export const useDeleteTag = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => tagService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tagKeys.lists() });
      queryClient.invalidateQueries({ queryKey: tagKeys.groups() });
    },
    onError: (error) => {
      console.error('Failed to delete tag:', error);
    },
  });
};

/**
 * Fetch tags for a specific transaction
 */
export const useTransactionTags = (
  transactionId: string,
  transactionType: string,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: tagKeys.forTransaction(transactionId, transactionType),
    queryFn: () => tagService.getTransactionTags(transactionId, transactionType),
    enabled: enabled && !!transactionId && !!transactionType,
    staleTime: 60 * 1000,
  });
};

/**
 * Apply tags to a transaction mutation
 */
export const useApplyTags = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: ApplyTagsToTransactionDto) => tagService.applyTags(data),
    onSuccess: (_, data) => {
      queryClient.invalidateQueries({
        queryKey: tagKeys.forTransaction(data.transactionId, data.transactionType),
      });
    },
    onError: (error) => {
      console.error('Failed to apply tags:', error);
    },
  });
};

/**
 * Remove a tag from a transaction mutation
 */
export const useRemoveTag = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      transactionId,
      transactionType,
      tagId,
    }: {
      transactionId: string;
      transactionType: string;
      tagId: string;
    }) => tagService.removeTag(transactionId, transactionType, tagId),
    onSuccess: (_, { transactionId, transactionType }) => {
      queryClient.invalidateQueries({
        queryKey: tagKeys.forTransaction(transactionId, transactionType),
      });
    },
    onError: (error) => {
      console.error('Failed to remove tag:', error);
    },
  });
};

/**
 * Auto-attribute tags to a transaction
 */
export const useAutoAttribute = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: AutoAttributeRequest) => tagService.autoAttribute(data),
    onSuccess: (_, data) => {
      queryClient.invalidateQueries({
        queryKey: tagKeys.forTransaction(data.transactionId, data.transactionType),
      });
    },
    onError: (error) => {
      console.error('Failed to auto-attribute tags:', error);
    },
  });
};

/**
 * Seed default tags for a company
 */
export const useSeedDefaultTags = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (companyId: string) => tagService.seedDefaults(companyId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tagKeys.lists() });
    },
    onError: (error) => {
      console.error('Failed to seed default tags:', error);
    },
  });
};

/**
 * Helper hook to build a tree structure from flat tags array
 */
export const useTagTree = (tags: Tag[] | undefined): Tag[] => {
  if (!tags) return [];

  const tagMap = new Map<string, Tag>();
  const rootTags: Tag[] = [];

  // First pass: create a map of all tags
  tags.forEach((tag) => {
    tagMap.set(tag.id, { ...tag, children: [] });
  });

  // Second pass: build the tree structure
  tags.forEach((tag) => {
    const tagWithChildren = tagMap.get(tag.id)!;
    if (tag.parentTagId && tagMap.has(tag.parentTagId)) {
      const parent = tagMap.get(tag.parentTagId)!;
      if (!parent.children) parent.children = [];
      parent.children.push(tagWithChildren);
    } else {
      rootTags.push(tagWithChildren);
    }
  });

  // Sort by sortOrder at each level
  const sortTags = (tagList: Tag[]): Tag[] => {
    return tagList
      .sort((a, b) => a.sortOrder - b.sortOrder)
      .map((tag) => ({
        ...tag,
        children: tag.children ? sortTags(tag.children) : [],
      }));
  };

  return sortTags(rootTags);
};
