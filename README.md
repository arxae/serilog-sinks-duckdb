# Serilog.Sinks.DuckDB
A Serilog sink that writes to a DuckDB database.

## Getting started
Install [Serilog.Sinks.DuckDB](https://www.nuget.org/packages/Serilog.Sinks.DuckDB) from NuGet

```PowerShell
dotnet add package Serilog.Sinks.DuckDB
```

Configure logger by calling `WriteTo.DuckDB()`

```C#
var logger = new LoggerConfiguration()
    .WriteTo.DuckDB("log.db")
    .CreateLogger();
    
logger.Information("This informational message will be written to the DuckDB database");
```