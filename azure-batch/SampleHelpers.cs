//Copyright (c) Microsoft Corporation

namespace Microsoft.Azure.Batch.Samples.HelloWorld
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.Extensions.Configuration;


    /// <summary>
    /// Class containing helpers for the GettingStarted samples.
    /// </summary>
    public static class SampleHelpers
    {
        public static void AddSetting(StringBuilder stringBuilder, string settingName, object settingValue)
        {
            stringBuilder.AppendFormat("{0} = {1}", settingName, settingValue).AppendLine();
        }

        public static AccountSettings LoadAccountSettings()
        {
            AccountSettings accountSettings = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("accountsettings.json")
                .Build()
                .Get<AccountSettings>();
            return accountSettings;
        }
    }
}
