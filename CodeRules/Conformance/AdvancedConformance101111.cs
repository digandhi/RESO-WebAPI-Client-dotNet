﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace ODataValidator.Rule
{
    #region Namespaces
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Net;
    using Newtonsoft.Json.Linq;
    using ODataValidator.Rule.Helper;
    using ODataValidator.RuleEngine;
    
    #endregion

    /// <summary>
    /// Class of extension rule for Advanced.Conformance.101111
    /// </summary>
    [Export(typeof(ExtensionRule))]
    public class AdvancedConformance101111 : ConformanceAdvancedExtensionRule
    {
        /// <summary>
        /// Gets rule name
        /// </summary>
        public override string Name
        {
            get
            {
                return "Advanced.Conformance.101111";
            }
        }

        /// <summary>
        /// Gets rule description
        /// </summary>
        public override string Description
        {
            get
            {
                return "9). When a request within a change set fails, the change set response is represented using the application/http media type and a Content-Transfer-Encoding header with a value of binary instead of using the multipart/mixed media type. (section 11.7.4)";
            }
        }

        /// <summary>
        /// Gets rule specification in OData document
        /// </summary>
        public override string V4SpecificationSection
        {
            get
            {
                return "11.7.4";
            }
        }

        /// <summary>
        /// Gets the resource type to which the rule applies.
        /// </summary>
        public override ConformanceServiceType? ResourceType
        {
            get
            {
                return ConformanceServiceType.ReadWrite;
            }
        }

        /// <summary>
        /// Verifies the extension rule.
        /// </summary>
        /// <param name="context">The Interop service context</param>
        /// <param name="info">out parameter to return violation information when rule does not pass</param>
        /// <returns>true if rule passes; false otherwise</returns>
        public override bool? Verify(ServiceContext context, out ExtensionRuleViolationInfo info)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            bool? passed = null;
            info = null;
            var termDocs = TermDocuments.GetInstance();
            var serviceStatus = ServiceStatus.GetInstance();
            var dFactory = DataFactory.Instance();
            var detail1 = new ExtensionRuleResultDetail(this.Name);
            var detail2 = new ExtensionRuleResultDetail(this.Name);
            List<string> keyPropertyTypes = new List<string>() { "Edm.Int32", "Edm.Int16", "Edm.Int64", "Edm.Guid", "Edm.String" };
            List<string> norPropertyTypes = new List<string>() { "Edm.String" };
            List<EntityTypeElement> entityTypeElements = MetadataHelper.GetEntityTypes(serviceStatus.MetadataDocument, 1, keyPropertyTypes, norPropertyTypes, NavigationRoughType.None).ToList();
            if (null == entityTypeElements || 0 == entityTypeElements.Count)
            {
                detail1.ErrorMessage = "To verify this rule it expects an entity type with Int32/Int64/Int16/Guid/String key property and a normal property with string type, but there is not this entity type in metadata, so can not verify this rule.";
                info = new ExtensionRuleViolationInfo(new Uri(serviceStatus.RootURL), serviceStatus.ServiceDocument, detail1);

                return passed;
            }

            EntityTypeElement entityType = null;
            foreach (var ele in entityTypeElements)
            {
                if (ele.EntityTypeShortName.IsMediaType())
                {
                    continue;
                }

                var funcs = new List<Func<string, string, string, List<NormalProperty>, List<NavigProperty>, bool>>()
                {
                    AnnotationsHelper.GetInsertRestrictions, AnnotationsHelper.GetUpdateRestrictions, AnnotationsHelper.GetDeleteRestrictions
                };
                var methods = new List<Func<string, string, List<string>, bool?>>()
                {
                    SupportiveFeatureHelper.IsSupportBatchOperation
                };
                var restrictions = ele.EntitySetName.GetRestrictions(serviceStatus.MetadataDocument, termDocs.VocCapabilitiesDoc, funcs, null, NavigationRoughType.None, methods);

                if (!string.IsNullOrEmpty(restrictions.Item1) &&
                    null != restrictions.Item2 && restrictions.Item2.Any())
                {
                    entityType = ele;
                    break;
                }
            }

            if (null == entityType || string.IsNullOrEmpty(entityType.EntitySetName))
            {
                detail1.ErrorMessage = "The service does not support batch operation.";
                info = new ExtensionRuleViolationInfo(new Uri(serviceStatus.RootURL), serviceStatus.ServiceDocument, detail1);

                return passed;
            }

            string entitySetUrl = entityType.EntitySetName.MapEntitySetNameToEntitySetURL();

            if (string.IsNullOrEmpty(entitySetUrl))
            {
                detail1.ErrorMessage = string.Format("Cannot find the entity-set URL which is matched with {0}", entityType.EntityTypeShortName);
                info = new ExtensionRuleViolationInfo(new Uri(serviceStatus.RootURL), serviceStatus.ServiceDocument, detail1);

                return passed;
            }

            string url = serviceStatus.RootURL.TrimEnd('/') + @"/" + entitySetUrl;
            var additionalInfos = new List<AdditionalInfo>();
            var reqData = dFactory.ConstructInsertedEntityData(entityType.EntitySetName, entityType.EntityTypeShortName, null, out additionalInfos);
            string reqDataStr = reqData.ToString();
            var resp = WebHelper.CreateEntity(url, context.RequestHeaders, reqData, false, ref additionalInfos);
            detail1 = new ExtensionRuleResultDetail(this.Name, url, HttpMethod.Post, string.Empty, resp, string.Empty, reqDataStr);

            if (HttpStatusCode.Created == resp.StatusCode)
            {
                string entityId = additionalInfos.Last().EntityId;
                bool hasEtag = additionalInfos.Last().HasEtag;
                resp = WebHelper.GetEntity(entityId);

                if (HttpStatusCode.OK == resp.StatusCode)
                {
                    JObject entity = JObject.Parse(resp.ResponsePayload);
                    List<string> norPropertyNames = entityType.NormalProperties.Where(norProp => norPropertyTypes.Contains(norProp.PropertyType)).Select(norProp => norProp.PropertyName).ToList();
                    reqDataStr = dFactory.ConstructUpdatedEntityData(entity, norPropertyNames).ToString();

                    string boundary = "batch_4e1a76dc-b738-4aa4-9f93-df661d0a4c9f";
                    string batchReqData = string.Format(
@"
--batch_4e1a76dc-b738-4aa4-9f93-df661d0a4c9f
Content-Type: multipart/mixed; boundary=changeset_77162fcd-b8da-41ac-a9f8-9357efbbd621

--changeset_77162fcd-b8da-41ac-a9f8-9357efbbd621
Content-Type: application/http 
Content-Transfer-Encoding: binary 
Content-ID: 1

PATCH {0} HTTP/1.1
"
+ (hasEtag ? "If-Match: *" : string.Empty) +
@"

{1}

--changeset_77162fcd-b8da-41ac-a9f8-9357efbbd621--

--batch_4e1a76dc-b738-4aa4-9f93-df661d0a4c9f--
", entityId, reqDataStr);
                    resp = WebHelper.BatchOperation(serviceStatus.RootURL, batchReqData, boundary);
                    detail2 = new ExtensionRuleResultDetail(this.Name, serviceStatus.RootURL + "/$batch", HttpMethod.Post, string.Empty, resp, string.Empty, batchReqData);

                    if (HttpStatusCode.OK == resp.StatusCode)
                    {
                        string expectedData =
@"Content-Type: application/http
Content-Transfer-Encoding: binary
Content-ID: 1
HTTP/1.1 204 No Content
";
                        int index = resp.ResponsePayload.IndexOf("--changesetresponse");
                        string payload = resp.ResponsePayload.Remove(0, index + 1);
                        if (expectedData == payload.Filtration(new List<string>() { "\nContent-Type:", "\nContent-Transfer-Encoding:", "\nContent-ID:", "\nHTTP/1.1 " }))
                        {
                            passed = true;
                        }
                        else
                        {
                            passed = false;
                            detail2.ErrorMessage = string.Format("The response payload does not accord with the expected pattern as follows:\r\n{0}", expectedData);
                        }
                    }
                    else
                    {
                        passed = false;
                        detail2.ErrorMessage = "The OData service does not return a 200 OK HTTP status code.";
                    }

                    // Restore the service.
                    var resps = WebHelper.DeleteEntities(context.RequestHeaders, additionalInfos);
                }
            }
            else
            {
                passed = false;
                detail1.ErrorMessage = string.Format("Created the new entity failed for above URI with entity data {0}.", reqDataStr);
            }

            var details = new List<ExtensionRuleResultDetail>() { detail1, detail2 }.RemoveNullableDetails();
            info = new ExtensionRuleViolationInfo(new Uri(serviceStatus.RootURL), serviceStatus.ServiceDocument, details);

            return passed;
        }
    }
}
