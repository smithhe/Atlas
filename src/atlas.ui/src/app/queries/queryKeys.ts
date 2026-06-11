export const queryKeys = {
  settings: ['settings'] as const,
  tasks: ['tasks'] as const,
  risks: ['risks'] as const,
  projects: ['projects'] as const,
  teamMembers: ['team-members'] as const,
  productOwners: ['product-owners'] as const,
  growth: (memberId: string) => ['growth', memberId] as const,
}
