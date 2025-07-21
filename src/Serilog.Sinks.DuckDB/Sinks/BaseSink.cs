using System.Data;
using System.Text;
using DuckDB.NET.Data;
using Serilog.Events;
using Serilog.Sinks.Extensions;

namespace Serilog.Sinks.DuckDB;

internal abstract class BaseSink
{
	internal const string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fff";

	internal string databasePath;
	internal IFormatProvider formatProvider;
	internal string tableName;

	internal void EnsureSchema()
	{
		using var connection = GetDuckDBConnection();
		using var cmd = connection.CreateCommand();

		cmd.CommandText = "CREATE SEQUENCE IF NOT EXISTS seq_logid START 1";
		cmd.ExecuteNonQuery();

		var sb = new StringBuilder()
			.Append($"CREATE TABLE IF NOT EXISTS {tableName} (")
			.Append("id INTEGER PRIMARY KEY DEFAULT nextval('seq_logid'), ")
			.Append("Timestamp VARCHAR, ")
			.Append("Level VARCHAR(10), ")
			.Append("Exception VARCHAR, ")
			.Append("RenderedMessage VARCHAR, ")
			.Append("Properties VARCHAR)");

		cmd.CommandText = sb.ToString();
		cmd.ExecuteNonQuery();
	}

	internal DuckDBConnection GetDuckDBConnection()
	{
		var conString = new DuckDBConnectionStringBuilder
		{
			DataSource = databasePath,
		}.ConnectionString;

		var connection = new DuckDBConnection(conString);
		connection.Open();

		return connection;
	}

	internal DuckDBCommand CreateSqlInsertCommand(DuckDBConnection connection)
	{
		var sqlInsertText = $"INSERT INTO {tableName} (Timestamp, Level, Exception, RenderedMessage, Properties)";
		sqlInsertText += " VALUES ($timeStamp, $level, $exception, $renderedMessage, $properties)";

		var sqlCommand = connection.CreateCommand();
		sqlCommand.CommandText = sqlInsertText;
		sqlCommand.CommandType = CommandType.Text;

		sqlCommand.Parameters.Add(new DuckDBParameter("timeStamp", DbType.String));
		sqlCommand.Parameters.Add(new DuckDBParameter("level", DbType.String));
		sqlCommand.Parameters.Add(new DuckDBParameter("exception", DbType.String));
		sqlCommand.Parameters.Add(new DuckDBParameter("renderedMessage", DbType.String));
		sqlCommand.Parameters.Add(new DuckDBParameter("properties", DbType.String));

		return sqlCommand;
	}

	internal void PopulateInsertCommand(DuckDBCommand cmd, LogEvent logEvent)
	{
		cmd.Parameters["timeStamp"].Value = logEvent.Timestamp.ToUniversalTime().ToString(TimestampFormat);
		cmd.Parameters["level"].Value = logEvent.Level.ToString();
		cmd.Parameters["exception"].Value = logEvent.Exception?.ToString() ?? string.Empty;
		cmd.Parameters["renderedMessage"].Value = logEvent.MessageTemplate.Render(logEvent.Properties, formatProvider);
		cmd.Parameters["properties"].Value = logEvent.Properties.Count > 0
			? logEvent.Properties.Json()
			: string.Empty;
	}
}
