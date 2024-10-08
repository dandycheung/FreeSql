﻿using System;
using System.Linq.Expressions;
using FreeSql;
using FreeSql.Duckdb.Curd;

public static partial class FreeSqlDuckdbGlobalExtensions
{

    /// <summary>
    /// 特殊处理类似 string.Format 的使用方法，防止注入，以及 IS NULL 转换
    /// </summary>
    /// <param name="that"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static string FormatDuckdb(this string that, params object[] args) => _duckdbAdo.Addslashes(that, args);
    static FreeSql.Duckdb.DuckdbAdo _duckdbAdo = new FreeSql.Duckdb.DuckdbAdo();
}
