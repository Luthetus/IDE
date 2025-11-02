using Clair.Common.RazorLib.Dropdowns.Models;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.Common.RazorLib.JavaScriptObjects.Models;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.Menus.Displays;
using Clair.Common.RazorLib.Menus.Models;
using Clair.CompilerServices.CSharp.BinderCase;
using Clair.CompilerServices.CSharp.ParserCase;
using Clair.Extensions.CompilerServices;
using Clair.Extensions.CompilerServices.Displays;
using Clair.Extensions.CompilerServices.Syntax;
using Clair.Extensions.CompilerServices.Syntax.Interfaces;
using Clair.Extensions.CompilerServices.Syntax.NodeReferences;
using Clair.Extensions.CompilerServices.Syntax.NodeValues;
using Clair.TextEditor.RazorLib;
using Clair.TextEditor.RazorLib.Autocompletes.Models;
using Clair.TextEditor.RazorLib.CompilerServices;
using Clair.TextEditor.RazorLib.Events.Models;
using Clair.TextEditor.RazorLib.Keymaps.Models.Defaults;
using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.TextEditor.RazorLib.TextEditors.Displays.Internals;
using Clair.TextEditor.RazorLib.TextEditors.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models.Internals;
using System.Text;

namespace Clair.CompilerServices.CSharp.CompilerServiceCase;

public sealed class CSharpCompilerService : IExtendedCompilerService
{
    // <summary>Public because the RazorCompilerService uses it.</summary>
    public readonly CSharpBinder __CSharpBinder;
    private readonly StreamReaderPooledBufferWrap _streamReaderWrap = new();
    // Where do I want the state...
    private readonly TokenWalkerBuffer _tokenWalkerBuffer = new();
    
    // Service dependencies
    private readonly TextEditorService _textEditorService;
    
    private const string EmptyFileHackForLanguagePrimitiveText = "NotApplicable empty" + " void int char string bool var";
    
    public const int GET_TEXT_BUFFER_SIZE = 32;
    
    public CSharpCompilerService(TextEditorService textEditorService)
    {
        _textEditorService = textEditorService;
        
        __CSharpBinder = new(_textEditorService, this);
        
        var primitiveKeywordsTextFile = new CSharpCompilationUnit(CompilationUnitKind.IndividualFile_AllData);
        
        __CSharpBinder.UpsertCompilationUnit(1, primitiveKeywordsTextFile);

        _safeOnlyUTF8Encoding = new SafeOnlyUTF8Encoding();

        _fileAbsolutePathToIntMap.Add(string.Empty, 1);
        _intToFileAbsolutePathMap.Add(1, string.Empty);
    }
    
    private const int MAX_AUTOCOMPLETE_OPTIONS = 25;
    private SynchronizationContext? _previousSynchronizationContext;
    private AutocompleteContainer? _uiAutocompleteContainer = new AutocompleteContainer(new AutocompleteValue[MAX_AUTOCOMPLETE_OPTIONS]);
    
    public TextEditorService TextEditorService => _textEditorService;

    /// <summary>
    /// unsafe vs safe are duplicates of the same code
    /// Safe implies the "TextEditorEditContext"
    /// </summary>
    private readonly StringBuilder _unsafeGetTextStringBuilder = new();
    private readonly char[] _unsafeGetTextBuffer = new char[GET_TEXT_BUFFER_SIZE];

    /// <summary>
    /// unsafe vs safe are duplicates of the same code
    /// Safe implies the "TextEditorEditContext"
    /// </summary>
    private readonly StringBuilder _safeGetTextStringBuilder = new();
    private readonly char[] _safeGetTextBufferOne = new char[GET_TEXT_BUFFER_SIZE];
    private readonly char[] _safeGetTextBufferTwo = new char[GET_TEXT_BUFFER_SIZE];

    /// <summary>
    /// The currently being parsed file should reflect the TextEditorModel NOT the file system.
    /// Furthermore, long term, all files should reflect their TextEditorModel IF it exists.
    /// 
    /// This is a bit of misnomer because the solution wide parse doesn't set this.
    /// It is specifically a TextEditor based event having led to a parse that sets this.
    /// </summary>
    public (int AbsolutePathId, string Content) _currentFileBeingParsedTuple;

    /// <summary>
    /// This needs to be ensured to be cleared after the solution wide parse.
    /// 
    /// In order to avoid a try-catch-finally per file being parsed,
    /// this is being made public so the DotNetBackgroundTaskApi can guarantee this is cleared
    /// by wrapping the solution wide parse as a whole in a try-catch-finally.
    /// 
    /// The StreamReaderTupleCache does NOT contain this StreamReader.
    /// </summary>
    public (int AbsolutePathId, StreamReaderPooledBuffer Sr) FastParseTuple;

    /// <summary>
    /// int AbsolutePathId, ...
    /// </summary>
    public Dictionary<int, StreamReaderPooledBuffer> StreamReaderTupleCache = new();
    /// <summary>
    /// int AbsolutePathId, ...
    /// 
    /// When you have two text spans that exist in the same file,
    /// and this file is not currently being parsed.
    /// 
    /// You must open 1 additional StreamReader, that reads from the same file
    /// as the existing cached one.
    /// </summary>
    public Dictionary<int, StreamReaderPooledBuffer> StreamReaderTupleCacheBackup = new();
    
    /*public int _poolHit;
    public int _poolMiss;
    public int _poolReturn;*/
    
    // new char[UTF8_MaxCharCount];
    private int UTF8_MaxCharCount => _safeOnlyUTF8Encoding.GetMaxCharCount(StreamReaderPooledBuffer.DefaultBufferSize);

    private readonly Dictionary<string, int> _fileAbsolutePathToIntMap = new();
    private readonly Dictionary<int, string> _intToFileAbsolutePathMap = new();
    private int _fileAbsolutePathIntId = 2;

    public int TryAddFileAbsolutePath(string fileAbsolutePath)
    {
        if (_fileAbsolutePathIntId == 0 || _fileAbsolutePathIntId == 1)
        {
            // TODO: approximately int.MaxValue + int.MinValue files are stored in the dictionary for some reason...
            // if this is a valid reason then it is worth implementing this case.
            //
            // 0 implies null
            // 1 implies empty
            //
            throw new NotImplementedException();
        }

        var fileAbsolutePathIntIdLocal = _fileAbsolutePathIntId++;

        if (_fileAbsolutePathToIntMap.TryAdd(fileAbsolutePath, fileAbsolutePathIntIdLocal))
        {
            _intToFileAbsolutePathMap.Add(fileAbsolutePathIntIdLocal, fileAbsolutePath);
            return fileAbsolutePathIntIdLocal;
        }
        else
        {
            return _fileAbsolutePathToIntMap[fileAbsolutePath];
        }
    }

    public int TryGetFileAbsolutePathToInt(string fileAbsolutePath)
    {
        if (_fileAbsolutePathToIntMap.TryGetValue(fileAbsolutePath, out var intId))
            return intId;

        return 0;
    }

    public string? TryGetIntToFileAbsolutePathMap(int intId)
    {
        if (_intToFileAbsolutePathMap.TryGetValue(intId, out var fileAbsolutePath))
            return fileAbsolutePath;

        return null;
    }

    /// <summary>
    /// Allocate it at the start of the solution wide parse, then null it when done.
    /// </summary>
    public readonly Queue<StreamReaderPooledBuffer> _safe_streamReaderPooledBuffer_Pool = new();

    public StreamReaderPooledBuffer aaa_Rent_StreamReaderPooledBuffer(string path)
    {
        var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, StreamReaderPooledBuffer.DefaultFileStreamBufferSize);

        if (_safe_streamReaderPooledBuffer_Pool.TryDequeue(out var streamReaderPooledBuffer))
        {
            streamReaderPooledBuffer.DiscardBufferedData(fileStream);
            return streamReaderPooledBuffer;
        }

