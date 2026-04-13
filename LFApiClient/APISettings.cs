namespace LFApiClient;

public class APISettings
{
    public string APIServer { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string RepositoryId { get; set; } = string.Empty;

    public int InvoiceWordTemplateEntryId { get; set; } = -1;

    public int EDIWorkingFolderEntryId { get; set; } = -1;

    public int ARWorkingFolderEntryId { get; set; } = -1;

    public int CopyInvoiceWordTemplateRetries { get; set; } = 2;

    public int CopyInvoiceWordTemplateRetryDelay { get; set; } = 1; // in seconds

    public int ApiClientRetries { get; set; } = 2;
    
    public int ApiClientRetryDelay { get; set; } = 60; // in seconds

}