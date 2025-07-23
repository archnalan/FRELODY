using FRELODYAPP.Data.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace FRELODYAPP.Data.Extensions
{
    public static class DatabaseSeederExtensions
    {
        public static IServiceCollection AddDatabaseSeeder(this IServiceCollection services)
        {
            services.AddScoped<DatabaseSeeder>();
            return services;
        }
    }
}
