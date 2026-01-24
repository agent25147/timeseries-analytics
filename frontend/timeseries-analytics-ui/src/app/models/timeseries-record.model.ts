export interface TimeSeriesRecord {
  id: string;
  transactionId: string;
  timestamp: Date;
  userId: number;
  region: string;
  category: string;
  productName: string;
  amount: number;
  quantity: number;
  status: string;
  paymentMethod: string;
  address: string;
  shippingLane: string;
  scacCode: string;
  billToName: string;
  invoiceNumber: string;
  currencyCode: string;
  weight: number;
  createdAt: Date;
}
