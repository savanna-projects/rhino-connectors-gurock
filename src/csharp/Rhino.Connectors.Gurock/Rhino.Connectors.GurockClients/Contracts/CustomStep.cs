/*
 * CHANGE LOG - keep only last 5 threads
 * 
 * RESOURCES
 * http://docs.gurock.com/testrail-api2/reference-cases
 */
using System.Runtime.Serialization;

namespace Rhino.Connectors.GurockClients.Contracts
{
    /// <summary>
    /// contract which describes test-rail test-step entity
    /// </summary>
    [DataContract]
    public class CustomStep
    {
        [DataMember]
        public string Content { get; set; }

        [DataMember]
        public string Expected { get; set; }

        [DataMember]
        public string Actual { get; set; }

        [DataMember]
        public int? StatusId { get; set; }
    }
}