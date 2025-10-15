# DigitalStudio SQL Server Integration

This update adds DigitalStudio SQL Server integration to retrieve Campaign Store Metrics data using a complex CTE (Common Table Expression) query.

## Overview

The application now supports:
- **DigitalStudio SQL Server Connection**: Connect to DigitalStudio database using configured connection string
- **Campaign Store Metrics Data Retrieval**: Execute complex CTE query to get campaign store metrics data
- **Data Export**: Save data to both JSON and CSV formats
- **MongoDB Storage**: Store data in MongoDB `campaign_store_metrics` collection
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

Enable the DigitalStudio Data service in your `App.config`:

```xml
<add key="RunDigitalStudioData" value="true" />
```

## SQL Query

The application executes a complex CTE query to retrieve Campaign Store Metrics data:

```sql
WITH DailyTotals AS (     
SELECT         pt.[Name] AS Platform
,         mt.[Name] AS MetricType
,         mt.Id AS MetricTypeId
,         p.Id AS PostID
,         p.[Name] AS PostName
,         CAST(p.ScheduledDate AS DATETIME) AS PostScheduledDate
,         CAST(p.PublishedDate AS DATETIME) AS PostPublishedDate
,         pmet.[Date] AS MetricDate
,         ISNULL(GroupCode, 'N/A') AS Customer
,         ap.[Name] AS Store
,         SUM(CASE WHEN [Count] >= 0 THEN [Count] ELSE 0 END) AS TotalMetricValue     
FROM Posts.Post p     
JOIN metrics.PostMetric pmet ON pmet.PostId = p.[Id]     
JOIN Types.MetricType mt ON mt.Id = pmet.MetricTypeId     
JOIN Posts.PostTarget ptar ON ptar.PostId = p.[Id]     
JOIN Posts.PostTargetAccountPlatform ptap ON ptap.PostTargetId = ptar.[Id]     
JOIN Accounts.AccountPlatform ap ON ap.Id = ptap.AccountPlatformId     
JOIN Platforms.PlatformTarget pt ON pt.Id = ptar.PlatformTargetId     
WHERE         p.[Name] NOT LIKE '%test%'         
GROUP BY         pt.[Name]
, mt.[Name]
, mt.Id
, p.Id
, p.[Name]
, CAST(p.ScheduledDate AS DATETIME)
,         CAST(p.PublishedDate AS DATETIME)
, pmet.[Date]
, ISNULL(GroupCode, 'N/A')
, ap.[Name] ), 
WithDelta AS (     
SELECT 
*
,         TotalMetricValue - LAG(TotalMetricValue) OVER (             
PARTITION BY Store, MetricTypeId, Platform, PostID             
ORDER BY MetricDate         ) AS MetricDelta     
FROM DailyTotals ) 
SELECT
 Platform
 ,MetricType 
, convert(varchar,MetricTypeId) as MetricTypeId 
, convert(varchar,PostID) as PostID 
, PostName 
, IsNull(convert(varchar,PostScheduledDate, 110), '01-01-1900') as PostScheduledDate 
, IsNull(convert(varchar,MetricDate, 110),'01-01-1900') as MetricDate 
, Customer as ClientToken 
, Store 
, isnull(TotalMetricValue,0) as TotalMetricValue 
, isnull(MetricDelta,0) as MetricDelta 
FROM WithDelta 
ORDER BY MetricDate DESC
, PostID
, Store
, MetricType
```

## Data Model

The `CampaignStoreMetrics` class represents the data:

```csharp
public class CampaignStoreMetrics
{
    public string Platform { get; set; }
    public string MetricType { get; set; }
    public string MetricTypeId { get; set; }
    public string PostID { get; set; }
    public string PostName { get; set; }
    public string PostScheduledDate { get; set; }
    public string MetricDate { get; set; }
    public string ClientToken { get; set; }
    public string Store { get; set; }
    public int TotalMetricValue { get; set; }
    public int MetricDelta { get; set; }
}
```

## MongoDB Integration

The DigitalStudio Data service also stores data in MongoDB for analysis and reporting:

- **Database**: `integration_test`
- **Collection**: `campaign_store_metrics`
- **Document Structure**: Campaign Store Metrics data with metadata (createdAt, source)

### MongoDB Document Structure

```json
{
  "_id": "ObjectId",
  "platform": "Facebook",
  "metricType": "Likes",
  "metricTypeId": "1",
  "postId": "123",
  "postName": "Campaign Post Name",
  "postScheduledDate": "01-01-2024",
  "metricDate": "01-01-2024",
  "clientToken": "N/A",
  "store": "Store Name",
  "totalMetricValue": 150,
  "metricDelta": 25,
  "createdAt": "2024-01-01T00:00:00Z",
  "source": "DigitalStudio"
}
```

### MongoDB Indexes

The service automatically creates optimized indexes:

- **PostId**: Fast lookups by post ID
- **Store**: Store-specific queries
- **Platform**: Platform-specific queries
- **MetricType**: Metric type queries
- **MetricDate**: Date-based queries
- **Store + Platform + MetricDate**: Compound index for complex queries

## Output Files

When the DigitalStudio Data service runs, it creates:

- **`campaign_store_metrics.json`** - Data in JSON format
- **`campaign_store_metrics.csv`** - Data in CSV format
- **MongoDB Collection** - Data stored in `campaign_store_metrics` collection

Both files are saved in the configured output directory (default: `output/`).

## Usage

1. **Configure Connection**: The DigitalStudio connection string is already configured
2. **Enable Service**: Set `RunDigitalStudioData` to `true` in App.config
3. **Enable MongoDB**: Ensure `EnableMongoDbStorage` is set to `true` for MongoDB storage
4. **Run Application**: The application will automatically process DigitalStudio data and store it in both files and MongoDB

## Processing Flow

1. **Connection Test**: Application tests the DigitalStudio database connection first
2. **Data Retrieval**: Executes the complex CTE query to get Campaign Store Metrics data
3. **Data Processing**: Processes the results into CampaignStoreMetrics objects
4. **File Export**: Saves data to JSON and CSV files
5. **MongoDB Storage**: Stores data in MongoDB `campaign_store_metrics` collection
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

The DigitalStudio Data service runs independently of other services:

- **Parallel Processing**: Can run alongside Emfluence API, Campaign Data, MediaStudio Data, and Coupon API processing
- **Independent Configuration**: Separate enable/disable setting
- **Separate Output**: DigitalStudio data files are separate from other service files

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
   - Review CTE query syntax

### Debug Information

The service provides debug output including:
- Connection test results
- Number of records retrieved
- File save confirmations
- MongoDB storage confirmations
- Detailed error messages

## File Structure

After running with DigitalStudio Data enabled:

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
├── campaign_store_metrics.json               (DigitalStudio data)
└── campaign_store_metrics.csv
```

## CTE Query Explanation

The query uses two Common Table Expressions:

1. **DailyTotals CTE**: 
   - Aggregates metric data by platform, metric type, post, date, and store
   - Filters out test posts
   - Calculates total metric values

2. **WithDelta CTE**:
   - Calculates metric deltas using LAG window function
   - Compares current day metrics with previous day
   - Partitions by Store, MetricTypeId, Platform, and PostID

The final SELECT statement formats the data and orders by MetricDate DESC, PostID, Store, and MetricType.

The DigitalStudio SQL Server integration provides a complete solution for retrieving and exporting Campaign Store Metrics data alongside the existing services.
