using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using McMaster.Extensions.CommandLineUtils;
using System.ComponentModel.DataAnnotations;
using System.Collections;

namespace upforgrabs
{
  [HelpOption]
  [Command(ThrowOnUnexpectedArgument = false)]
  class Program
  {

    [Option(Description = "Open first result", LongName = "lucky", ShortName = "l")]
    public bool Lucky { get; set; } = true;

    [Option(Description = "Number of results 1 - 25", LongName = "number", ShortName = "n")]
    [Range(1, 25)]
    public int Results { get; set; } = 5;

    [Argument(0)]
    public string Terms { get; set; }
    private static readonly HttpClient client = new HttpClient { BaseAddress = new Uri("https://shboyer.azureedge.net/up-for-grabs/") };
    private static List<Project> projects = new List<Project>();
    public static int Main(string[] args) =>
        CommandLineApplication.Execute<Program>(args);

    private void OnExecute(CommandLineApplication app)
    {
      if (String.IsNullOrEmpty(Terms))
      {
        app.ShowHelp();
      }
      else
      {
        getProjects().GetAwaiter().GetResult();
      }

    }

    private static async Task getProjects()
    {
      using (var response = await client.GetAsync("projects.json"))
      {
        var raw = await response.Content.ReadAsStringAsync();
        projects = JsonConvert.DeserializeObject<List<Project>>(raw)
        .FindAll(p => FindProjectsWithTags(p.tags));
      }
    }

    private static bool FindProjectsWithTags(List<string> tags)
    {
      var stringComparison = StringComparison.CurrentCultureIgnoreCase;
      var searchFor = new string[] { ".net", "c#", "f#", "dotnet", "csharp", "fsharp" };
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

    private static Stack ShuffleProjects()
    {
      var random = new Random();
      var stack = new Stack();
      var list = new List<Project>(projects);
      while (projects.Count > 0)
      {
        var randomIndex = random.Next(0, list.Count);
        var randomItem = list[randomIndex];
        // Remove the item from the list and push it to the top of the deck.
        list.RemoveAt(randomIndex);
        stack.Push(randomItem);
      }

      return stack;
    }
  }
}

