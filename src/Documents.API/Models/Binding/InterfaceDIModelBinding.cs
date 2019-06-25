namespace Documents.API.Models.Binding
{
    // This class is some super-tricky stuff that extends 
    // the dependency injection system into the JSON model-binding
    // for APIs... this means it won't work with XML content-type but...
    // ya know... http://www.dotnet-programming.com/post/2017/05/08/Aspnet-core-Deserializing-Json-with-Dependency-Injection.aspx

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json.Serialization;
    using System;
    using System.Collections.Generic;

    public interface IDIMeta
    {
        bool IsRegistred(Type t);
        Type RegistredTypeFor(Type t);
    }

    public class DIMetaDefault : IDIMeta
    {
        IDictionary<Type, Type> register = new Dictionary<Type, Type>();
        public DIMetaDefault(IServiceCollection services)
        {
            foreach (var s in services)
            {
                register[s.ServiceType] = s.ImplementationType;
            }
        }
        public bool IsRegistred(Type t)
        {
            return register.ContainsKey(t);
        }

        public Type RegistredTypeFor(Type t)
        {
            return register[t];
        }
    }

    public class DIContractResolver : CamelCasePropertyNamesContractResolver
    {
        IDIMeta diMeta;
        IServiceProvider sp;
        public DIContractResolver(IDIMeta diMeta, IServiceProvider sp)
        {
            this.diMeta = diMeta;
            this.sp = sp;
        }
        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {

            if (diMeta.IsRegistred(objectType))
            {
                JsonObjectContract contract = DIResolveContract(objectType);
                contract.DefaultCreator = () => sp.GetService(objectType);

                return contract;
            }

            return base.CreateObjectContract(objectType);
        }
        private JsonObjectContract DIResolveContract(Type objectType)
        {
            var fType = diMeta.RegistredTypeFor(objectType);
            if (fType != null) return base.CreateObjectContract(fType);
            else return CreateObjectContract(objectType);
        }
    }

    public class JsonOptionsSetup : IConfigureOptions<MvcJsonOptions>
    {
        IServiceProvider serviceProvider;
        public JsonOptionsSetup(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }
        public void Configure(MvcJsonOptions o)
        {
            o.SerializerSettings.ContractResolver =
                new DIContractResolver(serviceProvider.GetService<IDIMeta>(), serviceProvider);
        }
    }
}
