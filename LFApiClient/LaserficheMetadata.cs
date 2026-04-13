using System;
using System.Text.Json.Serialization;

namespace LFApiClient
{
    public class LaserficheMetadata
    {
        [JsonPropertyName("invoiceNumber")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [JsonPropertyName("poNumber")]
        public string? PONumber { get; set; }

        [JsonPropertyName("invoiceDate")]
        public DateTime InvoiceDate { get; set; }

        [JsonPropertyName("invoiceAmount")]
        public decimal? InvoiceAmount { get; set; }

        [JsonPropertyName("barcodeNumber")]
        public long BarcodeNumber { get; set; }

        [JsonPropertyName("vendorName")]
        public string? VendorName { get; set; }

        [JsonPropertyName("vendorCode")]
        public string? VendorCode { get; set; }

        [JsonPropertyName("customerNumber")]
        public string? CustomerNumber { get; set; }

        [JsonPropertyName("customerName")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("customerEmail")]
        public string? CustomerEmail { get; set; }

        [JsonPropertyName("declaredRecord")]
        public string? DeclaredRecord { get; set; } // Options: "Y", "N"

        [JsonPropertyName("tradeIndicator")]
        public string? TradeIndicator { get; set; } // Options: "Y", "N"

        [JsonPropertyName("glCode")]
        public string? GLCode { get; set; }

        [JsonPropertyName("glDate")]
        public DateTime? GLDate { get; set; }

        [JsonPropertyName("documentSource")]
        public string? DocumentSource { get; set; } // Options: "AR Posted", "AR Emailed"

        [JsonPropertyName("processedDate")]
        public DateTime? ProcessedDate { get; set; }

        [JsonPropertyName("retentionExpiryDate")]
        public DateTime? RetentionExpiryDate { get; set; }

        [JsonPropertyName("totalNetAmount")]
        public decimal? TotalNetAmount { get; set; }

        [JsonPropertyName("totalTaxAmount")]
        public decimal? TotalTaxAmount { get; set; }

        [JsonPropertyName("freightCharge")]
        public decimal? FreightCharge { get; set; }

        [JsonPropertyName("handlingCharge")]
        public decimal? HandlingCharge { get; set; }

        [JsonPropertyName("lineBarcode")]
        public List<string?> LineBarcode { get; set; } = [];

        [JsonPropertyName("lineDescription")]
        public List<string?> LineDescription { get; set; } = [];

        [JsonPropertyName("lineQuantity")]
        public List<decimal?> LineQuantity { get; set; } = [];

        [JsonPropertyName("lineUnitPrice")]
        public List<decimal?> LineUnitPrice { get; set; } = [];

        [JsonPropertyName("lineDiscount")]
        public List<decimal?> LineDiscount { get; set; } = [];

        [JsonPropertyName("lineDiscountAmount")]
        public List<decimal?> LineDiscountAmount { get; set; } = [];

        [JsonPropertyName("lineAmount")]
        public List<decimal?> LineAmount { get; set; } = [];

        [JsonPropertyName("lineNumber")]
        public List<long?> LineNumber { get; set; } = [];

        [JsonPropertyName("vendorGST")]
        public string? VendorGST { get; set; }

        [JsonPropertyName("vendorAddress")]
        public string? VendorAddress { get; set; }

        [JsonPropertyName("customerAddress")]
        public string? CustomerAddress { get; set; }

        [JsonPropertyName("deliveryAddress")]
        public string? DeliveryAddress { get; set; }

        [JsonPropertyName("deliveryName")]
        public string? DeliveryName { get; set; }

        [JsonPropertyName("supplierNote")]
        public string? SupplierNote { get; set; }
        
        [JsonPropertyName("deliveryLocationID")]
        public string? DeliveryLocationID { get; set; }
    }
}