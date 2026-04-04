export interface UserInfo {
  id: number;
  username: string;
  email: string;
  name: string;
  role: string;
  tenantId: number;
  tenantSlug: string;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
}

export interface Product {
  id: number;
  name: string;
  slug: string;
  description?: string;
  price: number;
  salePrice?: number;
  imageUrl?: string;
  categoryId: number;
  categoryName?: string;
  stock: number;
  isFeatured: boolean;
  isNew: boolean;
  rating?: number;
  reviewCount?: number;
}

export interface Category {
  id: number;
  name: string;
  slug: string;
  imageUrl?: string;
  productCount?: number;
  subCategories?: Category[];
}

export interface CartItem {
  id: number;
  productId: number;
  productName: string;
  productImage?: string;
  variantId?: number;
  variantName?: string;
  price: number;
  salePrice?: number;
  quantity: number;
}

export interface Cart {
  items: CartItem[];
  totalQuantity: number;
  totalAmount: number;
}

export interface Order {
  id: number;
  orderNumber: string;
  status: string;
  totalAmount: number;
  shippingFee: number;
  discountAmount: number;
  deliveryType?: string;
  deliveryOptionId?: number;
  note?: string;
  createdAt: string;
  items: OrderItem[];
}

export interface DeliveryTracking {
  assignmentId: number;
  status: string;
  driverName?: string;
  driverPhone?: string;
  vehicleType?: string;
  licensePlate?: string;
  driverLatitude?: number;
  driverLongitude?: number;
  estimatedDeliveryAt?: string;
  acceptedAt?: string;
  pickedUpAt?: string;
  inTransitAt?: string;
  deliveredAt?: string;
  deliveryPhotoUrl?: string;
  deliveryNote?: string;
  deliveryType?: string;
  deliveryOptionName?: string;
}

export interface OrderItem {
  id: number;
  productId: number;
  productName: string;
  productImage?: string;
  quantity: number;
  price: number;
}

export interface Address {
  id: number;
  recipientName: string;
  phone: string;
  zipCode: string;
  address1: string;
  address2?: string;
  isDefault: boolean;
}

export interface Notification {
  id: number;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  createdAt: string;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNext: boolean;
  hasPrev: boolean;
}

export interface TenantConfig {
  name: string;
  slug: string;
  primaryColor: string;
  secondaryColor: string;
  logoUrl?: string;
  companyName?: string;
}
