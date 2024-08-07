﻿using FreeSql;
using FreeSql.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text;

namespace repository_01
{

    /// <summary>
    /// 用户密码信息
    /// </summary>
    public class Sys1UserLogOn
    {
        [Column(IsPrimary = true, Name = "Id")]
        public Guid UserLogOnId { get; set; }
        public virtual Sys1User User { get; set; }
    }
    public class Sys1User
    {
        [Column(IsPrimary = true, Name = "Id")]
        public Guid UserId { get; set; }
        public virtual Sys1UserLogOn UserLogOn { get; set; }
    }

    public class Startup
    {
        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;

            Fsql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.Sqlite, @"Data Source=|DataDirectory|\document.db;Pooling=true;Max Pool Size=10")
                .UseAutoSyncStructure(true)
                .UseLazyLoading(true)

                .UseMonitorCommand(cmd => Trace.WriteLine(cmd.CommandText))
                .Build();

            var sysu = new Sys1User { };
            Fsql.Insert<Sys1User>().AppendData(sysu).ExecuteAffrows();
            Fsql.Insert<Sys1UserLogOn>().AppendData(new Sys1UserLogOn { UserLogOnId = sysu.UserId }).ExecuteAffrows();
            var a = Fsql.Select<Sys1UserLogOn>().ToList();
            var b = Fsql.Select<Sys1UserLogOn>().Any();
        }

        public IConfiguration Configuration { get; }
        public static IFreeSql Fsql { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {

            //services.AddTransient(s => s.)

            services.AddControllersWithViews();
            services.AddSingleton<IFreeSql>(Fsql);

            services.AddFreeRepository(this.GetType().Assembly);
        }

        public void Configure(IApplicationBuilder app)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.OutputEncoding = Encoding.GetEncoding("GB2312");
            Console.InputEncoding = Encoding.GetEncoding("GB2312");

            app.UseHttpMethodOverride(new HttpMethodOverrideOptions { FormFieldName = "X-Http-Method-Override" });
            app.UseDeveloperExceptionPage();
            app.UseRouting();
            app.UseEndpoints(a => a.MapControllers());
        }
    }

    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
    }

    public class TestSoftDelete : ISoftDelete
    {
        [Column(IsIdentity = true)]
        public int Id { get; set; }
        public string Title { get; set; }
        public bool IsDeleted { get; set; }
    }
}