        return new StreamReaderPooledBuffer(
            fileStream,
            _safeOnlyUTF8Encoding,
            byteBuffer: new byte[StreamReaderPooledBuffer.DefaultBufferSize],
            charBuffer: new char[UTF8_MaxCharCount]);
    }

    public StreamReaderPooledBuffer bbb_Rent_StreamReaderPooledBuffer(Stream stream)
    {
        if (_safe_streamReaderPooledBuffer_Pool.TryDequeue(out var streamReaderPooledBuffer))
        {
            streamReaderPooledBuffer.DiscardBufferedData(stream);
            return streamReaderPooledBuffer;
        }

        return new StreamReaderPooledBuffer(
            stream,
            _safeOnlyUTF8Encoding,
            byteBuffer: new byte[StreamReaderPooledBuffer.DefaultBufferSize],
            charBuffer: new char[UTF8_MaxCharCount]);
    }

    public void Return_StreamReaderPooledBuffer(StreamReaderPooledBuffer streamReaderPooledBuffer)
    {
        streamReaderPooledBuffer.DisposeBaseStream();
        _safe_streamReaderPooledBuffer_Pool.Enqueue(streamReaderPooledBuffer);
    }

    /// <summary>
    /// Full clears will NOT return the StreamReaderPooledBuffer to the pool of its kind.
    /// </summary>
    public void FULL_Clear_MAIN_StreamReaderTupleCache()
    {
        foreach (var streamReader in StreamReaderTupleCache.Values)
        {
            streamReader.Dispose();
        }
        StreamReaderTupleCache.Clear();
    }

    /// <summary>
    /// Full clears will NOT return the StreamReaderPooledBuffer to the pool of its kind.
    /// </summary>
    public void FULL_Clear_BACKUP_StreamReaderTupleCache()
    {
        foreach (var streamReader in StreamReaderTupleCacheBackup.Values)
        {
            streamReader.Dispose();
        }
        StreamReaderTupleCacheBackup.Clear();
    }

    /// <summary>
    /// Partial clears WILL return the StreamReaderPooledBuffer to the pool of its kind.
    /// </summary>
    public void PARTIAL_Clear_MAIN_StreamReaderTupleCache()
    {
        foreach (var streamReader in StreamReaderTupleCache.Values)
        {
            Return_StreamReaderPooledBuffer(streamReader);
        }
        StreamReaderTupleCache.Clear();
    }

    /// <summary>
    /// Partial clears WILL return the StreamReaderPooledBuffer to the pool of its kind.
    /// </summary>
    public void PARTIAL_Clear_BACKUP_StreamReaderTupleCache()
    {
        foreach (var streamReader in StreamReaderTupleCacheBackup.Values)
        {
            Return_StreamReaderPooledBuffer(streamReader);
        }
        StreamReaderTupleCacheBackup.Clear();
    }

    public IReadOnlyList<GenericParameter> GenericParameterEntryList => __CSharpBinder.GenericParameterList;
    public IReadOnlyList<FunctionParameter> FunctionParameterEntryList => __CSharpBinder.FunctionParameterList;
    public IReadOnlyList<FunctionArgument> FunctionArgumentEntryList => __CSharpBinder.FunctionArgumentList;
    
    public IReadOnlyList<TypeDefinitionTraits> TypeDefinitionTraitsList => __CSharpBinder.TypeDefinitionTraitsList;
    public IReadOnlyList<FunctionDefinitionTraits> FunctionDefinitionTraitsList => __CSharpBinder.FunctionDefinitionTraitsList;
    public IReadOnlyList<VariableDeclarationTraits> VariableDeclarationTraitsList => __CSharpBinder.VariableDeclarationTraitsList;
    public IReadOnlyList<ConstructorDefinitionTraits> ConstructorDefinitionTraitsList => __CSharpBinder.ConstructorDefinitionTraitsList;

    public const int MAIN_STREAM_READER_CACHE_COUNT_MAX = 360;
    public const int BACKUP_STREAM_READER_CACHE_COUNT_MAX = 140;

    public void RegisterResource(ResourceUri resourceUri, bool shouldTriggerResourceWasModified)
    {
        int absolutePathId = TryGetFileAbsolutePathToInt(resourceUri.Value);
        if (absolutePathId == 0)
            absolutePathId = TryAddFileAbsolutePath(resourceUri.Value);

        __CSharpBinder.UpsertCompilationUnit(absolutePathId, new CSharpCompilationUnit(CompilationUnitKind.IndividualFile_AllData));
            
        if (shouldTriggerResourceWasModified)
            ResourceWasModified(resourceUri, Array.Empty<TextEditorTextSpan>());
    }
    
    public void DisposeResource(ResourceUri resourceUri)
    {
        int absolutePathId = TryGetFileAbsolutePathToInt(resourceUri.Value);
        if (absolutePathId == 0)
            absolutePathId = TryAddFileAbsolutePath(resourceUri.Value);

        __CSharpBinder.RemoveCompilationUnit(absolutePathId);
    }

    public void ResourceWasModified(ResourceUri resourceUri, IReadOnlyList<TextEditorTextSpan> editTextSpansList)
    {
        _textEditorService.WorkerArbitrary.PostUnique(editContext =>
        {
            var modelModifier = editContext.GetModelModifier(resourceUri);

            if (modelModifier is null)
                return ValueTask.CompletedTask;

            return ParseAsync(editContext, modelModifier, shouldApplySyntaxHighlighting: true);
        });
    }

    public ICompilerServiceResource? GetResourceByResourceUri(ResourceUri resourceUri)
    {
        var absolutePathId = TryGetFileAbsolutePathToInt(resourceUri.Value);
        if (absolutePathId == 0)
            return null;

        __CSharpBinder.__CompilationUnitMap.TryGetValue(absolutePathId, out var compilerServiceResource);
        return compilerServiceResource;
    }
    
    public ICompilerServiceResource? GetResourceByAbsolutePathId(int absolutePathId)
    {
        __CSharpBinder.__CompilationUnitMap.TryGetValue(absolutePathId, out var compilerServiceResource);
        return compilerServiceResource;
    }
    
    public MenuContainer GetContextMenu(TextEditorVirtualizationResult virtualizationResult, ContextMenu contextMenu)
    {
        return contextMenu.GetDefaultMenuRecord();
    }

    /// <summary>
    /// unsafe vs safe are duplicates of the same code
    /// Safe implies the "TextEditorEditContext"
    /// </summary>
    public string? UnsafeGetText(int absolutePathId, TextEditorTextSpan textSpan)
    {
        var absolutePathString = TryGetIntToFileAbsolutePathMap(absolutePathId);
        if (absolutePathString is null)
            return null;

        return UnsafeGetText(absolutePathString, textSpan);
    }

    /// <summary>
    /// unsafe vs safe are duplicates of the same code
    /// Safe implies the "TextEditorEditContext"
    /// </summary>
    public string? UnsafeGetText(string absolutePathString, TextEditorTextSpan textSpan)
    {
        if (absolutePathString == string.Empty)
        {
            if (textSpan.EndExclusiveIndex > EmptyFileHackForLanguagePrimitiveText.Length)
                return null;
            return textSpan.GetText(EmptyFileHackForLanguagePrimitiveText, _textEditorService);
        }

        var model = _textEditorService.Model_GetOrDefault(new ResourceUri(absolutePathString));

        if (model is not null)
        {
            // !!!!!!!!!!
            if (textSpan.EndExclusiveIndex > model.AllText.Length)
                return null;
            return textSpan.GetText(model.AllText, _textEditorService);
        }
        else if (File.Exists(absolutePathString))
        {
            using (StreamReaderPooledBuffer sr = new StreamReaderPooledBuffer(
                new FileStream(absolutePathString, FileMode.Open, FileAccess.Read, FileShare.Read, StreamReaderPooledBuffer.DefaultFileStreamBufferSize),
                Encoding.UTF8,
                new byte[StreamReaderPooledBuffer.DefaultBufferSize],
                new char[UTF8_MaxCharCount]))
            {
                // I presume this is needed so the StreamReader can get the encoding.
                sr.Read();

                sr.BaseStream.Seek(textSpan.ByteIndex, SeekOrigin.Begin);
                sr.DiscardBufferedData();

                if (textSpan.Length <= GET_TEXT_BUFFER_SIZE)
                {
                    sr.Read(_unsafeGetTextBuffer, 0, textSpan.Length);
                    return new string(_unsafeGetTextBuffer, 0, textSpan.Length);
                }
                else
                {
                    _unsafeGetTextStringBuilder.Clear();
                    var remainder = textSpan.Length;
                    while (remainder > 0)
                    {
                        var countTryRead = remainder >= GET_TEXT_BUFFER_SIZE ? GET_TEXT_BUFFER_SIZE : remainder;
                        sr.Read(_unsafeGetTextBuffer, 0, countTryRead);
                        remainder -= countTryRead; // not necessarily the actual, perhaps a check that sr.Read(...) returns the correct amount of characters is could be useful?
                        for (int i = 0; i < countTryRead; i++)
                        {
                            _unsafeGetTextStringBuilder.Append(_unsafeGetTextBuffer[i]);
                        }
                    }
                    return _unsafeGetTextStringBuilder.ToString();
                }
            }
        }
        
        return null;
    }

    /// <summary>
    /// unsafe vs safe are duplicates of the same code
    /// Safe implies the "TextEditorEditContext"
    /// </summary>
    public string? SafeGetText(int absolutePathId, TextEditorTextSpan textSpan)
    {
        StreamReaderPooledBuffer sr;

        if (absolutePathId == ResourceUri.EmptyAbsolutePathId)
        {
            if (textSpan.EndExclusiveIndex > EmptyFileHackForLanguagePrimitiveText.Length)
                return null;
            return textSpan.GetText(EmptyFileHackForLanguagePrimitiveText, _textEditorService);
        }
        else if (absolutePathId == _currentFileBeingParsedTuple.AbsolutePathId)
        {
            if (textSpan.EndExclusiveIndex > _currentFileBeingParsedTuple.Content.Length)
                return null;
            return textSpan.GetText(_currentFileBeingParsedTuple.Content, _textEditorService);
        }
        else if (absolutePathId == FastParseTuple.AbsolutePathId)
        {
            // TODO: What happens if I split a multibyte word?
            FastParseTuple.Sr.BaseStream.Seek(textSpan.ByteIndex, SeekOrigin.Begin);
            FastParseTuple.Sr.DiscardBufferedData();

            if (textSpan.Length <= GET_TEXT_BUFFER_SIZE)
            {
                FastParseTuple.Sr.Read(_safeGetTextBufferOne, 0, textSpan.Length);
                return new string(_safeGetTextBufferOne, 0, textSpan.Length);
            }
            else
            {
                _safeGetTextStringBuilder.Clear();
                var remainder = textSpan.Length;
                while (remainder > 0)
                {
                    var countTryRead = remainder >= GET_TEXT_BUFFER_SIZE ? GET_TEXT_BUFFER_SIZE : remainder;
                    FastParseTuple.Sr.Read(_safeGetTextBufferOne, 0, countTryRead);
                    remainder -= countTryRead; // not necessarily the actual, perhaps a check that sr.Read(...) returns the correct amount of characters is could be useful?
                    for (int i = 0; i < countTryRead; i++)
                    {
                        _safeGetTextStringBuilder.Append(_safeGetTextBufferOne[i]);
                    }
                }
                return _safeGetTextStringBuilder.ToString();
            }
        }

        if (!StreamReaderTupleCache.TryGetValue(absolutePathId, out sr))
        {
            var absolutePathString = TryGetIntToFileAbsolutePathMap(absolutePathId);
            if (absolutePathString is null)
                return null;

            if (!File.Exists(absolutePathString))
                return null;
        
            sr = aaa_Rent_StreamReaderPooledBuffer(absolutePathString);
            // Solution wide parse on Clair.sln
            //
            // 350 -> _countCacheClear: 15
            // 450 -> _countCacheClear: 9
            // 500 -> _countCacheClear: 7
            // 800 -> _countCacheClear: 2
            // 1000 -> _countCacheClear: 0
            //
            // 512 is c library limit?
            // 1024 is linux DEFAULT soft limit?
            // The reality is that you can go FAR higher when not limited?
            // But how do I know the limit of each user?
            // So I guess 500 is a safe bet for now?
            //
            // CSharpCompilerService at ~2k lines of text needed `StreamReaderTupleCache.Count: 214`.
            // ParseExpressions at ~4k lines of text needed `StreamReaderTupleCache.Count: 139`.
            //
            // This isn't just used for single file parsing though, it is also used for solution wide.
            if (StreamReaderTupleCache.Count >= MAIN_STREAM_READER_CACHE_COUNT_MAX)
            {
                PARTIAL_Clear_MAIN_StreamReaderTupleCache();
            }

            StreamReaderTupleCache.Add(absolutePathId, sr);
            
            // I presume this is needed so the StreamReader can get the encoding.
            sr.Read();
        }

        // TODO: What happens if I split a multibyte word?
        sr.BaseStream.Seek(textSpan.ByteIndex, SeekOrigin.Begin);
        sr.DiscardBufferedData();
        
        if (textSpan.Length <= GET_TEXT_BUFFER_SIZE)
        {
            sr.Read(_safeGetTextBufferOne, 0, textSpan.Length);
            return new string(_safeGetTextBufferOne, 0, textSpan.Length);
        }
        else
        {
            _safeGetTextStringBuilder.Clear();
            var remainder = textSpan.Length;
            while (remainder > 0)
            {
                var countTryRead = remainder >= GET_TEXT_BUFFER_SIZE ? GET_TEXT_BUFFER_SIZE : remainder;
                sr.Read(_safeGetTextBufferOne, 0, countTryRead);
                remainder -= countTryRead; // not necessarily the actual, perhaps a check that sr.Read(...) returns the correct amount of characters is could be useful?
                for (int i = 0; i < countTryRead; i++)
                {
                    _safeGetTextStringBuilder.Append(_safeGetTextBufferOne[i]);
                }
            }
            return _safeGetTextStringBuilder.ToString();
        }
    }
    
    public bool SafeCompareText(int absolutePathId, string value, TextEditorTextSpan textSpan)
    {
        if (value.Length != textSpan.Length)
            return false;

        StreamReaderPooledBuffer sr;

        if (absolutePathId == 1)
        {
            if (textSpan.EndExclusiveIndex > EmptyFileHackForLanguagePrimitiveText.Length)
                return false;
            return value == textSpan.GetText(EmptyFileHackForLanguagePrimitiveText, _textEditorService);
            // The object allocation counts are nearly identical when I swap to using this code that compares
            // each character.
            //
            // Even odder, the counts actually end up on the slightly larger side (although incredibly minorly so).
            //
            // It seems there are a lot of SafeGetText(...) invocations that I'm still making, and that the majority
            // of string allocations are coming from those, not these sub cases?
            //
            /*for (int i = 0; i < textSpan.Length; i++)
            {
                
                if (value[i] != EmptyFileHackForLanguagePrimitiveText[textSpan.StartInclusiveIndex + i])
                    return false;
            }
            return true;*/
        }
        else if (absolutePathId == _currentFileBeingParsedTuple.AbsolutePathId)
        {
            if (textSpan.EndExclusiveIndex > _currentFileBeingParsedTuple.Content.Length)
                return false;
            return value == textSpan.GetText(_currentFileBeingParsedTuple.Content, _textEditorService);
            // The object allocation counts are nearly identical when I swap to using this code that compares
            // each character.
            //
            // Even odder, the counts actually end up on the slightly larger side (although incredibly minorly so).
            //
            // It seems there are a lot of SafeGetText(...) invocations that I'm still making, and that the majority
            // of string allocations are coming from those, not these sub cases?
            //
            /*
            for (int i = 0; i < textSpan.Length; i++)
            {

                if (value[i] != _currentFileBeingParsedTuple.Content[textSpan.StartInclusiveIndex + i])
                    return false;
            }
            return true;*/
        }
        else if (absolutePathId == FastParseTuple.AbsolutePathId)
        {
            // TODO: What happens if I split a multibyte word?
            FastParseTuple.Sr.BaseStream.Seek(textSpan.ByteIndex, SeekOrigin.Begin);
            FastParseTuple.Sr.DiscardBufferedData();

            if (textSpan.Length <= GET_TEXT_BUFFER_SIZE)
            {
                FastParseTuple.Sr.Read(_safeGetTextBufferOne, 0, textSpan.Length);
                for (int i = 0; i < textSpan.Length; i++)
                {
                    if (value[i] != _safeGetTextBufferOne[i])
                        return false;
                }
            }
            else
            {
                var remainder = textSpan.Length;
                while (remainder > 0)
                {
                    var countTryRead = remainder >= GET_TEXT_BUFFER_SIZE ? GET_TEXT_BUFFER_SIZE : remainder;
                    FastParseTuple.Sr.Read(_safeGetTextBufferOne, 0, countTryRead);
                    var offset = textSpan.Length - remainder;
                    for (int i = 0; i < countTryRead; i++)
                    {
                        if (value[offset + i] != _safeGetTextBufferOne[i])
                            return false;
                    }
                    remainder -= countTryRead; // not necessarily the actual, perhaps a check that sr.Read(...) returns the correct amount of characters is could be useful?
                }
            }

            return true;
        }
        
        if (!StreamReaderTupleCache.TryGetValue(absolutePathId, out sr))
        {
            var absolutePathString = TryGetIntToFileAbsolutePathMap(absolutePathId);
            if (absolutePathString is null)
                return false;

            if (!File.Exists(absolutePathString))
                return false;
        
            sr = aaa_Rent_StreamReaderPooledBuffer(absolutePathString);
            // Solution wide parse on Clair.sln
            //
            // 350 -> _countCacheClear: 15
            // 450 -> _countCacheClear: 9
            // 500 -> _countCacheClear: 7
            // 800 -> _countCacheClear: 2
            // 1000 -> _countCacheClear: 0
            //
            // 512 is c library limit?
            // 1024 is linux DEFAULT soft limit?
            // The reality is that you can go FAR higher when not limited?
            // But how do I know the limit of each user?
            // So I guess 500 is a safe bet for now?
            //
            // CSharpCompilerService at ~2k lines of text needed `StreamReaderTupleCache.Count: 214`.
            // ParseExpressions at ~4k lines of text needed `StreamReaderTupleCache.Count: 139`.
            //
            // This isn't just used for single file parsing though, it is also used for solution wide.
            if (StreamReaderTupleCache.Count >= MAIN_STREAM_READER_CACHE_COUNT_MAX)
            {
                PARTIAL_Clear_MAIN_StreamReaderTupleCache();
            }

            StreamReaderTupleCache.Add(absolutePathId, sr);
            
            // I presume this is needed so the StreamReader can get the encoding.
            sr.Read();
        }

        // TODO: What happens if I split a multibyte word?
        sr.BaseStream.Seek(textSpan.ByteIndex, SeekOrigin.Begin);
        sr.DiscardBufferedData();

        if (textSpan.Length <= GET_TEXT_BUFFER_SIZE)
        {
            sr.Read(_safeGetTextBufferOne, 0, textSpan.Length);
            for (int i = 0; i < textSpan.Length; i++)
            {
                if (value[i] != _safeGetTextBufferOne[i])
                    return false;
            }
        }
        else
        {
            var remainder = textSpan.Length;
            while (remainder > 0)
            {
                var countTryRead = remainder >= GET_TEXT_BUFFER_SIZE ? GET_TEXT_BUFFER_SIZE : remainder;
                sr.Read(_safeGetTextBufferOne, 0, countTryRead);
                var offset = textSpan.Length - remainder;
                for (int i = 0; i < countTryRead; i++)
                {
                    if (value[offset + i] != _safeGetTextBufferOne[i])
                        return false;
                }
                remainder -= countTryRead; // not necessarily the actual, perhaps a check that sr.Read(...) returns the correct amount of characters is could be useful?
            }
        }

        return true;
    }
    
    public bool SafeCompareTextSpans(int sourceAbsolutePathId, TextEditorTextSpan sourceTextSpan, int otherAbsolutePathId, TextEditorTextSpan otherTextSpan)
    {
        if (sourceTextSpan.Length != otherTextSpan.Length ||
            sourceTextSpan.CharIntSum != otherTextSpan.CharIntSum)
        {
            return false;
        }

        var length = otherTextSpan.Length;

        if (sourceAbsolutePathId == FastParseTuple.AbsolutePathId)
        {
            FastParseTuple.Sr.BaseStream.Seek(sourceTextSpan.ByteIndex, SeekOrigin.Begin);
            FastParseTuple.Sr.DiscardBufferedData();

            // string.Empty as file path is primitive keywords hack.
            if (otherAbsolutePathId == ResourceUri.EmptyAbsolutePathId)
            {
                if (otherTextSpan.StartInclusiveIndex + (length - 1) >= EmptyFileHackForLanguagePrimitiveText.Length)
                    return false;
            
                if (length <= GET_TEXT_BUFFER_SIZE)
                {
                    FastParseTuple.Sr.Read(_safeGetTextBufferOne, 0, length);
                    for (int i = 0; i < length; i++)
                    {
                        if (_safeGetTextBufferOne[i] !=
                            EmptyFileHackForLanguagePrimitiveText[otherTextSpan.StartInclusiveIndex + i])
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    var remainder = length;
                    while (remainder > 0)
                    {
                        var countTryRead = remainder >= GET_TEXT_BUFFER_SIZE ? GET_TEXT_BUFFER_SIZE : remainder;
                        FastParseTuple.Sr.Read(_safeGetTextBufferOne, 0, countTryRead);
                        var offset = length - remainder;
                        for (int i = 0; i < countTryRead; i++)
                        {
                            if (EmptyFileHackForLanguagePrimitiveText[otherTextSpan.StartInclusiveIndex + offset + i] != _safeGetTextBufferOne[i])
                                return false;
                        }
                        remainder -= countTryRead; // not necessarily the actual, perhaps a check that sr.Read(...) returns the correct amount of characters is could be useful?
                    }
                }
            }
            else
            {
                // StreamReader cache does not contain the FastParseTuple.Sr
                var otherSr = GetOtherStreamReader(otherAbsolutePathId, otherTextSpan);
                if (otherSr is null)
                    return false;

                if (length <= GET_TEXT_BUFFER_SIZE)
                {
                    FastParseTuple.Sr.Read(_safeGetTextBufferOne, 0, length);
                    otherSr.Read(_safeGetTextBufferTwo, 0, length);
                    for (int i = 0; i < length; i++)
                    {
                        if (_safeGetTextBufferOne[i] != _safeGetTextBufferTwo[i])
                            return false;
                    }
                }
                else
                {
                    var remainder = length;
                    while (remainder > 0)
                    {
                        var countTryRead = remainder >= GET_TEXT_BUFFER_SIZE ? GET_TEXT_BUFFER_SIZE : remainder;
                        FastParseTuple.Sr.Read(_safeGetTextBufferOne, 0, countTryRead);
                        otherSr.Read(_safeGetTextBufferTwo, 0, countTryRead);
                        for (int i = 0; i < countTryRead; i++)
                        {
                            if (_safeGetTextBufferOne[i] != _safeGetTextBufferTwo[i])
                                return false;
                        }
                        remainder -= countTryRead; // not necessarily the actual, perhaps a check that sr.Read(...) returns the correct amount of characters is could be useful?
                    }
                }
            }
        }
        else if (sourceAbsolutePathId == _currentFileBeingParsedTuple.AbsolutePathId)
        {
            // string.Empty as file path is primitive keywords hack.
            if (otherAbsolutePathId == ResourceUri.EmptyAbsolutePathId)
            {
                if (sourceTextSpan.StartInclusiveIndex + (length - 1) >= _currentFileBeingParsedTuple.Content.Length)
                    return false;
                if (otherTextSpan.StartInclusiveIndex + (length - 1) >= EmptyFileHackForLanguagePrimitiveText.Length)
                    return false;
                    
                for (int i = 0; i < length; i++)
                {
                    if (_currentFileBeingParsedTuple.Content[sourceTextSpan.StartInclusiveIndex + i] !=
                        EmptyFileHackForLanguagePrimitiveText[otherTextSpan.StartInclusiveIndex + i])
                    {
                        return false;
                    }
                }
            }
            else if (otherAbsolutePathId == _currentFileBeingParsedTuple.AbsolutePathId)
            {
                if (sourceTextSpan.StartInclusiveIndex + (length - 1) >= _currentFileBeingParsedTuple.Content.Length)
                    return false;
                if (otherTextSpan.StartInclusiveIndex + (length - 1) >= _currentFileBeingParsedTuple.Content.Length)
                    return false;
            
                for (int i = 0; i < length; i++)
                {
                    if (_currentFileBeingParsedTuple.Content[sourceTextSpan.StartInclusiveIndex + i] !=
                        _currentFileBeingParsedTuple.Content[otherTextSpan.StartInclusiveIndex + i])
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (sourceTextSpan.StartInclusiveIndex + (length - 1) >= _currentFileBeingParsedTuple.Content.Length)
                    return false;
                    
                var otherSr = GetOtherStreamReader(otherAbsolutePathId, otherTextSpan);
                if (otherSr is null)
                    return false;
                
                if (length <= GET_TEXT_BUFFER_SIZE)
                {
                    otherSr.Read(_safeGetTextBufferTwo, 0, length);
                    for (int i = 0; i < length; i++)
                    {
                        if (_currentFileBeingParsedTuple.Content[sourceTextSpan.StartInclusiveIndex + i] != _safeGetTextBufferTwo[i])
                            return false;
                    }
                }
                else
                {
                    var remainder = length;
                    while (remainder > 0)
                    {
                        var countTryRead = remainder >= GET_TEXT_BUFFER_SIZE ? GET_TEXT_BUFFER_SIZE : remainder;
                        otherSr.Read(_safeGetTextBufferTwo, 0, countTryRead);
                        var offset = length - remainder;
                        for (int i = 0; i < countTryRead; i++)
                        {
                            if (_currentFileBeingParsedTuple.Content[sourceTextSpan.StartInclusiveIndex + offset + i] != _safeGetTextBufferTwo[i])
                                return false;
                        }
                        remainder -= countTryRead; // not necessarily the actual, perhaps a check that sr.Read(...) returns the correct amount of characters is could be useful?
                    }
                }
            }
        }
        else
        {
            var sourceSr = GetOtherStreamReader(sourceAbsolutePathId, sourceTextSpan);
            if (sourceSr is null)
                return false;

            var otherSr = GetBackupStreamReader(otherAbsolutePathId, otherTextSpan);
            if (otherSr is null)
                return false;
            
            if (length <= GET_TEXT_BUFFER_SIZE)
            {
                sourceSr.Read(_safeGetTextBufferOne, 0, length);
                otherSr.Read(_safeGetTextBufferTwo, 0, length);
                for (int i = 0; i < length; i++)
                {
                    if (_safeGetTextBufferOne[i] != _safeGetTextBufferTwo[i])
                        return false;
                }
            }
            else
            {
                var remainder = length;
                while (remainder > 0)
                {
                    var countTryRead = remainder >= GET_TEXT_BUFFER_SIZE ? GET_TEXT_BUFFER_SIZE : remainder;
                    sourceSr.Read(_safeGetTextBufferOne, 0, countTryRead);
                    otherSr.Read(_safeGetTextBufferTwo, 0, countTryRead);
                    for (int i = 0; i < countTryRead; i++)
                    {
                        if (_safeGetTextBufferOne[i] != _safeGetTextBufferTwo[i])
                            return false;
                    }
                    remainder -= countTryRead; // not necessarily the actual, perhaps a check that sr.Read(...) returns the correct amount of characters is could be useful?
                }
            }
        }

        return true;
    }

    private StreamReaderPooledBuffer? GetOtherStreamReader(int otherAbsolutePathId, TextEditorTextSpan otherTextSpan)
    {
        if (!TryGetCachedStreamReader(otherAbsolutePathId, out var otherSr))
            return null;
        otherSr.BaseStream.Seek(otherTextSpan.ByteIndex, SeekOrigin.Begin);
        otherSr.DiscardBufferedData();
        return otherSr;
    }

    private bool TryGetCachedStreamReader(int absolutePathId, out StreamReaderPooledBuffer sr)
    {
        if (!StreamReaderTupleCache.TryGetValue(absolutePathId, out sr))
        {
            var absolutePathString = TryGetIntToFileAbsolutePathMap(absolutePathId);
            if (absolutePathString is null)
                return false;

            if (!File.Exists(absolutePathString))
                return false;

            sr = aaa_Rent_StreamReaderPooledBuffer(absolutePathString);
            // Solution wide parse on Clair.sln
            //
            // 350 -> _countCacheClear: 15
            // 450 -> _countCacheClear: 9
            // 500 -> _countCacheClear: 7
            // 800 -> _countCacheClear: 2
            // 1000 -> _countCacheClear: 0
            //
            // 512 is c library limit?
            // 1024 is linux DEFAULT soft limit?
            // The reality is that you can go FAR higher when not limited?
            // But how do I know the limit of each user?
            // So I guess 500 is a safe bet for now?
            //
            // CSharpCompilerService at ~2k lines of text needed `StreamReaderTupleCache.Count: 214`.
            // ParseExpressions at ~4k lines of text needed `StreamReaderTupleCache.Count: 139`.
            //
            // This isn't just used for single file parsing though, it is also used for solution wide.
            if (StreamReaderTupleCache.Count >= MAIN_STREAM_READER_CACHE_COUNT_MAX)
            {
                PARTIAL_Clear_MAIN_StreamReaderTupleCache();
            }

            StreamReaderTupleCache.Add(absolutePathId, sr);

            // I presume this is needed so the StreamReader can get the encoding.
            sr.Read();
        }

        return true;
    }
    
    private bool BACKUP_TryGetCachedStreamReader(int absolutePathId, out StreamReaderPooledBuffer sr)
    {
        if (!StreamReaderTupleCacheBackup.TryGetValue(absolutePathId, out sr))
        {
            var absolutePathString = TryGetIntToFileAbsolutePathMap(absolutePathId);
            if (absolutePathString is null)
                return false;

            if (!File.Exists(absolutePathString))
                return false;

            sr = aaa_Rent_StreamReaderPooledBuffer(absolutePathString);
            // Solution wide parse on Clair.sln
            //
            // 350 -> _countCacheClear: 15
            // 450 -> _countCacheClear: 9
            // 500 -> _countCacheClear: 7
            // 800 -> _countCacheClear: 2
            // 1000 -> _countCacheClear: 0
            //
            // 512 is c library limit?
            // 1024 is linux DEFAULT soft limit?
            // The reality is that you can go FAR higher when not limited?
            // But how do I know the limit of each user?
            // So I guess 500 is a safe bet for now?
            //
            // CSharpCompilerService at ~2k lines of text needed `StreamReaderTupleCacheBackup.Count: 214`.
            // ParseExpressions at ~4k lines of text needed `StreamReaderTupleCacheBackup.Count: 139`.
            //
            // This isn't just used for single file parsing though, it is also used for solution wide.
            if (StreamReaderTupleCacheBackup.Count >= BACKUP_STREAM_READER_CACHE_COUNT_MAX)
            {
                PARTIAL_Clear_BACKUP_StreamReaderTupleCache();
            }

            StreamReaderTupleCacheBackup.Add(absolutePathId, sr);

            // I presume this is needed so the StreamReader can get the encoding.
            sr.Read();
        }

        return true;
    }

    private StreamReaderPooledBuffer? GetBackupStreamReader(int otherAbsolutePathId, TextEditorTextSpan otherTextSpan)
    {
        if (!BACKUP_TryGetCachedStreamReader(otherAbsolutePathId, out var otherSr))
            return null;
        otherSr.BaseStream.Seek(otherTextSpan.ByteIndex, SeekOrigin.Begin);
        otherSr.DiscardBufferedData();
        return otherSr;
    }

    public AutocompleteContainer? GetAutocompleteMenu(TextEditorVirtualizationResult virtualizationResult, AutocompleteMenu autocompleteMenu)
    {
        AutocompleteContainer autocompleteContainer;
        int writeCount = 0;

        if (_previousSynchronizationContext == SynchronizationContext.Current)
        {
            autocompleteContainer = _uiAutocompleteContainer;
        }
        else
        {
            _previousSynchronizationContext = SynchronizationContext.Current;
            autocompleteContainer = new AutocompleteContainer(new AutocompleteValue[MAX_AUTOCOMPLETE_OPTIONS]);
        }
        
        for (int i = 0; i < MAX_AUTOCOMPLETE_OPTIONS)
        {
            autocompleteContainer.[i] = ;
        }
        
        return autocompleteContainer;
    }
    
    public ValueTask<MenuContainer> GetQuickActionsSlashRefactorMenu(
        TextEditorEditContext editContext,
        TextEditorModel modelModifier,
        TextEditorViewModel viewModelModifier)
    {
        var compilerService = modelModifier.PersistentState.CompilerService;
    
        var compilerServiceResource = viewModelModifier is null
            ? null
            : compilerService.GetResourceByResourceUri(modelModifier.PersistentState.ResourceUri);

        int? primaryCursorPositionIndex = modelModifier is null || viewModelModifier is null
            ? null
            : modelModifier.GetPositionIndex(viewModelModifier);

        ISyntaxNode? syntaxNode = primaryCursorPositionIndex is null || __CSharpBinder is null || compilerServiceResource?.CompilationUnit is null
            ? null
            : null; // __CSharpBinder.GetSyntaxNode(null, primaryCursorPositionIndex.Value, (CSharpCompilationUnit)compilerServiceResource);
            
        var menuOptionList = new List<MenuOptionValue>();
            
        menuOptionList.Add(new MenuOptionValue(
            "QuickActionsSlashRefactorMenu",
            MenuOptionKind.Other));
            
        if (syntaxNode is null)
        {
            menuOptionList.Add(new MenuOptionValue(
                "syntaxNode was null",
                MenuOptionKind.Other,
                onClickFunc: async _ => {}));
        }
        else
        {
            menuOptionList.Add(new MenuOptionValue(
                syntaxNode.SyntaxKind.ToString(),
                MenuOptionKind.Other,
                onClickFunc: async _ => {}));
        }
        
        MenuContainer menu;
        
        if (menuOptionList.Count == 0)
            menu = new MenuContainer();
        else
            menu = new MenuContainer(menuOptionList);
    
        return ValueTask.FromResult(menu);
    }
    
    public async ValueTask OnInspect(
        TextEditorEditContext editContext,
        TextEditorModel modelModifier,
        TextEditorViewModel viewModelModifier,
        double clientX,
        double clientY,
        bool shiftKey,
        bool ctrlKey,
        bool altKey,
        TextEditorComponentData componentData,
        ResourceUri resourceUri)
    {
        var absolutePathId = TryGetFileAbsolutePathToInt(modelModifier.PersistentState.ResourceUri.Value);
        if (absolutePathId == 0 || absolutePathId == 1)
            return;

        // Lazily calculate row and column index a second time. Otherwise one has to calculate it every mouse moved event.
        var lineAndColumnIndex = await EventUtils.CalculateLineAndColumnIndex(
                modelModifier,
                viewModelModifier,
                clientX,
                clientY,
                componentData,
                editContext)
            .ConfigureAwait(false);
    
        var cursorPositionIndex = modelModifier.GetPositionIndex(
            lineAndColumnIndex.LineIndex,
            lineAndColumnIndex.ColumnIndex);

        var foundMatch = false;
        
        var resource = GetResourceByResourceUri(modelModifier.PersistentState.ResourceUri);
        var compilationUnitLocal = (CSharpCompilationUnit)resource.CompilationUnit;
        
        // var diagnostics = compilationUnitLocal.DiagnosticList;

        /*if (diagnostics.Count != 0)
        {
            foreach (var diagnostic in diagnostics)
            {
                if (cursorPositionIndex >= diagnostic.TextSpan.StartInclusiveIndex &&
                    cursorPositionIndex < diagnostic.TextSpan.EndExclusiveIndex)
                {
                    // Prefer showing a diagnostic over a symbol when both exist at the mouse location.
                    foundMatch = true;

                    var parameterMap = new Dictionary<string, object?>
                    {
                        {
                            nameof(Clair.TextEditor.RazorLib.TextEditors.Displays.Internals.DiagnosticDisplay.Diagnostic),
                            diagnostic
                        }
                    };

                    viewModelModifier.PersistentState.TooltipModel = new Clair.Common.RazorLib.Tooltips.Models.TooltipModel<(TextEditorService TextEditorService, Key<TextEditorViewModel> ViewModelKey, int PositionIndex)>(
                        typeof(Clair.TextEditor.RazorLib.TextEditors.Displays.Internals.DiagnosticDisplay),
                        parameterMap,
                        clientX,
                        clientY,
                        cssClassString: null,
                        componentData.ContinueRenderingTooltipAsync,
                        Clair.TextEditor.RazorLib.Commands.Models.Defaults.TextEditorCommandDefaultFunctions.OnWheel,
                        (_textEditorService, viewModelModifier.PersistentState.ViewModelKey, cursorPositionIndex));
                    componentData.TextEditorViewModelSlimDisplay.TextEditorService.CommonService.SetTooltipModel(viewModelModifier.PersistentState.TooltipModel);
                }
            }
        }*/

        if (!foundMatch)
        {
            for (int i = compilationUnitLocal.SymbolOffset; i < compilationUnitLocal.SymbolOffset + compilationUnitLocal.SymbolLength; i++)
            {
                var symbol = __CSharpBinder.SymbolList[i];
                
                if (cursorPositionIndex >= symbol.ToTextSpan().StartInclusiveIndex &&
                    cursorPositionIndex < symbol.ToTextSpan().EndExclusiveIndex)
                {
                    foundMatch = true;

                    var parameters = new Dictionary<string, object?>
                    {
                        {
                            "Symbol",
                            symbol
                        },
                        {
                            "AbsolutePathId",
                            absolutePathId
                        }
                    };

                    viewModelModifier.PersistentState.TooltipModel = new Clair.Common.RazorLib.Tooltips.Models.TooltipModel<(TextEditorService TextEditorService, int ViewModelKey, int PositionIndex)>(
                        typeof(Clair.Extensions.CompilerServices.Displays.SymbolDisplay),
                        parameters,
                        clientX,
                        clientY,
                        cssClassString: null,
                        componentData.ContinueRenderingTooltipAsync,
                        Clair.TextEditor.RazorLib.Commands.Models.Defaults.TextEditorCommandDefaultFunctions.OnWheel,
                        (_textEditorService, viewModelModifier.PersistentState.ViewModelKey, cursorPositionIndex));
                    componentData.TextEditorViewModelSlimDisplay.TextEditorService.CommonService.SetTooltipModel(viewModelModifier.PersistentState.TooltipModel);
                    
                    break;
                }
            }
        }

        if (!foundMatch && viewModelModifier.PersistentState.TooltipModel is not null)
        {
            viewModelModifier.PersistentState.TooltipModel = null;
            componentData.TextEditorViewModelSlimDisplay.TextEditorService.CommonService.SetTooltipModel(viewModelModifier.PersistentState.TooltipModel);
        }

        // TODO: Measure the tooltip, and reposition if it would go offscreen.
    }
    
    public async ValueTask ShowCallingSignature(
        TextEditorEditContext editContext,
        TextEditorModel modelModifier,
        TextEditorViewModel viewModelModifier,
        int positionIndex,
        TextEditorComponentData componentData,
        ResourceUri resourceUri)
    {
        return;
        /*var success = __CSharpBinder.TryGetCompilationUnit(
            cSharpCompilationUnit: null,
            resourceUri,
            out CSharpCompilationUnit compilationUnit);
            
        if (!success)
            return;
        
        var scope = __CSharpBinder.GetScopeByPositionIndex(compilationUnit, resourceUri, positionIndex);
        
        if (!scope.ConstructorWasInvoked)
            return;
        
        if (scope.CodeBlockOwner is null)
            return;
        
        if (!scope.CodeBlockOwner.CodeBlock.ConstructorWasInvoked)
            return;
        
        FunctionInvocationNode? functionInvocationNode = null;
        
        foreach (var childSyntax in scope.CodeBlockOwner.CodeBlock.ChildList)
        {
            if (childSyntax.SyntaxKind == SyntaxKind.ReturnStatementNode)
            {
                var returnStatementNode = (ReturnStatementNode)childSyntax;
                
                if (returnStatementNode.ExpressionNode.SyntaxKind == SyntaxKind.FunctionInvocationNode)
                {
                    functionInvocationNode = (FunctionInvocationNode)returnStatementNode.ExpressionNode;
                    break;
                }
            }
        
            if (functionInvocationNode is not null)
                break;
        
            if (childSyntax.SyntaxKind == SyntaxKind.FunctionInvocationNode)
            {
                functionInvocationNode = (FunctionInvocationNode)childSyntax;
                break;
            }
        }
        
        if (functionInvocationNode is null)
            return;
        
        var foundMatch = false;
        
        var resource = modelModifier.PersistentState.ResourceUri;
        var compilationUnitLocal = compilationUnit;
        
        var symbols = compilationUnitLocal.SymbolList;
        
        var cursorPositionIndex = functionInvocationNode.FunctionInvocationIdentifierToken.TextSpan.StartInclusiveIndex;
        
        var lineAndColumnIndices = modelModifier.GetLineAndColumnIndicesFromPositionIndex(cursorPositionIndex);
        
        var elementPositionInPixels = await _textEditorService.JsRuntimeTextEditorApi
            .GetBoundingClientRect(componentData.PrimaryCursorContentId)
            .ConfigureAwait(false);

        elementPositionInPixels = elementPositionInPixels with
        {
            Top = elementPositionInPixels.Top +
                (.9 * viewModelModifier.CharAndLineMeasurements.LineHeight)
        };
        
        var mouseEventArgs = new MouseEventArgs
        {
            ClientX = elementPositionInPixels.Left,
            ClientY = elementPositionInPixels.Top
        };
            
        var relativeCoordinatesOnClick = new RelativeCoordinates(
            mouseEventArgs.ClientX - viewModelModifier.TextEditorDimensions.BoundingClientRectLeft,
            mouseEventArgs.ClientY - viewModelModifier.TextEditorDimensions.BoundingClientRectTop,
            viewModelModifier.ScrollLeft,
            viewModelModifier.ScrollTop);

        if (!foundMatch && symbols.Count != 0)
        {
            foreach (var symbol in symbols)
            {
                if (cursorPositionIndex >= symbol.TextSpan.StartInclusiveIndex &&
                    cursorPositionIndex < symbol.TextSpan.EndExclusiveIndex &&
                    symbol.SyntaxKind == SyntaxKind.FunctionSymbol)
                {
                    foundMatch = true;

                    var parameters = new Dictionary<string, object?>
                    {
                        {
                            "Symbol",
                            symbol
                        }
                    };

                    viewModelModifier.PersistentState.TooltipViewModel = new(
                        typeof(Clair.Extensions.CompilerServices.Displays.SymbolDisplay),
                        parameters,
                        relativeCoordinatesOnClick,
                        null,
                        componentData.ContinueRenderingTooltipAsync);
                        
                    break;
                }
            }
        }

        if (!foundMatch)
        {
            viewModelModifier.PersistentState.TooltipViewModel = null;
        }
        */
    }
    
    public async ValueTask GoToDefinition(
        TextEditorEditContext editContext,
        TextEditorModel modelModifier,
        TextEditorViewModel viewModelModifier,
        Category category,
        int positionIndex)
    {
        var cursorPositionIndex = positionIndex;

        var foundMatch = false;
        
        var resource = GetResourceByResourceUri(modelModifier.PersistentState.ResourceUri);
        var compilationUnitLocal = (CSharpCompilationUnit)resource.CompilationUnit;
        
        var symbolList = __CSharpBinder.SymbolList.Skip(compilationUnitLocal.SymbolOffset).Take(compilationUnitLocal.SymbolLength).ToList();
        var foundSymbol = default(Symbol);
        
        foreach (var symbol in symbolList)
        {
            if (cursorPositionIndex >= symbol.StartInclusiveIndex &&
                cursorPositionIndex < symbol.EndExclusiveIndex)
            {
                foundMatch = true;
                foundSymbol = symbol;
                break;
            }
        }
        
        if (!foundMatch)
            return;
    
        var symbolLocal = foundSymbol;
        var targetNode = SymbolDisplay.GetTargetNodeValue(_textEditorService, symbolLocal, modelModifier.PersistentState.ResourceUri);
        var definitionNode = SymbolDisplay.GetDefinitionNodeValue(_textEditorService, symbolLocal, targetNode, modelModifier.PersistentState.ResourceUri);
        
        if (definitionNode.IsDefault())
            return;
            
        int absolutePathId = ResourceUri.NullAbsolutePathId;
        var indexInclusiveStart = -1;
        var indexPartialTypeDefinition = -1;
        
        if (definitionNode.SyntaxKind == SyntaxKind.TypeDefinitionNode)
        {
            var typeDefinitionNode = definitionNode;
            absolutePathId = typeDefinitionNode.AbsolutePathId;
            indexInclusiveStart = typeDefinitionNode.IdentifierToken.TextSpan.StartInclusiveIndex;
            var typeDefinitionTraits = TypeDefinitionTraitsList[typeDefinitionNode.TraitsIndex];
            indexPartialTypeDefinition = typeDefinitionTraits.IndexPartialTypeDefinition;
        }
        else if (definitionNode.SyntaxKind == SyntaxKind.VariableDeclarationNode)
        {
            var variableDeclarationNode = definitionNode;
            absolutePathId = variableDeclarationNode.AbsolutePathId;
            indexInclusiveStart = variableDeclarationNode.IdentifierToken.TextSpan.StartInclusiveIndex;
        }
        else if (definitionNode.SyntaxKind == SyntaxKind.NamespaceStatementNode)
        {
            var namespaceStatementNode = definitionNode;
            absolutePathId = namespaceStatementNode.AbsolutePathId;
            indexInclusiveStart = namespaceStatementNode.IdentifierToken.TextSpan.StartInclusiveIndex;
        }
        else if (definitionNode.SyntaxKind == SyntaxKind.FunctionDefinitionNode)
        {
            var functionDefinitionNode = definitionNode;
            absolutePathId = functionDefinitionNode.AbsolutePathId;
            indexInclusiveStart = functionDefinitionNode.IdentifierToken.TextSpan.StartInclusiveIndex;
        }
        else if (definitionNode.SyntaxKind == SyntaxKind.ConstructorDefinitionNode)
        {
            var constructorDefinitionNode = definitionNode;
            absolutePathId = constructorDefinitionNode.AbsolutePathId;
            indexInclusiveStart = constructorDefinitionNode.IdentifierToken.TextSpan.StartInclusiveIndex;
        }
        
        if (absolutePathId == 0 || indexInclusiveStart == -1)
            return;
        
        var definitionAbsolutePathString = TryGetIntToFileAbsolutePathMap(absolutePathId);
        if (definitionAbsolutePathString is null || definitionAbsolutePathString == string.Empty)
            return;
        
        if (indexPartialTypeDefinition == -1)
        {
            if (_textEditorService.CommonService.GetTooltipState().TooltipModel is not null)
            {
                _textEditorService.CommonService.SetTooltipModel(tooltipModel: null);
            }
            
            _textEditorService.WorkerArbitrary.PostUnique(async editContext =>
            {
                if (category.Value == "CodeSearchService")
                {
                    await ((TextEditorKeymapDefault)TextEditorFacts.Keymap_DefaultKeymap).AltF12Func.Invoke(
                        editContext,
                        definitionAbsolutePathString,
                        indexInclusiveStart);
                }
                else
                {
                    await _textEditorService.OpenInEditorAsync(
                            editContext,
                            definitionAbsolutePathString,
                            true,
                            indexInclusiveStart,
                            category,
                            editContext.TextEditorService.NewViewModelKey())
                        .ContinueWith(_ => _textEditorService.ViewModel_StopCursorBlinking());
                }
            });
        }
        else
        {
            var componentData = viewModelModifier.PersistentState.ComponentData;
            if (componentData is null)
                return;
        
            MeasuredHtmlElementDimensions cursorDimensions;
            
            var tooltipState = _textEditorService.CommonService.GetTooltipState();
            
            if (positionIndex != modelModifier.GetPositionIndex(viewModelModifier) &&
                tooltipState.TooltipModel.ItemUntyped is ValueTuple<TextEditorService, Key<TextEditorViewModel>, int>)
            {
                cursorDimensions = new MeasuredHtmlElementDimensions(
                    WidthInPixels: 0,
                    HeightInPixels: 0,
                    LeftInPixels: tooltipState.TooltipModel.X,
                    TopInPixels: tooltipState.TooltipModel.Y,
                    ZIndex: 0);
                _textEditorService.CommonService.SetTooltipModel(tooltipModel: null);
            }
            else
            {
                cursorDimensions = await _textEditorService.CommonService.JsRuntimeCommonApi
                    .MeasureElementById(componentData.PrimaryCursorContentId)
                    .ConfigureAwait(false);
            }
    
            var resourceAbsolutePath = new AbsolutePath(
                modelModifier.PersistentState.ResourceUri.Value,
                false,
                _textEditorService.CommonService.FileSystemProvider,
                tokenBuilder: new StringBuilder(),
                formattedBuilder: new StringBuilder(),
                AbsolutePathNameKind.NameWithExtension);
        
            var siblingFileStringList = new List<(string ResourceUriValue, int ScopeIndexKey)>();
            
            int positionExclusive = indexPartialTypeDefinition;
            while (positionExclusive < __CSharpBinder.PartialTypeDefinitionList.Count)
            {
                if (__CSharpBinder.PartialTypeDefinitionList[positionExclusive].IndexStartGroup == indexPartialTypeDefinition)
                {
                    var absolutePathString = TryGetIntToFileAbsolutePathMap(__CSharpBinder.PartialTypeDefinitionList[positionExclusive].AbsolutePathId);
                    if (absolutePathString is not null)
                    {
                        siblingFileStringList.Add(
                            (
                                absolutePathString,
                                __CSharpBinder.PartialTypeDefinitionList[positionExclusive].ScopeSubIndex
                            ));
                    }
                    positionExclusive++;
                }
                else
                {
                    break;
                }
            }
            
            var menuOptionList = new List<MenuOptionValue>();
            
            siblingFileStringList = siblingFileStringList.OrderBy(x => x).ToList();
            
            var initialActiveMenuOptionRecordIndex = -1;
            
            for (int i = 0; i < siblingFileStringList.Count; i++)
            {
                var tuple = siblingFileStringList[i];
                var file = tuple.ResourceUriValue;
                
                var siblingAbsolutePath = new AbsolutePath(file, false, _textEditorService.CommonService.FileSystemProvider, tokenBuilder: new StringBuilder(), formattedBuilder: new StringBuilder(), AbsolutePathNameKind.NameWithExtension);
                
                menuOptionList.Add(new MenuOptionValue(
                    siblingAbsolutePath.Name,
                    MenuOptionKind.Other,
                    onClickFunc: async _ => 
                    {
                        int? positionIndex = null;
                        
                        if (__CSharpBinder.__CompilationUnitMap.TryGetValue(absolutePathId, out var innerCompilationUnit))
                        {
                            SyntaxNodeValue otherTypeDefinitionNode = default;
                            
                            for (int i = innerCompilationUnit.NodeOffset; i < innerCompilationUnit.NodeOffset + innerCompilationUnit.NodeLength; i++)
                            {
                                var x = __CSharpBinder.NodeList[i];
                                
                                if (x.SyntaxKind == SyntaxKind.TypeDefinitionNode &&
                                    x.SelfScopeSubIndex == tuple.ScopeIndexKey)
                                {
                                    otherTypeDefinitionNode = x;
                                    break;
                                }
                            }
                            
                            if (!otherTypeDefinitionNode.IsDefault())
                            {
                                var typeDefinitionNode = otherTypeDefinitionNode;
                                positionIndex = typeDefinitionNode.IdentifierToken.TextSpan.StartInclusiveIndex;
                            }
                        }
                    
                        _textEditorService.WorkerArbitrary.PostUnique(async editContext =>
                        {
                            if (category.Value == "CodeSearchService")
                            {
                                await ((TextEditorKeymapDefault)TextEditorFacts.Keymap_DefaultKeymap).AltF12Func.Invoke(
                                    editContext,
                                    file,
                                    positionIndex);
                            }
                            else
                            {
                                await _textEditorService.OpenInEditorAsync(
                                        editContext,
                                        file,
                                        true,
                                        positionIndex,
                                        category,
                                        editContext.TextEditorService.NewViewModelKey())
                                    .ContinueWith(_ => _textEditorService.ViewModel_StopCursorBlinking());
                            }
                        });
                    }));
                        
                if (siblingAbsolutePath.Name == resourceAbsolutePath.Name)
                    initialActiveMenuOptionRecordIndex = i;
            }
            
            if (menuOptionList.Count == 1)
            {
                await menuOptionList[0].OnClickFunc.Invoke(default);
            }
            else
            {
                MenuContainer menu;
                
                if (menuOptionList.Count == 0)
                    menu = new MenuContainer();
                else
                    menu = new MenuContainer(menuOptionList);
                
                menu.InitialActiveMenuOptionRecordIndex = initialActiveMenuOptionRecordIndex;
                
                var dropdownRecord = new DropdownRecord(
                    Key<DropdownRecord>.NewKey(),
                    cursorDimensions.LeftInPixels,
                    cursorDimensions.TopInPixels + cursorDimensions.HeightInPixels,
                    typeof(MenuDisplay),
                    new Dictionary<string, object?>
                    {
                        {
                            nameof(MenuDisplay.Menu),
                            menu
                        }
                    },
                    // TODO: this callback when the dropdown closes is suspect.
                    //       The editContext is supposed to live the lifespan of the
                    //       Post. But what if the Post finishes before the dropdown is closed?
                    async () => 
                    {
                        // TODO: Even if this '.single or default' to get the main group works it is bad and I am ashamed...
                        //       ...I'm too tired at the moment, need to make this sensible.
                        //       The key is in the IDE project yet its circular reference if I do so, gotta
                        //       make groups more sensible I'm not sure what to say here I'm super tired and brain checked out.
                        //       |
                        //       I ran this and it didn't work. Its for the best that it doesn't.
                        //       maybe when I wake up tomorrow I'll realize what im doing here.
                        var mainEditorGroup = _textEditorService.Group_GetTextEditorGroupState().GroupList.SingleOrDefault();
                        
                        if (mainEditorGroup is not null &&
                            mainEditorGroup.ActiveViewModelKey != 0)
                        {
                            var activeViewModel = _textEditorService.ViewModel_GetOrDefault(mainEditorGroup.ActiveViewModelKey);
        
                            if (activeViewModel is not null)
                                await activeViewModel.FocusAsync();
                        }
                        
                        await viewModelModifier.FocusAsync();
                    });
        
                _textEditorService.CommonService.Dropdown_ReduceRegisterAction(dropdownRecord);
            }
        }
    }
    
    /// <summary>
    /// This implementation is NOT thread safe.
    /// </summary>
    public ValueTask ParseAsync(TextEditorEditContext editContext, TextEditorModel modelModifier, bool shouldApplySyntaxHighlighting)
    {
        var resourceUri = modelModifier.PersistentState.ResourceUri;
        int absolutePathId = TryGetFileAbsolutePathToInt(resourceUri.Value);
        if (absolutePathId == 0)
            absolutePathId = TryAddFileAbsolutePath(resourceUri.Value);
    
        if (!__CSharpBinder.__CompilationUnitMap.ContainsKey(absolutePathId))
            return ValueTask.CompletedTask;
        
        var cSharpCompilationUnit = new CSharpCompilationUnit(CompilationUnitKind.IndividualFile_AllData);

        var contentAtRequest = modelModifier.xGetAllText();

        _currentFileBeingParsedTuple = (absolutePathId, contentAtRequest);

        MemoryStream memoryStream;
        StreamReaderPooledBuffer? lexer_reader = null;
        
        try
        {
            // Convert the string to a byte array using a specific encoding
            memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(contentAtRequest));
            lexer_reader = bbb_Rent_StreamReaderPooledBuffer(memoryStream);
            
            if (shouldApplySyntaxHighlighting)
            {
                editContext.TextEditorService.Model_BeginStreamSyntaxHighlighting(
                    editContext,
                    modelModifier);
            }

            _streamReaderWrap.ReInitialize(lexer_reader);
            
            _tokenWalkerBuffer.ReInitialize(
                __CSharpBinder,
                resourceUri,
                modelModifier,
                _tokenWalkerBuffer,
                _streamReaderWrap,
                shouldUseSharedStringWalker: true);
            
            __CSharpBinder.StartCompilationUnit(absolutePathId);
            CSharpParser.Parse(absolutePathId, _tokenWalkerBuffer, ref cSharpCompilationUnit, __CSharpBinder);
        }
        finally
        {
            //var diagnosticTextSpans = cSharpCompilationUnit.DiagnosticList
            //    .Select(x => x.TextSpan)
            //    .ToList();
            
            if (shouldApplySyntaxHighlighting)
            {
                editContext.TextEditorService.Model_FinalizeStreamSyntaxHighlighting(
                    editContext,
                    modelModifier);
            }

            _currentFileBeingParsedTuple = (absolutePathId, null);

            FULL_Clear_MAIN_StreamReaderTupleCache();
            FULL_Clear_BACKUP_StreamReaderTupleCache();
            _safe_streamReaderPooledBuffer_Pool.Clear();
            
            if (lexer_reader is not null)
                Return_StreamReaderPooledBuffer(lexer_reader);
        
            _textEditorService.EditContext_GetText_Clear();
        }
        
        return ValueTask.CompletedTask;
    }
    
    private readonly List<TextEditorTextSpan> _emptyDiagnosticTextSpans = new();
    private readonly SafeOnlyUTF8Encoding _safeOnlyUTF8Encoding;

    public ValueTask FastParseAsync(TextEditorEditContext editContext, ResourceUri resourceUri, IFileSystemProvider fileSystemProvider, CompilationUnitKind compilationUnitKind)
    {
        throw new NotImplementedException();
    }
    
    public void FastParse(TextEditorEditContext editContext, ResourceUri resourceUri, IFileSystemProvider fileSystemProvider, CompilationUnitKind compilationUnitKind)
    {
        int absolutePathId = TryGetFileAbsolutePathToInt(resourceUri.Value);
        if (absolutePathId == 0)
            absolutePathId = TryAddFileAbsolutePath(resourceUri.Value);

        if (!__CSharpBinder.__CompilationUnitMap.ContainsKey(absolutePathId))
            return;
    
        var cSharpCompilationUnit = new CSharpCompilationUnit(compilationUnitKind);

        StreamReaderPooledBuffer? lexer_sr = null;
        StreamReaderPooledBuffer? parser_sr = null;

        try
        {
            lexer_sr = aaa_Rent_StreamReaderPooledBuffer(resourceUri.Value);
            parser_sr = aaa_Rent_StreamReaderPooledBuffer(resourceUri.Value);

            _streamReaderWrap.ReInitialize(lexer_sr);

            _tokenWalkerBuffer.ReInitialize(
                __CSharpBinder,
                resourceUri,
                textEditorModel: null,
                _tokenWalkerBuffer,
                _streamReaderWrap,
                shouldUseSharedStringWalker: true);

            FastParseTuple = (absolutePathId, parser_sr);
            __CSharpBinder.StartCompilationUnit(absolutePathId);
            CSharpParser.Parse(absolutePathId, _tokenWalkerBuffer, ref cSharpCompilationUnit, __CSharpBinder);
        }
        finally
        {
            if (lexer_sr is not null)
                Return_StreamReaderPooledBuffer(lexer_sr);
            
            if (parser_sr is not null)
                Return_StreamReaderPooledBuffer(parser_sr);
        }

        FastParseTuple = (0, null);
    }
    
    /// <summary>
    /// Looks up the <see cref="IScope"/> that encompasses the provided positionIndex.
    ///
    /// Then, checks the <see cref="IScope"/>.<see cref="IScope.CodeBlockOwner"/>'s children
    /// to determine which node exists at the positionIndex.
    ///
    /// If the <see cref="IScope"/> cannot be found, then as a fallback the provided compilationUnit's
    /// <see cref="CompilationUnit.RootCodeBlockNode"/> will be treated
    /// the same as if it were the <see cref="IScope"/>.<see cref="IScope.CodeBlockOwner"/>.
    ///
    /// If the provided compilerServiceResource?.CompilationUnit is null, then the fallback step will not occur.
    /// The fallback step is expected to occur due to the global scope being implemented with a null
    /// <see cref="IScope"/>.<see cref="IScope.CodeBlockOwner"/> at the time of this comment.
    /// </summary>
    public SyntaxNodeValue GetSyntaxNode(int positionIndex, ResourceUri resourceUri, ICompilerServiceResource? compilerServiceResource)
    {
        return default;
        // return __CSharpBinder.GetSyntaxNode(compilationUnit: null, positionIndex, (CSharpCompilationUnit)compilerServiceResource);
    }
    
    /// <summary>
    /// Returns the <see cref="ISyntaxNode"/> that represents the definition in the <see cref="CompilationUnit"/>.
    ///
    /// The option argument 'symbol' can be provided if available. It might provide additional information to the method's implementation
    /// that is necessary to find certain nodes (ones that are in a separate file are most common to need a symbol to find).
    /// </summary>
    public SyntaxNodeValue GetDefinitionNodeValue(TextEditorTextSpan textSpan, int absolutePathId, ICompilerServiceResource compilerServiceResource, Symbol? symbol = null)
    {
        if (symbol is null)
            return default;
        
        if (__CSharpBinder.__CompilationUnitMap.TryGetValue(absolutePathId, out var compilationUnit))
            return __CSharpBinder.GetDefinitionNodeValue(absolutePathId, compilationUnit, textSpan, symbol.Value.SyntaxKind, symbol);
        
        return default;
    }

    public (Scope Scope, SyntaxNodeValue CodeBlockOwner) GetCodeBlockTupleByPositionIndex(int absolutePathId, int positionIndex)
    {
        if (__CSharpBinder.__CompilationUnitMap.TryGetValue(absolutePathId, out var compilationUnit))
        {
            var scope = __CSharpBinder.GetScopeByPositionIndex(compilationUnit, positionIndex);
            if (scope.NodeSubIndex != -1)
            {
                return (scope, __CSharpBinder.NodeList[compilationUnit.NodeOffset + scope.NodeSubIndex]);
            }
            else
            {
                return (scope, default);
            }
        }
        
        return default;
    }
    
    public string GetIdentifierText(ISyntaxNode node, int absolutePathId)
    {
        if (__CSharpBinder.__CompilationUnitMap.TryGetValue(absolutePathId, out var compilationUnit))
            return __CSharpBinder.GetIdentifierText(node, absolutePathId, compilationUnit) ?? string.Empty;
    
        return string.Empty;
    }
}
