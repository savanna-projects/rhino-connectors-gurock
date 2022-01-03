﻿/*
 * CHANGE LOG - keep only last 5 threads
 * 
 * RESOURCES
 * http://docs.gurock.com/testrail-api2/reference-attachments
 */
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Rhino.Connectors.GurockClients.Contracts
{
    [DataContract]
    public class TestRailAttachment
    {
        /// <summary>
        /// The unique ID for the attachment
        /// </summary>
        [DataMember]
        public int Id { get; set; }

        /// <summary>
        /// Name of the attachment
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// filename
        /// </summary>
        [DataMember]
        [JsonProperty(PropertyName = "filename")]
        [System.Text.Json.Serialization.JsonPropertyName("filename")]
        public string FileName { get; set; }

        /// <summary>
        /// Size of the attachment in bytes
        /// </summary>
        [DataMember]
        public int Size { get; set; }

        /// <summary>
        /// The time/date the attachment was uploaded
        /// </summary>
        [DataMember]
        public int? CreatedOn { get; set; }

        /// <summary>
        /// The ID of the project the attachment was uploaded against
        /// </summary>
        [DataMember]
        public int ProjectId { get; set; }

        /// <summary>
        /// The ID of the case the attachment belongs to
        /// </summary>
        [DataMember]
        public int CaseId { get; set; }

        /// <summary>
        /// The test change ID to which the attachment belongs to
        /// </summary>
        [DataMember]
        public int? TestChangeId { get; set; }

        /// <summary>
        /// The ID of the user who uploaded the attachment
        /// </summary>
        [DataMember]
        public int? UserId { get; set; }

        /// <summary>
        /// The ID of the attachment uploaded to TestRail
        /// </summary>
        [DataMember]
        public int AttachmentId { get; set; }
    }
}