using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Alex.API.Services;
using Alex.Gamestates.InGame;
using NLog;

namespace Alex.Utils.Assets
{
    public class MCBedrockAssetUtils
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger(typeof(MCBedrockAssetUtils));

        private const string DownloadURL = "https://aka.ms/resourcepacktemplate";
        private static readonly string CurrentBedrockVersionStorageKey = Path.Combine("assets", "bedrock-version.txt");
        
        private static readonly Regex ExtractVersionFromFilename = new Regex(@"Vanilla_.*Pack_(?<version>[\d\.]+).zip", RegexOptions.Compiled);
        
        private IStorageSystem Storage { get; }
        public MCBedrockAssetUtils(IStorageSystem storage)
        {
            Storage = storage;
        }

        public async Task<string> CheckAndDownloadResources(IProgressReceiver progressReceiver)
        {
            progressReceiver?.UpdateProgress(0, "Checking for resource updates...");
            
            using var httpClient = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = true,
            });

            try
            {
                string assetsZipSavePath = String.Empty;

                if (Storage.TryReadString(CurrentBedrockVersionStorageKey, out var currentVersion))
                {
                    assetsZipSavePath = Path.Combine("assets", $"bedrock-{currentVersion}.zip");

                    if (!Storage.Exists(assetsZipSavePath))
                        currentVersion = null;
                }

                try
                {
                 //   var preRedirectHeaders = await httpClient.SendAsync(
                  //      new HttpRequestMessage(HttpMethod.Get, DownloadURL), HttpCompletionOption.ResponseHeadersRead);
                 //   if (preRedirectHeaders.StatusCode == HttpStatusCode.MovedPermanently)
                  //  {
                        var zipDownloadHeaders =
                            await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                                DownloadURL), HttpCompletionOption.ResponseHeadersRead);

                        var fileName = zipDownloadHeaders.Content?.Headers?.ContentDisposition?.FileName ??
                                       zipDownloadHeaders.RequestMessage?.RequestUri?.LocalPath;
                        var versionMatch = ExtractVersionFromFilename.Match(fileName);
                        if (versionMatch.Success)
                        {
                            var latestVersion = versionMatch.Groups["version"].Value;

                            if (latestVersion != currentVersion
                                || (!string.IsNullOrWhiteSpace(assetsZipSavePath)
                                    && !Storage.Exists(assetsZipSavePath)))
                            {
                                progressReceiver?.UpdateProgress(
                                    0, "Downloading latest bedrock assets...", "This could take a while...");

                                /* zipDownloadHeaders = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                                     zipDownloadHeaders.Headers.Location), HttpCompletionOption.ResponseContentRead);
                                 var content = await zipDownloadHeaders.Content.ReadAsByteArrayAsync();*/

                                Stopwatch sw = Stopwatch.StartNew();
                                WebClient wc = new WebClient();

                                wc.DownloadProgressChanged += (sender, args) =>
                                {
                                    var downloadSpeed =
                                        $"Download speed: {PlayingState.GetBytesReadable((long) (Convert.ToDouble(args.BytesReceived) / sw.Elapsed.TotalSeconds), 2)}/s";

                                    progressReceiver?.UpdateProgress(
                                        args.ProgressPercentage,
                                        $"Downloading latest bedrock assets...",
                                        downloadSpeed);
                                };


                                var content = await wc.DownloadDataTaskAsync(zipDownloadHeaders.RequestMessage.RequestUri);

                                assetsZipSavePath = Path.Combine("assets", $"bedrock-{latestVersion}.zip");
                                
                                // save locally
                                Storage.TryWriteString(CurrentBedrockVersionStorageKey, latestVersion);

                                Storage.TryWriteBytes(assetsZipSavePath, content);

                                var existingPath = Path.Combine("assets", "bedrock");

                                Storage.TryDeleteDirectory(existingPath);
                            }

                        }
                   // }

                    return assetsZipSavePath;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to download bedrock assets...");
                    return assetsZipSavePath;
                }
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Failed to fetch latest bedrock assets pack.");
                throw;
            }
        }
    }
}