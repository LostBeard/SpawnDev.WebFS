using Dapper;
using System.Data;
using System.Globalization;

namespace SpawnDev.DB
{
    public class DateTimeHandler : SqlMapper.TypeHandler<DateTime>
    {
        public override void SetValue(IDbDataParameter parameter, DateTime value)
        {
            parameter.Value = value.ToString("o");
        }

        public override DateTime Parse(object value)
        {
            DateTime ret;
            switch (value)
            {
                case long valueLong:
                    ret = DateTimeOffset.FromUnixTimeMilliseconds(valueLong).DateTime;
                    break;
                case string valueStr:
                    ret = DateTime.ParseExact(valueStr, "o", CultureInfo.InvariantCulture.DateTimeFormat);
                    break;
                case DateTime valueDateTime:
                    ret = valueDateTime;
                    break;
                default:
                    ret = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
                    break;
            }
            switch (ret.Kind)
            {
                case DateTimeKind.Unspecified:
                    ret = DateTime.SpecifyKind(ret, DateTimeKind.Utc);
                    break;
                case DateTimeKind.Local:
                    ret = ret.ToUniversalTime();
                    break;
                case DateTimeKind.Utc:
                default:
                    break;
            }
            return ret;
        }
        public static void AddDateTimeHandler()
        {
            SqlMapper.RemoveTypeMap(typeof(DateTime));
            SqlMapper.AddTypeHandler(new DateTimeHandler());
        }
    }

    public class DateTimeNullableHandler : SqlMapper.TypeHandler<DateTime?>
    {
        public override void SetValue(IDbDataParameter parameter, DateTime? value)
        {
            // insert as UTC time
            // assume unspecified kind is UTC
            if (value != null)
            {
                parameter.Value = value.Value.ToString("o");
            }
        }

        public override DateTime? Parse(object value)
        {
            if (value == null)
            {
                return null;
            }
            DateTime ret;
            switch (value)
            {
                case long valueLong:
                    ret = DateTimeOffset.FromUnixTimeMilliseconds(valueLong).DateTime;
                    break;
                case string valueStr:
                    ret = DateTime.ParseExact(valueStr, "o", CultureInfo.InvariantCulture.DateTimeFormat);
                    break;
                case DateTime valueDateTime:
                    ret = valueDateTime;
                    break;
                default:
                    ret = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
                    break;
            }
            switch (ret.Kind)
            {
                case DateTimeKind.Unspecified:
                    ret = DateTime.SpecifyKind(ret, DateTimeKind.Utc);
                    break;
            }
            return ret;
        }
        public static void AddDateTimeNullableHandler()
        {
            SqlMapper.RemoveTypeMap(typeof(DateTime?));
            SqlMapper.AddTypeHandler(new DateTimeNullableHandler());
        }
    }
}
