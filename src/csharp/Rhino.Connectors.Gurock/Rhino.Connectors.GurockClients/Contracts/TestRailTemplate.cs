/*
 * CHANGE LOG - keep only last 5 threads
 * 
 * RESOURCES
 * https://www.gurock.com/testrail/docs/api/reference/templates
 */
using System.Runtime.Serialization;

namespace Rhino.Connectors.GurockClients.Contracts
{
    [DataContract]
    public class TestRailTemplate
    {
        /// <summary>
        /// Gets or sets the unique ID of the template
        /// </summary>
        [DataMember]
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets if this template is default.
        /// </summary>
        [DataMember]
        public bool IsDefault { get; set; }

        /// <summary>
        /// Gets or sets the name of the template
        /// </summary>
        [DataMember]
        public string Name { get; set; }
    }
}