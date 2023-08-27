using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using HtmlAgilityPack;
using System.Globalization;
using System.IO;

internal class NewBaseType
    {
        static readonly string SheetNameVRSupported = "VRSupported";
        static readonly string SheetNameVROnly = "VROnly";
        static readonly string SheetNameVROnlyPeak = "VROnlyPeak";
        static readonly string SheetNameVRSupportedPeak = "VRSupportedPeak";
        static void Main(string[] args)
        {   
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Pick which option you'd like to scrape");
            Console.WriteLine("-----------------------------------------");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Option 1: print all data from a specific time frame for 100 games");
            Console.WriteLine("Option 2: print average active players data from a specific game from a specific date");
            Console.WriteLine("Option 3: Misc data on a chosen game.");
            string optionPicked = Console.ReadLine();
            while (!string.IsNullOrWhiteSpace(optionPicked))
            {
                switch (optionPicked)
                {
                    case "1":
                        Option1();
                        break;

                    case "2":
                        PrintSpecificGame();
                        break;

                    case "3":
                        PrintMiscData();
                        break;

                    default: Console.WriteLine("Invalid option.");
                        break;
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Pick an option or press enter to exit");
                optionPicked = Console.ReadLine();
                Console.ResetColor();
            }
        }
        static void Option1()
        {
            bool shouldContinue = true;
            while (shouldContinue)
            {
                var url1 = "https://store.steampowered.com/search/?tags=21978&supportedlang=english&ndl=1&count=100";
                var url2 = "https://store.steampowered.com/search/?vrsupport=401&supportedlang=english&ndl=1&count=100";
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("What data would you like? (pick a number or click enter to exit)");
                Console.WriteLine("----------------------------------------------");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("1: Active players from as recently as possible from the top 50 VR supported games.");
                Console.WriteLine("2: Active players from as recently as possible from the top 50 VR only games.");
                Console.WriteLine("3: Peak active players in 24 hours from the top 50 VR supported games.");
                Console.WriteLine("4: Peak active players in 24 hours from the top 50 VR only games.");
                Console.ResetColor();
                string optionPicked = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(optionPicked))
                {
                    shouldContinue = false;
                    continue;
                }
                switch (optionPicked)
                {
                    case "1":
                        PrintActivePlayerData(url1, "active players from as recently as possible from the top 100 VR supported games", "/html/body/div[3]/div[3]/div[1]/span");
                        break;

                    case "2":
                        PrintActivePlayerData(url2, "active players from as recently as possible from the top 100 VR only games", "/html/body/div[3]/div[3]/div[1]/span");
                        break;

                    case "3":
                        PrintActivePlayerData(url1, "peak active players in 24 hours from the top 100 VR supported games", "/html/body/div[3]/div[3]/div[2]/span");
                        break;

                    case "4":
                        PrintActivePlayerData(url2, "peak active players in 24 hours from the top 100 VR only games", "/html/body/div[3]/div[3]/div[2]/span");
                        break;

                    default: Console.WriteLine("Invalid option.");
                        break;
                }
            }
        }
        static void PrintActivePlayerData(string url, string top100Message, string XPath)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Scraping {top100Message} from store.steampowered.com....\n");
            Console.ResetColor();
            List<object> games = new List<object>();
            float totalActivePlayers = 0;

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
                    var chartUrl = $"https://steamcharts.com/app/{appId}";
                    var chartHtml = "";

                    using (var chartClient = new WebClient())
                    {
                        try
                        {
                            chartHtml = chartClient.DownloadString(chartUrl);
                            time = GetTime(chartHtml, "yyyy-MM-dd HH:mm:ss");
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
                            activePlayers.ToString("N0"), // Format the active players number with commas
                            time,
                        };
                        games.Add(game);
                        totalActivePlayers += activePlayers;
                    }
                    else
                    {
                        Console.WriteLine($"Active players data not found for {gameName}.");
                    }
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nTotal Active players: {totalActivePlayers.ToString("N0")}"); // Format the total active players with commas
                Console.ResetColor();

                Console.WriteLine("Top 100 VR Games on Steam:");
                int count = 0;
                foreach (List<object> game in games)
                {
                    Console.WriteLine($"{game[0]}: {game[1]} active players at {game[2]}");
                    count++;
                    if (count == 100) break;
                }
            }
            Console.WriteLine();
        }

        static void PrintSpecificGame()
        {
            var time = DateTime.Now;
            string currentDate = DateTime.Now.ToString("MMMM yyyy");
            bool shouldContinue = true;
            while (shouldContinue)
            {
                bool gameFound = false;
                bool dateFound = false;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("What game would you like to scrape? (press enter to exit)");
                Console.ResetColor();
                string gamePicked = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(gamePicked))
                {
                    shouldContinue = false;
                    continue;
                }
                gamePicked = CapitalizeFirstLetter(gamePicked);
                int appId = GetAppId(gamePicked);
                List<object> games = new List<object>();
                float activePlayers = 0;
                var steamUrl = $"https://store.steampowered.com/app/{appId}/{gamePicked}/";
                using (var client = new WebClient())
                {
                    var html = client.DownloadString(steamUrl);
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    var gameName = doc.DocumentNode.SelectSingleNode("//div[@class='apphub_AppName']")?.InnerText;
                    if (gameName == gamePicked)
                    {
                        gameFound = true;
                    }
                }
                string url = $"https://steamcharts.com/app/{appId}";
                if (gameFound)
                {
                    try
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("From what date? Eg: 'May 2022'");
                        Console.ResetColor();
                        string datePicked = Console.ReadLine();
                        datePicked = CapitalizeFirstLetter(datePicked);
                        using (var client = new WebClient())
                        {
                            var html = client.DownloadString(url);
                            var doc = new HtmlDocument();
                            doc.LoadHtml(html);

                            var tableBody = doc.DocumentNode.SelectSingleNode("//tbody");
                            var tableRows = doc.DocumentNode.SelectNodes(".//tr");
                            foreach (var row in tableRows)
                            {
                                var dateNode = row.SelectSingleNode(".//td[1]");
                                if (dateNode != null)
                                {
                                    string date = dateNode.InnerText.Trim();
                                    if(datePicked.Contains($"{currentDate}"))
                                    {
                                        datePicked = "Last 30 Days";
                                    }
                                    float averageActivePlayers = float.Parse(row.SelectSingleNode(".//td[2]").InnerText);
                                    string averageActivePlayersFormatted = averageActivePlayers.ToString("N0");
                                    if (string.Equals(date, datePicked.Trim(), StringComparison.OrdinalIgnoreCase))
                                    {
                                        Console.WriteLine($"{gamePicked} had {averageActivePlayersFormatted} average active players in {date}");
                                        dateFound = true;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error occured: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Game not found");
                }
                if (!dateFound && gameFound)
                {
                Console.WriteLine("Date not found");
                }
            }
        }
        static void PrintMiscData()
        {
            bool shouldContinue = true;
            while (shouldContinue)
            {
                bool gameFound = false;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("What game would you like to scrape? (press enter to exit)");
                Console.ResetColor();
                string gamePicked = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(gamePicked))
                {
                    shouldContinue = false;
                    continue;
                }
                gamePicked = CapitalizeFirstLetter(gamePicked);
                int appId = GetAppId(gamePicked);
                List<object> games = new List<object>();
                float activePlayers;
                var steamUrl = $"https://store.steampowered.com/app/{appId}/{gamePicked}/";
                using (var client = new WebClient())
                {
                    var html = client.DownloadString(steamUrl);
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    string gameName = doc.DocumentNode.SelectSingleNode("//div[@class='apphub_AppName']")?.InnerText;
                    string recentReviews = doc.DocumentNode.SelectSingleNode("//*[@id='userReviews']/div[1]/div[2]/span[1]")?.InnerText;
                    string allReviews = doc.DocumentNode.SelectSingleNode("//*[@id='userReviews']/div[2]/div[2]/span[1]")?.InnerText;
                    string developer = doc.DocumentNode.SelectSingleNode("//*[@id='developers_list']/a")?.InnerText;
                    string releaseDate = doc.DocumentNode.SelectSingleNode("//*[@id='game_highlights']/div[1]/div/div[3]/div[2]/div[2]")?.InnerText;
                    if (gameName == gamePicked)
                    {
                        Console.WriteLine($"{gamePicked}:");
                        Console.WriteLine($"Recent reviews: {recentReviews}");
                        Console.WriteLine($"All reviews: {allReviews}");
                        Console.WriteLine($"Developer: {developer}");
                        Console.WriteLine($"Release date: {releaseDate}");
                        gameFound = true;
                    }
                }
                string url = $"https://steamcharts.com/app/{appId}";
                if (gameFound)
                {
                    try
                    {
                        using (var client = new WebClient())
                        {
                            var html = client.DownloadString(url);
                            var doc = new HtmlDocument();
                            doc.LoadHtml(html);
                            var currentTime = DateTime.Now;
                            int timeHours = int.Parse(GetTime(html, "HH"));
                            int timeMins = int.Parse(GetTime(html, "mm"));
                            int timeSecs = int.Parse(GetTime(html, "ss"));
                            int hoursAgo = currentTime.Hour - timeHours;
                            if (hoursAgo < 0)
                            {
                                hoursAgo = 0;
                            }
                            int minsAgo = currentTime.Minute - timeMins;
                            if (minsAgo < 0)
                            {
                                minsAgo = 0;
                            }
                            int secondsAgo = currentTime.Second - timeSecs;
                            if (secondsAgo < 0)
                            {
                                secondsAgo = 0;
                            }
                            
                            if (gameFound)
                            {
                                activePlayers = float.Parse(doc.DocumentNode.SelectSingleNode("//*[@id='app-heading']/div[1]/span").InnerText);
                                string activePlayersFormatted = activePlayers.ToString("N0");
                                Console.WriteLine($"{gamePicked} had {activePlayersFormatted} people playing {hoursAgo} hours {minsAgo} mins {secondsAgo} seconds ago");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error occured: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Game not found");
                }
            }
        }
        static string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            string[] words = input.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                char[] charArray = words[i].ToCharArray();
                if (charArray.Length > 0)
                {
                    charArray[0] = char.ToUpper(charArray[0]);
                    words[i] = new string(charArray);
                }
            }

            return string.Join(" ", words);
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
                    throw new ArgumentException($"App ID for {gameName} not found.");
                }
            }
        }
        static string GetTime(string html, string format)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var timeNode = doc.DocumentNode.SelectSingleNode("/html/body/div[3]/div[3]/div[1]/abbr");
            if (timeNode != null)
            {
                var timeValue = timeNode.Attributes["title"].Value;
                DateTime dateTime = DateTime.ParseExact(timeValue, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
                string timeOutput = dateTime.ToString(format);
                return timeOutput;
            }
            else
            {
                return "failed";
            }
        }
    }