using Dapper;
using System;
using System.Data;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

namespace Infrastructure.Configuration
{
    /// <summary>
    /// Configuration for Dapper type handlers
    /// </summary>
    public static class DapperConfiguration
    {
        /// <summary>
        /// Configure type handlers for Dapper
        /// </summary>
        public static void ConfigureTypeHandlers()
        {
            SqlMapper.AddTypeHandler(new DateOnlyHandler());
            SqlMapper.AddTypeHandler(new NullableDateOnlyHandler());
            SqlMapper.AddTypeHandler(new TimeOnlyHandler());
            SqlMapper.AddTypeHandler(new JsonStringHandler());
        }
    }

    /// <summary>
    /// Type handler for PostgreSQL JSONB columns mapped to string properties.
    /// Handles the Npgsql 7+ behavior where JSONB is returned as JsonDocument.
    /// </summary>
    public class JsonStringHandler : SqlMapper.TypeHandler<string>
    {
        public override void SetValue(IDbDataParameter parameter, string? value)
        {
            if (parameter is NpgsqlParameter npgsqlParam)
            {
                npgsqlParam.NpgsqlDbType = NpgsqlDbType.Jsonb;
                npgsqlParam.Value = value ?? (object)DBNull.Value;
            }
            else
            {
                parameter.Value = value ?? (object)DBNull.Value;
            }
        }

        public override string? Parse(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            // Handle JsonDocument from Npgsql 7+
            if (value is JsonDocument jsonDoc)
                return jsonDoc.RootElement.GetRawText();

            // Handle JsonElement
            if (value is JsonElement jsonElement)
                return jsonElement.GetRawText();

            // Handle string (already serialized)
            if (value is string str)
                return str;

            // Fallback: try to serialize
            return JsonSerializer.Serialize(value);
        }
    }

    /// <summary>
    /// Type handler for DateOnly to work with Dapper and PostgreSQL
    /// </summary>
    public class DateOnlyHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            parameter.Value = value.ToDateTime(TimeOnly.MinValue);
        }

        public override DateOnly Parse(object value)
        {
            return DateOnly.FromDateTime((DateTime)value);
        }
    }

    /// <summary>
    /// Type handler for nullable DateOnly
    /// </summary>
    public class NullableDateOnlyHandler : SqlMapper.TypeHandler<DateOnly?>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly? value)
        {
            parameter.Value = value?.ToDateTime(TimeOnly.MinValue) ?? (object)DBNull.Value;
        }

        public override DateOnly? Parse(object value)
        {
            return value == null || value == DBNull.Value 
                ? null 
                : DateOnly.FromDateTime((DateTime)value);
        }
    }

    /// <summary>
    /// Type handler for TimeOnly to work with Dapper and PostgreSQL
    /// </summary>
    public class TimeOnlyHandler : SqlMapper.TypeHandler<TimeOnly>
    {
        public override void SetValue(IDbDataParameter parameter, TimeOnly value)
        {
            parameter.Value = value.ToTimeSpan();
        }

        public override TimeOnly Parse(object value)
        {
            return TimeOnly.FromTimeSpan((TimeSpan)value);
        }
    }
}