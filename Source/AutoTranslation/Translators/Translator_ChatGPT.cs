using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace AutoTranslation.Translators
{
    public class Translator_ChatGPT : Translator_BaseOnlineAIModel
    {
        public override string Name => "ChatGPT";
        protected virtual string modelsUrl => "https://api.openai.com/v1/models";
        protected virtual string chatUrl => "https://api.openai.com/v1/chat/completions";
        public override List<string> GetModels()
        {
            try
            {
                var request = WebRequest.Create(modelsUrl);
                request.Method = "GET";
                request.Headers.Add("Authorization", "Bearer " + APIKey);

                var raw = request.GetResponseAndReadText();
                var models = raw.GetStringValuesFromJson("id");

                return models;
            }
            catch (Exception e)
            {
                Messages.Message("AT_Message_FailedToGetModels".Translate() + e.Message, MessageTypeDefOf.NegativeEvent);
                return null;
            }
        }

        protected override string GetResponseUnsafe(string text)
        {
            var requestBody = $@"{{
                ""model"": ""{Model}"",
                ""messages"": [
                  {{
                    ""role"": ""system"",
                    ""content"": ""{Prompt.EscapeJsonString()}""
                  }},
                  {{
                    ""role"": ""user"",
                    ""content"": ""{text.EscapeJsonString()}""
                  }}
                ]
            }}";

            var request = WebRequest.Create(chatUrl);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add("Authorization", "Bearer " + APIKey);

            using (var sw = new StreamWriter(request.GetRequestStream()))
            {
                sw.Write(requestBody);
            }

            return request.GetResponseAndReadText();
        }
    }
}
