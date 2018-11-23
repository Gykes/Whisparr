using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using NLog;
using NUnit.Framework;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Processes;
using RestSharp;

namespace NzbDrone.Test.Common
{
    public class NzbDroneRunner
    {
        private readonly IProcessProvider _processProvider;
        private readonly IRestClient _restClient;
        private Process _nzbDroneProcess;

        public string AppData { get; private set; }
        public string ApiKey { get; private set; }

        public NzbDroneRunner(Logger logger, int port = 7878)
        {
            _processProvider = new ProcessProvider(logger);
            _restClient = new RestClient("http://localhost:7878/api");
        }

        public void Start()
        {
            AppData = Path.Combine(TestContext.CurrentContext.TestDirectory, "_intg_" + DateTime.Now.Ticks);

            var radarrConsoleExe = OsInfo.IsWindows ? "Radarr.Console.exe" : "Radarr.exe";

            if (BuildInfo.IsDebug)
            {
                Start(Path.Combine(TestContext.CurrentContext.TestDirectory, "..\\..\\..\\..\\..\\_output\\Radarr.Console.exe"));
            }
            else
            {
                Start(Path.Combine(TestContext.CurrentContext.TestDirectory, "bin", radarrConsoleExe));
            }

            while (true)
            {
                _nzbDroneProcess.Refresh();

                if (_nzbDroneProcess.HasExited)
                {
                    Assert.Fail("Process has exited");
                }

                SetApiKey();

                var request = new RestRequest("system/status");
                request.AddHeader("Authorization", ApiKey);
                request.AddHeader("X-Api-Key", ApiKey);

                var statusCall = _restClient.Get(request);

                if (statusCall.ResponseStatus == ResponseStatus.Completed)
                {
                    Console.WriteLine("Radarr is started. Running Tests");
                    return;
                }

                Console.WriteLine("Waiting for Radarr to start. Response Status : {0}  [{1}] {2}", statusCall.ResponseStatus, statusCall.StatusDescription, statusCall.ErrorException);

                Thread.Sleep(500);
            }
        }

        public void KillAll()
        {
            if (_nzbDroneProcess != null)
            {
                _processProvider.Kill(_nzbDroneProcess.Id);
            }

            _processProvider.KillAll(ProcessProvider.RADARR_CONSOLE_PROCESS_NAME);
            _processProvider.KillAll(ProcessProvider.RADARR_PROCESS_NAME);
        }

        private void Start(string outputRadarrConsoleExe)
        {
            var args = "-nobrowser -data=\"" + AppData + "\"";
            _nzbDroneProcess = _processProvider.Start(outputRadarrConsoleExe, args, null, OnOutputDataReceived, OnOutputDataReceived);

        }

        private void OnOutputDataReceived(string data)
        {
            Console.WriteLine(data);

            if (data.Contains("Press enter to exit"))
            {
                _nzbDroneProcess.StandardInput.WriteLine(" ");
            }
        }

        private void SetApiKey()
        {
            var configFile = Path.Combine(AppData, "config.xml");
            var attempts = 0;

            while (ApiKey == null && attempts < 50)
            {
                try
                {
                    if (File.Exists(configFile))
                    {
                        var apiKeyElement = XDocument.Load(configFile)
                            .XPathSelectElement("Config/ApiKey");
                        if (apiKeyElement != null)
                        {
                            ApiKey = apiKeyElement.Value;
                        }
                    }
                }
                catch (XmlException ex)
                {
                    Console.WriteLine("Error getting API Key from XML file: " + ex.Message, ex);
                }

                attempts++;
                Thread.Sleep(1000);
            }
        }
    }
}
