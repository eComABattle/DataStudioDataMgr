# DataStudioDataMgr Environment Setup

This application uses environment variables to securely store sensitive configuration data like database credentials and API keys.

## Required Environment Variables

### MongoDB Configuration
```bash
MONGODB_USERNAME=your_mongodb_username
MONGODB_PASSWORD=your_mongodb_password
MONGODB_HOST=your_mongodb_host
MONGODB_DATABASE=your_mongodb_database
MONGODB_DATABASE_NAME=integration
MONGODB_TEST_DATABASE_NAME=integration_test
```

### ShopToCook SFTP Configuration
```bash
SHOPTOCOOK_SFTP_PASSWORD=your_sftp_password
```

### Emfluence API Configuration
```bash
EMFLUENCE_ACCESS_TOKEN=your_emfluence_access_token
```

### Emfluence Store Configuration
```bash
STORE_1_ACCESS_TOKEN=your_store_1_access_token
STORE_2_ACCESS_TOKEN=your_store_2_access_token
STORE_3_ACCESS_TOKEN=your_store_3_access_token
STORE_4_ACCESS_TOKEN=your_store_4_access_token
STORE_5_ACCESS_TOKEN=your_store_5_access_token
STORE_6_ACCESS_TOKEN=your_store_6_access_token
STORE_7_ACCESS_TOKEN=your_store_7_access_token
STORE_8_ACCESS_TOKEN=your_store_8_access_token
STORE_9_ACCESS_TOKEN=your_store_9_access_token
```

## Setting Environment Variables

### Windows (Command Prompt)
```cmd
set MONGODB_USERNAME=admin
set MONGODB_PASSWORD=your_password
set MONGODB_HOST=cluster0.rkrpr.mongodb.net
set MONGODB_DATABASE=integration
set MONGODB_DATABASE_NAME=integration
set MONGODB_TEST_DATABASE_NAME=integration_test
set SHOPTOCOOK_SFTP_PASSWORD=your_sftp_password
set EMFLUENCE_ACCESS_TOKEN=your_emfluence_access_token
set STORE_1_ACCESS_TOKEN=your_store_1_access_token
set STORE_2_ACCESS_TOKEN=your_store_2_access_token
set STORE_3_ACCESS_TOKEN=your_store_3_access_token
set STORE_4_ACCESS_TOKEN=your_store_4_access_token
set STORE_5_ACCESS_TOKEN=your_store_5_access_token
set STORE_6_ACCESS_TOKEN=your_store_6_access_token
set STORE_7_ACCESS_TOKEN=your_store_7_access_token
set STORE_8_ACCESS_TOKEN=your_store_8_access_token
set STORE_9_ACCESS_TOKEN=your_store_9_access_token
```

### Windows (PowerShell)
```powershell
$env:MONGODB_USERNAME="admin"
$env:MONGODB_PASSWORD="your_password"
$env:MONGODB_HOST="cluster0.rkrpr.mongodb.net"
$env:MONGODB_DATABASE="integration"
$env:MONGODB_DATABASE_NAME="integration"
$env:MONGODB_TEST_DATABASE_NAME="integration_test"
$env:SHOPTOCOOK_SFTP_PASSWORD="your_sftp_password"
$env:EMFLUENCE_ACCESS_TOKEN="your_emfluence_access_token"
$env:STORE_1_ACCESS_TOKEN="your_store_1_access_token"
$env:STORE_2_ACCESS_TOKEN="your_store_2_access_token"
$env:STORE_3_ACCESS_TOKEN="your_store_3_access_token"
$env:STORE_4_ACCESS_TOKEN="your_store_4_access_token"
$env:STORE_5_ACCESS_TOKEN="your_store_5_access_token"
$env:STORE_6_ACCESS_TOKEN="your_store_6_access_token"
$env:STORE_7_ACCESS_TOKEN="your_store_7_access_token"
$env:STORE_8_ACCESS_TOKEN="your_store_8_access_token"
$env:STORE_9_ACCESS_TOKEN="your_store_9_access_token"
```

### Windows (System Environment Variables)
1. Open System Properties → Advanced → Environment Variables
2. Add the variables under "User variables" or "System variables"
3. Restart your IDE/command prompt

### Linux/macOS
```bash
export MONGODB_USERNAME=admin
export MONGODB_PASSWORD=your_password
export MONGODB_HOST=cluster0.rkrpr.mongodb.net
export MONGODB_DATABASE=integration
export MONGODB_DATABASE_NAME=integration
export MONGODB_TEST_DATABASE_NAME=integration_test
export SHOPTOCOOK_SFTP_PASSWORD=your_sftp_password
export EMFLUENCE_ACCESS_TOKEN=your_emfluence_access_token
export STORE_1_ACCESS_TOKEN=your_store_1_access_token
export STORE_2_ACCESS_TOKEN=your_store_2_access_token
export STORE_3_ACCESS_TOKEN=your_store_3_access_token
export STORE_4_ACCESS_TOKEN=your_store_4_access_token
export STORE_5_ACCESS_TOKEN=your_store_5_access_token
export STORE_6_ACCESS_TOKEN=your_store_6_access_token
export STORE_7_ACCESS_TOKEN=your_store_7_access_token
export STORE_8_ACCESS_TOKEN=your_store_8_access_token
export STORE_9_ACCESS_TOKEN=your_store_9_access_token
```

## Configuration Files

- `App.config.example` - Template configuration file with environment variable placeholders
- `App.config` - Your local configuration file (excluded from Git)

## Setup Instructions

1. Copy `App.config.example` to `App.config`
2. Set the required environment variables
3. Run the application

## Security Notes

- Never commit `App.config` to version control
- Use strong, unique passwords for each environment
- Rotate credentials regularly
- Use different credentials for development, staging, and production environments
