﻿using ManagedApplicationScheduler.DataAccess.Context;
using ManagedApplicationScheduler.DataAccess.Contracts;
using ManagedApplicationScheduler.DataAccess.Services;
using ManagedApplicationScheduler.Services.Configurations;
using ManagedApplicationScheduler.Services.Contracts;
using ManagedApplicationScheduler.Services.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ManagedApplicationScheduler.MeteredTriggerJob;

class Program
{
    static void Main()
    {

        Console.WriteLine($"MeteredExecutor Webjob Started at: {DateTime.Now}");

        IConfiguration configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var config = new ManagedAppClientConfiguration()
        {
            AdAuthenticationEndPoint = configuration["AdAuthenticationEndPoint"],
            ClientId = configuration["ClientId"] ?? Guid.Empty.ToString(),
            ClientSecret = configuration["ClientSecret"] ?? String.Empty,
            GrantType = configuration["GrantType"],
            SignedOutRedirectUri = configuration["SignedOutRedirectUri"],
            TenantId = configuration["TenantId"] ?? Guid.Empty.ToString(),
            PC_TenantId = configuration["PC_TenantId"],
            PC_ClientID = configuration["PC_ClientId"],
            PC_ClientSecret = configuration["PC_ClientSecret"],
            PC_Scope = configuration["PC_Scope"],
            Scope = configuration["Scope"],
            Marketplace_Uri = configuration["Marketplace_Uri"]

        };


        var services = new ServiceCollection()
            .AddDbContext<ApplicationsDBContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")))
            .AddScoped<IScheduledTasksRepository, ScheduledTasksRepository>()
            .AddScoped<ISubscriptionsRepository, SubscriptionsRepository>()
            .AddScoped<IUsageResultRepository, UsageResultRepository>()
            .AddScoped<IApplicationLogRepository, ApplicationLogRepository>()
            .AddScoped<IApplicationConfigurationRepository, ApplicationConfigurationRepository>()
            .AddScoped<IEmailService, SMTPEmailService>()
            .AddSingleton<ManagedAppClientConfiguration>(config)
            .AddSingleton<Executor, Executor>()
            .BuildServiceProvider();

       _= services
            .GetService<Executor>()
            .ExecuteAsync();
        Console.WriteLine($"MeteredExecutor Webjob Ended at: {DateTime.Now}");

    }
}