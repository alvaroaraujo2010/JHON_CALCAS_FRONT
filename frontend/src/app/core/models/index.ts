export interface LoginResponse {
  token: string;
  fullName: string;
  email: string;
  role: string;
  expiresAt: string;
}

export interface Company {
  id: number;
  businessName: string;
  tagline: string;
  description?: string;
  address?: string;
  phone?: string;
  email?: string;
  website?: string;
  logoUrl?: string;
  taxId?: string;
  currency: string;
}

export interface Dashboard {
  totalSalesMonth: number;
  totalPurchasesMonth: number;
  productsCount: number;
  lowStockCount: number;
  customersCount: number;
  suppliersCount: number;
  recentSales: { id: number; documentNumber: string; customerName?: string; total: number; saleDate: string }[];
  lowStockProducts: { id: number; sku: string; name: string; stock: number; minStock: number }[];
}

export interface Category {
  id: number;
  name: string;
  description?: string;
  isActive: boolean;
  productCount: number;
}

export interface Product {
  id: number;
  sku: string;
  name: string;
  description?: string;
  categoryId: number;
  categoryName: string;
  unitCost: number;
  unitPrice: number;
  stock: number;
  minStock: number;
  unit: string;
  isActive: boolean;
  lowStock: boolean;
}

export interface Supplier {
  id: number;
  name: string;
  taxId?: string;
  contactName?: string;
  phone?: string;
  email?: string;
  address?: string;
  isActive: boolean;
}

export interface Customer {
  id: number;
  name: string;
  taxId?: string;
  contactName?: string;
  phone?: string;
  email?: string;
  address?: string;
  isActive: boolean;
}

export interface Purchase {
  id: number;
  documentNumber: string;
  supplierId: number;
  supplierName: string;
  purchaseDate: string;
  subtotal: number;
  tax: number;
  total: number;
  status: string;
  notes?: string;
  details: { productId: number; productName: string; quantity: number; unitCost: number; lineTotal: number }[];
}

export interface Sale {
  id: number;
  documentNumber: string;
  customerId?: number;
  customerName?: string;
  saleDate: string;
  subtotal: number;
  tax: number;
  total: number;
  paymentMethod: string;
  status: string;
  notes?: string;
  electronicInvoiceStatus: string;
  electronicInvoiceNumber?: string;
  cufe?: string;
  electronicInvoiceIssuedAt?: string;
  details: { productId: number; productName: string; quantity: number; unitPrice: number; lineTotal: number }[];
}

export interface ElectronicInvoice {
  saleId: number;
  documentNumber: string;
  electronicInvoiceNumber?: string;
  electronicInvoiceStatus: string;
  cufe?: string;
  issuedAt?: string;
  issuerName: string;
  issuerTaxId?: string;
  issuerAddress?: string;
  issuerPhone?: string;
  issuerEmail?: string;
  customerName: string;
  customerTaxId?: string;
  saleDate: string;
  paymentMethod: string;
  subtotal: number;
  tax: number;
  taxRate: number;
  total: number;
  lines: { description: string; quantity: number; unitPrice: number; lineTotal: number }[];
}

export interface InventoryMovement {
  id: number;
  productId: number;
  productName: string;
  type: string;
  quantity: number;
  stockBefore: number;
  stockAfter: number;
  reference?: string;
  notes?: string;
  createdAt: string;
}

export interface Account {
  id: number;
  code: string;
  name: string;
  type: string;
  parentId?: number;
  isActive: boolean;
}

export interface JournalEntry {
  id: number;
  entryNumber: string;
  entryDate: string;
  description: string;
  reference?: string;
  status: string;
  lines: { accountId: number; accountCode: string; accountName: string; debit: number; credit: number; description?: string }[];
  totalDebit: number;
  totalCredit: number;
}

export interface User {
  id: number;
  fullName: string;
  email: string;
  role: string;
  isActive: boolean;
}

export interface TrialBalance {
  lines: { code: string; name: string; type: string; debit: number; credit: number; balance: number }[];
  totalDebit: number;
  totalCredit: number;
}
