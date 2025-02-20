﻿using FreeSql.Internal.ObjectPool;
using System;
using System.Data;
using System.Data.Common;
#if MicrosoftData
using Microsoft.Data.Sqlite;
#else
using System.Data.SQLite;
#endif
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FreeSql.Sqlite
{

    class SqliteConnectionPool : ObjectPool<DbConnection>
    {

        internal Action availableHandler;
        internal Action unavailableHandler;

        public SqliteConnectionPool(string name, string connectionString, Action availableHandler, Action unavailableHandler) : base(null)
        {
            this.availableHandler = availableHandler;
            this.unavailableHandler = unavailableHandler;
            policy = new SqliteConnectionPoolPolicy
            {
                _pool = this,
                Name = name
            };
            this.Policy = policy;
            policy.ConnectionString = connectionString;
        }

        public void Return(Object<DbConnection> obj, Exception exception, bool isRecreate = false)
        {
            base.Return(obj, isRecreate);
        }

        internal SqliteConnectionPoolPolicy policy;

        public static DbConnection CreateConnection(string connectionString)
        {
#if MicrosoftData
            var conn = new SqliteConnection(connectionString);
#else
            var conn = new SQLiteConnection(connectionString);
#endif
            return conn;
        }
    }

    class SqliteConnectionPoolPolicy : IPolicy<DbConnection>
    {

        internal SqliteConnectionPool _pool;
        public string Name { get; set; } = $"Sqlite SQLiteConnection {CoreErrorStrings.S_ObjectPool}";
        public int PoolSize { get; set; } = 100;
        public TimeSpan SyncGetTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan IdleTimeout { get; set; } = TimeSpan.Zero;
        public int AsyncGetCapacity { get; set; } = 10000;
        public bool IsThrowGetTimeoutException { get; set; } = true;
        public bool IsAutoDisposeWithSystem { get; set; } = true;
        public int CheckAvailableInterval { get; set; } = 2;
        public int Weight { get; set; } = 1;
        public string[] Attaches = new string[0];

        private string _connectionString;
        public string ConnectionString
        {
            get => _connectionString;
            set
            {
                _connectionString = value ?? "";

                var minPoolSize = 1;
                var pattern = @"Min\s*pool\s*size\s*=\s*(\d+)";
                var m = Regex.Match(_connectionString, pattern, RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    minPoolSize = int.Parse(m.Groups[1].Value);
                    _connectionString = Regex.Replace(_connectionString, pattern, "", RegexOptions.IgnoreCase);
                }

                pattern = @"Max\s*pool\s*size\s*=\s*(\d+)";
                m = Regex.Match(_connectionString, pattern, RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    PoolSize = int.Parse(m.Groups[1].Value);
                    _connectionString = Regex.Replace(_connectionString, pattern, "", RegexOptions.IgnoreCase);
                }

                pattern = @"Connection\s*LifeTime\s*=\s*(\d+)";
                m = Regex.Match(_connectionString, pattern, RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    IdleTimeout = TimeSpan.FromSeconds(int.Parse(m.Groups[1].Value));
                    _connectionString = Regex.Replace(_connectionString, pattern, "", RegexOptions.IgnoreCase);
                }

                var att = Regex.Split(_connectionString, @"Pooling\s*=\s*", RegexOptions.IgnoreCase);
                if (att.Length == 2)
                {
                    var idx = att[1].IndexOf(';');
                    _connectionString = string.Concat(att[0], idx == -1 ? "" : att[1].Substring(idx));
                }

                att = Regex.Split(_connectionString, @"Attachs\s*=\s*", RegexOptions.IgnoreCase);
                if (att.Length == 2)
                {
                    var idx = att[1].IndexOf(';');
                    Attaches = (idx == -1 ? att[1] : att[1].Substring(0, idx)).Split(',');
                    _connectionString = string.Concat(att[0], idx == -1 ? "" : att[1].Substring(idx));
                }

                if (_connectionString.ToLower().Contains(":memory:"))
                {
                    //内存模式
                    PoolSize = 1;
                }

#if ns20
                minPoolSize = 1;
#endif
                FreeSql.Internal.CommonUtils.PrevReheatConnectionPool(_pool, minPoolSize);

            }
        }

        public bool OnCheckAvailable(Object<DbConnection> obj)
        {
            if (obj.Value == null) return false;
            if (obj.Value.State == ConnectionState.Closed) obj.Value.OpenAndAttach(Attaches);
            return obj.Value.Ping(true);
        }

        public DbConnection OnCreate() => SqliteConnectionPool.CreateConnection(_connectionString);

        public void OnDestroy(DbConnection obj)
        {
#if MicrosoftData
			if (obj is SqliteConnection sqlconn && sqlconn != null) SqliteConnection.ClearPool(sqlconn);
#else
            if (obj is SQLiteConnection sqlconn && sqlconn != null) SQLiteConnection.ClearPool(sqlconn);
#endif
			if (obj.State != ConnectionState.Closed) obj.Close();
            obj.Dispose();
        }

        public void OnGet(Object<DbConnection> obj)
        {

            if (_pool.IsAvailable)
            {
                if (obj.Value == null)
                    throw new Exception(CoreErrorStrings.S_ConnectionStringError_CheckProjectConnection(this.Name));

                if (obj.Value.State != ConnectionState.Open)
                    obj.Value.OpenAndAttach(Attaches);
            }
        }

#if net40
#else
        async public Task OnGetAsync(Object<DbConnection> obj)
        {

            if (_pool.IsAvailable)
            {
                if (obj.Value == null)
                    throw new Exception(CoreErrorStrings.S_ConnectionStringError_Check(this.Name));

                if (obj.Value.State != ConnectionState.Open)
                    await obj.Value.OpenAndAttachAsync(Attaches);
            }
        }
#endif

        public void OnGetTimeout()
        {

        }

        public void OnReturn(Object<DbConnection> obj)
        {
            //if (obj?.Value != null && obj.Value.State != ConnectionState.Closed) try { obj.Value.Close(); } catch { }
        }

        public void OnAvailable()
        {
            _pool.availableHandler?.Invoke();
        }

        public void OnUnavailable()
        {
            _pool.unavailableHandler?.Invoke();
        }
    }
    static class DbConnectionExtensions
    {

        static DbCommand PingCommand(DbConnection conn)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandTimeout = 5;
            cmd.CommandText = "select 1";
            return cmd;
        }
        public static bool Ping(this DbConnection that, bool isThrow = false)
        {
            try
            {
                using (var cmd = PingCommand(that))
                {
                    cmd.ExecuteNonQuery();
                }
                return true;
            }
            catch
            {
                if (that.State != ConnectionState.Closed) try { that.Close(); } catch { }
                if (isThrow) throw;
                return false;
            }
        }
        public static void OpenAndAttach(this DbConnection that, string[] attach)
        {
            that.Open();

            if (attach?.Any() == true)
            {
                var sb = new StringBuilder();
                foreach (var att in attach)
                    sb.Append($"attach database [{att}] as [{att.Split('/', '\\').Last().Split('.').First()}];\r\n");

                var cmd = that.CreateCommand();
                cmd.CommandText = sb.ToString();
                cmd.ExecuteNonQuery();
                cmd.Dispose();
            }
        }

#if net40
#else
        async public static Task<bool> PingAsync(this DbConnection that, bool isThrow = false)
        {
            try
            {
                using (var cmd = PingCommand(that))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                return true;
            }
            catch
            {
                if (that.State != ConnectionState.Closed) try { that.Close(); } catch { }
                if (isThrow) throw;
                return false;
            }
        }
        async public static Task OpenAndAttachAsync(this DbConnection that, string[] attach)
        {
            await that.OpenAsync();

            if (attach?.Any() == true)
            {
                var sb = new StringBuilder();
                foreach (var att in attach)
                    sb.Append($"attach database [{att}] as [{att.Split('/', '\\').Last().Split('.').First()}];\r\n");

                var cmd = that.CreateCommand();
                cmd.CommandText = sb.ToString();
                await cmd.ExecuteNonQueryAsync();
                cmd.Dispose();
            }
        }
#endif
    }
}
