import type { ProductOwner } from '../types'
import { getJson } from './client'
import type { ProductOwnerListItemDto } from './mappers'
import { mapProductOwner } from './mappers'

export async function listProductOwners(): Promise<ProductOwner[]> {
  const dtos = await getJson<ProductOwnerListItemDto[]>('/product-owners')
  return dtos.map(mapProductOwner)
}
