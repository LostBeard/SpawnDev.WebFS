using Dapper;
using Dapper.Contrib.Extensions;
using System.Data.SQLite;
using System.Diagnostics;
using System.Reflection;

namespace SpawnDev.DB
{
    public class ClassTableInfo : TableInfo
    {
        public bool unique { get; set; }
    }
    public class TableInfo
    {
        public uint cid { get; set; }
        public string name { get; set; } = "";
        public string type { get; set; } = "";
        public bool notnull { get; set; }
        public string? dflt_value { get; set; } = null;
        public uint pk { get; set; }
    }
    public class CollateNoCaseAttribute : Attribute { }
    public class UniqueAttribute : Attribute { }
    public class DefaultCurrentTimestampAttribute : Attribute { }
    public class DefaultCurrentTimeAttribute : Attribute { }
    public class DefaultCurrentDateAttribute : Attribute { }
    public static class SQLiteConnectionExtension
    {
        public static bool ReplaceInto<T>(this SQLiteConnection conn, T obj) where T : class
        {
            using var transAction = conn.BeginTransaction();
            try
            {
                conn.Delete<T>(obj);
                conn.Insert<T>(obj);
                transAction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
            }
            transAction.Rollback();
            return false;
        }

        /// <summary>
        /// runs PRAGMA table_info(TableName)
        /// WARNING: TableName is not sanitized. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="conn"></param>
        /// <returns></returns>
        public static IEnumerable<TableInfo> GetTableInfo<T>(this SQLiteConnection conn) where T : class
        {
            return conn.Query<TableInfo>($"PRAGMA table_info(`{conn.TableName<T>()}`)");
        }

        public static string GetSQLiteTableName(this Type _this)
        {
            var attr = _this.GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault() as TableAttribute;
            return attr != null ? attr.Name : $"{_this.Name}s";
        }

        public static string TableName<T>(this SQLiteConnection conn) where T : class => conn.TableName(typeof(T));
        public static string TableName(this SQLiteConnection conn, Type type)
        {
            var attr = type.GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault() as TableAttribute;
            return attr != null ? attr.Name : $"{type.Name}s";
        }

        public static List<ClassTableInfo> GetClassTableInfo<T>(this SQLiteConnection conn) where T : class => conn.GetClassTableInfo(typeof(T));
        public static List<ClassTableInfo> GetClassTableInfo(this SQLiteConnection conn, Type typeT)
        {
            var tmpInstance = Activator.CreateInstance(typeT);
            var ret = new List<ClassTableInfo>();
            var tableName = conn.TableName(typeT);
            var primaryKeys = new HashSet<string>();
            var uniqueProps = new HashSet<string>();
            var props = typeT.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(o => o.CanWrite);
            PropertyInfo idProp = null;
            PropertyInfo classNameIdProp = null;
            PropertyInfo tableNameIdProp = null;
            foreach (var prop in props)
            {
                if (!prop.CanWrite) continue;
                var attrs = Attribute.GetCustomAttributes(prop);
                var computed = attrs.FirstOrDefault(o => o is ComputedAttribute) != null;
                if (computed) continue;

                var nullableUnderlyingType = Nullable.GetUnderlyingType(prop.PropertyType);
                var sqliteType = MapTypeToSQLiteType(nullableUnderlyingType ?? prop.PropertyType);
                if (string.IsNullOrEmpty(sqliteType))
                {
                    continue;
                }
                var primaryKey = false;
                var unique = false;
                var collateNoCase = false;
                var defaultVal = prop.GetValue(tmpInstance);
                var notNull = defaultVal != null;
                foreach (var attr in attrs)
                {
                    if (attr is KeyAttribute)
                    {
                        if (sqliteType == "TEXT")
                        {
                            throw new Exception("[Key] Attribute used for TEXT field. [ExplicitKey] should be used.");
                        }
                        primaryKey = true;
                    }
                    if (attr is CollateNoCaseAttribute) collateNoCase = true;
                    if (attr is ExplicitKeyAttribute) primaryKey = true;
                    if (attr is UniqueAttribute) unique = true;
                    if (attr is DefaultCurrentTimestampAttribute) defaultVal = "CURRENT_TIMESTAMP";
                    if (attr is DefaultCurrentTimeAttribute) defaultVal = "CURRENT_TIME";
                    if (attr is DefaultCurrentDateAttribute) defaultVal = "CURRENT_DATE";
                }
                if (primaryKey) primaryKeys.Add(prop.Name);
                var info = new ClassTableInfo();
                info.cid = (uint)ret.Count;
                info.name = prop.Name;
                info.type = sqliteType;
                info.notnull = notNull;
                info.unique = unique;
                info.dflt_value = defaultVal == null ? null : defaultVal.ToString();
                ret.Add(info);
                // save for if possibly the primary key property
                if (prop.Name.Equals("id", StringComparison.OrdinalIgnoreCase)) idProp = prop;
                if (prop.Name.Equals($"{tableName}id", StringComparison.OrdinalIgnoreCase)) tableNameIdProp = prop;
                if (prop.Name.Equals($"{typeT.Name}id", StringComparison.OrdinalIgnoreCase)) classNameIdProp = prop;
            }
            // if no primary key is set, use fallback properties (form of Id, [ClassName]Id, or [TableName]Id)
            if (primaryKeys.Count == 0 && idProp != null) primaryKeys.Add(idProp.Name);
            if (primaryKeys.Count == 0 && tableNameIdProp != null) primaryKeys.Add(tableNameIdProp.Name);
            if (primaryKeys.Count == 0 && classNameIdProp != null) primaryKeys.Add(classNameIdProp.Name);
            var pKeys = primaryKeys.ToList();
            for (var i = 0; i < pKeys.Count; i++)
            {
                var pk = pKeys[i];
                var info1 = ret.Where(o => o.name == pk).First();
                info1.pk = (uint)(i + 1);
            }
            return ret;
        }

