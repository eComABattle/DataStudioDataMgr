# Multi-Store Support for DataStudio Data Manager

This update adds support for processing multiple stores with different access tokens for the Emfluence emails/search endpoint.

## Overview

The application now supports:
- **Multiple Store Configurations**: Configure up to 20 additional stores beyond the default store
- **Store-Specific Processing**: Each store is processed independently with its own access token and settings
- **Store-Specific Output Files**: Output files are prefixed with store identifiers for easy identification
- **MongoDB Store Tracking**: Store information is stored in MongoDB for data segregation and analysis

## Configuration

### App.config Setup

To configure multiple stores, add the following configuration keys for each store:

```xml
<!-- Store N Configuration -->
<add key="Store_N_AccessToken" value="YOUR_ACCESS_TOKEN_HERE" />
<add key="Store_N_Name" value="Store N Name" />
<add key="Store_N_DeliveryType" value="manual" />
<add key="Store_N_Status" value="sent" />
<add key="Store_N_DateSentStart" value="2024-01-01" />
<add key="Store_N_MaxRecords" value="10000" />
<add key="Store_N_Enabled" value="true" />
```

Where `N` is the store number (1-20).

### Configuration Parameters

- **AccessToken**: The Emfluence API access token for this store
- **Name**: Display name for the store (used in logs and MongoDB)
- **DeliveryType**: Email delivery type filter (default: "manual")
- **Status**: Email status filter (default: "sent")
- **DateSentStart**: Start date for email filtering (YYYY-MM-DD format)
- **MaxRecords**: Maximum number of records to fetch (0 = no limit)
- **Enabled**: Whether to process this store (true/false)

## File Changes

### New Files
- `StoreConfiguration.cs` - Manages multiple store configurations
- `App.config.multi-store-example` - Complete example configuration

### Modified Files
- `EmfluenceApiService.cs` - Added store-specific constructors and logging
- `program.cs` - Updated to process all stores sequentially
- `MongoDbService.cs` - Added store information to MongoDB documents
- `App.config.example` - Updated with multi-store configuration examples

## Output Files

Output files are now prefixed with the store ID:
- `default_all_records_detail.json` - Default store data
- `store_1_all_records_detail.json` - Store 1 data
- `store_2_all_records_detail.json` - Store 2 data
- etc.

## MongoDB Changes

### New Fields
- `storeId`: Store identifier (e.g., "default", "store_1", "store_2")
- `storeName`: Store display name

### New Indexes
- Index on `storeId` for store-specific queries
- Compound index on `storeId` and `dateSent` for store-specific date range queries

## Usage

1. **Configure Stores**: Update your `App.config` with the store configurations
2. **Set Access Tokens**: Replace placeholder tokens with actual Emfluence API tokens
3. **Enable Stores**: Set `Store_N_Enabled` to `true` for stores you want to process
4. **Run Application**: The application will automatically process all enabled stores

## Backward Compatibility

The original single-store configuration is still supported:
- `EmfluenceAccessToken` - Still works as the default store
- All existing functionality remains unchanged
- Existing output files will be prefixed with "default_"

## Processing Flow

1. **Load Configurations**: Application loads all store configurations from App.config
2. **Filter Enabled Stores**: Only processes stores with `Enabled = true`
3. **Sequential Processing**: Each store is processed independently
4. **Store-Specific Output**: Files are saved with store-specific prefixes
5. **MongoDB Storage**: Records are stored with store identification

## Error Handling

- **Store-Level Errors**: If one store fails, others continue processing
- **Detailed Logging**: All operations include store identification in logs
- **Graceful Degradation**: MongoDB errors don't stop file operations

## Example Configuration

See `App.config.multi-store-example` for a complete configuration example with 13 additional stores configured.

## Benefits

- **Scalability**: Support for multiple stores without code changes
- **Data Segregation**: Store-specific data in MongoDB and files
- **Flexibility**: Per-store configuration for different requirements
- **Monitoring**: Store-specific logging and error tracking
- **Performance**: Optimized MongoDB indexes for store-specific queries
