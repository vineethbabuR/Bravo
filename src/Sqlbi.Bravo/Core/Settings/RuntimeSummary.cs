﻿using Microsoft.AnalysisServices;
using Sqlbi.Bravo.Client.PowerBI.Desktop;
using Sqlbi.Bravo.Client.PowerBI.PowerBICloud.Models;
using Sqlbi.Bravo.Core.Services.Interfaces;
using System;
using System.Data.Common;

namespace Sqlbi.Bravo.Core.Settings
{

    //internal enum AsInstanceType
    //{
    //    Other,
    //    AsAzure,
    //    PbiPremiumInternal,
    //    PbiPremiumXmlaEp,
    //    PbiDataset
    //}

    /// <summary>
    /// This is a selection of the information from RuntimeSettings that is held by each tab.
    /// If different tabs coudn't connect to different data sources then could access `settings.Runtime.Xxxx` directly.
    /// </summary>
    internal class RuntimeSummary
    {
        public static RuntimeSummary CreateFrom(RuntimeSettings settings)
        {
            var runtimeSummary = new RuntimeSummary
            {
                ConnectionName = settings.ParentProcessMainWindowTitle,
                ServerName = settings.ServerName,
                DatabaseName = settings.DatabaseName,
                ConnectionString = BuildConnectionString(settings.ServerName, settings.DatabaseName),
            };

            return runtimeSummary;
        }

        public static RuntimeSummary CreateFrom(PowerBIDesktopInstance instance)
        {
            var databaseName = GetDatabaseName();

            var runtimeSummary = new RuntimeSummary
            {
                ConnectionName = instance.Name,
                ServerName = instance.ServerName,
                DatabaseName = databaseName,
                ConnectionString = BuildConnectionString(instance.ServerName, databaseName),
            };

            return runtimeSummary;

            string GetDatabaseName()
            {
                var connectionString = BuildConnectionString(instance.ServerName, databaseName: null);

                using var server = new Server();
                server.Connect(connectionString);

                if (server.Databases.Count != 1)
                    throw new InvalidOperationException($"Invalid database count [{ server.Databases.Count }]");

                var database = server.Databases[index: 0];
                var name = database.Name;

                return name;
            }
        }

        public static RuntimeSummary CreateFrom(MetadataSharedDataset dataset, IPowerBICloudService service)
        {
            var backendUri = new Uri(service.CloudEnvironment.BackendEndpointUri);
            var serverName = $"pbiazure://{ backendUri.Host }"; 

            var runtimeSummary = new RuntimeSummary
            {
                ConnectionName = dataset.Model.DisplayName,
                ServerName = serverName,
                DatabaseName = dataset.Model.DisplayName,
                ConnectionString = BuildLiveConnectionConnectionString(service, dataset),
            };

            return runtimeSummary;
        }

        private static string BuildConnectionString(string serverName, string databaseName)
        {
            const string ProviderKey = "Provider";
            const string DataSourceKey = "Data Source";
            const string InitialCatalogKey = "Initial Catalog";
            const string IntegratedSecurityKey = "Integrated Security";
            const string PersistSecurityInfoKey = "Persist Security Info";
            const string ApplicationNameKey = "Application Name";

            var builder = new DbConnectionStringBuilder(useOdbcRules: false)
            {
                { ProviderKey, "MSOLAP" },
                { DataSourceKey, serverName },
                { InitialCatalogKey, databaseName },
                { IntegratedSecurityKey, "SSPI" },
                { PersistSecurityInfoKey, "True" },
                { ApplicationNameKey, AppConstants.ApplicationInstanceUniqueName }
            };

            return builder.ConnectionString;
        }

        //public static string BuildLiveConnectionConnectionString(string serverName, IPowerBICloudService service, MetadataSharedDataset dataset)
        //{
        //    const string ProviderKey = "Provider";
        //    const string DataSourceKey = "Data Source";
        //    const string InitialCatalogKey = "Initial Catalog";
        //    const string IdentityProvider = "Identity Provider";
        //    const string PersistSecurityInfoKey = "Persist Security Info";
        //    const string IntegratedSecurityKey = "Integrated Security";
        //    const string ApplicationNameKey = "Application Name";
        //    const string PasswordKey = "Password";

        //    var identityProvider = $"{ service.CloudEnvironment.AuthorityUri }, { service.CloudEnvironment.ResourceUri }, { service.CloudEnvironment.ClientId }";
        //    var databaseName = $"{ dataset.Model.VSName }-{ dataset.Model.DBName }";

        //    var builder = new DbConnectionStringBuilder(useOdbcRules: false)
        //    {
        //        { ProviderKey, "MSOLAP.8" },
        //        { PersistSecurityInfoKey, "True" },
        //        { IntegratedSecurityKey, "ClaimsToken" },
        //        { DataSourceKey, serverName },
        //        { InitialCatalogKey, databaseName },
        //        { PasswordKey, service.AccessToken },
        //        { IdentityProvider, identityProvider },
        //        { ApplicationNameKey, AppConstants.ApplicationInstanceUniqueName }
        //    };

        //    return builder.ConnectionString;
        //}

        private static string BuildLiveConnectionConnectionString(IPowerBICloudService service, MetadataSharedDataset dataset)
        {
            const string ProviderKey = "Provider";
            const string DataSourceKey = "Data Source";
            const string InitialCatalogKey = "Initial Catalog";
            const string PersistSecurityInfoKey = "Persist Security Info";
            const string ApplicationNameKey = "Application Name";
            const string PasswordKey = "Password";

            var builder = new DbConnectionStringBuilder(useOdbcRules: false)
            {
                { ProviderKey, "MSOLAP" },
                { PersistSecurityInfoKey, "True" },
                { DataSourceKey, $"powerbi://api.powerbi.com/v1.0/myorg/{ dataset.WorkspaceName }" },
                { InitialCatalogKey, dataset.Model.DisplayName },
                { PasswordKey, service.AccessToken },
                { ApplicationNameKey, AppConstants.ApplicationInstanceUniqueName }
            };

            return builder.ConnectionString;
        }

        public string ServerName { get; init; }

        public string DatabaseName { get; init; }

        public string ConnectionName { get; init; }

        public string ConnectionString { get; init; }

        public bool UsingLocalModelForAnanlysis { get; internal set; } = false;
    }
}