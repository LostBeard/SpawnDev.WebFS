using Dapper;
using System.Data;
using System.Globalization;

namespace SpawnDev.DB
{
    public class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
        {
            // insert as UTC time
            // assume unspecified kind is UTC
            parameter.Value = value.ToString("o");
        }

        public override DateTimeOffset Parse(object value)
        {
            DateTimeOffset ret;
            switch (value)
            {
                case long valueLong:
                    ret = DateTimeOffset.FromUnixTimeMilliseconds(valueLong);
                    break;
                case string valueStr:
                    ret = DateTimeOffset.ParseExact(valueStr, "o", CultureInfo.InvariantCulture.DateTimeFormat);
                    break;
                case DateTimeOffset valueDateTimeOffset:
                    ret = valueDateTimeOffset;
                    break;
                default:
                    ret = (DateTimeOffset)default;
                    break;
            }
            return ret;
        }
        public static void AddDateTimeOffsetHandler()
        {
            SqlMapper.RemoveTypeMap(typeof(DateTimeOffset));
            SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
        }
    }

    public class DateTimeOffsetNullableHandler : SqlMapper.TypeHandler<DateTimeOffset?>
    {
        public override void SetValue(IDbDataParameter parameter, DateTimeOffset? value)
        {
            // insert as UTC time
            // assume unspecified kind is UTC
            if (value != null)
            {
                parameter.Value = value.Value.ToString("o");
            }
        }

        public override DateTimeOffset? Parse(object value)
        {
            if (value == null)
            {
                return null;
            }
            DateTimeOffset ret;
            switch (value)
            {
                case long valueLong:
                    ret = DateTimeOffset.FromUnixTimeMilliseconds(valueLong);
                    break;
                case string valueStr:
                    ret = DateTimeOffset.ParseExact(valueStr, "o", CultureInfo.InvariantCulture.DateTimeFormat);
                    break;
                case DateTimeOffset valueDateTimeOffset:
                    ret = valueDateTimeOffset;
                    break;
                default:
                    ret = (DateTimeOffset)default;
                    break;
            }
            return ret;
        }
        public static void AddDateTimeOffsetNullableHandler()
        {
            SqlMapper.RemoveTypeMap(typeof(DateTimeOffset?));
            SqlMapper.AddTypeHandler(new DateTimeOffsetNullableHandler());
        }
    }
}
