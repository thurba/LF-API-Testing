namespace TWGARProcessorService;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using LFApiClient;

using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.IO;


public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IOptions<ARProcessorSettings> _settings;
    private readonly ApiClient _apiClient;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private CancellationToken _stoppingToken;


    public Worker(ILogger<Worker> logger, IOptions<ARProcessorSettings> options, ApiClient apiClient, IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _settings = options;
        _apiClient = apiClient;
        _hostApplicationLifetime = hostApplicationLifetime;
        
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

        _logger.LogInformation("AR Processor Service Starting");
        _logger.LogInformation("Information: {Info}", "");

       
        var inputDir = _settings.Value.MonitorFilePath;
        var processedFiles = new HashSet<string>(); // Track processed files

        
        var decryptedDir = Path.Combine(inputDir, "Decrypted"); // Target folder for processed files
        // Ensure decrypted folder exists
        Directory.CreateDirectory(decryptedDir);


        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var files = Directory.GetFiles(inputDir,"*.gpg");

                foreach (var file in files)
                {
                    if (!processedFiles.Contains(file))
                    {
                        _logger.LogInformation("New file detected: {File}", file);

                        // Call your processing logic here
                        await ProcessFileAsync(file);

                        processedFiles.Add(file);
                        
                        // Move file to decrypted folder after successful processing
                        var destinationPath = Path.Combine(decryptedDir, Path.GetFileName(file));
                        File.Move(file, destinationPath);

                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during polling");
            }

            // Wait before next poll (adjust interval as needed)
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.LogInformation("AR Processor Service Stopping");

  
    }

    
    private async Task ProcessFileAsync(string? file)
    {

         _logger.LogInformation("New file detected: {FullPath}", file);
        // decrypt - unzip - process

        // Decrypt
        string baseFilePath = Path.Combine(Path.GetDirectoryName(file),Path.GetFileNameWithoutExtension(file));
        string decryptedFilePath = baseFilePath + "_decrypted.zip";
        string privateKeyPath = _settings.Value.PrivateKeyPath;

        _logger.LogInformation("Decrypting file: {FullPath}", file);

        FileDecryptor.DecryptFile(file, decryptedFilePath, privateKeyPath, _settings.Value.PassPhrase);

        _logger.LogInformation("File decrypted: {FullPath}",decryptedFilePath);
        string extractPath = baseFilePath;
        _logger.LogInformation("Unzipping file to : {ExtractPath}",extractPath);         
        
        // Unzip
        ZipExtractor.Extract(decryptedFilePath, extractPath);

         _logger.LogInformation("File unzipped to: {ExtractPath}", extractPath);

        var csvFiles = Directory.GetFiles(extractPath,"*.csv"); // TODO:  Add logic to check for just one csv file

        foreach (var csvFile in csvFiles)
        {
            _logger.LogInformation("Processing {csvFile}",csvFile); 
            // Find csv file and process
            using var reader = new StreamReader(csvFile);

            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null // Ignore missing fields
            });
            var records = csv.GetRecords<InvoiceMetadata>().ToList();

            foreach (var record in records)
            {
                Console.WriteLine($"Processing Invoice: {record.INVOICE_NUMBER}, Amount: {record.INVOICE_AMOUNT}, Invoice Date: {record.INVOICE_DATE}");
                _logger.LogInformation("Processing Invoice: {InvoiceNumber}, Amount: {Amount}, Due Date: {DueDate}", record.INVOICE_NUMBER, record.INVOICE_AMOUNT, record.INVOICE_DATE);
                
                var metadata = new LaserficheMetadata
                {
                    // Map fields CSV values to LaserficheMetadata
                    InvoiceNumber = record.INVOICE_NUMBER != null ? record.INVOICE_NUMBER : "UnknownInvoiceNumber",
                    PONumber = record.PO_NUMBER,
                    InvoiceDate = DateTime.TryParse(record.INVOICE_DATE, out var invoiceDate) ? invoiceDate : default,
                    CustomerName = record.CUSTOMER_NAME,
                    CustomerNumber = record.CUSTOMER_NUMBER,
                    CustomerEmail = record.CUSTOMER_EMAIL,
                    BarcodeNumber = long.TryParse(record.BARCODE, out var barcode) ? barcode : 0,
                    VendorName = record.VENDOR_NAME,
                    VendorCode = record.VENDOR_CODE,
                    DeclaredRecord = record.DECLARED_RECORD,
                    DocumentSource = record.DOCUMENT_SOURCE,
                    GLCode = record.GL_CODE,
                    GLDate = DateTime.TryParse(record.GL_DATE, out var glDate) ? glDate : default,
                    ProcessedDate = DateTime.TryParse(record.PROCESSED_DATE, out var processedDate) ? processedDate : default,
                    TradeIndicator = record.TRADE_INDICATOR,
                    TotalNetAmount = record.TOTAL_NET_AMOUNT,
                    TotalTaxAmount = record.TOTAL_TAX_AMOUNT,
                    FreightCharge = record.FREIGHT_CHARGE,
                    CustomerAddress = record.CUSTOMER_ADDRESS,
                    HandlingCharge = record.HANDLING_CHARGE,
                    VendorAddress = record.VENDOR_ADDRESS,
                    VendorGST = record.VENDOR_GST,
                    InvoiceAmount = record.INVOICE_AMOUNT

                };

                string invoiceFileName = "INV_" + metadata.InvoiceNumber + "_1";

                bool success = await _apiClient.UploadFileAndMetadataToLF(extractPath, invoiceFileName, metadata, _stoppingToken);

            }
        }


    }


}