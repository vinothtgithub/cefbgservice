using CefSharp;
using CefSharp.OffScreen;

namespace CEFSharpDemo.Net6.WokerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> log;

        public Worker(ILogger<Worker> logger)
        {
            log = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                log.LogInformation("Starting service");

                const string testUrl = "https://www.google.com/";

                AsyncContext.Run(async delegate
                {
                    var settings = new CefSettings()
                    {
                        //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                        CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
                    };
                    settings.CefCommandLineArgs.Add("disable-pdf-extension");

                    //Perform dependency check to make sure all relevant resources are in our output directory.
                    var success = await Cef.InitializeAsync(settings, performDependencyCheck: true, browserProcessHandler: null);

                    if (!success)
                    {
                        throw new Exception("Unable to initialize CEF, check the log file.");
                    }

                    using (var browser = new ChromiumWebBrowser(testUrl))
                    {
                        var initialLoadResponse = await browser.WaitForInitialLoadAsync();

                        if (!initialLoadResponse.Success)
                        {
                            throw new Exception(string.Format("Page load failed with ErrorCode:{0}, HttpStatusCode:{1}", initialLoadResponse.ErrorCode, initialLoadResponse.HttpStatusCode));
                        }

                        _ = await browser.EvaluateScriptAsync("document.querySelector('[name=q]').value = 'CefSharp Was Here!'");

                        //Give the browser a little time to render
                        await Task.Delay(500);
                        // Wait for the screenshot to be taken.
                        var bitmapAsByteArray = await browser.CaptureScreenshotAsync();

                        // File path to save our screenshot e.g. C:\Users\{username}\Desktop\CefSharp screenshot.png
                        var screenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CefSharp screenshot.png");

                        log.LogInformation("Screenshot ready. Saving to {0}", screenshotPath);

                        File.WriteAllBytes(screenshotPath, bitmapAsByteArray);

                        log.LogInformation("Screenshot saved.");
                    }

                    //// Wait for user to press a key before exit
                    //Console.ReadKey();

                    // Clean up Chromium objects. You need to call this in your application otherwise
                    // you will get a crash when closing.
                    Cef.Shutdown();
                });
            }
            catch (Exception ex)
            {
                log.Log(LogLevel.Error, ex, "Worker: {0}", ex.Message);

                // Terminates this process and returns an exit code to the operating system.
                // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
                // performs one of two scenarios:
                // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
                // 2. When set to "StopHost": will cleanly stop the host, and log errors.
                //
                // In order for the Windows Service Management system to leverage configured
                // recovery options, we need to terminate the process with a non-zero exit code.
                Environment.Exit(2);
            }

            return Task.CompletedTask;
        }
    }
}