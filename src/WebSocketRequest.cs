using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using log4net;

namespace BambooPlug
{
    internal class WebSocketRequest
    {
        internal WebSocketRequest(HttpClient httpClient)
        {
            mHttpClient = httpClient;
        }

        internal async Task<string> ProcessMessage(string message)
        {
            string requestId = Messages.GetRequestId(message);
            string type = string.Empty;
            try
            {
                type = Messages.GetActionType(message);
                switch (type)
                {
                    case "launchplan":
                        return await ProcessLaunchPlanMessage(
                            requestId,
                            Messages.ReadLaunchPlanMessage(message),
                            mHttpClient);

                    case "getstatus":
                        return await ProcessGetStatusMessage(
                            requestId,
                            Messages.ReadGetStatusMessage(message),
                            mHttpClient);

                    default:
                        return Messages.BuildErrorResponse(requestId,
                            string.Format("The action '{0}' is not supported", type));
                }
            }
            catch (Exception ex)
            {
                mLog.ErrorFormat("Error processing message {0}: \nMessage:{1}. Error: {2}",
                    type, message, ex.Message);
                BambooBuild.LogException(ex);
                return Messages.BuildErrorResponse(requestId, ex.Message);
            }
        }

        static async Task<string> ProcessLaunchPlanMessage(
            string requestId,
            LaunchPlanMessage message,
            HttpClient bambooHttpClient)
        {
            LogLaunchPlanMessage(message);

            string bambooBuildId = await BambooBuild.QueueBuildAsync(
                message.PlanName,
                message.ObjectSpec,
                message.Comment,
                message.Properties,
                bambooHttpClient);

            return Messages.BuildLaunchPlanResponse(requestId, bambooBuildId);
        }

        static async Task<string> ProcessGetStatusMessage(
            string requestId,
            GetStatusMessage message,
            HttpClient bambooHttpClient)
        {
            LogGetStatusMessage(message);

            BuildStatus status = await BambooBuild.QueryStatusAsync(
                message.PlanName,
                message.ExecutionId,
                bambooHttpClient);

            bool bIsFinished;
            bool bIsSuccessful;
            ParseStatus(status, out bIsFinished, out bIsSuccessful);

#warning bamboo API wrapper does not retrieve an explanation yet.
            return Messages.BuildGetStatusResponse(
                requestId, bIsFinished, bIsSuccessful, string.Empty);
        }

        internal static void LogException(Exception exception)
        {
            if (exception.InnerException != null)
                exception = exception.InnerException;

            mLog.ErrorFormat("Unexpected error: {0}", exception.Message);
            mLog.DebugFormat("Stack trace: {0}", exception.StackTrace);
        }

        static void ParseStatus(BuildStatus status, out bool bIsFinished, out bool bIsSuccessful)
        {
            if (status == null)
            {
                bIsFinished = true;
                bIsSuccessful = false;
                return;
            }

            if (status.Progress.Equals(
                    NOT_BUILT_TAG, StringComparison.InvariantCultureIgnoreCase))
            {
                bIsFinished = true;
                bIsSuccessful = false;
                return;
            }

            bIsFinished = status.Progress.Equals(
                FINISHED_BUILD_TAG, StringComparison.InvariantCultureIgnoreCase);

            bIsSuccessful = status.BuildResult.Equals(
                SUCESSFUL_BUILD_TAG, StringComparison.InvariantCultureIgnoreCase);
        }

        static void LogLaunchPlanMessage(LaunchPlanMessage message)
        {
            mLog.InfoFormat("Launch plan was requested. Fields:");
            mLog.InfoFormat("\tPlanName: " + message.PlanName);
            mLog.InfoFormat("\tObjectSpec: " + message.ObjectSpec);
            mLog.InfoFormat("\tComment: " + message.Comment);
            mLog.InfoFormat("\tProperties:");

            foreach (KeyValuePair<string, string> pair in message.Properties)
                mLog.InfoFormat("\t\t{0}: {1}", pair.Key, pair.Value);
        }

        static void LogGetStatusMessage(GetStatusMessage message)
        {
            mLog.InfoFormat("Plan status requested. Fields:");
            mLog.InfoFormat("\tPlanName: " + message.PlanName);
            mLog.InfoFormat("\tExecutionId: " + message.ExecutionId);
        }

        const string FINISHED_BUILD_TAG = "Finished";
        const string SUCESSFUL_BUILD_TAG = "Successful";
        const string NOT_BUILT_TAG = "NotBuilt";

        readonly HttpClient mHttpClient;

        static readonly ILog mLog = LogManager.GetLogger("bambooplug");
    }
}
