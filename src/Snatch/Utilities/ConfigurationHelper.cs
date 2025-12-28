using System.Reflection;
using Microsoft.Extensions.Configuration;
using Snatch.Core.Utilities.Extensions;

namespace Snatch.Utilities;

public static class ConfigurationHelper
{
    public class ConfigurationBuilderOptions
    {
        /// <summary>
        /// Used to set assembly which is used to get the user secret id for the application.
        /// Use this or <see cref="UserSecretsId"/> (higher priority)
        /// </summary>
        public Assembly? UserSecretsAssembly { get; set; }

        /// <summary>
        /// Used to set user secret id for the application.
        /// Use this (higher priority) or <see cref="UserSecretsAssembly"/>
        /// </summary>
        public string? UserSecretsId { get; set; }

        /// <summary>
        /// Default value: "appsettings".
        /// </summary>
        public string FileName { get; set; } = "appsettings";

        /// <summary>
        /// Whether the file is optional, Default value: true.
        /// </summary>
        public bool Optional { get; set; } = true;

        /// <summary>
        /// Whether the configuration should be reloaded if the file changes, Default value: true.
        /// </summary>
        public bool ReloadOnChange { get; set; } = true;

        /// <summary>
        /// Environment name. Generally used "Development", "Staging" or "Production".
        /// </summary>
        public string? EnvironmentName { get; set; }

        /// <summary>
        /// Base path to read the configuration file indicated by <see cref="FileName"/>.
        /// </summary>
        public string? BasePath { get; set; }

        /// <summary>
        /// Prefix for the environment variables.
        /// </summary>
        public string? EnvironmentVariablesPrefix { get; set; }

        /// <summary>
        /// Command line arguments.
        /// </summary>
        public string[]? CommandLineArgs { get; set; }
    }

    public static IConfigurationRoot BuildConfiguration(
        ConfigurationBuilderOptions? options = null,
        Action<IConfigurationBuilder>? builderAction = null
    )
    {
        options ??= new ConfigurationBuilderOptions();

        if (options.BasePath.IsNullOrEmpty())
        {
            options.BasePath = Directory.GetCurrentDirectory();
        }

        var builder = new ConfigurationBuilder()
            .SetBasePath(options.BasePath!)
            .AddJsonFile(
                options.FileName + ".json",
                optional: options.Optional,
                reloadOnChange: options.ReloadOnChange
            )
            .AddJsonFile(
                options.FileName + ".secrets.json",
                optional: true,
                reloadOnChange: options.ReloadOnChange
            );

        if (!options.EnvironmentName.IsNullOrEmpty())
        {
            builder = builder.AddJsonFile(
                $"{options.FileName}.{options.EnvironmentName}.json",
                optional: true,
                reloadOnChange: options.ReloadOnChange
            );
        }

        if (options.EnvironmentName == "Development")
        {
            if (options.UserSecretsId != null)
            {
                builder.AddUserSecrets(options.UserSecretsId);
            }
            else if (options.UserSecretsAssembly != null)
            {
                builder.AddUserSecrets(options.UserSecretsAssembly, true);
            }
        }

        builder = builder.AddEnvironmentVariables(options.EnvironmentVariablesPrefix);

        if (options.CommandLineArgs != null)
        {
            builder = builder.AddCommandLine(options.CommandLineArgs);
        }

        builderAction?.Invoke(builder);

        return builder.Build();
    }
}
