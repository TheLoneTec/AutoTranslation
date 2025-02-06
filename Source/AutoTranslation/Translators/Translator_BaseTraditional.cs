using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTranslation.Translators
{
    public abstract class Translator_BaseTraditional : ITranslator
    {
        public abstract string Name { get; }
        public bool Ready { get; set; }
        public virtual bool RequiresKey { get; } = true;

        public virtual string StartLanguage => "auto";
        public abstract string TranslateLanguage { get; }

        public abstract void Prepare();

        public abstract bool TryTranslate(string text, out string translated);

        public abstract bool SupportsCurrentLanguage();
    }
}
