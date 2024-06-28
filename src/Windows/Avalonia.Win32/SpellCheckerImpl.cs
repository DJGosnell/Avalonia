using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
using Avalonia.Platform.Spelling;
using Windows.Win32.Foundation;
using Windows.Win32.Globalization;

namespace Avalonia.Win32;
internal unsafe class SpellCheckerImpl : Platform.Spelling.ISpellChecker
{
    private ISpellCheckerFactory? _spellCheckerFactory;
    private Windows.Win32.Globalization.ISpellChecker? _spellChecker;
    private bool _isInitialized;
    private ConcurrentBag<List<SpellCheckError>> _errorListCache = new ConcurrentBag<List<SpellCheckError>>();
    public SpellCheckerImpl()
    {
        
    }

    private void Initialize(bool setLanguage)
    {
        if (_isInitialized)
            return;

        _isInitialized = true;

        // ReSharper disable once SuspiciousTypeConversion.Global
        _spellCheckerFactory = new SpellCheckerFactory() as ISpellCheckerFactory;

        // Try initializing the current language.
        TryInitializeLanguage(CultureInfo.CurrentCulture.Name);
    }

    public bool TryInitializeLanguage(string language)
    {
        Initialize(false);

        if (_spellChecker != null || _spellCheckerFactory == null)
            return false;

        if (!IsLanguageSupported(language))
            return false;

        fixed (char* pLanguage = language)
        {
            _spellChecker = _spellCheckerFactory.CreateSpellChecker(pLanguage);
        }

        return true;
    }

    public bool IsLanguageSupported(string language)
    {
        fixed (char* pLanguage = language)
        {
            return _spellCheckerFactory?.IsSupported(new PCWSTR(pLanguage)) == new BOOL(true);
        }
    }

    public SpellCheckError[] SpellCheck(string input)
    {
        if (_spellChecker == null)
            throw new InvalidOperationException("Spell Check is not initialized.");

        if(!_errorListCache.TryTake(out var errorList))
            errorList = new List<SpellCheckError>();

        fixed (char* pInput = input)
        {
            var pcwInput = new PCWSTR(pInput);

            var errors = _spellChecker.Check(pcwInput);

            while (true)
            {
                if (errors.Next(out var error).ThrowOnFailure() == HRESULT.S_FALSE)
                {
                    break;
                }

                switch (error.CorrectiveAction)
                {
                    case CORRECTIVE_ACTION.CORRECTIVE_ACTION_NONE:
                    case CORRECTIVE_ACTION.CORRECTIVE_ACTION_GET_SUGGESTIONS:
                    case CORRECTIVE_ACTION.CORRECTIVE_ACTION_DELETE:
                        errorList.Add(new SpellCheckError()
                        {
                            StartIndex = (int)error.StartIndex,
                            Length = (int)error.Length,
                            Action = (SpellCheckCorrectiveAction)error.CorrectiveAction,
                            Replace = null,
                        });
                        break;
                    case CORRECTIVE_ACTION.CORRECTIVE_ACTION_REPLACE:
                        // Replace allocates a replacement word which needs to be freed.

                        var replacement = error.Replacement;

                        errorList.Add(new SpellCheckError()
                        {
                            StartIndex = (int)error.StartIndex,
                            Length = (int)error.Length,
                            Action = (SpellCheckCorrectiveAction)error.CorrectiveAction,
                            Replace = replacement.ToString(),
                        });
                        
                        PInvoke.CoTaskMemFree(error.Replacement);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        var errorArray = errorList.ToArray();
        errorList.Clear();

        _errorListCache.Add(errorList);

        return errorArray;

    }
}
