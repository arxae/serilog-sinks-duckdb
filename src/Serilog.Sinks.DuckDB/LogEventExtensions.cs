// Based on https://github.com/saleem-mirza/serilog-sinks-sqlite/blob/dev/src/Serilog.Sinks.SQLite/Sinks/Extensions/LogEventExtensions.cs

// Copyright 2019 Zethian Inc.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Text.Json;
using Serilog.Events;

namespace Serilog.Sinks.Extensions;

internal static class LogEventExtensions
{
	internal static string Json(this LogEvent logEvent, bool storeTimestampInUtc = false)
	{
		return JsonSerializer.Serialize(ConvertToDictionary(logEvent, storeTimestampInUtc));
	}

	internal static IDictionary<string, object> Dictionary(
		this LogEvent logEvent,
		bool storeTimestampInUtc = false,
		IFormatProvider formatProvider = null)
	{
		return ConvertToDictionary(logEvent, storeTimestampInUtc, formatProvider);
	}

	internal static string Json(this IReadOnlyDictionary<string, LogEventPropertyValue> properties)
	{
		return JsonSerializer.Serialize(ConvertToDictionary(properties));
	}

	internal static IDictionary<string, object> Dictionary(
		this IReadOnlyDictionary<string, LogEventPropertyValue> properties)
	{
		return ConvertToDictionary(properties);
	}

	static Dictionary<string, object> ConvertToDictionary(IReadOnlyDictionary<string, LogEventPropertyValue> properties)
	{
		var dict = new Dictionary<string, object>();
		foreach (var property in properties)
			dict.Add(property.Key, Simplify(property.Value));
		return dict;
	}

	static Dictionary<string, object> ConvertToDictionary(
		LogEvent logEvent,
		bool storeTimestampInUtc,
		IFormatProvider formatProvider = null)
	{
		var dict = new Dictionary<string, object>
		{
			["Timestamp"] = storeTimestampInUtc
				? logEvent.Timestamp.ToUniversalTime().ToString("o")
				: logEvent.Timestamp.ToString("o"),
			["LogLevel"] = logEvent.Level.ToString(),
			["LogMessageTemplate"] = logEvent.MessageTemplate.Text,
			["LogMessage"] = logEvent.RenderMessage(formatProvider),
			["LogException"] = logEvent.Exception,
			["LogProperties"] = ConvertToDictionary(logEvent.Properties)
		};
		return dict;
	}

	static object Simplify(LogEventPropertyValue data)
	{
		if (data is ScalarValue value)
			return value.Value;

		if (data is DictionaryValue dictValue)
		{
			var dict = new Dictionary<string, object>();
			foreach (var item in dictValue.Elements)
			{
				if (item.Key.Value is string key)
					dict[key] = Simplify(item.Value);
			}
			return dict;
		}

		if (data is SequenceValue seq)
			return seq.Elements.Select(Simplify).ToArray();

		if (data is not StructureValue str)
			return null;

		try
		{
			if (str.TypeTag == null)
				return str.Properties.ToDictionary(p => p.Name, p => Simplify(p.Value));

			if (!str.TypeTag.StartsWith("DictionaryEntry") && !str.TypeTag.StartsWith("KeyValuePair"))
				return str.Properties.ToDictionary(p => p.Name, p => Simplify(p.Value));

			var key = Simplify(str.Properties[0].Value);

			if (key == null)
				return null;

			var dict = new Dictionary<string, object>
			{
				[key.ToString()] = Simplify(str.Properties[1].Value)
			};

			return dict;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
		}

		return null;
	}
}