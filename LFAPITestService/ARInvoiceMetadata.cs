namespace TWGARProcessorService
{
    public class InvoiceMetadata
    {
        public string? INVOICE_NUMBER { get; set; }
        public string? PO_NUMBER { get; set; }
        public string? INVOICE_DATE { get; set; } // Consider DateTime if always valid
        public string? CUSTOMER_NAME { get; set; }
        public string? CUSTOMER_NUMBER { get; set; }
        public string? CUSTOMER_EMAIL { get; set; }
        public string? BARCODE { get; set; }
        public string? VENDOR_NAME { get; set; }
        public string? VENDOR_CODE { get; set; }
        public string? DECLARED_RECORD { get; set; }
        public string? DOCUMENT_SOURCE { get; set; }
        public string? GL_CODE { get; set; }
        public string? GL_DATE { get; set; }
        public string? PROCESSED_DATE { get; set; }
        public string? TRADE_INDICATOR { get; set; }
        public string? RETENTION_EXPIRY_DATE { get; set; }
        public decimal? TOTAL_NET_AMOUNT { get; set; }
        public decimal? TOTAL_TAX_AMOUNT { get; set; }
        public decimal? FREIGHT_CHARGE { get; set; }
        public string? CUSTOMER_ADDRESS { get; set; }
        public decimal? HANDLING_CHARGE { get; set; }
        public string? VENDOR_ADDRESS { get; set; }
        public string? VENDOR_GST { get; set; }
        public decimal? INVOICE_AMOUNT { get; set; }
    }
}