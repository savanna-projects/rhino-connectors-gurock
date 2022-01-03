/*
 * CHANGE LOG - keep only last 5 threads
 * 
 * RESOURCES
 * http://docs.gurock.com/testrail-api2/reference-plans
 */

using System.Runtime.Serialization;

namespace Rhino.Connectors.GurockClients.Contracts
{
    [DataContract]
    public class TestRailPriority
    {
        /// <summary>
        /// Gets or sets the priority unique ID.
        /// </summary>
        [DataMember]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the order number of the priority.
        /// </summary>
        [DataMember]
        public int Priority { get; set; }

        /// <summary>
        /// Gets or sets the priority name.
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the priority short name.
        /// </summary>
        [DataMember]
        public string ShortName { get; set; }

        /// <summary>
        /// Gets or sets is the priority is default.
        /// </summary>
        [DataMember]
        public bool IsDefault { get; set; }
    }
}
