using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

using log4net;

namespace BambooPlug
{
    internal static class BambooBuild
    {
        internal static async Task<string> QueueBuildAsync(
            string projectPlanKey,
            string plasticUpdateToSpec,
            string buildComment,
            Dictionary<string, string> properties,
            HttpClient httpClient)
        {
            if (!string.IsNullOrEmpty(buildComment))
                properties[BUILD_COMMENT_PROPERTY_NAME] = buildComment;

            string propertiesUriPart = BuildPropertiesUriPart(properties);
            string endPoint = Uri.EscapeUriString(
                string.Format(
                    QUEUE_BUILD_URI_FORMAT,
                    projectPlanKey,
                    plasticUpdateToSpec,
                    propertiesUriPart));

            HttpResponseMessage response = await httpClient.PostAsync(endPoint, null);

            if (!response.IsSuccessStatusCode)
                return string.Empty;

            string responseStr = await response.Content.ReadAsStringAsync();

            return LoadBuildId(responseStr);
        }

        internal static async Task<BuildStatus> QueryStatusAsync(
            string projectPlanKey, string buildNumberId, HttpClient httpClient)
        {
            string endPoint = string.Format(QUERY_BUILD_URI_FORMAT, projectPlanKey, buildNumberId);
            HttpResponseMessage response = await httpClient.GetAsync(endPoint);

            if (!response.IsSuccessStatusCode)
                return null;

            string responseStr = await response.Content.ReadAsStringAsync();

            return LoadBuildStatus(responseStr);
        }

        static BuildStatus LoadBuildStatus(string responseStr)
        {
            XmlDocument xmlOutput = new XmlDocument();
            xmlOutput.LoadXml(responseStr);

            BuildStatus buildStatus = new BuildStatus();
            buildStatus.Progress = GetBuildAttribute(xmlOutput, "result", "lifeCycleState");
            buildStatus.BuildResult = GetBuildAttribute(xmlOutput, "result", "state");
            return buildStatus;
        }

        static string LoadBuildId(string responseStr)
        {
            if (string.IsNullOrEmpty(responseStr))
                return string.Empty;

            XmlDocument xmlOutput = new XmlDocument();
            xmlOutput.LoadXml(responseStr);

            string buildId = GetBuildAttribute(xmlOutput, "restQueuedBuild", "buildNumber");
            return buildId;
        }

        static string GetBuildAttribute(XmlDocument xmlOutput, string rootNodeName, string attrName)
        {
            if (xmlOutput == null)
                return string.Empty;

            XmlNode buildNode = xmlOutput.SelectSingleNode("/" + rootNodeName);

            if (buildNode == null)
                return string.Empty;

            XmlAttribute attr = buildNode.Attributes[attrName];

            if (attr == null)
                return string.Empty;

            return attr.Value;
        }

        static string BuildPropertiesUriPart(Dictionary<string, string> properties)
        {
            if (properties == null || properties.Count == 0)
                return string.Empty;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach(string key in properties.Keys)
            {
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(properties[key]))
                    continue;

                sb.AppendFormat(QUEUE_BUILD_PARAMETERS_URI_FORMAT, key, properties[key]);
            }

            return sb.ToString();
        }

        internal static void LogException(Exception ex)
        {
            string exceptionErrorMsg = GetErrorMessage(ex);
            string innerExceptionErrorMsg = GetErrorMessage(ex == null ? null : ex.InnerException);

            bool bHasInnerEx = !string.IsNullOrEmpty(innerExceptionErrorMsg);

            mLog.ErrorFormat("{0}{1}{2}{3}",
                exceptionErrorMsg,
                bHasInnerEx ? " - [" : string.Empty,
                innerExceptionErrorMsg,
                bHasInnerEx ? "]" : string.Empty);

            mLog.Debug(ex.StackTrace);
        }

        static string GetErrorMessage(Exception ex)
        {
            return ex == null || string.IsNullOrEmpty(ex.Message) ? string.Empty : ex.Message;
        }

        const string QUEUE_BUILD_URI_FORMAT =
            "rest/api/latest/queue/{0}?os_authType=basic&bamboo.variable.plasticscm.mergebot.update.spec={1}{2}";

        const string QUEUE_BUILD_PARAMETERS_URI_FORMAT =
            "&bamboo.variable.plasticscm.mergebot.{0}={1}";

        const string QUERY_BUILD_URI_FORMAT = "rest/api/latest/result/{0}-{1}";

        const string BUILD_COMMENT_PROPERTY_NAME = "buildComment";

        static readonly ILog mLog = LogManager.GetLogger("bambooplug");
    }
}
