using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using HtmlAgilityPack;

namespace Name{
    
    internal class NewBaseType
    {
        static void Main(string[] args)
        {
            var url1 = "https://store.steampowered.com/search/?tags=21978&supportedlang=english&ndl=1&count=100";
            var url2 = "https://store.steampowered.com/search/?vrsupport=401&supportedlang=english&ndl=1&count=100";
            Console.WriteLine("Which option would you like to scrape?");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Option1: active player data from as recent as possible.");
            Console.WriteLine("Option2: peak active player data from the past 24 hours.");
            Console.ResetColor();
            string optionPicked = Console.ReadLine();
            if(optionPicked.Contains("1")||optionPicked.Contains("first"))
            {
                RunProgram(url1, "active players from as recently as possible from the top 100 VR supported games", "/html/body/div[3]/div[3]/div[1]/span");
                RunProgram(url2, "active players from as recently as possible from the top 100 VR only games", "/html/body/div[3]/div[3]/div[1]/span");
            }
            if(optionPicked.Contains("2")||optionPicked.Contains("second"))
            {
                RunProgram(url1, "peak active players in 24 hours from the top 100 VR supported games", "/html/body/div[3]/div[3]/div[2]/span");
                RunProgram(url2, "peak active players in 24 hours from the top 100 VR only games", "/html/body/div[3]/div[3]/div[2]/span");
            } 
            Console.ReadKey();
        }

        static void RunProgram(string url, string top100Message,string XPath)
        {
            Console.WriteLine($"Scraping {top100Message} from store.steampowered.com....\n");
            var games = new List<Tuple<string, float>>();
            float totalMonthlyPlayers = 0;

            using (var client = new WebClient())
            {
                var html = client.DownloadString(url);

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

                    var monthlyPlayersNode = chartDoc.DocumentNode.SelectSingleNode(XPath);

                    if (monthlyPlayersNode != null && float.TryParse(monthlyPlayersNode.InnerText, out var monthlyPlayers))
                    {
                        games.Add(new Tuple<string, float>(gameName, monthlyPlayers));
                        totalMonthlyPlayers += monthlyPlayers;
                    }
                    else
                    {
                        Console.WriteLine($"Active players data not found for {gameName}.");
                    }
                }

                games = games.OrderByDescending(x => x.Item2 == float.MinValue).ThenByDescending(x => x.Item2).ToList();

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nTotal Active players: {totalMonthlyPlayers.ToString("F1")}");
                Console.ResetColor();

                Console.WriteLine("Top 100 VR Games on Steam:");
                int count = 0;
                foreach (var game in games.Where(g => g.Item2 >= 0).OrderByDescending(g => g.Item2))
                {
                    Console.WriteLine($"{game.Item1}: {game.Item2:F1} active players.");
                    count++;
                    if (count == 200) break;
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
                    throw new ArgumentException($"App ID for {gameName} not found.");
                }
            }
        }
    }
}