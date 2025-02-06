using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTranslation.Translators
{
    public class Translator_DeepSeek : Translator_ChatGPT
    {
        public override string Name => "DeepSeek";
        protected override string modelsUrl => "https://api.openai.com/v1/models";
        protected override string chatUrl => "https://api.deepseek.com/chat/completions";
    }
}
