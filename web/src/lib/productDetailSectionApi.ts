import api from "./api";

export interface ProductDetailSection {
  id: number;
  title: string;
  content: string | null;
  imageUrl: string | null;
  imageAltText: string | null;
  sectionType: string;
  sortOrder: number;
  isActive: boolean;
}

export async function getProductDetailSections(productId: number): Promise<ProductDetailSection[]> {
  const { data } = await api.get<ProductDetailSection[]>(`/products/${productId}/sections`);
  return data;
}

export async function createProductDetailSection(
  productId: number,
  body: { title: string; content?: string; imageUrl?: string; imageAltText?: string; sectionType: string; sortOrder: number }
): Promise<{ sectionId: number }> {
  const { data } = await api.post(`/products/${productId}/sections`, body);
  return data;
}

export async function updateProductDetailSection(
  productId: number,
  id: number,
  body: { title?: string; content?: string; imageUrl?: string; imageAltText?: string; sectionType?: string; sortOrder?: number; isActive?: boolean }
): Promise<void> {
  await api.put(`/products/${productId}/sections/${id}`, body);
}

export async function deleteProductDetailSection(productId: number, id: number): Promise<void> {
  await api.delete(`/products/${productId}/sections/${id}`);
}

export async function reorderProductDetailSections(productId: number, sectionIds: number[]): Promise<void> {
  await api.put(`/products/${productId}/sections/reorder`, { sectionIds });
}
