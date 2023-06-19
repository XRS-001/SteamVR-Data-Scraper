using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using HtmlAgilityPack;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using System.Globalization;
using System.IO;
using Google.Apis.Sheets.v4.Data;

internal class NewBaseType
    {
        static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static readonly string ApplicationName = "Google Sheets Example";
        static readonly string SpreadsheetId = "1iNCpvySskw4ANIOHQirU8vm66C86sLTGugGrXH1i9dk";
        static readonly string SheetNameVRSupported = "VRSupported";
        static readonly string SheetNameVROnly = "VROnly";
        static readonly string SheetNameVROnlyPeak = "VROnlyPeak";
        static readonly string SheetNameVRSupportedPeak = "VRSupportedPeak";
        static readonly string CredentialsPath = "C:\\Users\\Con_P\\Desktop\\Steam VR Data\\webscraper-389916-1e30a707e640.json";
        private static SheetsService InitializeSheetsService(string credentialsPath)
        {
            GoogleCredential credential;

            using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(Scopes);
            }

            return new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });
        }

        private static void AppendData(SheetsService service, string spreadsheetId, string sheetName, IList<object> rowData, int collumA)
        {
            // Define the range where the data will be appended
            string range = $"{sheetName}!A{collumA}:B3";

            // Create the value range object
            var valueRange = new ValueRange
            {
                Values = new List<IList<object>> { rowData }
            };

            // Create the AppendValuesRequest
            var request = service.Spreadsheets.Values.Append(valueRange, spreadsheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

            // Execute the request
            var response = request.Execute();
        }
        static void Main(string[] args)
        {   
            var url1 = "https://store.steampowered.com/search/?tags=21978&supportedlang=english&ndl=1&count=100";
            var url2 = "https://store.steampowered.com/search/?vrsupport=401&supportedlang=english&ndl=1&count=100";
            RunProgram(url1, "active players from as recently as possible from the top 50 VR supported games", "/html/body/div[3]/div[3]/div[1]/span", SheetNameVRSupported);
            RunProgram(url2, "active players from as recently as possible from the top 50 VR only games", "/html/body/div[3]/div[3]/div[1]/span", SheetNameVROnly);
            RunProgram(url1, "peak active players in 24 hours from the top 50 VR supported games", "/html/body/div[3]/div[3]/div[2]/span", SheetNameVRSupportedPeak);
            RunProgram(url2, "peak active players in 24 hours from the top 50 VR only games", "/html/body/div[3]/div[3]/div[2]/span", SheetNameVROnlyPeak);
            Console.ReadKey();
        }
        static void CallGoogle(string SheetName, List<object> rowData, int collumA)
        {
            try
            {
                // Initialize the Sheets service
                SheetsService service = InitializeSheetsService(CredentialsPath);

                // Append the data to the spreadsheet
                AppendData(service, SpreadsheetId, SheetName, rowData, collumA);
                Console.WriteLine("Data appended successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

        static void RunProgram(string url, string top50Message, string XPath, string sheetName)
        {
            int collumA = 1;
            Console.WriteLine($"Scraping {top50Message} from store.steampowered.com....\n");
            List<object> games = new List<object>();
            float totalactivePlayers = 0;

            using (var client = new WebClient())
            {
                var html = client.DownloadString(url);
                var time = "";
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var gameNodes = doc.DocumentNode.SelectNodes("//div[@id='search_resultsRows']/a");

                foreach (var gameNode in gameNodes)
                {
                    var gameName = gameNode.SelectSingleNode(".//span[@class='title']").InnerText;
                    var appId = GetAppId(gameName);
                    string imageUrl = $"//steamcharts.com/assets/steam-images/{appId}.jpg";
                    var chartUrl = $"https://steamcharts.com/app/{appId}";
                    var chartHtml = "";

                    using (var chartClient = new WebClient())
                    {
                        try
                        {
                            chartHtml = chartClient.DownloadString(chartUrl);
                            time = GetTime(chartHtml);
                        }
                        catch (WebException ex)
                        {
                            if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                            {
                                var resp = (HttpWebResponse)ex.Response;
                                if (resp.StatusCode == HttpStatusCode.NotFound)
                                {
                                    Console.WriteLine($"Active players data not found for {gameName}.");
                                    continue;
                                }
                            }
                        }
                    }
                    var chartDoc = new HtmlDocument();
                    chartDoc.LoadHtml(chartHtml);

                    var activePlayersNode = chartDoc.DocumentNode.SelectSingleNode(XPath);

                    if (activePlayersNode != null && float.TryParse(activePlayersNode.InnerText, out var activePlayers))
                    {
                        List<object> game = new List<object>
                        {
                            gameName,
                            activePlayers,
                            time,
                            appId,
                            imageUrl,
                        };
                        games.Add(game);
                        totalactivePlayers += activePlayers;
                    }
                    else
                    {
                        Console.WriteLine($"Active players data not found for {gameName}.");
                    } 
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nTotal Active players: {totalactivePlayers.ToString("F1")}");
                Console.ResetColor();

                Console.WriteLine("Top 100 VR Games on Steam:");
                int count = 0;
                foreach (List<object> game in games)
                {
                    collumA ++;
                    CallGoogle(sheetName, game, collumA);
                    Console.WriteLine($"App id: {game[3]} ImageURL: {game[4]} {game[0]}: {game[1]:F1} active players at {game[2]}");
                    count++;
                    if (count == 50) break;
                }
            }
            Console.WriteLine();
        }

        static int GetAppId(string gameName)
        {
            var url = $"https://store.steampowered.com/search/?term={gameName}&type=vg&count=100";
            using (var client = new WebClient())
            {
                var html = client.DownloadString(url);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var gameNode = doc.DocumentNode.SelectSingleNode("//div[@id='search_resultsRows']/a");
              if (gameNode != null)
                {
                    var href = gameNode.Attributes["href"].Value;
                    var parts = href.Split('/');
                    return int.Parse(parts[4]);
                }
                else
                {

                    Console.ForegroundColor = ConsoleColor.Red;
                    throw new ArgumentException($"App ID for {gameName} not found.");
                    Console.ResetColor();
                }
            }
        }
        static string GetTime(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var timeNode = doc.DocumentNode.SelectSingleNode("/html/body/div[3]/div[3]/div[1]/abbr");
            if (timeNode != null)
            {
                var timeValue = timeNode.Attributes["title"].Value;
                DateTime dateTime = DateTime.ParseExact(timeValue, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
                string timeOutput = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
                return timeOutput;
            }
            else
            {
                return "failed";
            }
        }
    }