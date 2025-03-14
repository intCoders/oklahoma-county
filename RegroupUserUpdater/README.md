# RegroupUserUpdater

A .NET Core application for updating user groups in Regroup based on CSV data.

## Project Structure

The project follows a clean architecture with separation of concerns:

- **Models**: Contains all data models used in the application
  - `Address.cs`: Address model
  - `ContactResponse.cs`: Contact response models
  - `CsvData.cs`: CSV data model
  - `EmailAlert.cs`: Email alert models
  - `GroupResponse.cs`: Group response models
  - `UserResponse.cs`: User response models

- **Interfaces**: Contains interfaces for services
  - `ICsvService.cs`: Interface for CSV parsing service
  - `IRegroupApiService.cs`: Interface for Regroup API service

- **Services**: Contains service implementations
  - `CsvService.cs`: Service for parsing CSV files
  - `RegroupApiService.cs`: Service for interacting with the Regroup API

- **Endpoints**: Contains API endpoint definitions
  - `CsvEndpoints.cs`: Endpoints for CSV upload and processing

## Features

- Upload CSV files with user data
- Parse CSV data and extract relevant information
- Retrieve group information from Regroup API
- Create new groups if they don't exist
- Add contacts to groups
- Send email notifications for contacts not found in the system

## API Endpoints

- `POST /uploadcsv`: Upload a CSV file for processing

## Getting Started

1. Clone the repository
2. Build the project: `dotnet build`
3. Run the application: `dotnet run`
4. Access the Swagger UI at: `https://localhost:5001/swagger` (or the port configured in your environment)

## Dependencies

- .NET Core 8.0
- ASP.NET Core
- System.Text.Json 