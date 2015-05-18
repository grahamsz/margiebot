﻿using System;
using System.Net;
using System.Text.RegularExpressions;
using Bazam.NoobWebClient;
using MargieBot.MessageProcessors;
using MargieBot.Models;
using Newtonsoft.Json.Linq;

namespace MargieBot.ExampleResponseProcessors.ResponseProcessors
{
    public class WikipediaResponseProcessor : IResponseProcessor
    {
        private const string WIKI_SINGLEWORD_REGEX = @"\b\b(wiki|wikipedia)\b\s+(?<term>\w+)";
        private const string WIKI_MULTIWORD_REGEX = @"\b(wiki|wikipedia)\b\s+""(?<term>[\s\S]+)""";

        public bool CanRespond(ResponseContext context)
        {
            return context.Message.MentionsBot && (Regex.IsMatch(context.Message.Text, WIKI_MULTIWORD_REGEX) || Regex.IsMatch(context.Message.Text, WIKI_SINGLEWORD_REGEX));
        }

        public BotMessage GetResponse(ResponseContext context)
        {
            Match match = Regex.Match(context.Message.Text, WIKI_MULTIWORD_REGEX);
            string searchTerm = string.Empty;
            if (match != null) {
                searchTerm = match.Groups["term"].Value;
            }
            else {
                match = Regex.Match(context.Message.Text, WIKI_SINGLEWORD_REGEX);
                searchTerm = match.Groups["term"].Value;
            }
            string requestUrl = string.Format("http://en.wikipedia.org/w/api.php?action=query&list=search&format=json&prop=extracts&exintro=&explaintext=&srsearch={0}&utf8=&continue=", WebUtility.UrlEncode(searchTerm.Trim()));
            string response = new NoobWebClient().GetResponse(requestUrl, RequestMethod.Get).GetAwaiter().GetResult();
            JObject responseData = JObject.Parse(response);

            if (responseData["query"] != null && responseData["query"]["searchinfo"] != null) {
                int totalHits = responseData["query"]["searchinfo"]["totalhits"].Value<int>();

                if (totalHits > 0) {
                    string articleTitle = responseData["query"]["search"][0]["title"].Value<string>();
                    string articleRequestUrl = "https://en.wikipedia.org/w/api.php?format=json&action=query&prop=extracts&exintro=&explaintext=&titles=" + WebUtility.UrlEncode(articleTitle);
                    string articleResponse = new NoobWebClient().GetResponse(articleRequestUrl, RequestMethod.Get).GetAwaiter().GetResult();
                    JObject articleData = JObject.Parse(articleResponse);

                    if (articleData["query"]["pages"]["-1"] == null) {
                        return new BotMessage() {
                            Text = "Awwww yeah. I know all about that. Check it, y'all!: " + string.Format("http://en.wikipedia.org/wiki/{0}", articleTitle.Replace("_", string.Empty)) + " > " + articleData["query"]["pages"][0]["extract"]
                        };
                    }
                }
            }

            return new BotMessage() {
                Text = "I never heard of that, which isn't all that surprisin'. What IS surprisin' is that neither has Wikipedia. Have you been hangin' out behind the barn again with Skeeterbot?"
            };
        }
    }
}