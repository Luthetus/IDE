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
    
    public MenuRecord GetContextMenu(TextEditorVirtualizationResult virtualizationResult, ContextMenu contextMenu)
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

    private MenuRecord? GetAutocompleteMenuPart(TextEditorVirtualizationResult virtualizationResult, AutocompleteMenu autocompleteMenu, int positionIndex)
    {
        if (virtualizationResult.Model is null)
            return null;
        var virtualizationResultAbsolutePathId = TryGetFileAbsolutePathToInt(virtualizationResult.Model.PersistentState.ResourceUri.Value);
        if (virtualizationResultAbsolutePathId == 0 || virtualizationResultAbsolutePathId == 1)
            return null;

        var character = '\0';
        
        var foundMemberAccessToken = false;
        var memberAccessTokenPositionIndex = -1;
        
        var isParsingIdentifier = false;
        var isParsingNumber = false;
        
        // banana.Price
        //
        // 'banana.' is  the context
        // 'banana' is the operating word
        var operatingWordEndExclusiveIndex = -1;
        
        var filteringWordEndExclusiveIndex = -1;
        var filteringWordStartInclusiveIndex = -1;
        
        // '|' indicates cursor position:
        //
        // "apple banana.Pri|ce"
        // "apple.banana Pri|ce"
        var notParsingButTouchingletterOrDigit = false;
        var letterOrDigitIntoNonMatchingCharacterKindOccurred = false;
        
        var i = positionIndex - 1;
        
        for (; i >= 0; i--)
        {
            character = virtualizationResult.Model.GetCharacter(i);
            
            switch (character)
            {
                /* Lowercase Letters */
                case 'a':
                case 'b':
                case 'c':
                case 'd':
                case 'e':
                case 'f':
                case 'g':
                case 'h':
                case 'i':
                case 'j':
                case 'k':
                case 'l':
                case 'm':
                case 'n':
                case 'o':
                case 'p':
                case 'q':
                case 'r':
                case 's':
                case 't':
                case 'u':
                case 'v':
                case 'w':
                case 'x':
                case 'y':
                case 'z':
                /* Uppercase Letters */
                case 'A':
                case 'B':
                case 'C':
                case 'D':
                case 'E':
                case 'F':
                case 'G':
                case 'H':
                case 'I':
                case 'J':
                case 'K':
                case 'L':
                case 'M':
                case 'N':
                case 'O':
                case 'P':
                case 'Q':
                case 'R':
                case 'S':
                case 'T':
                case 'U':
                case 'V':
                case 'W':
                case 'X':
                case 'Y':
                case 'Z':
                /* Underscore */
                case '_':
                    if (foundMemberAccessToken)
                    {
                        isParsingIdentifier = true;
                        
                        if (operatingWordEndExclusiveIndex == -1)
                            operatingWordEndExclusiveIndex = i;
                    }
                    else
                    {
                        if (!notParsingButTouchingletterOrDigit)
                        {
                            notParsingButTouchingletterOrDigit = true;
                            
                            if (filteringWordEndExclusiveIndex == -1)
                                filteringWordEndExclusiveIndex = i + 1;
                        }
                    }
                    break;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    if (foundMemberAccessToken)
                    {
                        if (!isParsingIdentifier)
                        {
                            isParsingNumber = true;
                            
                            if (operatingWordEndExclusiveIndex == -1)
                                operatingWordEndExclusiveIndex = i;
                        }
                    }
                    else
                    {
                        if (!notParsingButTouchingletterOrDigit)
                        {
                            notParsingButTouchingletterOrDigit = true;
                            
                            if (filteringWordEndExclusiveIndex == -1)
                                filteringWordEndExclusiveIndex = i + 1;
                        }
                    }
                    break;
                case '\r':
                case '\n':
                case '\t':
                case ' ':
                    if (isParsingIdentifier || isParsingNumber)
                        goto exitOuterForLoop;
        
                    if (notParsingButTouchingletterOrDigit)
                    {
                        if (letterOrDigitIntoNonMatchingCharacterKindOccurred)
                        {
                            goto exitOuterForLoop;
                        }
                        else
                        {
                            letterOrDigitIntoNonMatchingCharacterKindOccurred = true;
                        }
                    }
                    break;
                case '.':
                    if (!foundMemberAccessToken)
                    {
                        if (notParsingButTouchingletterOrDigit && filteringWordStartInclusiveIndex == -1)
                            filteringWordStartInclusiveIndex = i + 1;
                    
                        foundMemberAccessToken = true;
                        notParsingButTouchingletterOrDigit = false;
                        letterOrDigitIntoNonMatchingCharacterKindOccurred = false;
                        
                        if (i > 0)
                        {
                            var innerCharacter = virtualizationResult.Model.GetCharacter(i - 1);
                            
                            if (innerCharacter == '?' || innerCharacter == '!')
                                i--;
                        }
                    }
                    else
                    {
                        goto exitOuterForLoop;
                    }
                    break;
                default:
                    goto exitOuterForLoop;
            }
        }
        
        exitOuterForLoop:
        
        // Invalidate the parsed identifier if it starts with a number.
        if (isParsingIdentifier)
        {
            switch (character)
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    isParsingIdentifier = false;
                    break;
            }
        }
        
        var filteringWord = string.Empty;
        
        if (filteringWordStartInclusiveIndex != -1 && filteringWordEndExclusiveIndex != -1)
        {
            var textSpan = new TextEditorTextSpan(
                filteringWordStartInclusiveIndex,
                filteringWordEndExclusiveIndex,
                DecorationByte: 0);
                
            filteringWord = textSpan.GetText(virtualizationResult.Model.GetAllText(), _textEditorService);
        }
            
        if (foundMemberAccessToken && operatingWordEndExclusiveIndex != -1)
        {
            var autocompleteEntryList = new List<AutocompleteEntry>();
            
            var operatingWordAmongPositionIndex = operatingWordEndExclusiveIndex - 1;
           
            if (operatingWordAmongPositionIndex < 0)
                operatingWordAmongPositionIndex = 0;
       
            var foundMatch = false;
            
            var resource = GetResourceByResourceUri(virtualizationResult.Model.PersistentState.ResourceUri);
            var compilationUnitLocal = (CSharpCompilationUnit)resource.CompilationUnit;
            
            var symbols = __CSharpBinder.SymbolList.Skip(compilationUnitLocal.SymbolOffset).Take(compilationUnitLocal.SymbolLength).ToList();
            // var diagnostics = compilationUnitLocal.DiagnosticList;
            
            Symbol foundSymbol = default;
    
            if (!foundMatch && symbols.Count != 0)
            {
                foreach (var symbol in symbols)
                {
                    if (operatingWordAmongPositionIndex >= symbol.StartInclusiveIndex &&
                        operatingWordAmongPositionIndex < symbol.EndExclusiveIndex)
                    {
                        foundMatch = true;
                        foundSymbol = symbol;
                        break;
                    }
                }
            }
            
            if (foundMatch)
            {
                // var textEditorModel = _textEditorService.ModelApi.GetOrDefault(foundSymbol.TextSpan.ResourceUri);
                var textEditorModel = virtualizationResult.Model;
                var extendedCompilerService = (IExtendedCompilerService)textEditorModel.PersistentState.CompilerService;
                var compilerServiceResource = extendedCompilerService.GetResourceByResourceUri(textEditorModel.PersistentState.ResourceUri);
        
                var definitionNode = extendedCompilerService.GetDefinitionNodeValue(foundSymbol.ToTextSpan(), virtualizationResultAbsolutePathId, compilerServiceResource, foundSymbol);
                
                if (!definitionNode.IsDefault())
                {
                    /*if (definitionNode.SyntaxKind == SyntaxKind.NamespaceClauseNode)
                    {
                        var namespaceClauseNode = definitionNode;
                    
                        NamespacePrefixNode? namespacePrefixNode = null;
                        
                        if (namespaceClauseNode.NamespacePrefixNode is not null)
                        {
                            namespacePrefixNode = namespaceClauseNode.NamespacePrefixNode;
                        }
                        else
                        {
                            namespacePrefixNode = __CSharpBinder.FindPrefix(
                                __CSharpBinder.NamespacePrefixTree.__Root,
                                foundSymbol.TextSpan,
                                textEditorModel.PersistentState.ResourceUri.Value);
                        }

                        if (namespacePrefixNode is not null)
                        {
                            var filteringWordCharIntSum = 0;
                            foreach (var c in filteringWord)
                            {
                                filteringWordCharIntSum += (int)c;
                            }
                        
                            var findTuple = __CSharpBinder.NamespacePrefixTree.FindRange(
                                namespacePrefixNode,
                                filteringWordCharIntSum);
                        
                            for (int prefixIndex = findTuple.StartIndex; prefixIndex < findTuple.EndIndex; prefixIndex++)
                            {
                                var prefix = namespacePrefixNode.Children[prefixIndex];
                                var prefixText = SafeGetText(prefix.ResourceUri.Value, prefix.TextSpan);
                                if (prefixText.Contains(filteringWord))
                                {
                                    autocompleteEntryList.Add(new AutocompleteEntry(
                                        prefixText,
                                        AutocompleteEntryKind.Namespace,
                                        () => MemberAutocomplete(prefixText, filteringWord, virtualizationResult.Model.PersistentState.ResourceUri, virtualizationResult.ViewModel.PersistentState.ViewModelKey)));
                                }
                            }
                            
                            var namespaceGroup = __CSharpBinder.FindNamespaceGroup_Reversed_WithMatchedIndex(
                                    virtualizationResult.Model.PersistentState.ResourceUri,
                                    foundSymbol.TextSpan)
                                .TargetGroup;
                            
                            if (namespaceGroup.ConstructorWasInvoked)
                            {
                                foreach (var typeDefinitionNode in __CSharpBinder.GetTopLevelTypeDefinitionNodes_NamespaceGroup(namespaceGroup).Where(x => x.IdentifierToken.TextSpan.GetText(virtualizationResult.Model.GetAllText(), _textEditorService).Contains(filteringWord)).Take(5))
                                {
                                    var resourceUriValue = virtualizationResult.Model.PersistentState.ResourceUri.Value;


                                    if (typeDefinitionNode.ResourceUri != virtualizationResult.Model.PersistentState.ResourceUri)
                                    {
                                        if (__CSharpBinder.__CompilationUnitMap.TryGetValue(typeDefinitionNode.ResourceUri, out var innerCompilationUnit))
                                        {
                                            resourceUriValue = typeDefinitionNode.ResourceUri.Value;
                                        }
                                    }
                                
                                    autocompleteEntryList.Add(new AutocompleteEntry(
                                        UnsafeGetText(resourceUriValue, typeDefinitionNode.IdentifierToken.TextSpan) ?? string.Empty,
                                        AutocompleteEntryKind.Type,
                                        () => MemberAutocomplete(UnsafeGetText(resourceUriValue, typeDefinitionNode.IdentifierToken.TextSpan) ?? string.Empty, filteringWord, virtualizationResult.Model.PersistentState.ResourceUri, virtualizationResult.ViewModel.PersistentState.ViewModelKey)));
                                }
                            }
                            
                            return new MenuRecord(
                                autocompleteEntryList.Select(entry =>
                                {
                                    var menuOptionRecord = new MenuOptionRecord(
                                        entry.DisplayName,
                                        MenuOptionKind.Other,
                                        _ => entry.SideEffectFunc?.Invoke() ?? Task.CompletedTask);
                                    menuOptionRecord.IconKind = entry.AutocompleteEntryKind;
                                    return menuOptionRecord;
                                })
                                .ToList());
                        }
                        
                        return null;
                    }*/
                
                    TypeReferenceValue typeReference = default;
                    
                    if (definitionNode.SyntaxKind == SyntaxKind.VariableReferenceNode)
                    {
                        var variableReferenceNode = definitionNode;
                        var variableTraits = __CSharpBinder.VariableDeclarationTraitsList[variableReferenceNode.TraitsIndex];
                        typeReference = variableTraits.TypeReference;
                    }
                    else if (definitionNode.SyntaxKind == SyntaxKind.VariableDeclarationNode)
                    {
                        var variableDeclarationNode = definitionNode;
                        var variableTraits = __CSharpBinder.VariableDeclarationTraitsList[variableDeclarationNode.TraitsIndex];
                        typeReference = variableTraits.TypeReference;
                    }
                    else if (definitionNode.SyntaxKind == SyntaxKind.FunctionInvocationNode)
                    {
                        var functionInvocationTraits = __CSharpBinder.FunctionDefinitionTraitsList[definitionNode.TraitsIndex];
                        typeReference = functionInvocationTraits.ReturnTypeReference;
                    }
                    else if (definitionNode.SyntaxKind == SyntaxKind.TypeClauseNode)
                    {
                        //typeReference = new TypeReference(definitionNode);
                    }
                    else if (definitionNode.SyntaxKind == SyntaxKind.TypeDefinitionNode)
                    {
                        //var typeDefinitionNode = definitionNode;
                        //typeReference = typeDefinitionNode.ToTypeReference();
                    }
                    
                    if (!typeReference.IsDefault())
                    {
                        Symbol innerFoundSymbol = default;
                        var innerCompilationUnit = compilationUnitLocal;
                        var innerAbsolutePathId = virtualizationResultAbsolutePathId;

                        if (typeReference.ExplicitDefinitionAbsolutePathId != ResourceUri.NullAbsolutePathId && typeReference.ExplicitDefinitionAbsolutePathId != virtualizationResultAbsolutePathId)
                        {
                            if (__CSharpBinder.__CompilationUnitMap.TryGetValue(typeReference.ExplicitDefinitionAbsolutePathId, out innerCompilationUnit))
                            {
                                innerAbsolutePathId = typeReference.ExplicitDefinitionAbsolutePathId;
                                symbols = __CSharpBinder.SymbolList.Skip(innerCompilationUnit.SymbolOffset).Take(innerCompilationUnit.SymbolLength).ToList();
                            }
                        }
                        
                        if (symbols.Count != 0)
                        {
                            foreach (var symbol in symbols)
                            {
                                if (typeReference.ExplicitDefinitionTextSpan.StartInclusiveIndex >= symbol.StartInclusiveIndex &&
                                    typeReference.ExplicitDefinitionTextSpan.StartInclusiveIndex < symbol.EndExclusiveIndex)
                                {
                                    innerFoundSymbol = symbol;
                                    break;
                                }
                            }
                        }
                        
                        if (innerFoundSymbol != default)
                        {
                            var maybeTypeDefinitionNode = __CSharpBinder.GetDefinitionNodeValue(
                                innerAbsolutePathId,
                                innerCompilationUnit,
                                innerFoundSymbol.ToTextSpan(),
                                syntaxKind: innerFoundSymbol.SyntaxKind,
                                symbol: innerFoundSymbol);
                            
                            if (!maybeTypeDefinitionNode.IsDefault() && maybeTypeDefinitionNode.SyntaxKind == SyntaxKind.TypeDefinitionNode)
                            {
                                var memberList = __CSharpBinder.GetMemberList_TypeDefinitionNode(maybeTypeDefinitionNode);
                                ISyntaxNode? foundDefinitionNode = null;
                                
                                foreach (var member in memberList.Where(x => UnsafeGetText(innerAbsolutePathId, x.IdentifierToken.TextSpan)?.Contains(filteringWord) ?? false).Take(25))
                                {
                                    switch (member.SyntaxKind)
                                    {
                                        case SyntaxKind.VariableDeclarationNode:
                                        {
                                            int absolutePathId;
                                            var variableDeclarationNode = member;
                                            
                                            if (variableDeclarationNode.AbsolutePathId != innerAbsolutePathId)
                                            {
                                                if (__CSharpBinder.__CompilationUnitMap.TryGetValue(variableDeclarationNode.AbsolutePathId, out var variableDeclarationCompilationUnit))
                                                    absolutePathId = variableDeclarationNode.AbsolutePathId;
                                                else
                                                    absolutePathId = innerAbsolutePathId;
                                            }
                                            else
                                            {
                                                absolutePathId = innerAbsolutePathId;
                                            }
                                            
                                            autocompleteEntryList.Add(new AutocompleteEntry(
                                                UnsafeGetText(absolutePathId, variableDeclarationNode.IdentifierToken.TextSpan) ?? string.Empty,
                                                AutocompleteEntryKind.Variable,
                                                () => MemberAutocomplete(UnsafeGetText(absolutePathId, variableDeclarationNode.IdentifierToken.TextSpan) ?? string.Empty, filteringWord, virtualizationResult.Model.PersistentState.ResourceUri, virtualizationResult.ViewModel.PersistentState.ViewModelKey)));
                                            break;
                                        }
                                        case SyntaxKind.FunctionDefinitionNode:
                                        {
                                            string sourceText;
                                            int absolutePathId;
                                            var functionDefinitionNode = member;
                                            
                                            if (functionDefinitionNode.AbsolutePathId != innerAbsolutePathId)
                                            {
                                                if (__CSharpBinder.__CompilationUnitMap.TryGetValue(functionDefinitionNode.AbsolutePathId, out var functionDefinitionCompilationUnit))
                                                    absolutePathId = functionDefinitionNode.AbsolutePathId;
                                                else
                                                    absolutePathId = innerAbsolutePathId;
                                            }
                                            else
                                            {
                                                absolutePathId = innerAbsolutePathId;
                                            }
                                            
                                            autocompleteEntryList.Add(new AutocompleteEntry(
                                                UnsafeGetText(absolutePathId, functionDefinitionNode.IdentifierToken.TextSpan) ?? string.Empty,
                                                AutocompleteEntryKind.Function,
                                                () => MemberAutocomplete(UnsafeGetText(absolutePathId, functionDefinitionNode.IdentifierToken.TextSpan) ?? string.Empty, filteringWord, virtualizationResult.Model.PersistentState.ResourceUri, virtualizationResult.ViewModel.PersistentState.ViewModelKey)));
                                            break;
                                        }
                                        case SyntaxKind.TypeDefinitionNode:
                                        {
                                            var innerTypeDefinitionNode = member;
                                            autocompleteEntryList.Add(new AutocompleteEntry(
                                                UnsafeGetText(innerAbsolutePathId, innerTypeDefinitionNode.IdentifierToken.TextSpan) ?? string.Empty,
                                                AutocompleteEntryKind.Type,
                                                () => MemberAutocomplete(UnsafeGetText(innerAbsolutePathId, innerTypeDefinitionNode.IdentifierToken.TextSpan) ?? string.Empty, filteringWord, virtualizationResult.Model.PersistentState.ResourceUri, virtualizationResult.ViewModel.PersistentState.ViewModelKey)));
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        
            if (autocompleteEntryList.Count == 0)
                return new MenuRecord(MenuRecord.NoMenuOptionsExistList);
            
            return new MenuRecord(
                autocompleteEntryList.Select(entry =>
                {
                    var menuOptionRecord = new MenuOptionRecord(
                        entry.DisplayName,
                        MenuOptionKind.Other,
                        _ => entry.SideEffectFunc?.Invoke() ?? Task.CompletedTask);
                    
                    menuOptionRecord.IconKind = entry.AutocompleteEntryKind;
                    return menuOptionRecord;
                })
                .ToList());
        }
        
        return null;
    }

    public MenuRecord GetAutocompleteMenu(TextEditorVirtualizationResult virtualizationResult, AutocompleteMenu autocompleteMenu)
    {
        var positionIndex = virtualizationResult.Model.GetPositionIndex(virtualizationResult.ViewModel);
        
        var autocompleteMenuPart = GetAutocompleteMenuPart(virtualizationResult, autocompleteMenu, positionIndex);
        if (autocompleteMenuPart is not null)
            return autocompleteMenuPart;
        
        var word = virtualizationResult.Model.ReadPreviousWordOrDefault(
            virtualizationResult.ViewModel.LineIndex,
            virtualizationResult.ViewModel.ColumnIndex);
    
        // The cursor is 1 character ahead.
        var textSpan = new TextEditorTextSpan(
            positionIndex - 1,
            positionIndex,
            0);
    
        var compilerServiceAutocompleteEntryList = OBSOLETE_GetAutocompleteEntries(
            word,
            textSpan,
            virtualizationResult);
    
        return autocompleteMenu.GetDefaultMenuRecord(compilerServiceAutocompleteEntryList);
    }
    
    private Task MemberAutocomplete(string text, string filteringWord, ResourceUri resourceUri, int viewModelKey)
    {
        _textEditorService.WorkerArbitrary.PostUnique(async editContext =>
        {
            var model = editContext.GetModelModifier(resourceUri);
            var viewModel = editContext.GetViewModelModifier(viewModelKey);
            
            model.Insert(new string(text.Skip(filteringWord.Length).ToArray()), viewModel);
            await viewModel.FocusAsync();
            
            await ParseAsync(editContext, model, shouldApplySyntaxHighlighting: true);
        });
        
        return Task.CompletedTask;
    }
    
    public ValueTask<MenuRecord> GetQuickActionsSlashRefactorMenu(
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
            
        var menuOptionList = new List<MenuOptionRecord>();
            
        menuOptionList.Add(new MenuOptionRecord(
            "QuickActionsSlashRefactorMenu",
            MenuOptionKind.Other));
            
        if (syntaxNode is null)
        {
            menuOptionList.Add(new MenuOptionRecord(
                "syntaxNode was null",
                MenuOptionKind.Other,
                onClickFunc: async _ => {}));
        }
        else
        {
            /*if (syntaxNode.SyntaxKind == SyntaxKind.TypeClauseNode)
            {
                var allTypeDefinitions = __CSharpBinder.AllTypeDefinitions;
                
                var typeClauseNode = (TypeClauseNode)syntaxNode;
                
                if (allTypeDefinitions.TryGetValue(typeClauseNode.TypeIdentifierToken.TextSpan.GetText(modelModifier.GetAllText(), _textEditorService), out var typeDefinitionNode))
                {
                    var usingStatementText = $"using NamespaceName_WasHere;";
                        
                    menuOptionList.Add(new MenuOptionRecord(
                        $"Copy: {usingStatementText}",
                        MenuOptionKind.Other,
                        onClickFunc: async _ =>
                        {
                            await _textEditorService.CommonService.SetClipboard(usingStatementText).ConfigureAwait(false);
                        }));
                }
                else
                {
                    menuOptionList.Add(new MenuOptionRecord(
                        "type not found",
                        MenuOptionKind.Other,
                        onClickFunc: async _ => {}));
                }
            }
            else
            {*/
                menuOptionList.Add(new MenuOptionRecord(
                    syntaxNode.SyntaxKind.ToString(),
                    MenuOptionKind.Other,
                    onClickFunc: async _ => {}));
            /*}*/
        }
        
        MenuRecord menu;
        
        if (menuOptionList.Count == 0)
            menu = new MenuRecord(MenuRecord.NoMenuOptionsExistList);
        else
            menu = new MenuRecord(menuOptionList);
    
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
            
            var menuOptionList = new List<MenuOptionRecord>();
            
            siblingFileStringList = siblingFileStringList.OrderBy(x => x).ToList();
            
            var initialActiveMenuOptionRecordIndex = -1;
            
            for (int i = 0; i < siblingFileStringList.Count; i++)
            {
                var tuple = siblingFileStringList[i];
                var file = tuple.ResourceUriValue;
                
                var siblingAbsolutePath = new AbsolutePath(file, false, _textEditorService.CommonService.FileSystemProvider, tokenBuilder: new StringBuilder(), formattedBuilder: new StringBuilder(), AbsolutePathNameKind.NameWithExtension);
                
                menuOptionList.Add(new MenuOptionRecord(
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
                MenuRecord menu;
                
                if (menuOptionList.Count == 0)
                    menu = new MenuRecord(MenuRecord.NoMenuOptionsExistList);
                else
                    menu = new MenuRecord(menuOptionList);
                
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

        var contentAtRequest = modelModifier.GetAllText();

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
    
    public List<AutocompleteEntry>? OBSOLETE_GetAutocompleteEntries(string word, TextEditorTextSpan textSpan, TextEditorVirtualizationResult virtualizationResult)
    {
        if (virtualizationResult.Model is null)
            return null;

        var absolutePathId = TryGetFileAbsolutePathToInt(virtualizationResult.Model.PersistentState.ResourceUri.Value);
        if (absolutePathId == 0)
            return null;

        if (word is null || !__CSharpBinder.__CompilationUnitMap.TryGetValue(absolutePathId, out var compilationUnit))
            return null;
            
        var boundScope = __CSharpBinder.GetScope(compilationUnit, textSpan);
        if (boundScope.IsDefault())
            return null;
        
        var autocompleteEntryList = new List<AutocompleteEntry>();

        var targetScope = boundScope;
        
        if (textSpan.GetText(virtualizationResult.Model.GetAllText(), _textEditorService) == ".")
        {
            var textEditorModel = virtualizationResult.Model;
            if (textEditorModel is null)
                return autocompleteEntryList.DistinctBy(x => x.DisplayName).ToList();
            
            var compilerService = textEditorModel.PersistentState.CompilerService;
            
            var compilerServiceResource = compilerService.GetResourceByResourceUri(textEditorModel.PersistentState.ResourceUri);
            if (compilerServiceResource is null)
                return autocompleteEntryList.DistinctBy(x => x.DisplayName).ToList();
    
            var targetNode = __CSharpBinder.GetSyntaxNode(
                (CSharpCompilationUnit)compilerServiceResource.CompilationUnit,
                textSpan.StartInclusiveIndex - 1,
                (CSharpCompilationUnit)compilerServiceResource);
                
            if (targetNode is null)
                return autocompleteEntryList.DistinctBy(x => x.DisplayName).ToList();
        
            TypeReferenceValue typeReference = default;
    
            if (targetNode.SyntaxKind == SyntaxKind.VariableReferenceNode)
            {
                var variableReferenceNode = (VariableReferenceNode)targetNode;
                typeReference = variableReferenceNode.TypeReference;
            }
            else if (targetNode.SyntaxKind == SyntaxKind.VariableDeclarationNode)
            {
                typeReference = ((VariableDeclarationNode)targetNode).TypeReference;
            }
            else if (targetNode.SyntaxKind == SyntaxKind.TypeClauseNode)
            {
                typeReference = new TypeReferenceValue((TypeClauseNode)targetNode);
            }
            else if (targetNode.SyntaxKind == SyntaxKind.TypeDefinitionNode)
            {
                typeReference = ((TypeDefinitionNode)targetNode).ToTypeReference();
            }
            else if (targetNode.SyntaxKind == SyntaxKind.ConstructorDefinitionNode)
            {
                typeReference = ((ConstructorDefinitionNode)targetNode).ReturnTypeReference;
            }
            
            if (typeReference.IsDefault())
                return autocompleteEntryList.DistinctBy(x => x.DisplayName).ToList();
            
            var maybeTypeDefinitionNode = __CSharpBinder.GetDefinitionNodeValue(absolutePathId, (CSharpCompilationUnit)compilerServiceResource.CompilationUnit, typeReference.TypeIdentifierToken.TextSpan, SyntaxKind.TypeClauseNode);
            if (maybeTypeDefinitionNode.IsDefault() || maybeTypeDefinitionNode.SyntaxKind != SyntaxKind.TypeDefinitionNode)
                return autocompleteEntryList.DistinctBy(x => x.DisplayName).ToList();
            
            var typeDefinitionNode = maybeTypeDefinitionNode;
            var memberList = __CSharpBinder.GetMemberList_TypeDefinitionNode(typeDefinitionNode);
            
            autocompleteEntryList.AddRange(
                memberList
                .Select(node => 
                {
                    if (node.SyntaxKind == SyntaxKind.VariableDeclarationNode)
                    {
                        var variableDeclarationNode = node;
                        return variableDeclarationNode.IdentifierToken.TextSpan.GetText(virtualizationResult.Model.GetAllText(), _textEditorService);
                    }
                    else if (node.SyntaxKind == SyntaxKind.TypeDefinitionNode)
                    {
                        var typeDefinitionNode = node;
                        return typeDefinitionNode.IdentifierToken.TextSpan.GetText(virtualizationResult.Model.GetAllText(), _textEditorService);
                    }
                    else if (node.SyntaxKind == SyntaxKind.FunctionDefinitionNode)
                    {
                        var functionDefinitionNode = node;
                        return functionDefinitionNode.IdentifierToken.TextSpan.GetText(virtualizationResult.Model.GetAllText(), _textEditorService);
                    }
                    else
                    {
                        return string.Empty;
                    }
                })
                .ToArray()
                //.Where(x => x.Contains(word, StringComparison.InvariantCulture))
                .Distinct()
                .Take(5)
                .Select(x =>
                {
                    return new AutocompleteEntry(
                        x,
                        AutocompleteEntryKind.Variable,
                        null);
                }));
        }
        else
        {
            while (!targetScope.IsDefault())
            {
                autocompleteEntryList.AddRange(
                    __CSharpBinder.GetVariableDeclarationNodesByScope(absolutePathId, compilationUnit, targetScope.SelfScopeSubIndex)
                    .ToArray()
                    .Where(x => x?.Contains(word, StringComparison.InvariantCulture) ?? false)
                    .Distinct()
                    .Take(5)
                    .Select(x =>
                    {
                        return new AutocompleteEntry(
                            x,
                            AutocompleteEntryKind.Variable,
                            null);
                    }));
    
                autocompleteEntryList.AddRange(
                    __CSharpBinder.GetFunctionDefinitionNamesByScope(virtualizationResult.Model.PersistentState.ResourceUri, compilationUnit, targetScope.SelfScopeSubIndex)
                    .ToArray()
                    .Where(x => x.Contains(word, StringComparison.InvariantCulture))
                    .Distinct()
                    .Take(5)
                    .Select(x =>
                    {
                        return new AutocompleteEntry(
                            x,
                            AutocompleteEntryKind.Function,
                            null);
                    }));
    
                if (targetScope.ParentScopeSubIndex == -1)
                    targetScope = default;
                else
                    targetScope = __CSharpBinder.GetScopeByOffset(compilationUnit, targetScope.ParentScopeSubIndex);
            }
        
            /*var allTypeDefinitions = __CSharpBinder.AllTypeDefinitionList;
    
            autocompleteEntryList.AddRange(
                allTypeDefinitions
                .Where(x => x.Key.Contains(word, StringComparison.InvariantCulture))
                .Distinct()
                .Take(5)
                .Select(x =>
                {
                    return new AutocompleteEntry(
                        x.Key,
                        AutocompleteEntryKind.Type,
                        sideEffectFunc: null);
                }));*/
        }
        
        /*foreach (var namespaceGroupKvp in __CSharpBinder.NamespacePrefixTree.__Root.Children.Where(x => x.Key.Contains(word)).Take(5))
        {
            autocompleteEntryList.Add(new AutocompleteEntry(
                namespaceGroupKvp.Key,
                AutocompleteEntryKind.Namespace,
                () => Task.CompletedTask));
        }*/
            
        AddSnippets(autocompleteEntryList, word, textSpan, virtualizationResult.Model.PersistentState.ResourceUri);

        return autocompleteEntryList.DistinctBy(x => x.DisplayName).ToList();
    }
    
    private void AddSnippets(List<AutocompleteEntry> autocompleteEntryList, string word, TextEditorTextSpan textSpan, ResourceUri resourceUri)
    {
        if ("prop".Contains(word))
        {
            autocompleteEntryList.Add(new AutocompleteEntry(
                "prop",
                AutocompleteEntryKind.Snippet,
                () => PropSnippet(word, textSpan, "public TYPE NAME { get; set; }", resourceUri)));
        }
        
        if ("propnn".Contains(word))
        {
            autocompleteEntryList.Add(new AutocompleteEntry(
                "propnn",
                AutocompleteEntryKind.Snippet,
                () => PropSnippet(word, textSpan, "public TYPE NAME { get; set; } = null!;", resourceUri)));
        }
    }
    
    private Task PropSnippet(string word, TextEditorTextSpan textSpan, string textToInsert, ResourceUri resourceUri)
    {
        _textEditorService.WorkerArbitrary.PostUnique((Func<TextEditorEditContext, ValueTask>)(editContext =>
        {
            var modelModifier = editContext.GetModelModifier(resourceUri);

            if (modelModifier is null)
                return ValueTask.CompletedTask;

            var viewModelList = _textEditorService.Model_GetViewModelsOrEmpty(resourceUri);
            
            var viewModel = viewModelList.FirstOrDefault(x => x.PersistentState.Category.Value == "main")
                ?? viewModelList.FirstOrDefault();
            
            if (viewModel is null)
                return ValueTask.CompletedTask;
                
            var viewModelModifier = editContext.GetViewModelModifier(viewModel.PersistentState.ViewModelKey);
            
            if (viewModelModifier is null)
                return ValueTask.CompletedTask;

            var cursorPositionIndex = modelModifier.GetPositionIndex(viewModelModifier);
            var behindPositionIndex = cursorPositionIndex - 1;
                    
            modelModifier.Insert(
                textToInsert,
                viewModelModifier);
                
            /*if (behindPositionIndex > 0 && modelModifier.GetCharacter(behindPositionIndex) == 'p')
            {
                modelModifier.Delete(
                    viewModelModifier,
                    1,
                    expandWord: false,
                    TextEditorModel.DeleteKind.Delete);
            }*/

            modelModifier.PersistentState.CompilerService.ResourceWasModified(
                (ResourceUri)modelModifier.PersistentState.ResourceUri,
                (IReadOnlyList<TextEditorTextSpan>)Array.Empty<TextEditorTextSpan>());
                
            return ValueTask.CompletedTask;
        }));
            
        return Task.CompletedTask;
    }
    
    public string GetIdentifierText(ISyntaxNode node, int absolutePathId)
    {
        if (__CSharpBinder.__CompilationUnitMap.TryGetValue(absolutePathId, out var compilationUnit))
            return __CSharpBinder.GetIdentifierText(node, absolutePathId, compilationUnit) ?? string.Empty;
    
        return string.Empty;
    }
}
