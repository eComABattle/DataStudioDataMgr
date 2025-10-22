# Entry Metrics SQL Server Integration

This update adds Entry Metrics SQL Server integration to retrieve Entry Metrics data from the DigitalStudio database.

## Overview

The application now supports:
- **DigitalStudio SQL Server Connection**: Connect to DigitalStudio database using configured connection string
- **Entry Metrics Data Retrieval**: Execute SQL query to get entry metrics data
- **Data Export**: Save data to both JSON and CSV formats
- **MongoDB Storage**: Store data in MongoDB `entry_metrics` collection
- **Error Handling**: Comprehensive error handling for database operations

## Configuration

### Connection String Setup

The DigitalStudio connection string is already configured in your `App.config`:

```xml
<connectionStrings>
    <add name="DigitalStudio" connectionString="Data Source=vm-testdb;Integrated Security=true;Initial Catalog=DigitalStudio"/>
</connectionStrings>
```

### Service Configuration

Enable the Entry Metrics Data service in your `App.config`:

```xml
<add key="RunEntryMetricsData" value="true" />
```

## SQL Query

The application executes the following query to retrieve Entry Metrics data:

```sql
select 
a.Id AS AccountId 
, a.Name AS AccountName 
, a.GroupCode AS Customer 
, b.Name AS CampaignName 
, em.EntryId AS EntryId 
,  et.Name AS EntryType 
 ,mt.Name AS MetricType 
 , CAST(CAST(em.[Timestamp] AS datetime) AS date) as [Timestamp] 
, e.MetaData  
FROM Metrics.EntryMetric em with (nolock) 
JOIN Accounts.Account a with (nolock) ON a.Id = em.AccountId 
JOIN Entries.Entry e with (nolock) ON e.Id = em.EntryId 
JOIN Entries.Batch b with (nolock) ON b.Id = e.BatchId 
JOIN Types.EntryType et with (nolock) ON et.Id = em.EntryTypeId 
JOIN Types.MetricType mt with (nolock) ON mt.Id = em.MetricTypeId
```

## Data Model

The `EntryMetrics` class represents the data:

```csharp
public class EntryMetrics
{
    public int AccountId { get; set; }
    public string AccountName { get; set; }
    public string Customer { get; set; }
    public string CampaignName { get; set; }
    public int EntryId { get; set; }
    public string EntryType { get; set; }
    public string MetricType { get; set; }
    public string Timestamp { get; set; }
    public string MetaData { get; set; }
}
```

## MongoDB Integration

The Entry Metrics Data service also stores data in MongoDB for analysis and reporting:

- **Database**: `integration_test`
- **Collection**: `entry_metrics`
- **Document Structure**: Entry Metrics data with metadata (createdAt, source)

### MongoDB Document Structure

```json
{
  "_id": "ObjectId",
  "accountId": 123,
  "accountName": "Account Name",
  "customer": "Customer Code",
  "campaignName": "Campaign Name",
  "entryId": 456,
  "entryType": "Entry Type",
  "metricType": "Metric Type",
  "timestamp": "2024-01-01",
  "metaData": "Metadata JSON",
  "createdAt": "2024-01-01T00:00:00Z",
  "source": "DigitalStudio"
}
```

### MongoDB Indexes

The service automatically creates optimized indexes:

- **AccountId**: Fast lookups by account ID
- **EntryId**: Entry-specific queries
- **Customer**: Customer-specific queries
- **CampaignName**: Campaign-specific queries
- **MetricType**: Metric type queries
- **Timestamp**: Date-based queries
- **AccountId + EntryId + Timestamp**: Compound index for complex queries

## Output Files

When the Entry Metrics Data service runs, it creates:

- **`entry_metrics.json`** - Data in JSON format
- **`entry_metrics.csv`** - Data in CSV format
- **MongoDB Collection** - Data stored in `entry_metrics` collection

Both files are saved in the configured output directory (default: `output/`).

## Usage

1. **Configure Connection**: The DigitalStudio connection string is already configured
2. **Enable Service**: Set `RunEntryMetricsData` to `true` in App.config
3. **Enable MongoDB**: Ensure `EnableMongoDbStorage` is set to `true` for MongoDB storage
4. **Run Application**: The application will automatically process Entry Metrics data and store it in both files and MongoDB

## Processing Flow

1. **Connection Test**: Application tests the DigitalStudio database connection first
2. **Data Retrieval**: Executes the SQL query to get Entry Metrics data
3. **Data Processing**: Processes the results into EntryMetrics objects
4. **File Export**: Saves data to JSON and CSV files
5. **MongoDB Storage**: Stores data in MongoDB `entry_metrics` collection
6. **Index Creation**: Creates optimized indexes for query performance
7. **Logging**: Provides detailed logging throughout the process

## Error Handling

The service includes comprehensive error handling for:

- **Connection Issues**: Invalid connection strings, server unavailable
- **SQL Errors**: Query execution problems, permission issues
- **Data Processing**: Null values, data type conversion issues
- **File Operations**: File system errors, permission problems
- **MongoDB Operations**: Database connection issues, collection access problems

## Logging

The service provides detailed logging:

- Connection test results
- Number of records retrieved
- File save operations
- MongoDB storage operations
- Index creation status
- Error messages with troubleshooting hints

## Integration with Other Services

The Entry Metrics Data service runs independently of other services:

- **Parallel Processing**: Can run alongside Emfluence API, Campaign Data, MediaStudio Data, DigitalStudio Campaign Store Metrics, and Coupon API processing
- **Independent Configuration**: Separate enable/disable setting
- **Separate Output**: Entry Metrics data files are separate from other service files

## Troubleshooting

### Common Issues

1. **Connection Failed**
   - Verify server name and database name
   - Check network connectivity
   - Verify authentication credentials

2. **Permission Denied**
   - Ensure database user has SELECT permissions on all required tables
   - Check if tables exist in the specified database

3. **No Data Returned**
   - Verify table names and column names
   - Check if required tables contain data
   - Review SQL query syntax

### Debug Information

The service provides debug output including:
- Connection test results
- Number of records retrieved
- File save confirmations
- MongoDB storage confirmations
- Detailed error messages

## File Structure

After running with Entry Metrics Data enabled:

```
output/
├── default_all_records_detail.json           (Emfluence data)
├── default_all_records_detail.csv
├── store_1_all_records_detail.json           (Store 1 data)
├── store_1_all_records_detail.csv
├── store_2_all_records_detail.json           (Store 2 data)
├── store_2_all_records_detail.csv
├── campaign_data.json                         (Campaign data)
├── campaign_data.csv
├── unfi_post_campaign_store.json             (MediaStudio data)
├── unfi_post_campaign_store.csv
├── campaign_store_metrics.json               (DigitalStudio Campaign Store Metrics data)
├── campaign_store_metrics.csv
├── entry_metrics.json                        (DigitalStudio Entry Metrics data)
└── entry_metrics.csv
```

## SQL Query Explanation

The query joins multiple tables to retrieve comprehensive entry metrics data:

- **Metrics.EntryMetric**: Main metrics table
- **Accounts.Account**: Account information
- **Entries.Entry**: Entry details
- **Entries.Batch**: Batch information (campaigns)
- **Types.EntryType**: Entry type definitions
- **Types.MetricType**: Metric type definitions

The query retrieves:
- Account information (ID, Name, GroupCode)
- Campaign information (Batch Name)
- Entry details (ID, Type)
- Metric information (Type, Timestamp)
- Metadata from entries

The Entry Metrics SQL Server integration provides a complete solution for retrieving and exporting Entry Metrics data alongside the existing services.

