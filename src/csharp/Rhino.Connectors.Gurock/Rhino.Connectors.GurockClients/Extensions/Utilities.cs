/*
 * CHANGE LOG - keep only last 5 threads
 * 
 * RESOURCES
 */
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Rhino.Connectors.GurockClients.Extensions
{
    internal static class Utilities
    {
        /// <summary>
        /// Gets Json Setting based on naming strategy
        /// </summary>
        /// <typeparam name="T">Naming strategy type</typeparam>
        /// <returns>The settings applied on a Newtonsoft.Json.JsonSerializer object</returns>
        public static JsonSerializerSettings GetJsonSettings<T>() where T : NamingStrategy, new()
        {
            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new T()
            };
            return new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                Formatting = Formatting.Indented
            };
        }
    }
}