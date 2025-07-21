using Serilog.Configuration;
using Serilog.Debugging;
using Serilog.Events;

namespace Serilog.Sinks.DuckDB;

public static class LoggerConfigurationDuckDBExtensions
{
	public static LoggerConfiguration DuckDB(
		this LoggerSinkConfiguration loggerConfiguration,
		string duckDbPath,
		string tableName = "Logs",
		LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
		IFormatProvider formatProvider = null,
		bool useBatchedSink = true,
		int batchTimeLimit = 3,
		int batchSizeLimit = 25)
	{
		if (loggerConfiguration == null)
		{
			SelfLog.WriteLine("Logger configuration is null");
			throw new ArgumentNullException(nameof(loggerConfiguration));
		}

		if (string.IsNullOrEmpty(duckDbPath))
		{
			SelfLog.WriteLine("Invalid duckDbPath");
			throw new ArgumentNullException(nameof(duckDbPath));
		}

		if (Uri.TryCreate(duckDbPath, UriKind.RelativeOrAbsolute, out var duckDbPathUri) == false)
		{
			throw new ArgumentException($"Invalid path {nameof(duckDbPath)}");
		}

		if (duckDbPathUri.IsAbsoluteUri == false)
		{
			var entryAssembly = System.Reflection.Assembly.GetEntryAssembly()
				?? throw new NullReferenceException("Entry assembly is null.");
			var basePath = entryAssembly.Location;
			duckDbPath = Path.Combine(Path.GetDirectoryName(basePath)
				?? throw new NullReferenceException(), duckDbPath);
		}

		try
		{
			var duckDbFile = new FileInfo(duckDbPath);
			duckDbFile.Directory?.Create();

			DuckDBSink sink = new(
				duckDbFile.FullName,
				tableName,
				formatProvider ?? System.Globalization.CultureInfo.InvariantCulture);

			if (useBatchedSink)
			{
				BatchingOptions batchingOptions = new()
				{
					BufferingTimeLimit = TimeSpan.FromSeconds(batchTimeLimit),
					BatchSizeLimit = batchSizeLimit
				};

				return loggerConfiguration.Sink(sink, batchingOptions, restrictedToMinimumLevel);
			}

			return loggerConfiguration.Sink(sink, restrictedToMinimumLevel);
		}
		catch (Exception ex)
		{
			SelfLog.WriteLine(ex.Message);

			throw;
		}
	}
}