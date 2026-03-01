export interface OrderItem {
  id: number;
  productId: number;
  productName: string;
  primaryImageUrl: string | null;
  variantId: number | null;
  variantName: string | null;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface Address {
  id: number;
  recipientName: string;
  phone: string;
  zipCode: string;
  address1: string;
  address2: string | null;
  isDefault: boolean;
}

export interface Order {
  id: number;
  orderNumber: string;
  status: string;
  items: OrderItem[];
  totalAmount: number;
  shippingFee: number;
  note: string | null;
  shippingAddress: Address | null;
  createdAt: string;
}

export interface OrderSummary {
  id: number;
  orderNumber: string;
  status: string;
  itemCount: number;
  totalAmount: number;
  firstProductName: string | null;
  firstProductImageUrl: string | null;
  createdAt: string;
}

export interface PagedOrders {
  items: OrderSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export type OrderStatusType =
  | "Pending"
  | "Confirmed"
  | "Processing"
  | "Shipped"
  | "Delivered"
  | "Cancelled"
  | "Refunded";

export const ORDER_STATUS_LABELS: Record<OrderStatusType, string> = {
  Pending: "주문접수",
  Confirmed: "주문확인",
  Processing: "준비중",
  Shipped: "배송중",
  Delivered: "배송완료",
  Cancelled: "주문취소",
  Refunded: "환불완료",
};

export interface OrderHistory {
  id: number;
  status: string;
  note: string | null;
  trackingNumber: string | null;
  trackingCarrier: string | null;
  createdBy: string;
  createdAt: string;
}

export interface OrderDetail extends Order {
  discountAmount: number;
  pointsUsed: number;
  histories?: OrderHistory[];
  trackingNumber?: string | null;
  trackingCarrier?: string | null;
}
