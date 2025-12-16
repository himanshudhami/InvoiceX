using Dapper;
using System;
using System.Data;

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