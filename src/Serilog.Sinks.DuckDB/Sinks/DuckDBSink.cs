using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace Serilog.Sinks.DuckDB;

internal class DuckDBSink : BaseSink, ILogEventSink, IBatchedLogEventSink
{
	public DuckDBSink(string duckDbFilePath, string tableName, IFormatProvider formatProvider)
	{
		databasePath = duckDbFilePath;
		this.tableName = tableName;
		this.formatProvider = formatProvider;

		EnsureSchema();
	}

	public void Emit(LogEvent logEvent)
	{
		using var con = GetDuckDBConnection();
		using var tr = con.BeginTransaction();
		using var cmd = CreateSqlInsertCommand(con);

		cmd.Transaction = tr;
		PopulateInsertCommand(cmd, logEvent);
		cmd.ExecuteNonQuery();

		try
		{
			tr.Commit();
		}
		catch (Exception ex)
		{
			tr.Rollback();
			SelfLog.WriteLine("Error writing to DuckDB: {0}", ex);
			throw;
		}
	}

	public async Task EmitBatchAsync(IReadOnlyCollection<LogEvent> batch)
	{
		if (batch == null || batch.Count == 0)
			return;

		using var con = GetDuckDBConnection();
		using var tr = con.BeginTransaction();
		using var cmd = CreateSqlInsertCommand(con);

		cmd.Transaction = tr;

		foreach (var logEvent in batch)
		{
			try
			{
				PopulateInsertCommand(cmd, logEvent);
				await cmd.ExecuteNonQueryAsync();
			}
			catch (Exception ex)
			{
				SelfLog.WriteLine($"Error processing log event in batch: {ex.Message}");
			}
		}

		try
		{
			await tr.CommitAsync();
		}
		catch (Exception ex)
		{
			await tr.RollbackAsync();
			SelfLog.WriteLine($"Error committing batch to DuckDB: {ex.Message}");
			throw; // Re-throw to inform Serilog of a batch write failure
		}
	}
}