        //static object GetDefault(Type type)
        //{
        //    if (type.IsValueType)
        //    {
        //        return Activator.CreateInstance(type);
        //    }
        //    return null;
        //}


        //public static object GetDefault(Type t)
        //{
        //    Func<object> f = GetDefault<object>;
        //    return f.Method.GetGenericMethodDefinition().MakeGenericMethod(t).Invoke(null, null);
        //}

        //private static TValue GetDefault<TValue>()
        //{
        //    return default(TValue);
        //}

        // Dapper GetAll method does not support tables with multiple primary keys
        public static IEnumerable<T> QueryAll<T>(this SQLiteConnection conn) where T : class => conn.Query<T>($"SELECT * FROM {conn.TableName<T>()}");

        // TODO - add static properties cache? Depends on how often reflection will be used

        // There are some restrictions on the new column:
        // The new column cannot have a UNIQUE or PRIMARY KEY constraint.
        // If the new column has a NOT NULL constraint, you must specify a default value for the column other than a NULL value.
        // The new column cannot have a default of CURRENT_TIMESTAMP, CURRENT_DATE, and CURRENT_TIME, or an expression.
        // If the new column is a foreign key and the foreign key constraint check is enabled, the new column must accept a default value NULL

        /// <summary>
        /// Designed to be compatible with Dapper
        /// Uses Dapper Attributes to create a table if it does not exist
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="conn"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool CreateTableIfNotExists<T>(this SQLiteConnection conn, IEnumerable<string>? usePrimaryKeys = null) where T : class
        {
            var typeT = typeof(T);
            var tmpInstance = Activator.CreateInstance(typeT);
            var tableName = conn.TableName(typeT);
            var primaryKeys = new List<string>();
            if (usePrimaryKeys != null && usePrimaryKeys.Count() > 0) primaryKeys.AddRange(usePrimaryKeys);
            primaryKeys = primaryKeys ?? new List<string>();
            var uniqueProps = new List<string>();
            var fields = new List<string>();
            var props = typeT.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(o => o.CanWrite);
            PropertyInfo idProp = null;
            PropertyInfo classNameIdProp = null;
            PropertyInfo tableNameIdProp = null;
            var tableInfos = conn.GetTableInfo<T>();
            var tableExists = tableInfos != null && tableInfos.Any();
            var tableInfosProps = tableInfos == null ? new List<string>() : tableInfos.Select(o => o.name).ToList();
            var classInfos = conn.GetClassTableInfo<T>();
            var classInfosProps = classInfos.Select(o => o.name).ToList();
            if (tableExists)
            {
                var missingColumns = classInfosProps.Except(tableInfosProps).ToList();
                foreach (var colName in missingColumns)
                {
                    var newProp = classInfos.Where(o => o.name == colName).First();
                    var addColSql = $"ALTER TABLE `{tableName}` ADD COLUMN `{colName}` {newProp.type}";
                    if (newProp.notnull) addColSql += " NOT NULL";
                    if (newProp.unique) addColSql += " UNIQUE";
                    if (newProp.type == "TEXT")
                    {
                        if (newProp.dflt_value != null) addColSql += $" DEFAULT '{newProp.dflt_value}'";
                    }
                    else
                    {
                        if (newProp.dflt_value != null) addColSql += $" DEFAULT {newProp.dflt_value}";
                    }
                    if (newProp.pk > 0) throw new Exception("SQLite does not allow adding columns to an existing table as primary keys");
                    conn.Execute(addColSql);
                }
                return true;
            }
            foreach (var prop in props)
            {
                if (!prop.CanWrite) continue;
                var attrs = Attribute.GetCustomAttributes(prop);
                var computed = attrs.FirstOrDefault(o => o is ComputedAttribute) != null;
                if (computed) continue;
                var nullableUnderlyingType = Nullable.GetUnderlyingType(prop.PropertyType);
                var sqliteType = MapTypeToSQLiteType(nullableUnderlyingType ?? prop.PropertyType);
                if (string.IsNullOrEmpty(sqliteType))
                {
                    continue;
                }
                var primaryKey = false;
                var unique = false;
                var collateNoCase = false;
                var defaultVal = prop.GetValue(tmpInstance);
                var notNull = defaultVal != null;
                foreach (var attr in attrs)
                {
                    if (attr is KeyAttribute)
                    {
                        if (sqliteType == "TEXT")
                        {
                            throw new Exception("[Key] Attribute used for TEXT field. [ExplicitKey] should be used.");
                        }
                        primaryKey = true;
                    }
                    if (attr is CollateNoCaseAttribute) collateNoCase = true;
                    if (attr is ExplicitKeyAttribute || (primaryKeys.Contains(prop.Name))) primaryKey = true;
                    if (attr is UniqueAttribute) unique = true;
                    if (attr is DefaultCurrentTimestampAttribute) defaultVal = " DEFAULT CURRENT_TIMESTAMP";
                    if (attr is DefaultCurrentTimeAttribute) defaultVal = " DEFAULT CURRENT_TIME";
                    if (attr is DefaultCurrentDateAttribute) defaultVal = " DEFAULT CURRENT_DATE";
                }
                var entry = $"`{prop.Name}` {sqliteType}";
                if (primaryKey && !primaryKeys.Contains(prop.Name)) primaryKeys.Add(prop.Name);
                if (notNull) entry += " NOT NULL";
                if (unique && !primaryKey) entry += " UNIQUE";
                if (collateNoCase) entry += " COLLATE NOCASE";
                //if (defaultVal != null) entry += defaultVal;
                fields.Add(entry);

                // save for if possibly the primary key property
                if (prop.Name.Equals("id", StringComparison.OrdinalIgnoreCase)) idProp = prop;
                if (prop.Name.Equals($"{tableName}id", StringComparison.OrdinalIgnoreCase)) tableNameIdProp = prop;
                if (prop.Name.Equals($"{typeT.Name}id", StringComparison.OrdinalIgnoreCase)) classNameIdProp = prop;
            }
            if (primaryKeys.Count == 0 && idProp != null) primaryKeys.Add(idProp.Name);
            if (primaryKeys.Count == 0 && tableNameIdProp != null) primaryKeys.Add(tableNameIdProp.Name);
            if (primaryKeys.Count == 0 && classNameIdProp != null) primaryKeys.Add(classNameIdProp.Name);
            var fieldStr = string.Join(", ", fields);
            var primaryKeyStr = primaryKeys.Count > 0 ? $", PRIMARY KEY ({string.Join(", ", primaryKeys.Select(o => $"`{o}`"))})" : "";
            var sql = $"CREATE TABLE IF NOT EXISTS {tableName} ({fieldStr}{primaryKeyStr})";
            var ret = false;
            try
            {
                ret = conn.Execute(sql) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SQL Exception: {sql}");
            }
            return ret;
        }
        static Dictionary<string, Type[]> _typeMappings = new Dictionary<string, Type[]>
        {
            { "TEXT", new [] {
                typeof(string),
                typeof(TimeSpan),
                typeof(TimeSpan?),
                typeof(DateTimeOffset),
                typeof(DateTimeOffset?),
                typeof(DateTime),
                typeof(DateTime?),
            } },
            { "INTEGER", new [] {
                typeof(byte),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong),
                typeof(short),
                typeof(ushort),
            } },
            { "REAL", new [] {
                typeof(float),
                typeof(double),
            } },
            { "NUMERIC", new [] {
                typeof(decimal),
                typeof(bool),
            } },
            { "BLOB", new [] {
                typeof(byte[]),
            } },

        };
        static string MapTypeToSQLiteType(Type type)
        {
            var ret = _typeMappings.Where(o => o.Value.Contains(type));
            return ret.Count() == 0 ? "" : ret.FirstOrDefault().Key;
        }
    }
}


