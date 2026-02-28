export interface CartItem {
  id: number;
  productId: number;
  productName: string;
  primaryImageUrl: string | null;
  price: number;
  salePrice: number | null;
  priceType: string;
  variantId: number | null;
  variantName: string | null;
  quantity: number;
  subTotal: number;
}

export interface Cart {
  id: number;
  items: CartItem[];
  totalAmount: number;
  totalQuantity: number;
}

export interface AddToCartRequest {
  productId: number;
  variantId?: number | null;
  quantity?: number;
}
