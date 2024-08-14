﻿using FreeSql.DataAnnotations;
using FreeSql.Internal;
using FreeSql.Internal.Model;
using Newtonsoft.Json.Linq;
using Npgsql.LegacyPostgis;
using NpgsqlTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace FreeSql.PostgreSQL
{

    class PostgreSQLCodeFirst : Internal.CommonProvider.CodeFirstProvider
    {

        public PostgreSQLCodeFirst(IFreeSql orm, CommonUtils commonUtils, CommonExpression commonExpression) : base(orm, commonUtils, commonExpression) { }

        static object _dicCsToDbLock = new object();
        static Dictionary<string, CsToDb<NpgsqlDbType>> _dicCsToDb = new Dictionary<string, CsToDb<NpgsqlDbType>>() {

                { typeof(sbyte).FullName, CsToDb.New(NpgsqlDbType.Smallint, "int2","int2 NOT NULL", false, false, 0) },{ typeof(sbyte?).FullName, CsToDb.New(NpgsqlDbType.Smallint, "int2", "int2", false, true, null) },
                { typeof(short).FullName, CsToDb.New(NpgsqlDbType.Smallint, "int2","int2 NOT NULL", false, false, 0) },{ typeof(short?).FullName, CsToDb.New(NpgsqlDbType.Smallint, "int2", "int2", false, true, null) },
                { typeof(int).FullName, CsToDb.New(NpgsqlDbType.Integer, "int4","int4 NOT NULL", false, false, 0) },{ typeof(int?).FullName, CsToDb.New(NpgsqlDbType.Integer, "int4", "int4", false, true, null) },
                { typeof(long).FullName, CsToDb.New(NpgsqlDbType.Bigint, "int8","int8 NOT NULL", false, false, 0) },{ typeof(long?).FullName, CsToDb.New(NpgsqlDbType.Bigint, "int8", "int8", false, true, null) },

                { typeof(byte).FullName, CsToDb.New(NpgsqlDbType.Smallint, "int2","int2 NOT NULL", false, false, 0) },{ typeof(byte?).FullName, CsToDb.New(NpgsqlDbType.Smallint, "int2", "int2", false, true, null) },
                { typeof(ushort).FullName, CsToDb.New(NpgsqlDbType.Integer, "int4","int4 NOT NULL", false, false, 0) },{ typeof(ushort?).FullName, CsToDb.New(NpgsqlDbType.Integer, "int4", "int4", false, true, null) },
                { typeof(uint).FullName, CsToDb.New(NpgsqlDbType.Bigint, "int8","int8 NOT NULL", false, false, 0) },{ typeof(uint?).FullName, CsToDb.New(NpgsqlDbType.Bigint, "int8", "int8", false, true, null) },
                { typeof(ulong).FullName, CsToDb.New(NpgsqlDbType.Numeric, "numeric","numeric(20,0) NOT NULL", false, false, 0) },{ typeof(ulong?).FullName, CsToDb.New(NpgsqlDbType.Numeric, "numeric", "numeric(20,0)", false, true, null) },

                { typeof(float).FullName, CsToDb.New(NpgsqlDbType.Real, "float4","float4 NOT NULL", false, false, 0) },{ typeof(float?).FullName, CsToDb.New(NpgsqlDbType.Real, "float4", "float4", false, true, null) },
                { typeof(double).FullName, CsToDb.New(NpgsqlDbType.Double, "float8","float8 NOT NULL", false, false, 0) },{ typeof(double?).FullName, CsToDb.New(NpgsqlDbType.Double, "float8", "float8", false, true, null) },
                { typeof(decimal).FullName, CsToDb.New(NpgsqlDbType.Numeric, "numeric", "numeric(10,2) NOT NULL", false, false, 0) },{ typeof(decimal?).FullName, CsToDb.New(NpgsqlDbType.Numeric, "numeric", "numeric(10,2)", false, true, null) },

                { typeof(string).FullName, CsToDb.New(NpgsqlDbType.Varchar, "varchar", "varchar(255)", false, null, "") },
                { typeof(char).FullName, CsToDb.New(NpgsqlDbType.Char, "bpchar", "bpchar(1)", false, null, '\0') },

                { typeof(TimeSpan).FullName, CsToDb.New(NpgsqlDbType.Time, "time","time NOT NULL", false, false, 0) },{ typeof(TimeSpan?).FullName, CsToDb.New(NpgsqlDbType.Time, "time", "time",false, true, null) },
                { typeof(DateTime).FullName, CsToDb.New(NpgsqlDbType.Timestamp, "timestamp", "timestamp NOT NULL", false, false, new DateTime(1970,1,1)) },{ typeof(DateTime?).FullName, CsToDb.New(NpgsqlDbType.Timestamp, "timestamp", "timestamp", false, true, null) },

#if net60
                { typeof(TimeOnly).FullName, CsToDb.New(NpgsqlDbType.Time, "time", "time NOT NULL", false, false, 0) },{ typeof(TimeOnly?).FullName, CsToDb.New(NpgsqlDbType.Time, "time", "time", false, true, null) },
                { typeof(DateOnly).FullName, CsToDb.New(NpgsqlDbType.Date, "date", "date NOT NULL", false, false, new DateTime(1970,1,1)) },{ typeof(DateOnly?).FullName, CsToDb.New(NpgsqlDbType.Date, "date", "date", false, true, null) },
#endif

                { typeof(bool).FullName, CsToDb.New(NpgsqlDbType.Boolean, "bool","bool NOT NULL", null, false, false) },{ typeof(bool?).FullName, CsToDb.New(NpgsqlDbType.Boolean, "bool","bool", null, true, null) },
                { typeof(Byte[]).FullName, CsToDb.New(NpgsqlDbType.Bytea, "bytea", "bytea", false, null, new byte[0]) },
                { typeof(BitArray).FullName, CsToDb.New(NpgsqlDbType.Varbit, "varbit", "varbit(64)", false, null, new BitArray(new byte[64])) },
                { typeof(BigInteger).FullName, CsToDb.New(NpgsqlDbType.Numeric, "numeric", "numeric(78,0) NOT NULL", false, false, 0) },{ typeof(BigInteger?).FullName, CsToDb.New(NpgsqlDbType.Numeric, "numeric", "numeric(78,0)", false, true, null) },

                { typeof(NpgsqlPoint).FullName, CsToDb.New(NpgsqlDbType.Point, "point", "point NOT NULL", false, false, new NpgsqlPoint(0, 0)) },{ typeof(NpgsqlPoint?).FullName, CsToDb.New(NpgsqlDbType.Point, "point", "point", false, true, null) },
                { typeof(NpgsqlLine).FullName, CsToDb.New(NpgsqlDbType.Line, "line", "line NOT NULL", false, false, new NpgsqlLine(0, 0, 1)) },{ typeof(NpgsqlLine?).FullName, CsToDb.New(NpgsqlDbType.Line, "line", "line", false, true, null) },
                { typeof(NpgsqlLSeg).FullName, CsToDb.New(NpgsqlDbType.LSeg, "lseg", "lseg NOT NULL", false, false, new NpgsqlLSeg(0, 0, 0, 0)) },{ typeof(NpgsqlLSeg?).FullName, CsToDb.New(NpgsqlDbType.LSeg, "lseg", "lseg", false, true, null) },
                { typeof(NpgsqlBox).FullName, CsToDb.New(NpgsqlDbType.Box, "box", "box NOT NULL", false, false, new NpgsqlBox(0, 0, 0, 0)) },{ typeof(NpgsqlBox?).FullName, CsToDb.New(NpgsqlDbType.Box, "box", "box", false, true, null) },
                { typeof(NpgsqlPath).FullName, CsToDb.New(NpgsqlDbType.Path, "path", "path NOT NULL", false, false, new NpgsqlPath(new NpgsqlPoint(0, 0))) },{ typeof(NpgsqlPath?).FullName, CsToDb.New(NpgsqlDbType.Path, "path", "path", false, true, null) },
                { typeof(NpgsqlPolygon).FullName, CsToDb.New(NpgsqlDbType.Polygon, "polygon", "polygon NOT NULL", false, false, new NpgsqlPolygon(new NpgsqlPoint(0, 0), new NpgsqlPoint(0, 0))) },{ typeof(NpgsqlPolygon?).FullName, CsToDb.New(NpgsqlDbType.Polygon, "polygon", "polygon", false, true, null) },
                { typeof(NpgsqlCircle).FullName, CsToDb.New(NpgsqlDbType.Circle, "circle", "circle NOT NULL", false, false, new NpgsqlCircle(0, 0, 0)) },{ typeof(NpgsqlCircle?).FullName, CsToDb.New(NpgsqlDbType.Circle, "circle", "circle", false, true, null) },

                { typeof((IPAddress Address, int Subnet)).FullName, CsToDb.New(NpgsqlDbType.Cidr, "cidr", "cidr NOT NULL", false, false, (IPAddress.Any, 0)) },{ typeof((IPAddress Address, int Subnet)?).FullName, CsToDb.New(NpgsqlDbType.Cidr, "cidr", "cidr", false, true, null) },
                { typeof(IPAddress).FullName, CsToDb.New(NpgsqlDbType.Inet, "inet", "inet", false, null, IPAddress.Any) },
                { typeof(PhysicalAddress).FullName, CsToDb.New(NpgsqlDbType.MacAddr, "macaddr", "macaddr", false, null, PhysicalAddress.None) },

                { typeof(JToken).FullName, CsToDb.New(NpgsqlDbType.Jsonb, "jsonb", "jsonb", false, null, JToken.Parse("{}")) },
                { typeof(JObject).FullName, CsToDb.New(NpgsqlDbType.Jsonb, "jsonb", "jsonb", false, null, JObject.Parse("{}")) },
                { typeof(JArray).FullName, CsToDb.New(NpgsqlDbType.Jsonb, "jsonb", "jsonb", false, null, JArray.Parse("[]")) },
                { typeof(Guid).FullName, CsToDb.New(NpgsqlDbType.Uuid, "uuid", "uuid NOT NULL", false, false, Guid.Empty) },{ typeof(Guid?).FullName, CsToDb.New(NpgsqlDbType.Uuid, "uuid", "uuid", false, true, null) },

                { typeof(NpgsqlRange<int>).FullName, CsToDb.New(NpgsqlDbType.Range | NpgsqlDbType.Integer, "int4range", "int4range NOT NULL", false, false, NpgsqlRange<int>.Empty) },{ typeof(NpgsqlRange<int>?).FullName, CsToDb.New(NpgsqlDbType.Range | NpgsqlDbType.Integer, "int4range", "int4range", false, true, null) },
                { typeof(NpgsqlRange<long>).FullName, CsToDb.New(NpgsqlDbType.Range | NpgsqlDbType.Bigint, "int8range", "int8range NOT NULL", false, false, NpgsqlRange<long>.Empty) },{ typeof(NpgsqlRange<long>?).FullName, CsToDb.New(NpgsqlDbType.Range | NpgsqlDbType.Bigint, "int8range", "int8range", false, true, null) },
                { typeof(NpgsqlRange<decimal>).FullName, CsToDb.New(NpgsqlDbType.Range | NpgsqlDbType.Numeric, "numrange", "numrange NOT NULL", false, false, NpgsqlRange<decimal>.Empty) },{ typeof(NpgsqlRange<decimal>?).FullName, CsToDb.New(NpgsqlDbType.Range | NpgsqlDbType.Numeric, "numrange", "numrange", false, true, null) },
                { typeof(NpgsqlRange<DateTime>).FullName, CsToDb.New(NpgsqlDbType.Range | NpgsqlDbType.Timestamp, "tsrange", "tsrange NOT NULL", false, false, NpgsqlRange<DateTime>.Empty) },{ typeof(NpgsqlRange<DateTime>?).FullName, CsToDb.New(NpgsqlDbType.Range | NpgsqlDbType.Timestamp, "tsrange", "tsrange", false, true, null) },

                { typeof(Dictionary<string, string>).FullName, CsToDb.New(NpgsqlDbType.Hstore, "hstore", "hstore", false, null, new Dictionary<string, string>()) },

                { typeof(PostgisPoint).FullName, CsToDb.New(NpgsqlDbType.Geometry, "geometry", "geometry", false, null, new PostgisPoint(0, 0)) },
                { typeof(PostgisLineString).FullName, CsToDb.New(NpgsqlDbType.Geometry, "geometry", "geometry", false, null, new PostgisLineString(new []{new Coordinate2D(0, 0),new Coordinate2D(0, 0) })) },
                { typeof(PostgisPolygon).FullName, CsToDb.New(NpgsqlDbType.Geometry, "geometry", "geometry", false, null, new PostgisPolygon(new []{new []{new Coordinate2D(0, 0),new Coordinate2D(0, 0) }, new []{new Coordinate2D(0, 0),new Coordinate2D(0, 0) } })) },
                { typeof(PostgisMultiPoint).FullName, CsToDb.New(NpgsqlDbType.Geometry, "geometry", "geometry", false, null, new PostgisMultiPoint(new []{new Coordinate2D(0, 0),new Coordinate2D(0, 0) })) },
                { typeof(PostgisMultiLineString).FullName, CsToDb.New(NpgsqlDbType.Geometry, "geometry", "geometry", false, null, new PostgisMultiLineString(new[]{new PostgisLineString(new []{new Coordinate2D(0, 0),new Coordinate2D(0, 0) }),new PostgisLineString(new []{new Coordinate2D(0, 0),new Coordinate2D(0, 0) }) })) },
                { typeof(PostgisMultiPolygon).FullName, CsToDb.New(NpgsqlDbType.Geometry, "geometry", "geometry", false, null, new PostgisMultiPolygon(new[]{new PostgisPolygon(new []{new []{new Coordinate2D(0, 0),new Coordinate2D(0, 0) }, new []{new Coordinate2D(0, 0),new Coordinate2D(0, 0) } }),new PostgisPolygon(new []{new []{new Coordinate2D(0, 0),new Coordinate2D(0, 0) }, new []{new Coordinate2D(0, 0),new Coordinate2D(0, 0) } }) })) },
                { typeof(PostgisGeometry).FullName, CsToDb.New(NpgsqlDbType.Geometry, "geometry", "geometry", false, null, new PostgisPoint(0, 0)) },
                { typeof(PostgisGeometryCollection).FullName, CsToDb.New(NpgsqlDbType.Geometry, "geometry", "geometry", false, null, new PostgisGeometryCollection(new[]{new PostgisPoint(0, 0),new PostgisPoint(0, 0) })) },

#if nts
                { typeof(NetTopologySuite.Geometries.Point).FullName, CsToDb.New(NpgsqlDbType.Geometry, "geometry", "geometry", false, null, new NetTopologySuite.Geometries.Point(0, 0)) },
                { typeof(NetTopologySuite.Geometries.LineString).FullName, CsToDb.New(NpgsqlDbType.Geometry, "geometry", "geometry", false, null, new NetTopologySuite.Geometries.LineString(new []{new NetTopologySuite.Geometries.Coordinate(0, 0),new NetTopologySuite.Geometries.Coordinate(0, 0) })) },
                { typeof(NetTopologySuite.Geometries.Polygon).FullName, CsToDb.New(NpgsqlDbType.Geometry, "geometry", "geometry", false, null, new NetTopologySuite.Geometries.Polygon(new NetTopologySuite.Geometries.LinearRing(new []{ new NetTopologySuite.Geometries.Coordinate(0, 0),new NetTopologySuite.Geometries.Coordinate(0, 0), new NetTopologySuite.Geometries.Coordinate(0, 0),new NetTopologySuite.Geometries.Coordinate(0, 0) }))) },
                { typeof(NetTopologySuite.Geometries.MultiPoint).FullName, CsToDb.New(NpgsqlDbType.Geometry, "geometry", "geometry", false, null, new NetTopologySuite.Geometries.MultiPoint(new []{new NetTopologySuite.Geometries.Point(0, 0),new NetTopologySuite.Geometries.Point(0, 0) })) },
                { typeof(NetTopologySuite.Geometries.MultiLineString).FullName, CsToDb.New(NpgsqlDbType.Geometry, "geometry", "geometry", false, null, new NetTopologySuite.Geometries.MultiLineString(new[]{new NetTopologySuite.Geometries.LineString(new []{new NetTopologySuite.Geometries.Coordinate(0, 0),new NetTopologySuite.Geometries.Coordinate(0, 0) }),new NetTopologySuite.Geometries.LineString(new []{new NetTopologySuite.Geometries.Coordinate(0, 0),new NetTopologySuite.Geometries.Coordinate(0, 0) }) })) },
                { typeof(NetTopologySuite.Geometries.MultiPolygon).FullName, CsToDb.New(NpgsqlDbType.Geometry, "geometry", "geometry", false, null, new NetTopologySuite.Geometries.MultiPolygon(new[]{new NetTopologySuite.Geometries.Polygon(new NetTopologySuite.Geometries.LinearRing(new []{ new NetTopologySuite.Geometries.Coordinate(0, 0),new NetTopologySuite.Geometries.Coordinate(0, 0), new NetTopologySuite.Geometries.Coordinate(0, 0),new NetTopologySuite.Geometries.Coordinate(0, 0) })), new NetTopologySuite.Geometries.Polygon(new NetTopologySuite.Geometries.LinearRing(new []{ new NetTopologySuite.Geometries.Coordinate(0, 0),new NetTopologySuite.Geometries.Coordinate(0, 0), new NetTopologySuite.Geometries.Coordinate(0, 0),new NetTopologySuite.Geometries.Coordinate(0, 0) })) })) },
                { typeof(NetTopologySuite.Geometries.Geometry).FullName, CsToDb.New(NpgsqlDbType.Geometry, "geometry", "geometry", false, null, new NetTopologySuite.Geometries.Point(0, 0)) },
                { typeof(NetTopologySuite.Geometries.GeometryCollection).FullName, CsToDb.New(NpgsqlDbType.Geometry, "geometry", "geometry", false, null, new NetTopologySuite.Geometries.GeometryCollection(new[]{new NetTopologySuite.Geometries.Point(0, 0),new NetTopologySuite.Geometries.Point(0, 0) })) },
#endif
        };

        public override DbInfoResult GetDbInfo(Type type)
        {
            var isarray = type.FullName != "System.Byte[]" && type.IsArray;
            var elementType = isarray ? type.GetElementType() : type;
            var info = GetDbInfoNoneArray(elementType);
            if (info == null) return null;
            if (isarray == false) return new DbInfoResult((int)info.type, info.dbtype, info.dbtypeFull, info.isnullable, info.defaultValue);
            var dbtypefull = Regex.Replace(info.dbtypeFull, $@"{info.dbtype}(\s*\([^\)]+\))?", "$0[]").Replace(" NOT NULL", "");
            return new DbInfoResult((int)(info.type | NpgsqlDbType.Array), $"{info.dbtype}[]", dbtypefull, null, Array.CreateInstance(elementType, 0));
        }
        CsToDb<NpgsqlDbType> GetDbInfoNoneArray(Type type)
        {
            if (_dicCsToDb.TryGetValue(type.FullName, out var trydc)) return trydc;
            if (type.IsArray) return null;
            var enumType = type.IsEnum ? type : null;
            if (enumType == null && type.IsNullableType() && type.GenericTypeArguments.Length == 1 && type.GenericTypeArguments.First().IsEnum) enumType = type.GenericTypeArguments.First();
            if (enumType != null)
            {
                var newItem = enumType.GetCustomAttributes(typeof(FlagsAttribute), false).Any() ?
                    CsToDb.New(NpgsqlDbType.Bigint, "int8", $"int8{(type.IsEnum ? " NOT NULL" : "")}", false, type.IsEnum ? false : true, enumType.CreateInstanceGetDefaultValue()) :
                    CsToDb.New(NpgsqlDbType.Integer, "int4", $"int4{(type.IsEnum ? " NOT NULL" : "")}", false, type.IsEnum ? false : true, enumType.CreateInstanceGetDefaultValue());
                if (_dicCsToDb.ContainsKey(type.FullName) == false)
                {
                    lock (_dicCsToDbLock)
                    {
                        if (_dicCsToDb.ContainsKey(type.FullName) == false)
                            _dicCsToDb.Add(type.FullName, newItem);
                    }
                }
                return newItem;
            }
            return null;
        }

        protected override string GetComparisonDDLStatements(params TypeSchemaAndName[] objects)
        {
            var sb = new StringBuilder();
            var seqcols = new List<NativeTuple<ColumnInfo, string[], bool>>(); //序列

            var isPg10 = (_orm.DbFirst as PostgreSQLDbFirst).IsPg10;
            var isPg95 = (_orm.DbFirst as PostgreSQLDbFirst).IsPg95;
            var isPg96 = (_orm.DbFirst as PostgreSQLDbFirst).IsPg96;

            foreach (var obj in objects)
            {
                if (sb.Length > 0) sb.Append("\r\n");
                var tb = obj.tableSchema;
                if (tb == null) throw new Exception(CoreStrings.S_Type_IsNot_Migrable(obj.tableSchema.Type.FullName));
                if (tb.Columns.Any() == false) throw new Exception(CoreStrings.S_Type_IsNot_Migrable_0Attributes(obj.tableSchema.Type.FullName));
                var tbname = _commonUtils.SplitTableName(tb.DbName);
                if (tbname?.Length == 1) tbname = new[] { "public", tbname[0] };

                var tboldname = _commonUtils.SplitTableName(tb.DbOldName); //旧表名
                if (tboldname?.Length == 1) tboldname = new[] { "public", tboldname[0] };
                if (string.IsNullOrEmpty(obj.tableName) == false)
                {
                    var tbtmpname = _commonUtils.SplitTableName(obj.tableName);
                    if (tbtmpname?.Length == 1) tbtmpname = new[] { "public", tbtmpname[0] };
                    if (tbname[0] != tbtmpname[0] || tbname[1] != tbtmpname[1])
                    {
                        tbname = tbtmpname;
                        tboldname = null;
                    }
                }
                //codefirst 不支持表名、模式名、数据库名中带 .

                if (string.Compare(tbname[0], "public", true) != 0 && _orm.Ado.ExecuteScalar(CommandType.Text, _commonUtils.FormatSql(" select 1 from pg_namespace where nspname={0}", tbname[0])) == null) //创建模式
                    sb.Append("CREATE SCHEMA IF NOT EXISTS ").Append(tbname[0]).Append(";\r\n");

                var sbalter = new StringBuilder();
                var istmpatler = false; //创建临时表，导入数据，删除旧表，修改
                if (_orm.Ado.ExecuteScalar(CommandType.Text, string.Format(" select 1 from pg_tables a inner join pg_namespace b on b.nspname = a.schemaname where b.nspname || '.' || a.tablename = '{0}.{1}'", tbname)) == null)
                { //表不存在
                    if (tboldname != null)
                    {
                        if (_orm.Ado.ExecuteScalar(CommandType.Text, string.Format(" select 1 from pg_tables a inner join pg_namespace b on b.nspname = a.schemaname where b.nspname || '.' || a.tablename = '{0}.{1}'", tboldname)) == null)
                            //旧表不存在
                            tboldname = null;
                    }
                    if (tboldname == null)
                    {
                        //创建表
                        var createTableName = _commonUtils.QuoteSqlName($"{tbname[0]}.{tbname[1]}");
                        sb.Append("CREATE TABLE IF NOT EXISTS ").Append(createTableName).Append(" ( ");
                        foreach (var tbcol in tb.ColumnsByPosition)
                        {
                            sb.Append(" \r\n  ").Append(_commonUtils.QuoteSqlName(tbcol.Attribute.Name)).Append(" ").Append(tbcol.Attribute.DbType);
                            if (tbcol.Attribute.IsIdentity == true)
                            {
                                if (isPg10) sb.Append(" GENERATED BY DEFAULT AS IDENTITY");
                                else seqcols.Add(NativeTuple.Create(tbcol, tbname, true));
                            }
                            sb.Append(",");
                        }
                        if (tb.Primarys.Any())
                        {
                            var pkname = $"{tbname[0]}_{tbname[1]}_pkey";
                            sb.Append(" \r\n  CONSTRAINT ").Append(_commonUtils.QuoteSqlName(pkname)).Append(" PRIMARY KEY (");
                            foreach (var tbcol in tb.Primarys) sb.Append(_commonUtils.QuoteSqlName(tbcol.Attribute.Name)).Append(", ");
                            sb.Remove(sb.Length - 2, 2).Append("),");
                        }
                        sb.Remove(sb.Length - 1, 1);
                        sb.Append("\r\n) WITH (OIDS=FALSE);\r\n");
                        //创建表的索引
                        foreach (var uk in tb.Indexes)
                        {
                            sb.Append("CREATE ");
                            if (uk.IsUnique) sb.Append("UNIQUE ");
                            sb.Append("INDEX ");
                            if (isPg95) sb.Append("IF NOT EXISTS ");
                            sb.Append(_commonUtils.QuoteSqlName(ReplaceIndexName(uk.Name, tbname[1]))).Append(" ON ").Append(createTableName);
                            if (uk.IndexMethod != IndexMethod.B_Tree) sb.Append(" USING ").Append(uk.IndexMethod.ToString().ToUpper().Replace("_", ""));
                            sb.Append("(");
                            foreach (var tbcol in uk.Columns)
                            {
                                sb.Append(_commonUtils.QuoteSqlName(tbcol.Column.Attribute.Name));
                                if (tbcol.IsDesc) sb.Append(" DESC");
                                sb.Append(", ");
                            }
                            sb.Remove(sb.Length - 2, 2).Append(");\r\n");
                        }
                        //备注
                        foreach (var tbcol in tb.ColumnsByPosition)
                        {
                            if (string.IsNullOrEmpty(tbcol.Comment) == false)
                                sb.Append("COMMENT ON COLUMN ").Append(_commonUtils.QuoteSqlName($"{tbname[0]}.{tbname[1]}.{tbcol.Attribute.Name}")).Append(" IS ").Append(_commonUtils.FormatSql("{0}", tbcol.Comment)).Append(";\r\n");
                        }
                        if (string.IsNullOrEmpty(tb.Comment) == false)
                            sb.Append("COMMENT ON TABLE ").Append(_commonUtils.QuoteSqlName($"{tbname[0]}.{tbname[1]}")).Append(" IS ").Append(_commonUtils.FormatSql("{0}", tb.Comment)).Append(";\r\n");
                        continue;
                    }
                    //如果新表，旧表在一个数据库和模式下，直接修改表名
                    if (string.Compare(tbname[0], tboldname[0], true) == 0)
                        sbalter.Append("ALTER TABLE ").Append(_commonUtils.QuoteSqlName($"{tboldname[0]}.{tboldname[1]}")).Append(" RENAME TO ").Append(_commonUtils.QuoteSqlName($"{tbname[1]}")).Append(";\r\n");
                    else
                    {
                        //如果新表，旧表不在一起，创建新表，导入数据，删除旧表
                        istmpatler = true;
                    }
                }
                else
                    tboldname = null; //如果新表已经存在，不走改表名逻辑

                //对比字段，只可以修改类型、增加字段、有限的修改字段名；保证安全不删除字段
                var sql = _commonUtils.FormatSql($@"
select
a.attname,
t.typname,
case when a.atttypmod > 0 and a.atttypmod < 32767 then a.atttypmod - 4 else a.attlen end len,
case when t.typelem > 0 and t.typinput::varchar = 'array_in' then t2.typname else t.typname end,
case when a.attnotnull then '0' else '1' end as is_nullable,
--e.adsrc,
(select pg_get_expr(adbin, adrelid) from pg_attrdef where adrelid = e.adrelid limit 1) is_identity,
a.attndims,
d.description as comment{(isPg10 ? ", a.attidentity" : "")}
from pg_class c
inner join pg_attribute a on a.attnum > 0 and a.attrelid = c.oid
inner join pg_type t on t.oid = a.atttypid
left join pg_type t2 on t2.oid = t.typelem
left join pg_description d on d.objoid = a.attrelid and d.objsubid = a.attnum
left join pg_attrdef e on e.adrelid = a.attrelid and e.adnum = a.attnum
inner join pg_namespace ns on ns.oid = c.relnamespace
inner join pg_namespace ns2 on ns2.oid = t.typnamespace
where ns.nspname = {{0}} and c.relname = {{1}}", tboldname ?? tbname);
                var ds = _orm.Ado.ExecuteArray(CommandType.Text, sql);
                var tbstruct = ds.ToDictionary(a => string.Concat(a[0]), a =>
                {
                    var attndims = int.Parse(string.Concat(a[6]));
                    var type = string.Concat(a[1]);
                    var sqlType = string.Concat(a[3]);
                    var max_length = long.Parse(string.Concat(a[2]));
                    type = type.Replace("smallint", "int2")
                        .Replace("integer", "int4")
                        .Replace("bigint", "int8")
                        .Replace("real", "float4")
                        .Replace("double precision", "float8")
                        .Replace("character varying", "varchar"); //pg15+
                    sqlType = type.Replace("smallint", "int2")
                        .Replace("integer", "int4")
                        .Replace("bigint", "int8")
                        .Replace("real", "float4")
                        .Replace("double precision", "float8")
                        .Replace("character varying", "varchar"); //pg15+
                    switch (sqlType.ToLower())
                    {
                        case "bool": case "name": case "bit": case "varbit": case "bpchar": case "varchar": case "bytea": case "text": case "uuid":
                        default: max_length *= 8; break;
                    }
                    if (type.StartsWith("_"))
                    {
                        type = type.Substring(1);
                        if (attndims == 0) attndims++;
                    }
                    if (sqlType.StartsWith("_")) sqlType = sqlType.Substring(1);
                    var is_identity_pg9 = string.Concat(a[5]).StartsWith(@"nextval('") && (string.Concat(a[5]).EndsWith(@"'::regclass)") || string.Concat(a[5]).EndsWith(@"')"));
                    return new
                    {
                        column = string.Concat(a[0]),
                        sqlType = string.Concat(sqlType),
                        max_length = long.Parse(string.Concat(a[2])),
                        is_nullable = string.Concat(a[4]) == "1",
                        is_identity_pg9 = is_identity_pg9,
                        is_identity = is_identity_pg9
                            || isPg10 && new[] { "a", "d" }.Contains(string.Concat(a[8])), //pg10 GENERATED { BY DEFAULT | AWAYS } AS IDENTITY
                        attndims,
                        comment = string.Concat(a[7])
                    };
                }, StringComparer.CurrentCultureIgnoreCase);

                if (istmpatler == false)
                {
                    foreach (var tbcol in tb.ColumnsByPosition)
                    {
                        if (tbstruct.TryGetValue(tbcol.Attribute.Name, out var tbstructcol) ||
                            string.IsNullOrEmpty(tbcol.Attribute.OldName) == false && tbstruct.TryGetValue(tbcol.Attribute.OldName, out tbstructcol))
                        {
                            var isCommentChanged = tbstructcol.comment != (tbcol.Comment ?? "");
                            var sqlTypeSize = tbstructcol.sqlType;
                            if (sqlTypeSize.Contains("(") == false)
                            {
                                switch (sqlTypeSize.ToLower())
                                {
                                    case "bit": case "varbit": case "bpchar": case "varchar":
                                        sqlTypeSize = $"{sqlTypeSize}({tbstructcol.max_length})"; break;
                                }
                            }
                            if (tbcol.Attribute.DbType.StartsWith(sqlTypeSize, StringComparison.CurrentCultureIgnoreCase) == false ||
                                tbcol.Attribute.DbType.Contains("[]") != (tbstructcol.attndims > 0))
                                sbalter.Append("ALTER TABLE ").Append(_commonUtils.QuoteSqlName($"{tbname[0]}.{tbname[1]}")).Append(" ALTER COLUMN ").Append(_commonUtils.QuoteSqlName(tbstructcol.column)).Append(" TYPE ").Append(tbcol.Attribute.DbType.Split(' ').First()).Append(";\r\n");
                            if (tbcol.Attribute.IsNullable != tbstructcol.is_nullable)
                            {
                                if (tbcol.Attribute.IsNullable != true || tbcol.Attribute.IsNullable == true && tbcol.Attribute.IsPrimary == false)
                                {
                                    if (tbcol.Attribute.IsNullable == false)
                                        sbalter.Append("UPDATE ").Append(_commonUtils.QuoteSqlName($"{tbname[0]}.{tbname[1]}")).Append(" SET ").Append(_commonUtils.QuoteSqlName(tbstructcol.column)).Append(" = ").Append(tbcol.DbDefaultValue).Append(" WHERE ").Append(_commonUtils.QuoteSqlName(tbstructcol.column)).Append(" IS NULL;\r\n");
                                    sbalter.Append("ALTER TABLE ").Append(_commonUtils.QuoteSqlName($"{tbname[0]}.{tbname[1]}")).Append(" ALTER COLUMN ").Append(_commonUtils.QuoteSqlName(tbstructcol.column)).Append(" ").Append(tbcol.Attribute.IsNullable == true ? "DROP" : "SET").Append(" NOT NULL;\r\n");
                                }
                            }
                            if (tbcol.Attribute.IsIdentity != tbstructcol.is_identity)
                            {
                                if (isPg10)
                                {
                                    if (tbstructcol.is_identity)
                                    {
                                        if (tbstructcol.is_identity_pg9)
                                            seqcols.Add(NativeTuple.Create(tbcol, tbname, false));
                                        sbalter.Append("ALTER TABLE ").Append(_commonUtils.QuoteSqlName($"{tbname[0]}.{tbname[1]}")).Append(" ALTER COLUMN ").Append(_commonUtils.QuoteSqlName(tbcol.Attribute.Name)).Append(" DROP IDENTITY IF EXISTS;\r\n");
                                    }
                                    else
                                    {
                                        sbalter.Append("ALTER TABLE ").Append(_commonUtils.QuoteSqlName($"{tbname[0]}.{tbname[1]}")).Append(" ALTER COLUMN ").Append(_commonUtils.QuoteSqlName(tbcol.Attribute.Name)).Append(" ADD GENERATED BY DEFAULT AS IDENTITY");
                                        var maxval = _orm.Ado.QuerySingle<int>($"select max({_commonUtils.QuoteSqlName(tbcol.Attribute.Name)}) from {_commonUtils.QuoteSqlName($"{tbname[0]}.{tbname[1]}")}");
                                        if (maxval > 0)
                                            sbalter.Append(" (START ").Append(maxval + 1).Append(")");
                                        sbalter.Append(";\r\n");
                                    }
                                }
                                else
                                    seqcols.Add(NativeTuple.Create(tbcol, tbname, tbcol.Attribute.IsIdentity == true));
                            }
                            if (string.Compare(tbstructcol.column, tbcol.Attribute.OldName, true) == 0)
                                //修改列名
                                sbalter.Append("ALTER TABLE ").Append(_commonUtils.QuoteSqlName($"{tbname[0]}.{tbname[1]}")).Append(" RENAME COLUMN ").Append(_commonUtils.QuoteSqlName(tbstructcol.column)).Append(" TO ").Append(_commonUtils.QuoteSqlName(tbcol.Attribute.Name)).Append(";\r\n");
                            if (isCommentChanged)
                                sbalter.Append("COMMENT ON COLUMN ").Append(_commonUtils.QuoteSqlName($"{tbname[0]}.{tbname[1]}.{tbcol.Attribute.Name}")).Append(" IS ").Append(_commonUtils.FormatSql("{0}", tbcol.Comment)).Append(";\r\n");
                            continue;
                        }
                        //添加列
                        sbalter.Append("ALTER TABLE ").Append(_commonUtils.QuoteSqlName($"{tbname[0]}.{tbname[1]}")).Append(" ADD COLUMN ").Append(_commonUtils.QuoteSqlName(tbcol.Attribute.Name)).Append(" ").Append(tbcol.Attribute.DbType.Split(' ').First()).Append(";\r\n");
                        sbalter.Append("UPDATE ").Append(_commonUtils.QuoteSqlName($"{tbname[0]}.{tbname[1]}")).Append(" SET ").Append(_commonUtils.QuoteSqlName(tbcol.Attribute.Name)).Append(" = ").Append(tbcol.DbDefaultValue).Append(";\r\n");
                        if (tbcol.Attribute.IsNullable == false) sbalter.Append("ALTER TABLE ").Append(_commonUtils.QuoteSqlName($"{tbname[0]}.{tbname[1]}")).Append(" ALTER COLUMN ").Append(_commonUtils.QuoteSqlName(tbcol.Attribute.Name)).Append(" SET NOT NULL;\r\n");
                        if (tbcol.Attribute.IsIdentity == true)
                        {
                            if (isPg10) 
                                sbalter.Append("ALTER TABLE ").Append(_commonUtils.QuoteSqlName($"{tbname[0]}.{tbname[1]}")).Append(" ALTER COLUMN ").Append(_commonUtils.QuoteSqlName(tbcol.Attribute.Name)).Append(" ADD GENERATED BY DEFAULT AS IDENTITY;\r\n");
                            else 
                                seqcols.Add(NativeTuple.Create(tbcol, tbname, tbcol.Attribute.IsIdentity == true));
                        }
                        if (string.IsNullOrEmpty(tbcol.Comment) == false) sbalter.Append("COMMENT ON COLUMN ").Append(_commonUtils.QuoteSqlName($"{tbname[0]}.{tbname[1]}.{tbcol.Attribute.Name}")).Append(" IS ").Append(_commonUtils.FormatSql("{0}", tbcol.Comment)).Append(";\r\n");
                    }
                    var dsuksql = _commonUtils.FormatSql($@"
select
c.attname,
b.relname,
{(isPg96 ? "case when pg_index_column_has_property(b.oid, c.attnum, 'desc') = 't' then 1 else 0 end" : "0")} IsDesc,
case when indisunique = 't' then 1 else 0 end IsUnique
from pg_index a
inner join pg_class b on b.oid = a.indexrelid
inner join pg_attribute c on c.attnum > 0 and c.attrelid = b.oid
inner join pg_namespace ns on ns.oid = b.relnamespace
inner join pg_class d on d.oid = a.indrelid
where ns.nspname in ({{0}}) and d.relname in ({{1}}) and a.indisprimary = 'f'", tboldname ?? tbname);
                    var dsuk = _orm.Ado.ExecuteArray(CommandType.Text, dsuksql).Select(a => new[] { string.Concat(a[0]), string.Concat(a[1]), string.Concat(a[2]), string.Concat(a[3]) });
                    foreach (var uk in tb.Indexes)
                    {
                        if (string.IsNullOrEmpty(uk.Name) || uk.Columns.Any() == false) continue;
                        var ukname = ReplaceIndexName(uk.Name, tbname[1]);
                        var dsukfind1 = dsuk.Where(a => string.Compare(a[1], ukname, true) == 0).ToArray();
                        if (dsukfind1.Any() == false || dsukfind1.Length != uk.Columns.Length || dsukfind1.Where(a => uk.Columns.Where(b => (a[3] == "1") == uk.IsUnique && string.Compare(b.Column.Attribute.Name, a[0], true) == 0 && ((a[2] == "1") == b.IsDesc || isPg96 == false)).Any()).Count() != uk.Columns.Length)
                        {
                            if (dsukfind1.Any()) sbalter.Append("DROP INDEX ").Append(_commonUtils.QuoteSqlName(ukname)).Append(";\r\n");
                            sbalter.Append("CREATE ");
                            if (uk.IsUnique) sbalter.Append("UNIQUE ");
                            sbalter.Append("INDEX ");
                            if (isPg95) sbalter.Append("IF NOT EXISTS ");
                            sbalter.Append(_commonUtils.QuoteSqlName(ukname)).Append(" ON ").Append(_commonUtils.QuoteSqlName($"{tbname[0]}.{tbname[1]}"));
                            if (uk.IndexMethod != IndexMethod.B_Tree) sbalter.Append(" USING ").Append(uk.IndexMethod.ToString().ToUpper().Replace("_", ""));
                            sbalter.Append("(");
                            foreach (var tbcol in uk.Columns)
                            {
                                sbalter.Append(_commonUtils.QuoteSqlName(tbcol.Column.Attribute.Name));
                                if (tbcol.IsDesc) sbalter.Append(" DESC");
                                sbalter.Append(", ");
                            }
                            sbalter.Remove(sbalter.Length - 2, 2).Append(");\r\n");
                        }
                    }
                }
                if (istmpatler == false)
                {
                    var dbcomment = string.Concat(_orm.Ado.ExecuteScalar(CommandType.Text, _commonUtils.FormatSql(@" select
d.description
from pg_class a
inner join pg_namespace b on b.oid = a.relnamespace
left join pg_description d on d.objoid = a.oid and objsubid = 0
where b.nspname not in ('pg_catalog', 'information_schema') and a.relkind in ('r') and b.nspname = {0} and a.relname = {1}
and b.nspname || '.' || a.relname not in ('public.geography_columns','public.geometry_columns','public.raster_columns','public.raster_overviews')", tbname[0], tbname[1])));
                    if (dbcomment != (tb.Comment ?? ""))
                        sbalter.Append("COMMENT ON TABLE ").Append(_commonUtils.QuoteSqlName($"{tbname[0]}.{tbname[1]}")).Append(" IS ").Append(_commonUtils.FormatSql("{0}", tb.Comment)).Append(";\r\n");

                    sb.Append(sbalter);
                    continue;
                }
                var oldpk = _orm.Ado.ExecuteScalar(CommandType.Text, _commonUtils.FormatSql(@" select pg_constraint.conname as pk_name from pg_constraint
inner join pg_class on pg_constraint.conrelid = pg_class.oid
inner join pg_namespace on pg_namespace.oid = pg_class.relnamespace
where pg_namespace.nspname={0} and pg_class.relname={1} and pg_constraint.contype='p'
", tbname))?.ToString();
                if (string.IsNullOrEmpty(oldpk) == false)
                    sb.Append("ALTER TABLE ").Append(_commonUtils.QuoteSqlName($"{tbname[0]}.{tbname[1]}")).Append(" DROP CONSTRAINT ").Append(oldpk).Append(";\r\n");

                //创建临时表，数据导进临时表，然后删除原表，将临时表改名为原表名
                var tablename = tboldname == null ? _commonUtils.QuoteSqlName($"{tbname[0]}.{tbname[1]}") : _commonUtils.QuoteSqlName($"{tboldname[0]}.{tboldname[1]}");
                var tmptablename = _commonUtils.QuoteSqlName($"{tbname[0]}.FreeSqlTmp_{tbname[1]}");
                //创建临时表
                sb.Append("CREATE TABLE IF NOT EXISTS ").Append(tmptablename).Append(" ( ");
                foreach (var tbcol in tb.ColumnsByPosition)
                {
                    sb.Append(" \r\n  ").Append(_commonUtils.QuoteSqlName(tbcol.Attribute.Name)).Append(" ").Append(tbcol.Attribute.DbType);
                    if (tbcol.Attribute.IsIdentity == true)
                    {
                        if (isPg10) sb.Append(" GENERATED BY DEFAULT AS IDENTITY");
                        else seqcols.Add(NativeTuple.Create(tbcol, tbname, true));
                    }
                    sb.Append(",");
                }
                if (tb.Primarys.Any())
                {
                    var pkname = $"{tbname[0]}_{tbname[1]}_pkey";
                    sb.Append(" \r\n  CONSTRAINT ").Append(_commonUtils.QuoteSqlName(pkname)).Append(" PRIMARY KEY (");
                    foreach (var tbcol in tb.Primarys) sb.Append(_commonUtils.QuoteSqlName(tbcol.Attribute.Name)).Append(", ");
                    sb.Remove(sb.Length - 2, 2).Append("),");
                }
                sb.Remove(sb.Length - 1, 1);
                sb.Append("\r\n) WITH (OIDS=FALSE);\r\n");
                //备注
                foreach (var tbcol in tb.ColumnsByPosition)
                {
                    if (string.IsNullOrEmpty(tbcol.Comment) == false)
                        sb.Append("COMMENT ON COLUMN ").Append(_commonUtils.QuoteSqlName($"{tbname[0]}.FreeSqlTmp_{tbname[1]}.{tbcol.Attribute.Name}")).Append(" IS ").Append(_commonUtils.FormatSql("{0}", tbcol.Comment)).Append(";\r\n");
                }
                if (string.IsNullOrEmpty(tb.Comment) == false)
                    sb.Append("COMMENT ON TABLE ").Append(_commonUtils.QuoteSqlName($"{tbname[0]}.FreeSqlTmp_{tbname[1]}")).Append(" IS ").Append(_commonUtils.FormatSql("{0}", tb.Comment)).Append(";\r\n");

                sb.Append("INSERT INTO ").Append(tmptablename).Append(" (");
                foreach (var tbcol in tb.ColumnsByPosition)
                    sb.Append(_commonUtils.QuoteSqlName(tbcol.Attribute.Name)).Append(", ");
                sb.Remove(sb.Length - 2, 2).Append(")\r\nSELECT ");
                foreach (var tbcol in tb.ColumnsByPosition)
                {
                    var insertvalue = "NULL";
                    if (tbstruct.TryGetValue(tbcol.Attribute.Name, out var tbstructcol) ||
                        string.IsNullOrEmpty(tbcol.Attribute.OldName) == false && tbstruct.TryGetValue(tbcol.Attribute.OldName, out tbstructcol))
                    {
                        insertvalue = _commonUtils.QuoteSqlName(tbstructcol.column);
                        if (tbcol.Attribute.DbType.StartsWith(tbstructcol.sqlType, StringComparison.CurrentCultureIgnoreCase) == false)
                            insertvalue = $"cast({insertvalue} as {tbcol.Attribute.DbType.Split(' ').First()})";
                        if (tbcol.Attribute.IsNullable != tbstructcol.is_nullable)
                            insertvalue = $"coalesce({insertvalue},{tbcol.DbDefaultValue})";
                    }
                    else if (tbcol.Attribute.IsNullable == false)
                        insertvalue = tbcol.DbDefaultValue;
                    sb.Append(insertvalue).Append(", ");
                }
                sb.Remove(sb.Length - 2, 2).Append(" FROM ").Append(tablename).Append(";\r\n");
                sb.Append("DROP TABLE ").Append(tablename).Append(";\r\n");
                sb.Append("ALTER TABLE ").Append(tmptablename).Append(" RENAME TO ").Append(_commonUtils.QuoteSqlName(tbname[1])).Append(";\r\n");
                //创建表的索引
                foreach (var uk in tb.Indexes)
                {
                    sb.Append("CREATE ");
                    if (uk.IsUnique) sb.Append("UNIQUE ");
                    sb.Append("INDEX ");
                    if (isPg95) sb.Append("IF NOT EXISTS ");
                    sb.Append(_commonUtils.QuoteSqlName(ReplaceIndexName(uk.Name, tbname[1]))).Append(" ON ").Append(tablename);
                    if (uk.IndexMethod != IndexMethod.B_Tree) sb.Append(" USING ").Append(uk.IndexMethod.ToString().ToUpper().Replace("_", ""));
                    sb.Append("(");
                    foreach (var tbcol in uk.Columns)
                    {
                        sb.Append(_commonUtils.QuoteSqlName(tbcol.Column.Attribute.Name));
                        if (tbcol.IsDesc) sb.Append(" DESC");
                        sb.Append(", ");
                    }
                    sb.Remove(sb.Length - 2, 2).Append(");\r\n");
                }
                if (isPg10)
                {
                    foreach (var tbcol in tb.ColumnsByPosition)
                    {
                        if (tbcol.Attribute.IsIdentity)
                        {
                            var maxval = _orm.Ado.QuerySingle<int>($"select max({_commonUtils.QuoteSqlName(tbcol.Attribute.Name)}) from {tablename}");
                            if (maxval > 0)
                            {
                                sb.Append("ALTER TABLE ").Append(_commonUtils.QuoteSqlName($"{tbname[0]}.{tbname[1]}")).Append(" ALTER COLUMN ").Append(_commonUtils.QuoteSqlName(tbcol.Attribute.Name)).Append(" SET GENERATED BY DEFAULT");
                                sb.Append(" RESTART ").Append(maxval + 1).Append("");
                                sb.Append(";\r\n");
                            }
                        }
                    }
                }
            }
            foreach (var seqcol in seqcols)
            {
                var tbname = seqcol.Item2;
                var seqname = Utils.GetCsName($"{tbname[0]}.{tbname[1]}_{seqcol.Item1.Attribute.Name}_sequence_name").ToLower();
                var tbname2 = _commonUtils.QuoteSqlName($"{tbname[0]}.{tbname[1]}");
                var colname2 = _commonUtils.QuoteSqlName(seqcol.Item1.Attribute.Name);
                sb.Append("ALTER TABLE ").Append(tbname2).Append(" ALTER COLUMN ").Append(colname2).Append(" SET DEFAULT null;\r\n");
                sb.Append("DROP SEQUENCE IF EXISTS ").Append(seqname).Append(";\r\n");
                if (seqcol.Item3)
                {
                    sb.Append("CREATE SEQUENCE ").Append(seqname).Append(";\r\n");
                    sb.Append("ALTER TABLE ").Append(tbname2).Append(" ALTER COLUMN ").Append(colname2).Append(" SET DEFAULT nextval('").Append(seqname).Append("'::regclass);\r\n");
                    sb.Append(" SELECT case when max(").Append(colname2).Append(") is null then 0 else setval('").Append(seqname).Append("', max(").Append(colname2).Append(")) end FROM ").Append(tbname2).Append(";\r\n");
                }
            }
            return sb.Length == 0 ? null : sb.ToString();
        }
    }
}