using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Platform.Spelling;

[NotClientImplementable]
public interface ISpellChecker
{
    bool TryInitializeLanguage(string language);
    bool IsLanguageSupported(string language); 
    SpellCheckError[] SpellCheck(string input);
}
