using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SqlServerManager.Services
{
    /// <summary>
    /// Service for managing application configuration with IConfiguration pattern
    /// </summary>
    public class ConfigurationService
    {
        private static IServiceProvider _serviceProvider;
        private static IConfiguration _configuration;

        /// <summary>
        /// Initialize configuration service
        /// </summary>
        public static void Initialize()
        {
            // Create configuration
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true);

            _configuration = configBuilder.Build();

            // Create services
            var services = new ServiceCollection();
            ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Configure services for dependency injection
        /// </summary>
        private static void ConfigureServices(IServiceCollection services)
        {
            // Add configuration
            services.AddSingleton(_configuration);

            // Add logging
            services.AddLogging();

            // Add application services
            services.AddSingleton<ConnectionService>();
            
            // Add more services as needed
        }

        /// <summary>
        /// Get service from dependency injection container
        /// </summary>
        public static T GetService<T>() where T : class
        {
            if (_serviceProvider == null)
                Initialize();

            return _serviceProvider.GetService<T>();
        }

        /// <summary>
        /// Get configuration value
        /// </summary>
        public static T GetValue<T>(string key, T defaultValue = default)
        {
            if (_configuration == null)
                Initialize();

            // Use Microsoft.Extensions.Configuration extensions
            return _configuration.GetValue<T>(key) ?? defaultValue;
        }

        /// <summary>
        /// Get configuration section
        /// </summary>
        public static IConfigurationSection GetSection(string key)
        {
            if (_configuration == null)
                Initialize();

            return _configuration.GetSection(key);
        }

        /// <summary>
        /// Get connection string from configuration
        /// </summary>
        public static string GetConnectionString(string name)
        {
            if (_configuration == null)
                Initialize();

            return _configuration.GetConnectionString(name);
        }

        /// <summary>
        /// Save a setting to the configuration
        /// </summary>
        public static void SaveSetting<T>(string key, T value)
        {
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                var json = File.ReadAllText(path);
                
                // This is a simple implementation - in production, use a library like Newtonsoft.Json
                // to properly handle nested settings
                var tempConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(json);
                
                var parts = key.Split(':');
                if (parts.Length == 1)
                {
                    // Top-level setting
                    tempConfig[key] = value;
                }
                else
                {
                    // Nested setting - would need recursive implementation for deeply nested settings
                    var section = parts[0];
                    var setting = parts[1];
                    
                    if (tempConfig.TryGetValue(section, out var sectionObj))
                    {
                        var sectionDict = sectionObj as Newtonsoft.Json.Linq.JObject;
                        if (sectionDict != null)
                        {
                            sectionDict[setting] = Newtonsoft.Json.Linq.JToken.FromObject(value);
                        }
                    }
                }
                
                var updatedJson = Newtonsoft.Json.JsonConvert.SerializeObject(tempConfig, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(path, updatedJson);
                
                // Force reload of configuration
                Initialize();
                
                LoggingService.LogInformation("Configuration setting {Key} saved successfully", key);
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Failed to save configuration setting {Key}", key);
                throw;
            }
        }
    }
}
