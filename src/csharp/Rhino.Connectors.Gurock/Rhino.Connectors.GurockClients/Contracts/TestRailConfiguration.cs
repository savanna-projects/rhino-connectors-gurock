/*
 * CHANGE LOG - keep only last 5 threads
 * 
 * RESOURCES
 * http://docs.gurock.com/testrail-api2/reference-configs
 */
using System.Runtime.Serialization;

namespace Rhino.Connectors.GurockClients.Contracts
{
    [DataContract]
    public class TestRailConfiguration : Contract
    {
        [DataMember]
        public TestRailConfigurationGroup[] Configs { get; set; }

        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int ProjectId { get; set; }
    }
}