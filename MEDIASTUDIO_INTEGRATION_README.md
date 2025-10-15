# MediaStudio SQL Server Integration

This update adds MediaStudio SQL Server integration to retrieve UNFI Post Campaign Store data.

## Overview

The application now supports:
- **MediaStudio SQL Server Connection**: Connect to MediaStudio database using configured connection string
- **UNFI Post Campaign Store Data Retrieval**: Execute SQL query to get post campaign store data
- **Data Export**: Save data to both JSON and CSV formats
- **MongoDB Storage**: Store data in MongoDB `unfi_post_campaign_store` collection
- **Error Handling**: Comprehensive error handling for database operations

## Configuration

### Connection String Setup

Add the following connection string to your `App.config`:

```xml
<connectionStrings>
    <add name="MediaStudio" 
         connectionString="YOUR_MEDIASTUDIO_CONNECTION_STRING_HERE" 
         providerName="System.Data.SqlClient" />
</connectionStrings>
```

### Service Configuration

Enable the MediaStudio Data service in your `App.config`:

```xml
<add key="RunMediaStudioData" value="true" />
```

## SQL Query

The application executes the following query to retrieve UNFI Post Campaign Store data:

```sql
select 
'UNFI' as ClientToken
,pcs.Id
, isnull(pcs.GroupCampaignId,0) as GroupCampaignId
, pcs.CustomerGuid
, pcs.ExternalId
, isnull(pcs.SecondaryExternalId, '') as SecondaryExternalId
, pcs.Url
from [MediaStudio].[Posts].[UNFIPostCampaignStore] pcs with (nolock)
```

## Data Model

The `UnfiPostCampaignStore` class represents the data:

```csharp
public class UnfiPostCampaignStore
{
    public string ClientToken { get; set; }
    public int Id { get; set; }
    public int GroupCampaignId { get; set; }
    public string CustomerGuid { get; set; }
    public string ExternalId { get; set; }
    public string SecondaryExternalId { get; set; }
    public string Url { get; set; }
}
```

## MongoDB Integration

The MediaStudio Data service also stores data in MongoDB for analysis and reporting:

- **Database**: `integration_test`
- **Collection**: `unfi_post_campaign_store`
- **Document Structure**: UNFI Post Campaign Store data with metadata (createdAt, source)

### MongoDB Document Structure

```json
{
  "_id": "ObjectId",
  "clientToken": "UNFI",
  "recordId": 123,
  "groupCampaignId": 456,
  "customerGuid": "guid-string",
  "externalId": "external-id",
  "secondaryExternalId": "secondary-id",
  "url": "https://example.com",
  "createdAt": "2024-01-01T00:00:00Z",
  "source": "MediaStudio"
}
```

### MongoDB Indexes

The service automatically creates optimized indexes:

- **RecordId**: Fast lookups by record ID
- **GroupCampaignId**: Group-specific queries
- **ClientToken**: Client-specific queries
- **ExternalId**: External ID lookups
- **ClientToken + GroupCampaignId**: Compound index for complex queries

## Output Files

When the MediaStudio Data service runs, it creates:

- **`unfi_post_campaign_store.json`** - Data in JSON format
- **`unfi_post_campaign_store.csv`** - Data in CSV format
- **MongoDB Collection** - Data stored in `unfi_post_campaign_store` collection

Both files are saved in the configured output directory (default: `output/`).

## Usage

1. **Configure Connection**: Update the `MediaStudio` connection string with your server details
2. **Enable Service**: Set `RunMediaStudioData` to `true` in App.config
3. **Enable MongoDB**: Ensure `EnableMongoDbStorage` is set to `true` for MongoDB storage
4. **Run Application**: The application will automatically process MediaStudio data and store it in both files and MongoDB

## Connection String Examples

### Windows Authentication
```xml
<add name="MediaStudio" 
     connectionString="Server=MyServer;Database=MediaStudioDB;Integrated Security=true;TrustServerCertificate=true;" 
     providerName="System.Data.SqlClient" />
```

### SQL Server Authentication
```xml
<add name="MediaStudio" 
     connectionString="Server=MyServer;Database=MediaStudioDB;User Id=myuser;Password=mypassword;TrustServerCertificate=true;" 
     providerName="System.Data.SqlClient" />
```

### Local SQL Server
```xml
<add name="MediaStudio" 
     connectionString="Server=(local);Database=MediaStudioDB;Integrated Security=true;" 
     providerName="System.Data.SqlClient" />
```

## Processing Flow

1. **Connection Test**: Application tests the MediaStudio database connection first
2. **Data Retrieval**: Executes the SQL query to get UNFI Post Campaign Store data
3. **Data Processing**: Processes the results into UnfiPostCampaignStore objects
4. **File Export**: Saves data to JSON and CSV files
5. **MongoDB Storage**: Stores data in MongoDB `unfi_post_campaign_store` collection
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

The MediaStudio Data service runs independently of other services:

- **Parallel Processing**: Can run alongside Emfluence API, Campaign Data, and Coupon API processing
- **Independent Configuration**: Separate enable/disable setting
- **Separate Output**: MediaStudio data files are separate from other service files

## Troubleshooting

### Common Issues

1. **Connection Failed**
   - Verify server name and database name
   - Check network connectivity
   - Verify authentication credentials

2. **Permission Denied**
   - Ensure database user has SELECT permissions on UNFIPostCampaignStore table
   - Check if table exists in the specified database

3. **No Data Returned**
   - Verify table name and column names
   - Check if UNFIPostCampaignStore table contains data
   - Review SQL query syntax

### Debug Information

The service provides debug output including:
- Connection test results
- Number of records retrieved
- File save confirmations
- MongoDB storage confirmations
- Detailed error messages

## File Structure

After running with MediaStudio Data enabled:

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
└── unfi_post_campaign_store.csv
```

The MediaStudio SQL Server integration provides a complete solution for retrieving and exporting UNFI Post Campaign Store data alongside the existing services.
