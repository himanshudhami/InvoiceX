export const employeeHubKeys = {
  all: ['employee-hub'] as const,

  // Employee details with related data
  details: (employeeId: string) => [...employeeHubKeys.all, 'details', employeeId] as const,

  // Assets assigned to employee
  assets: (employeeId: string) => [...employeeHubKeys.all, 'assets', employeeId] as const,

  // Subscriptions assigned to employee
  subscriptions: (employeeId: string) => [...employeeHubKeys.all, 'subscriptions', employeeId] as const,
}
