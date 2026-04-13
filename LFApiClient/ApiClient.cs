namespace LFApiClient;

using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Laserfiche.Repository.Api.Client;
using Laserfiche.Api.Client;

public class ApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<APISettings> _apiOptions;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(ILogger<ApiClient> logger, IHttpClientFactory httpClientFactory, IOptions<APISettings> apiOptions)
    {
        _httpClientFactory = httpClientFactory;
        _apiOptions = apiOptions;
        _logger = logger;
        _logger.LogInformation("ApiClient initialized with API server: {APIServer}", _apiOptions.Value.BaseUrl);
    }

    public async Task<HttpResponseMessage> PostMetadataAsync(object? metadata, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        var content = new StringContent(
            JsonSerializer.Serialize(metadata),
            Encoding.UTF8,
            "application/json");

        return await client.PostAsync(_apiOptions.Value.APIServer, content, cancellationToken);
    }

    public async Task<string> GetRootEntryPath()
    {

        IRepositoryApiClient client = RepositoryApiClient.CreateFromUsernamePassword(_apiOptions.Value.RepositoryId, _apiOptions.Value.Username, _apiOptions.Value.Password, _apiOptions.Value.BaseUrl);

        var entry = await client.EntriesClient.GetEntryAsync(_apiOptions.Value.RepositoryId, 1);
        Console.WriteLine($"Root Folder Path: '{entry.FullPath}'");
        return entry.FullPath;

    }

    public async Task<HttpResponseMessage> GetRepositories(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        var endpoint = $"{_apiOptions.Value.BaseUrl}/repositories";

        return await client.GetAsync(endpoint, cancellationToken);
    }

    public async Task<bool> SendInvoiceMetadataToLF(string invoiceFileName, LaserficheMetadata metadata, CancellationToken cancellationToken)
    {
        try
        {

            IRepositoryApiClient client = RepositoryApiClient.CreateFromUsernamePassword(_apiOptions.Value.RepositoryId, _apiOptions.Value.Username, _apiOptions.Value.Password, _apiOptions.Value.BaseUrl);

            CopyAsyncRequest request = new CopyAsyncRequest
            {
                SourceId = _apiOptions.Value.InvoiceWordTemplateEntryId,
                Name = invoiceFileName
            };

            _logger.LogInformation("Copying entry with ID {SourceId} to new entry with name {Name}, parent folder: {ParentId}", request.SourceId, request.Name, _apiOptions.Value.EDIWorkingFolderEntryId);

            var operationToken = await client.EntriesClient.CopyEntryAsync(_apiOptions.Value.RepositoryId, _apiOptions.Value.EDIWorkingFolderEntryId, request, true, null, cancellationToken);

            int retries = 0;
            OperationProgress progress;
            do
            {
                progress = await client.TasksClient.GetOperationStatusAndProgressAsync(_apiOptions.Value.RepositoryId, operationToken.Token, cancellationToken);
                _logger.LogInformation("Copy invoice Word template status: {Status}. Filename: {Filename}", progress.Status, invoiceFileName);

                if (progress.Status == OperationStatus.InProgress || progress.Status == OperationStatus.NotStarted)
                {
                    _logger.LogInformation("Operation status: {Status}, waiting {seconds} seconds for completion... Filename: {Filename}. Retries = {retries}; Max retries = {MaxRetries}", progress.Status.ToString(), _apiOptions.Value.CopyInvoiceWordTemplateRetryDelay,invoiceFileName, retries, _apiOptions.Value.CopyInvoiceWordTemplateRetries);

                    await Task.Delay(TimeSpan.FromSeconds(_apiOptions.Value.CopyInvoiceWordTemplateRetryDelay), cancellationToken);
                    retries++;
                }
                else if (progress.Status == OperationStatus.Cancelled)
                {
                    _logger.LogWarning("Operation was cancelled. Filename: {Filename}", invoiceFileName);
                    return false;
                }
                else if (progress.Status == OperationStatus.Failed)
                {
                    _logger.LogError("Operation has failed. Filename: {Filename}", invoiceFileName);
                    if (progress.Errors != null && progress.Errors.Any())
                    {
                        var allErrors = string.Join(" || ", progress.Errors.Select(e => e.ErrorMessage));
                        _logger.LogError("Errors: {Errors}", allErrors);
                    }
                    return false;
                }

            }
            while (progress.Status != OperationStatus.Completed && retries < _apiOptions.Value.CopyInvoiceWordTemplateRetries);

            _logger.LogInformation("SendInvoiceMetadataToLF: Metadata Summary - Invoice #: {InvoiceNumber}, Invoice Date: {InvoiceDate}, Invoice Amount: {InvoiceAmount}, Vendor: {Vendor}", metadata.InvoiceNumber, metadata.InvoiceDate, metadata.InvoiceAmount, metadata.VendorName);



            if (progress.Status == OperationStatus.Completed)
            {
                PutTemplateRequest templateRequest = new()
                {
                    Fields = new Dictionary<string, FieldToUpdate>
                    {
                        { "Document Source", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = "EDI" }] } },
                        { "Invoice Number", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.InvoiceNumber) ? "UnknownInvoice" : (metadata.InvoiceNumber.Length > 40 ? metadata.InvoiceNumber.Substring(0,40) : metadata.InvoiceNumber) }] } },
                        { "PO Number", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.PONumber) ? string.Empty : (metadata.PONumber.Length > 40 ? metadata.PONumber.Substring(0, 40) : metadata.PONumber) }] } },
                        { "Invoice Date", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = metadata.InvoiceDate.ToString("yyyy-MM-dd")  }] } },
                        { "Invoice Amount", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = metadata.InvoiceAmount.ToString() }] } },
                        { "Barcode", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = metadata.BarcodeNumber.ToString() }] } },
                        { "Vendor Name", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.VendorName) ? string.Empty : (metadata.VendorName.Length > 100 ? metadata.VendorName.Substring(0, 100) : metadata.VendorName) }] } },
                        { "Vendor Code", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.VendorCode) ? string.Empty : (metadata.VendorCode.Length > 40 ? metadata.VendorCode.Substring(0, 40) : metadata.VendorCode) }] } },
                        { "Customer Number", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.CustomerNumber) ? string.Empty : (metadata.CustomerNumber.Length > 100 ? metadata.CustomerNumber.Substring(0, 100) : metadata.CustomerNumber) }] } },
                        { "Customer Name", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.CustomerName) ? string.Empty : (metadata.CustomerName.Length > 100 ? metadata.CustomerName.Substring(0, 100) : metadata.CustomerName) }] } },
                        { "Delivery Name", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.DeliveryName) ? string.Empty : (metadata.DeliveryName.Length > 100 ? metadata.DeliveryName.Substring(0, 100) : metadata.DeliveryName) }] } },
                        { "Supplier Note", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.SupplierNote) ? "" : (metadata.SupplierNote.Length > 100 ? metadata.SupplierNote.Substring(0,100) : metadata.SupplierNote) }] } },
                        { "Total Net Amount", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = metadata.TotalNetAmount.ToString() }] } },
                        { "Total Tax Amount", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = metadata.TotalTaxAmount.ToString() }] } },
                        { "Vendor GST", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.VendorGST) ? string.Empty : (metadata.VendorGST.Length > 11 ? metadata.VendorGST.Substring(0, 11) : metadata.VendorGST) }] } },
                        { "Vendor Address", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.VendorAddress) ? string.Empty : (metadata.VendorAddress.Length > 100 ? metadata.VendorAddress.Substring(0, 100) : metadata.VendorAddress) }] } },
                        { "Customer Address", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.CustomerAddress) ? string.Empty : (metadata.CustomerAddress.Length > 100 ? metadata.CustomerAddress.Substring(0, 100) : metadata.CustomerAddress) }] } },
                        { "Delivery Address", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.DeliveryAddress) ? string.Empty : (metadata.DeliveryAddress.Length > 100 ? metadata.DeliveryAddress.Substring(0, 100) : metadata.DeliveryAddress) }] } },
                        { "Delivery Location ID", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.DeliveryLocationID) ? "" : (metadata.DeliveryLocationID.Length > 40 ? metadata.DeliveryLocationID.Substring(0,40) : metadata.DeliveryLocationID) }] } },
                        { "Line Number", new FieldToUpdate  { Values = (metadata.LineNumber != null && metadata.LineNumber.Any()) 
                                                                    ? metadata.LineNumber
                                                                    .Select((number, idx) => new ValueToUpdate
                                                                    {
                                                                        Value = number.ToString(),
                                                                        Position = idx + 1
                                                                    })
                                                                    .ToList() : new List<ValueToUpdate>{ new ValueToUpdate { Value = String.Empty, Position = 0 } }                                                                  
                                                            } },
                        { "Line Barcode", new FieldToUpdate  { Values = (metadata.LineBarcode != null && metadata.LineBarcode.Any()) 
                                                                    ? metadata.LineBarcode
                                                                    .Select((barcode, idx) => new ValueToUpdate
                                                                    {
                                                                        Value = string.IsNullOrEmpty(barcode) ? string.Empty : (barcode.Length > 100 ? barcode.Substring(0,100) : barcode),
                                                                        Position = idx + 1
                                                                    })
                                                                    .ToList() : new List<ValueToUpdate>{ new ValueToUpdate { Value = String.Empty, Position = 0 } }                                                                  
                                                            } },
                        { "Line Description", new FieldToUpdate  {  Values = (metadata.LineDescription != null && metadata.LineDescription.Any())
                                                                    ? metadata.LineDescription
                                                                    .Select((description, idx) => new ValueToUpdate
                                                                    {
                                                                        Value = string.IsNullOrEmpty(description) ? string.Empty : (description.Length > 255 ? description.Substring(0,255) : description),
                                                                        Position = idx + 1
                                                                    })
                                                                    .ToList() : new List<ValueToUpdate>{ new ValueToUpdate { Value = String.Empty, Position = 0 } }                                                                 
                                                            } },
                        { "Line Quantity", new FieldToUpdate  { Values = (metadata.LineQuantity != null && metadata.LineQuantity.Any())
                                                                    ? metadata.LineQuantity
                                                                    .Select((quantity, idx) => new ValueToUpdate
                                                                    {
                                                                        Value = quantity.ToString(),
                                                                        Position = idx + 1
                                                                    })
                                                                    .ToList() : new List<ValueToUpdate>{ new ValueToUpdate { Value = String.Empty, Position = 0 } }                                                                
                                                            } },
                        { "Line Unit Price", new FieldToUpdate  {  Values = (metadata.LineUnitPrice != null && metadata.LineUnitPrice.Any())
                                                                    ? metadata.LineUnitPrice
                                                                    .Select((unitPrice, idx) => new ValueToUpdate
                                                                    {
                                                                        Value = unitPrice.ToString(),
                                                                        Position = idx + 1
                                                                    })
                                                                    .ToList() : new List < ValueToUpdate >{ new ValueToUpdate { Value = String.Empty, Position = 0 } }                                                                
                                                            } },
                        {"Line Discount", new FieldToUpdate  { Values = (metadata.LineDiscount != null && metadata.LineDiscount.Any())
                                                                    ? metadata.LineDiscount
                                                                    .Select((discount, idx) => new ValueToUpdate
                                                                    {
                                                                        Value = discount.ToString(),
                                                                        Position = idx + 1
                                                                    })
                                                                    .ToList() : new List < ValueToUpdate >{ new ValueToUpdate { Value = String.Empty, Position = 0 } }                                                                  
                                                            } },
                        {"Line Discount Amount", new FieldToUpdate  { Values = (metadata.LineDiscountAmount != null && metadata.LineDiscountAmount.Any())
                                                                    ? metadata.LineDiscountAmount
                                                                    .Select((discountAmount, idx) => new ValueToUpdate
                                                                    {
                                                                        Value = discountAmount.ToString(),
                                                                        Position = idx + 1
                                                                    })
                                                                    .ToList() : new List < ValueToUpdate >{ new ValueToUpdate { Value = String.Empty, Position = 0 } }                                                                  
                                                            } },
                        { "Line Amount", new FieldToUpdate  { Values = (metadata.LineAmount != null && metadata.LineAmount.Any())
                                                                    ? metadata.LineAmount
                                                                    .Select((amount, idx) => new ValueToUpdate
                                                                    {
                                                                        Value = amount.ToString(),
                                                                        Position = idx + 1
                                                                    })
                                                                    .ToList() : new List < ValueToUpdate >{ new ValueToUpdate { Value = String.Empty, Position = 0 } }                                                                
                                                            } }

                    },
                    TemplateName = "Invoice"
                };

                var result = await client.EntriesClient.WriteTemplateValueToEntryAsync(_apiOptions.Value.RepositoryId, progress.EntryId, templateRequest, null, cancellationToken);

                _logger.LogInformation("WriteTemplateValueToEntryAsync: Entry Result: TemplateId = {TemplateId}", result.TemplateId);

                return result.TemplateId > 0;
            }

        }
        catch (ApiException ex)
        {
            _logger.LogError("An API exception occurred, FileName: {FileName}, Message: {Message}",invoiceFileName, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending invoice metadata to Laserfiche. Filename: {Filename}", invoiceFileName);
        }


        return false;
                
    }

     public async Task<bool> UploadFileAndMetadataToLF(string invoiceFilePath, string invoiceFileName, LaserficheMetadata metadata, CancellationToken cancellationToken)
    {
        try
        {
            IRepositoryApiClient client = RepositoryApiClient.CreateFromUsernamePassword(_apiOptions.Value.RepositoryId, _apiOptions.Value.Username, _apiOptions.Value.Password, _apiOptions.Value.BaseUrl);

            PostEntryWithEdocMetadataRequest postEntryRequest = new ()
            {                
                Template = "Invoice", 
                Metadata = new ()
                {
                    Fields = new Dictionary<string, FieldToUpdate>
                    {
                        { "Document Source", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = metadata.DocumentSource }] } },
                        { "Invoice Number", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.InvoiceNumber) ? "UnknownInvoice" : (metadata.InvoiceNumber.Length > 40 ? metadata.InvoiceNumber.Substring(0,40) : metadata.InvoiceNumber) }] } },
                        { "PO Number", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.PONumber) ? string.Empty : (metadata.PONumber.Length > 40 ? metadata.PONumber.Substring(0, 40) : metadata.PONumber) }] } },
                        { "Invoice Date", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = metadata.InvoiceDate.ToString("yyyy-MM-dd")  }] } },
                        { "Invoice Amount", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = metadata.InvoiceAmount.ToString() }] } },
                        { "Barcode", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = metadata.BarcodeNumber.ToString() }] } },
                        { "Vendor Name", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.VendorName) ? "UnknownVendor" : (metadata.VendorName.Length > 100 ? metadata.VendorName.Substring(0, 100) : metadata.VendorName) }] } },
                        { "Vendor Code", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.VendorCode) ? string.Empty : (metadata.VendorCode.Length > 40 ? metadata.VendorCode.Substring(0, 40) : metadata.VendorCode) }] } },
                        { "Customer Number", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.CustomerNumber) ? string.Empty : (metadata.CustomerNumber.Length > 100 ? metadata.CustomerNumber.Substring(0, 100) : metadata.CustomerNumber) }] } },
                        { "Customer Name", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.CustomerName) ? string.Empty : (metadata.CustomerName.Length > 100 ? metadata.CustomerName.Substring(0, 100) : metadata.CustomerName) }] } },
                        { "Total Net Amount", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = metadata.TotalNetAmount.ToString() }] } },
                        { "Total Tax Amount", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = metadata.TotalTaxAmount.ToString() }] } },
                        { "Vendor GST", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.VendorGST) ? string.Empty : (metadata.VendorGST.Length > 11 ? metadata.VendorGST.Substring(0, 11) : metadata.VendorGST) }] } },
                        { "Vendor Address", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.VendorAddress) ? string.Empty : (metadata.VendorAddress.Length > 100 ? metadata.VendorAddress.Substring(0, 100) : metadata.VendorAddress) }] } },
                        { "Customer Address", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.CustomerAddress) ? string.Empty : (metadata.CustomerAddress.Length > 100 ? metadata.CustomerAddress.Substring(0, 100) : metadata.CustomerAddress) }] } },
                        { "Handling Charge", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = metadata.HandlingCharge.ToString() }] } },
                        { "Freight Charge", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = metadata.FreightCharge.ToString() }] } },
                        { "Trade Indicator", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.TradeIndicator) ? "Y" : metadata.TradeIndicator}]}},  
                        { "Declared Record", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = string.IsNullOrEmpty(metadata.DeclaredRecord) ? "N" : metadata.DeclaredRecord}]}},
                        { "Customer Email", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = metadata.CustomerEmail}] } },
                        { "GL Code", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = metadata.GLCode}] } },
                        { "GL Date", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = metadata.GLDate?.ToString("yyyy-MM-dd") ?? String.Empty}] } },
                        { "Processed Date", new FieldToUpdate { Values = [new ValueToUpdate {
                            Value = metadata.ProcessedDate?.ToString("yyyy-MM-dd") ?? String.Empty}]} }

                    }
                }  
            };
           
            
            var result = await client.EntriesClient.ImportDocumentAsync(_apiOptions.Value.RepositoryId, _apiOptions.Value.ARWorkingFolderEntryId, invoiceFileName, true, null, GetFileParameter(invoiceFilePath,invoiceFileName), postEntryRequest, cancellationToken);

            _logger.LogInformation("ImportDocumentAsync: Entry Result: TemplateId = {EntryId}", result.Operations.EntryCreate.EntryId);

            return result.Operations.EntryCreate.EntryId > 0;
            

        }
        catch (ApiException ex)
        {
            _logger.LogError("An API exception occurred, FileName: {FileName}, Message: {Message}",invoiceFileName, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending invoice to Laserfiche. Filename: {Filename}", invoiceFileName);
        }


        return false;
                
    }

    private static FileParameter GetFileParameter(string filePath, string fileName)
    {
        Stream fileStream = null;
        string fileLocation = Path.Combine(filePath,fileName + ".pdf");
        fileStream = File.OpenRead(fileLocation);
        return new FileParameter(fileStream, fileName, "application/pdf");
    }

}