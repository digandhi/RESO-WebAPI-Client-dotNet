// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace ODataValidator.Rule
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Xml;
    using System.Xml.XPath;
    using ODataValidator.Rule.Helper;
    using ODataValidator.RuleEngine;
    

    /// <summary>
    /// Class of extension rule
    /// </summary>
    [Export(typeof(ExtensionRule))]
    public class MetadataCore4027 : ExtensionRule
    {
        /// <summary>
        /// Gets Category property
        /// </summary>
        public override string Category
        {
            get
            {
                return "core";
            }
        }

        /// <summary>
        /// Gets rule name
        /// </summary>
        public override string Name
        {
            get
            {
                return "Metadata.Core.4027";
            }
        }

        /// <summary>
        /// Gets rule description
        /// </summary>
        public override string Description
        {
            get
            {
                return "Edm.EntityType cannot be used as the type of a singleton in an entity container.";
            }
        }

        /// <summary>
        /// Gets rule specification in OData document
        /// </summary>
        public override string SpecificationSection
        {
            get
            {
                return "4.5";
            }
        }

        /// <summary>
        /// Gets location of help information of the rule
        /// </summary>
        public override string HelpLink
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the error message for validation failure
        /// </summary>
        public override string ErrorMessage
        {
            get
            {
                return this.Description;
            }
        }

        /// <summary>
        /// Gets the requirement level.
        /// </summary>
        public override RequirementLevel RequirementLevel
        {
            get
            {
                return RequirementLevel.Must;
            }
        }

        /// <summary>
        /// Gets the payload type to which the rule applies.
        /// </summary>
        public override PayloadType? PayloadType
        {
            get
            {
                return RuleEngine.PayloadType.Metadata;
            }
        }

        /// <summary>
        /// Gets the version.
        /// </summary>
        public override ODataVersion? Version
        {
            get
            {
                return ODataVersion.V4;
            }
        }

        /// <summary>
        /// Gets the flag whether the rule requires metadata document
        /// </summary>
        public override bool? RequireMetadata
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the offline context to which the rule applies
        /// </summary>
        public override bool? IsOfflineContext
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the payload format to which the rule applies.
        /// </summary>
        public override PayloadFormat? PayloadFormat
        {
            get
            {
                return RuleEngine.PayloadFormat.Xml;
            }
        }

        /// <summary>
        /// Verify Metadata.Core.4027
        /// </summary>
        /// <param name="context">Service context</param>
        /// <param name="info">out parameter to return violation information when rule fail</param>
        /// <returns>true if rule passes; false otherwise</returns>
        public override bool? Verify(ServiceContext context, out ExtensionRuleViolationInfo info)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            bool? passed = null;
            info = null;

            // Load MetadataDocument into XMLDOM
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(context.MetadataDocument);

           // Singleton cannot use the abstract type Edm.EntityType as its type.
            string xpath = "//*[local-name()='Singleton' and @Type='Edm.EntityType']";
            XmlNodeList propertyNodeList = xmlDoc.SelectNodes(xpath);

            if (propertyNodeList.Count == 0)
            {
                passed = true;
            }
            else
            {
                passed = false;
                info = new ExtensionRuleViolationInfo(this.ErrorMessage, context.Destination, context.ResponsePayload);
            }            

            return passed;
        }
    }
}
