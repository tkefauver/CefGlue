using System;
using Xilium.CefGlue.Common.Helpers;
using Xilium.CefGlue.Common.ObjectBinding;
using Xilium.CefGlue.Common.RendererProcessCommunication;
using Xilium.CefGlue.Common.Serialization;

namespace Xilium.CefGlue.Common.JavascriptExecution
{
    internal class JavascriptExecutionEngineRenderSide
    {
        public JavascriptExecutionEngineRenderSide(MessageDispatcher dispatcher)
        {
            PromiseFactory.Register();
            dispatcher.RegisterMessageHandler(Messages.JsEvaluationRequest.Name, HandleScriptEvaluation);
        }

        private void HandleScriptEvaluation(MessageReceivedEventArgs args)
        {
            // TODO get the appropriate frame
            var browser = args.Browser;
            var context = browser.GetMainFrame().V8Context;

            if (context.Enter())
            {
                try
                {
                    var message = Messages.JsEvaluationRequest.FromCefMessage(args.Message);

                    // send script to browser
                    var success = context.TryEval(message.Script, message.Url, message.Line, out var value, out var exception);
                    
                    var response = new Messages.JsEvaluationResult()
                    {
                        TaskId = message.TaskId,
                        Success = success,
                        Exception = success ? null : BuildExceptionString(exception)
                    };

                    var cefResponseMessage = response.ToCefProcessMessage();

                    if (success)
                    {
                        V8ValueSerialization.SerializeV8Object(value, cefResponseMessage.Arguments, 2);
                    }

                    browser.SendProcessMessage(CefProcessId.Browser, cefResponseMessage);
                }
                finally
                {
                    context.Exit();
                }
            }
            else
            {
                // TODO
            }
        }

        private static string BuildExceptionString(CefV8Exception exception)
        {
            // TODO improve exception: shall we send all data in an object?
            var result = exception.Message + Environment.NewLine;
            if (!string.IsNullOrEmpty(exception.ScriptResourceName))
            {
                result += exception.ScriptResourceName;
            }
            result += ":" + exception.LineNumber + ":" + exception.StartColumn;
            return result;
        }
    }
}