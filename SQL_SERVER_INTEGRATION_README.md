# SQL Server Campaign Data Integration

This update adds SQL Server integration to retrieve Ad Campaign data from the "AdStudioUnfi" database.

## Overview

The application now supports:
- **SQL Server Connection**: Connect to AdStudioUnfi database using configured connection string
- **Campaign Data Retrieval**: Execute SQL query to get campaign data
- **Data Export**: Save campaign data to both JSON and CSV formats
- **Error Handling**: Comprehensive error handling for database operations

## Configuration

### Connection String Setup

Add the following connection string to your `App.config`:

```xml
<connectionStrings>
    <add name="AdStudioUnfi" 
         connectionString="Server=YOUR_SERVER_NAME;Database=YOUR_DATABASE_NAME;Integrated Security=true;TrustServerCertificate=true;" 
         providerName="System.Data.SqlClient" />
</connectionStrings>
```

### Service Configuration

Enable the Campaign Data service in your `App.config`:

```xml
<add key="RunCampaignData" value="true" />
```

## SQL Query

The application executes the following query to retrieve campaign data:

```sql
SELECT Id
      ,IsNull(BrandId,0) as BrandId
      ,Name
      ,Description
      ,convert(varchar, StartDate, 110) as StartDate
      ,convert(varchar, EndDate, 110) as EndDate
  FROM [dbo].[Campaign] with (nolock)
```

## Data Model

The `Campaign` class represents the campaign data:

```csharp
public class Campaign
{
    public int Id { get; set; }
    public int BrandId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string StartDate { get; set; }
    public string EndDate { get; set; }
}
```

## MongoDB Integration

The Campaign Data service also stores data in MongoDB for analysis and reporting:

- **Database**: `integration_test`
- **Collection**: `ad_campaign`
- **Document Structure**: Campaign data with metadata (createdAt, source)

### MongoDB Document Structure

```json
{
  "_id": "ObjectId",
  "campaignId": 123,
  "brandId": 456,
  "name": "Campaign Name",
  "description": "Campaign Description",
  "startDate": "01-01-2024",
  "endDate": "12-31-2024",
  "createdAt": "2024-01-01T00:00:00Z",
  "source": "SqlServer"
}
```

### MongoDB Indexes

The service automatically creates optimized indexes:

- **CampaignId**: Fast lookups by campaign ID
- **BrandId**: Brand-specific queries
- **Name**: Text searches on campaign names
- **BrandId + CampaignId**: Compound index for complex queries

## Output Files

When the Campaign Data service runs, it creates:

- **`campaign_data.json`** - Campaign data in JSON format
- **`campaign_data.csv`** - Campaign data in CSV format
- **MongoDB Collection** - Campaign data stored in `ad_campaign` collection

Both files are saved in the configured output directory (default: `output/`).

## Usage

1. **Configure Connection**: Update the `AdStudioUnfi` connection string with your server details
2. **Enable Service**: Set `RunCampaignData` to `true` in App.config
3. **Enable MongoDB**: Ensure `EnableMongoDbStorage` is set to `true` for MongoDB storage
4. **Run Application**: The application will automatically process campaign data and store it in both files and MongoDB

## Connection String Examples

### Windows Authentication
```xml
<add name="AdStudioUnfi" 
     connectionString="Server=MyServer;Database=AdStudioDB;Integrated Security=true;TrustServerCertificate=true;" 
     providerName="System.Data.SqlClient" />
```

### SQL Server Authentication
```xml
<add name="AdStudioUnfi" 
     connectionString="Server=MyServer;Database=AdStudioDB;User Id=myuser;Password=mypassword;TrustServerCertificate=true;" 
     providerName="System.Data.SqlClient" />
```

### Local SQL Server
```xml
<add name="AdStudioUnfi" 
     connectionString="Server=(local);Database=AdStudioDB;Integrated Security=true;" 
     providerName="System.Data.SqlClient" />
```

## Processing Flow

1. **Connection Test**: Application tests the database connection first
2. **Data Retrieval**: Executes the SQL query to get campaign data
3. **Data Processing**: Processes the results into Campaign objects
4. **File Export**: Saves data to JSON and CSV files
5. **MongoDB Storage**: Stores data in MongoDB `ad_campaign` collection
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
- Number of campaigns retrieved
- File save operations
- MongoDB storage operations
- Index creation status
- Error messages with troubleshooting hints

## Integration with Multi-Store System

The Campaign Data service runs independently of the multi-store Emfluence API system:

- **Parallel Processing**: Can run alongside Emfluence API processing
- **Independent Configuration**: Separate enable/disable setting
- **Separate Output**: Campaign data files are separate from store-specific files

## Troubleshooting

### Common Issues

1. **Connection Failed**
   - Verify server name and database name
   - Check network connectivity
   - Verify authentication credentials

2. **Permission Denied**
   - Ensure database user has SELECT permissions on Campaign table
   - Check if table exists in the specified database

3. **No Data Returned**
   - Verify table name and column names
   - Check if Campaign table contains data
   - Review SQL query syntax

### Debug Information

The service provides debug output including:
- Connection test results
- Number of records retrieved
- File save confirmations
- Detailed error messages

## File Structure

After running with Campaign Data enabled:

```
output/
├── default_all_records_detail.json    (Emfluence data)
├── default_all_records_detail.csv
├── store_1_all_records_detail.json    (Store 1 data)
├── store_1_all_records_detail.csv
├── store_2_all_records_detail.json    (Store 2 data)
├── store_2_all_records_detail.csv
├── campaign_data.json                 (Campaign data)
└── campaign_data.csv
```

The SQL Server integration provides a complete solution for retrieving and exporting Ad Campaign data alongside the existing Emfluence API functionality.
