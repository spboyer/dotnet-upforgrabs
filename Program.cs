﻿using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using McMaster.Extensions.CommandLineUtils;
using System.ComponentModel.DataAnnotations;
using System.Collections;
using Kurukuru;
using Octokit;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace upforgrabs
{
    [HelpOption]
    [Command(ThrowOnUnexpectedArgument = false)]
    class Program
    {
        [Option(Description = "Auto open the selected issue", LongName = "open", ShortName = "o")]
        public bool Open { get; set; } = false;

        [Option(Description = "Open first result", LongName = "lucky", ShortName = "l")]
        public bool Lucky { get; set; } = false;

        [Option(Description = "Number of results 1 - 25", LongName = "number", ShortName = "n")]
        [Range(1, 25)]
        public int Results { get; set; } = 5;

        [Option(Description = "Select a .NET programming language (csharp, c#, fsharp, f#)", LongName = "language", ShortName = "lang")]
        [RegularExpression("c#|f#|csharp|fsharp|", ErrorMessage = "The language must be a .NET programming language")]
        public string Language { get; set; }

        [Argument(0)]
        public string ProjectName { get; set; }
        private static readonly HttpClient client = new HttpClient { BaseAddress = new Uri("https://shboyer.azureedge.net/up-for-grabs/") };
        private static List<Project> projects = new List<Project>();
        private static IEnumerable<string> searchFor = new List<string> { ".net", "c#", "f#", "dotnet", "csharp", "fsharp" };
        private static readonly IEnumerable<(string firstLang, string secondLang)> Langs = new List<(string firstLang, string secondLang)>
    {
        ("c#", "csharp"), ("f#", "fsharp")
    };
        public static int Main(string[] args) =>
            CommandLineApplication.Execute<Program>(args);

        private void OnExecute(CommandLineApplication app)
        {
            if (!String.IsNullOrEmpty(ProjectName))
            {
                getProjects().GetAwaiter().GetResult();
                var selected = projects.Where(p => p.name.Equals(ProjectName, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
                if (selected != null)
                {
                    GetRandomIssue(selected, Open);
                }
                else
                {
                    Console.WriteLine($"{ProjectName} not found in list.");
                }
            }
            else
            {
                Spinner.Start("Getting .NET Open Source Projects", spinner =>
                {
                    getProjects().GetAwaiter().GetResult();
                    spinner.Succeed();

                    var selected = RandomArrayEntries(projects.ToArray(), Results);
                    Project chosen = null;

                    if (!string.IsNullOrWhiteSpace(Language))
                    {
                        searchFor = GetLangs(Language.ToLowerInvariant());
                    }

                    if (!Lucky)
                    {
                        chosen = ShowPicker(selected);
                    }
                    else
                    {
                        chosen = selected.First();
                    }
                    GetRandomIssue(chosen, Open);
                });
            }
        }
        private static IEnumerable<string> GetLangs(string language)
        {
            var langs = Langs
            .Where(x => x.firstLang == language || x.secondLang == language)
            .Select(x => new List<string> { x.firstLang, x.secondLang })
            .Single();
            return searchFor.Where(y => langs.Contains(y) || y == "dotnet" || y == ".net");
        }
        private static async Task getProjects()
        {
            using (var response = await client.GetAsync("projects.json"))
            {
                var raw = await response.Content.ReadAsStringAsync();
                projects = JsonConvert.DeserializeObject<List<Project>>(raw)
                .FindAll(p => FindProjectsWithTags(p.tags) && p.site.StartsWith("https://github.com"));
            }
        }

        private static bool FindProjectsWithTags(List<string> tags)
        {
            var stringComparison = StringComparison.CurrentCultureIgnoreCase;
            foreach (var t in tags)
            {
                foreach (var s in searchFor)
                {
                    if (t.Equals(s, stringComparison))
                        return true;
                }
            }
            return false;
        }

        private static Stack ShuffleProjects(IEnumerable<Project> values)
        {
            var random = new Random();
            var stack = new Stack();
            var list = new List<Project>(values);
            while (list.Count > 0)
            {
                var randomIndex = random.Next(0, list.Count);
                var randomItem = list[randomIndex];
                // Remove the item from the list and push it to the top of the deck.
                list.RemoveAt(randomIndex);
                stack.Push(randomItem);
            }

            return stack;
        }

        private static Project[] RandomArrayEntries(Project[] arrayItems, int count)
        {
            var listToReturn = new List<Project>();

            if (arrayItems.Length != count)
            {
                var deck = ShuffleProjects(projects);

                for (var i = 0; i < count; i++)
                {
                    var item = deck.Pop() as Project;
                    listToReturn.Add(item);
                }

                return listToReturn.ToArray();
            }

            return arrayItems;
        }
        private static Project ShowPicker(Project[] projects)
        {

            Console.WriteLine();
            Console.WriteLine("Please select a project:");
            Console.WriteLine();

            int left = Console.CursorLeft;
            int top = Console.CursorTop;

            void WriteList(int item)
            {
                Console.SetCursorPosition(left, top);
                for (var i = 0; i < projects.Length; i++)
                {
                    if (i == item)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.Write("> ");
                    }
                    else
                    {
                        Console.ResetColor();
                    }

                    Console.WriteLine($"{@i + 1}. {@projects[i].name.PadRight(100, Convert.ToChar(" "))}");
                }

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("(Use up/down to navigate, # or Enter to select.)");
                Console.WriteLine();
                Console.ResetColor();
            }

            Project GetSelected(int s)
            {
                return projects[s];
            }

            Project selected = null;
            int indicator = 0;
            ConsoleKeyInfo info = new ConsoleKeyInfo();

            while (selected == null)
            {
                if (info.Key == ConsoleKey.DownArrow)
                {
                    indicator += 1;
                    if (indicator == projects.Length)
                        indicator = 0;
                }

                if (info.Key == ConsoleKey.UpArrow)
                {
                    indicator -= 1;
                    if (indicator < 0)
                        indicator = projects.Length - 1;
                }

                if (info.Key == ConsoleKey.Enter)
                {
                    selected = GetSelected(indicator);
                    break;
                }

                int line = -1;
                if (int.TryParse(info.KeyChar.ToString(), out line) && line <= projects.Length)
                {
                    selected = GetSelected(line - 1);
                    break;
                }

                WriteList(indicator);
                info = Console.ReadKey();
            }

            return selected;
        }
        private static void GetRandomIssue(Project project, bool open)
        {
            Spinner.Start($"Getting random issue for {project.name}", spinner =>
             {
                 if (project.site.EndsWith("/"))
                     project.site = project.site.TrimEnd(Convert.ToChar("/"));

                 var parts = project.site.Split(Convert.ToChar("/"));
                 var owner = parts[parts.Length - 2];
                 var repoName = parts[parts.Length - 1];

                 var client = new GitHubClient(new ProductHeaderValue("dotnet-upforgrabs"));
                 var requestIssues = new RepositoryIssueRequest
                 {
                     State = ItemStateFilter.Open,
                 };
                 requestIssues.Labels.Add(project.upforgrabs.name);
                 try
                 {
                     var issues = client.Issue.GetAllForRepository(owner, repoName, requestIssues).GetAwaiter().GetResult();

                     if (issues.Count == 0)
                     {
                         spinner.Fail($"{project.name} currently has 0 open issues for {project.upforgrabs.name}");
                     }
                     else
                     {
                         spinner.Succeed();

                         var rand = new Random(issues.Count);
                         var item = issues.OrderBy(s => rand.NextDouble()).First();

                         Console.WriteLine();
                         Console.Write("UpForGrabs Issue for ");
                         Console.ForegroundColor = ConsoleColor.Yellow;
                         Console.WriteLine($"{project.name}");
                         Console.ResetColor();
                         Console.WriteLine();

                         Console.Write("   - Title: ");
                         Console.ForegroundColor = ConsoleColor.Yellow;
                         Console.WriteLine($"{item.Title}");
                         Console.Write("");
                         Console.ForegroundColor = ConsoleColor.Yellow;
                         Console.ResetColor();

                         Console.Write("   - Repository: ");
                         Console.ForegroundColor = ConsoleColor.Yellow;
                         Console.WriteLine($"{owner}/{repoName}");
                         Console.ResetColor();

                         Console.Write("   - Issue: #");
                         Console.ForegroundColor = ConsoleColor.Yellow;
                         Console.WriteLine($"{item.Number.ToString()}");
                         Console.ResetColor();

                         Console.Write("   - Status: ");
                         Console.ForegroundColor = ConsoleColor.Yellow;
                         Console.WriteLine($"{item.State.StringValue}");
                         Console.ResetColor();
                         Console.WriteLine();

                         var url = $"https://github.com/{owner}/{repoName}/issues/{item.Number.ToString()}";
                         Console.Write("Start now: ");
                         Console.ForegroundColor = ConsoleColor.DarkCyan;
                         Console.WriteLine(url);
                         Console.WriteLine();
                         Console.ResetColor();

                         if (open)
                         {
                             OpenResult(url);
                         }
                         else
                         {
                             string test = string.Empty;
                             Console.WriteLine($"{Environment.NewLine}Open issue to get started? (Y/N)");
                             ConsoleKeyInfo answer = new ConsoleKeyInfo();
                             while (string.IsNullOrEmpty(test))
                             {
                                 answer = Console.ReadKey();

                                 if (answer.Key == ConsoleKey.Y)
                                 {
                                     OpenResult(url);
                                     test = "yes";
                                 }

                                 if (answer.Key == ConsoleKey.N)
                                     test = "no";
                             }
                         }
                     }
                 }
                 catch (Exception)
                 {
                     spinner.Fail($"Something went wrong trying to get issues for repo:{project.site}. {Environment.NewLine}[DebugInfo: site:{project.site} {Environment.NewLine}owner:{owner} {Environment.NewLine}repoName:{repoName}]");
                 }
             });
        }

        private static void OpenResult(string url)
        {
            string cmd;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                cmd = "xdg-open";
                Process.Start(new ProcessStartInfo(cmd, url));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                cmd = "open";
                Process.Start(new ProcessStartInfo(cmd, url));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                cmd = "cmd";
                Process.Start(new ProcessStartInfo(cmd, $"/c start {url}") { CreateNoWindow = true });
            }
        }

    }
}