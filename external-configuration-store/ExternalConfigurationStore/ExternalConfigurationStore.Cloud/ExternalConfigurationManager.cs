// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace ExternalConfigurationStore.Cloud
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Threading;
    using System.Threading.Tasks;
    using SettingsStore;

    public class ExternalConfigurationManager : IDisposable
    {
        // An abstraction of the configuration store.
        private readonly ISettingsStore settings;
        private readonly ISubject<KeyValuePair<string, string>> changed;

        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private Task monitoringTask;
        private readonly TimeSpan interval;

        private readonly SemaphoreSlim timerSemaphore = new SemaphoreSlim(1);
        private readonly ReaderWriterLockSlim settingsCacheLock = new ReaderWriterLockSlim();
        private readonly SemaphoreSlim syncCacheSemaphore = new SemaphoreSlim(1);

        private Dictionary<string, string> settingsCache;
        private string currentVersion;

        public ExternalConfigurationManager(string environment) : this(new BlobSettingsStore(environment), TimeSpan.FromSeconds(15), environment)
        {
        }

        public ExternalConfigurationManager(ISettingsStore settings, TimeSpan interval, string environment)
        {
            this.settings = settings;
            this.interval = interval;
            this.CheckForConfigurationChangesAsync().Wait();
            this.changed = new Subject<KeyValuePair<string, string>>();
            this.Environment = environment;
        }

        public string Environment
        {
            get; private set;
        }

        public IObservable<KeyValuePair<string, string>> Changed => this.changed.AsObservable();

        /// <summary>
        /// Check to see if the current instance is monitoring for changes
        /// </summary>
        public bool IsMonitoring => this.monitoringTask != null && !this.monitoringTask.IsCompleted;

        /// <summary>
        /// Start the background monitoring for configuration changes in the central store
        /// </summary>
        public void StartMonitor()
        {
            if (this.IsMonitoring)
            {
                return;
            }

            try
            {
                this.timerSemaphore.Wait();

                //Check again to make sure we are not already running.
                if (this.IsMonitoring)
                {
                    return;
                }

                //Start runnin our task loop.
                this.monitoringTask = ConfigChangeMonitor();
            }
            finally
            {
                this.timerSemaphore.Release();
            }
        }

        /// <summary>
        /// Loop that monitors for configuration changes
        /// </summary>
        /// <returns></returns>
        public async Task ConfigChangeMonitor()
        {
            while (!cts.Token.IsCancellationRequested)
            {
                await this.CheckForConfigurationChangesAsync();

                await Task.Delay(this.interval, cts.Token);
            }
        }

        /// <summary>
        /// Stop Monitoring for Configuration Changes
        /// </summary>
        public void StopMonitor()
        {
            try
            {
                this.timerSemaphore.Wait();

                //Signal the task to stop
                this.cts.Cancel();

                //Wait for the loop to stop
                this.monitoringTask.Wait();

                this.monitoringTask = null;
            }
            finally
            {
                this.timerSemaphore.Release();
            }
        }

        public void Dispose()
        {
            this.cts.Cancel();
        }

        /// <summary>
        /// Retrieve application setting from the local cache
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetAppSetting(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key), "Value cannot be null or empty.");
            }

            // Try and get the value from the settings cache.  If there's a miss, get the setting from the settings store and refresh the settings cache.

            string value;
            try
            {
                this.settingsCacheLock.EnterReadLock();

                this.settingsCache.TryGetValue(key, out value);
            }
            finally
            {
                this.settingsCacheLock.ExitReadLock();
            }

            return value;
        }

        /// <summary>
        /// Check the central repository for configuration changes and update the local cache
        /// </summary>
        private async Task CheckForConfigurationChangesAsync()
        {
            try
            {
                // It is assumed that updates are infrequent.
                // To avoid race conditions in refreshing the cache synchronize access to the in memory cache
                await this.syncCacheSemaphore.WaitAsync();

                var latestVersion = await this.settings.GetVersionAsync();

                // If the versions are the same, nothing has changed in the configuration.
                if (this.currentVersion == latestVersion) return;

                // Get the latest settings from the settings store and publish changes.
                var latestSettings = await this.settings.FindAllAsync();

                // Refresh the settings cache.
                try
                {
                    this.settingsCacheLock.EnterWriteLock();

                    if (this.settingsCache != null)
                    {
                        //Notify settings changed
                        latestSettings.Except(this.settingsCache).ToList().ForEach(kv => this.changed.OnNext(kv));
                    }
                    this.settingsCache = latestSettings;
                }
                finally
                {
                    this.settingsCacheLock.ExitWriteLock();
                }

                // Update the current version.
                this.currentVersion = latestVersion;
            }
            catch (Exception ex)
            {
                this.changed.OnError(ex);
            }
            finally
            {
                this.syncCacheSemaphore.Release();
            }
        }
    }
}
