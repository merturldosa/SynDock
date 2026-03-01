import api from "./api";

export interface CollectionSummary {
  id: number;
  name: string;
  description: string | null;
  isPublic: boolean;
  itemCount: number;
  createdAt: string;
}

export interface CollectionItemDetail {
  productId: number;
  productName: string;
  price: number;
  salePrice: number | null;
  imageUrl: string | null;
  note: string | null;
  addedAt: string;
}

export interface CollectionDetail {
  id: number;
  name: string;
  description: string | null;
  isPublic: boolean;
  items: CollectionItemDetail[];
}

export async function getMyCollections(): Promise<CollectionSummary[]> {
  const { data } = await api.get("/api/collections");
  return data;
}

export async function createCollection(
  name: string,
  description?: string,
  isPublic = false
): Promise<{ collectionId: number }> {
  const { data } = await api.post("/api/collections", {
    name,
    description,
    isPublic,
  });
  return data;
}

export async function getCollectionDetail(
  id: number
): Promise<CollectionDetail> {
  const { data } = await api.get(`/api/collections/${id}`);
  return data;
}

export async function deleteCollection(id: number): Promise<void> {
  await api.delete(`/api/collections/${id}`);
}

export async function addToCollection(
  collectionId: number,
  productId: number,
  note?: string
): Promise<void> {
  await api.post(`/api/collections/${collectionId}/items`, {
    productId,
    note,
  });
}

export async function removeFromCollection(
  collectionId: number,
  productId: number
): Promise<void> {
  await api.delete(`/api/collections/${collectionId}/items/${productId}`);
}
