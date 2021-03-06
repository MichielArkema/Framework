﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Redox.API.Commands;
using Redox.API.Configuration;
using Redox.API.Plugins;
using Redox.API.Roles;

namespace Redox.Core.Plugins.CSharp
{
    /// <summary>
    /// Generic presentation for a CSharp Plugin.
    /// </summary>
    public abstract class CSPlugin : IBasePlugin
    {

        private IDictionary<string, MethodInfo> _methods;


        public PluginInfo Info { get; set; }
        public PluginContact Contact { get; set; }
        
        public PluginAnalytics Analytics { get; set; }

        public IConfigurationProvider Configurations { get; }
        
        public ICommandProvider Commands { get; }

        public IRolesProvider Roleses => RedoxMod.GetMod().RolesProvider;

        public IPluginManager PluginManager => RedoxMod.GetMod().PluginManager;

        public FileInfo FileInfo { get; internal set; }

        public bool Loaded { get; private set; }

        protected abstract Task OnEnableAsync();
        protected abstract Task OnDisableAsync();
        
        protected virtual Task LoadTranslationsAsync()
        {
            return Task.CompletedTask;
        }
        protected virtual void OnPluginsLoaded() {}
        public async Task LoadAsync()
        {
            _methods = new Dictionary<string, MethodInfo>();
            Type type = this.GetType();
            this.Info = type.GetCustomAttribute<PluginInfo>() ?? new PluginInfo();
            this.Contact = type.GetCustomAttribute<PluginContact>() ?? new PluginContact();
            
            //TODO: Insert command loader, config loader and translations loader.

            await this.LoadMethodsAsync();

            await this.OnEnableAsync();
        }

        public Task UnloadAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task LoadMethodsAsync()
        {
            Type type = this.GetType();
            foreach(MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if(method.GetCustomAttribute<Collectable>() != null){
                    _methods.Add(method.Name, method);
                }
            }
            return Task.CompletedTask;
        }

        public async Task<object> CallAsync(string name, params object[] args)
        {
            return await Task.Run(() => this.Call(name, args));
        }

        public object Call(string name, params object[] args)
        {
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                object ob = null;
                if(_methods.TryGetValue(name, out MethodInfo method))
                {
                    ob = method.Invoke(this, args);
                }
                return ob;
            }
            catch(Exception ex) 
            {
                RedoxMod.GetMod().Logger.Error("[CSharp-Error] Failed to invoke method {0} in {1} due to error: {2}", name, Info.Title, ex.Message);
                return null;
            }
            finally
            {
                sw.Stop();
                if(sw.ElapsedMilliseconds > 500)
                {
                    RedoxMod.GetMod().Logger.Info("[{0}] Invoking method {1} took more than 500 milliseconds. This is considered slow!");
                }
            }
        }

        public T Call<T>(string name, params object[] args)
        {
            return (T)this.Call(name, args);
        }

        public async Task<T> GetConfig<T>(string name)
        {
            return (T) await this.Configurations.ResolveAsync(this, name);
        }
    }
}