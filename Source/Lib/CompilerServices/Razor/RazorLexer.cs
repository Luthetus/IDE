using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.TextEditor.RazorLib.Decorations.Models;
using Clair.Extensions.CompilerServices.Syntax;
using Clair.CompilerServices.CSharp.BinderCase;
using Clair.CompilerServices.CSharp.CompilerServiceCase;

namespace Clair.CompilerServices.Razor;

public static class RazorLexer
{
    public enum RazorLexerContextKind
    {
        Expect_TagOrText = 0,
        // Expect_TagName, // There is no expect tag name, you can't have whitespace here
        Expect_AttributeName = 1,
        Expect_AttributeValue = 2,
    }

    public static SyntaxToken Lex(
        CSharpBinder binder,
        TokenWalkerBuffer tokenWalkerBuffer,
        byte contextKindByte = 0/*RazorLexerContextKind.Expect_TagOrText*/)
    {
        var context = (RazorLexerContextKind)contextKindByte;
        //var output = new RazorLexerOutput(modelModifier);
        
        // This gets updated throughout the loop
        var startPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
        var startByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
        
        TextEditorTextSpan textSpanOfMostRecentTagOpen = default;
        
        while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
        {
            switch (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter)
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
                /* At */
                case '@':
                    if (context == RazorLexerContextKind.Expect_AttributeName)
                    {
                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '@')
                        {
                            var atCharStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                            var atCharStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                            _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                            // Attribute skips HTML identifier because ':' example: 'onclick:stopPropagation="true"'
                            SkipHtmlIdentifier(tokenWalkerBuffer);
                            tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                atCharStartPosition,
                                tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                                (byte)GenericDecorationKind.Razor_AttributeNameInjectedLanguageFragment);
                        }
                        else
                        {
                            var attributeNameStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                            var attributeNameStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                            var wasInjectedLanguageFragment = false;
                            while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                            {
                                if (!char.IsLetterOrDigit(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter))
                                {
                                    if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter != '_' &&
                                        tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter != '-' &&
                                        tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter != ':')
                                    {
                                        break;
                                    }
                                }
                                _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                            }

                            tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                attributeNameStartPosition,
                                tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                                (byte)GenericDecorationKind.Razor_AttributeName);
                        }
                        
                        context = RazorLexerContextKind.Expect_AttributeValue;
                        break;
                    }
                    else if (context == RazorLexerContextKind.Expect_AttributeValue)
                    {
                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '@')
                        {
                            var atCharStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                            var atCharStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                            _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                            
                            if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '(')
                            {
                                var matchParenthesis = 0;
                                while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                                {
                                    if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '(')
                                    {
                                        ++matchParenthesis;
                                    }
                                    else if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == ')')
                                    {
                                        --matchParenthesis;
                                        if (matchParenthesis == 0)
                                        {
                                            _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                                            break;
                                        }
                                    }
                                    _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                                }
                            }
                            else
                            {
                                SkipCSharpdentifier(tokenWalkerBuffer);
                            }
                            
                            tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                atCharStartPosition,
                                tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                                (byte)GenericDecorationKind.Razor_AttributeValueInjectedLanguageFragment);
                        }
                        else
                        {
                            var attributeValueStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                            var attributeValueStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                            while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                            {
                                if (!char.IsLetterOrDigit(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter))
                                {
                                    if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '@' &&
                                        tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter != '_' &&
                                        tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter != '-' &&
                                        tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter != ':')
                                    {
                                        break;
                                    }
                                }
                                _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                            }
                            tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                attributeValueStartPosition,
                                tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                                (byte)GenericDecorationKind.Razor_AttributeValue);
                        }
                        
                        context = RazorLexerContextKind.Expect_AttributeName;
                        break;
                    }
                    else if (context == RazorLexerContextKind.Expect_TagOrText)
                    {
                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '@')
                        {
                            var startInclusiveIndex = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                            var byteIndex = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;

                            _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();

                            tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                startInclusiveIndex,
                                tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                                (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);

                            return new SyntaxToken(
                                SyntaxKind.AtToken,
                                new TextEditorTextSpan(
                                    startInclusiveIndex,
                                    endExclusiveIndex: tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                                    decorationByte: (byte)GenericDecorationKind.Razor_InjectedLanguageFragment,
                                    byteIndex,
                                    charIntSum: 64));
                        }

                        var textStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                        var textStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                        while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                        {
                            if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '<')
                            {
                                break;
                            }
                            else if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '@')
                            {
                                tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                    textStartPosition,
                                    tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                                    (byte)GenericDecorationKind.Razor_Text);
                                var atCharStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                                var atCharStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                                _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                            
                                if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '*')
                                {
                                    _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                        atCharStartPosition,
                                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                                        
                                    var commentStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                                    var commentStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                                    
                                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                                    {
                                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '*' && tokenWalkerBuffer.StreamReaderWrap.PeekCharacter(1) == '@')
                                            break;
                                    
                                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                                    }
                                    
                                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                        commentStartPosition,
                                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                                        (byte)GenericDecorationKind.Razor_Comment);
                                    
                                    // The while loop has 2 break cases, thus !IsEof means "*@" was the break cause.
                                    if (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                                    {
                                        var starStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                                        var starStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                                        
                                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                                        tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                            starStartPosition,
                                            tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                                            (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                                    }
                                }
                                else if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '{')
                                {
                                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                        atCharStartPosition,
                                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                                        
                                    LexCSharpCodeBlock(tokenWalkerBuffer);
                                    if (tokenWalkerBuffer.UseCSharpLexer && !tokenWalkerBuffer.IsInitialParse)
                                        return default;
                                }
                                else
                                {
                                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                        atCharStartPosition,
                                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                                
                                    var wordStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                                    var wordStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                                    
                                    var everythingWasHandledForMe = SkipCSharpdentifierOrKeyword(binder.KeywordCheckBuffer, tokenWalkerBuffer);
                                    if (tokenWalkerBuffer.UseCSharpLexer && !tokenWalkerBuffer.IsInitialParse)
                                        return default;
                                    if (everythingWasHandledForMe.SyntaxKind == SyntaxKind.NotApplicable)
                                    {
                                        tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                            wordStartPosition,
                                            tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                                            (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                                    }
                                }
                                
                                textStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                                textStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                                continue;
                            }
                            _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                        }
                        tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                            textStartPosition,
                            tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                            (byte)GenericDecorationKind.Razor_Text);
                        context = RazorLexerContextKind.Expect_TagOrText;
                        break;
                    }
                    
                    goto default;
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
                    if (context == RazorLexerContextKind.Expect_AttributeValue)
                    {
                        var attributeValueStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                        var attributeValueStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                        while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                        {
                            if (!char.IsLetterOrDigit(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter) &&
                                tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter != '_' &&
                                tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter != '-' &&
                                tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter != ':')
                            {
                                break;
                            }
                            _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                        }
                        tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                            attributeValueStartPosition,
                            tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                            (byte)GenericDecorationKind.Razor_AttributeValue);
                        context = RazorLexerContextKind.Expect_AttributeName;
                        break;
                    }
                    
                    goto default;
                case '\'':
                    if (context == RazorLexerContextKind.Expect_AttributeValue)
                    {
                        var delimiterStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                        var delimiterStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                        tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                            delimiterStartPosition,
                            tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                            (byte)GenericDecorationKind.Razor_AttributeDelimiter);
                            
                        var attributeValueStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                        var attributeValueStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                        var attributeValueEnd = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                        var hasSeenInterpolation = false;
                        while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                        {
                            if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '\'')
                            {
                                attributeValueEnd = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                                delimiterStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                                delimiterStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                                _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                                break;
                            }
                            else if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '@')
                            {
                                if (!hasSeenInterpolation)
                                {
                                    hasSeenInterpolation = true;
                                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                        attributeValueStartPosition,
                                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                                        (byte)GenericDecorationKind.Razor_AttributeValueInterpolationStart);
                                }
                                else
                                {
                                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                        attributeValueStartPosition,
                                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                                        (byte)GenericDecorationKind.Razor_AttributeValueInterpolationContinue);
                                }
                                
                                var interpolationStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                                var interpolationStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                                _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                                
                                if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '(')
                                {
                                    var matchParenthesis = 0;
                                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                                    {
                                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '(')
                                        {
                                            ++matchParenthesis;
                                        }
                                        else if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == ')')
                                        {
                                            --matchParenthesis;
                                            if (matchParenthesis == 0)
                                            {
                                                _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                                                break;
                                            }
                                        }
                                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                                    }
                                }
                                else
                                {
                                    SkipCSharpdentifier(tokenWalkerBuffer);
                                }
                                
                                tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                    interpolationStartPosition,
                                    tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                                    (byte)GenericDecorationKind.Razor_AttributeValueInjectedLanguageFragment);
                                
                                attributeValueStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                                attributeValueStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                                continue;
                            }
                            _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                        }
                        
                        if (hasSeenInterpolation)
                        {
                            tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                attributeValueStartPosition,
                                attributeValueEnd,
                                (byte)GenericDecorationKind.Razor_AttributeValueInterpolationContinue);
                        
                            tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                delimiterStartPosition,
                                delimiterStartPosition,
                                (byte)GenericDecorationKind.Razor_AttributeValueInterpolationEnd);
                        }
                        else
                        {
                            tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                attributeValueStartPosition,
                                attributeValueEnd,
                                (byte)GenericDecorationKind.Razor_AttributeValue);
                        }
                        
                        tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                            delimiterStartPosition,
                            tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                            (byte)GenericDecorationKind.Razor_AttributeDelimiter);
                            
                        context = RazorLexerContextKind.Expect_AttributeName;
                        break;
                    }
                    goto default;
                case '"':
                    if (context == RazorLexerContextKind.Expect_AttributeValue)
                    {
                        var delimiterStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                        var delimiterStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                        tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                            delimiterStartPosition,
                            tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                            (byte)GenericDecorationKind.Razor_AttributeDelimiter);
                        
                        var attributeValueStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                        var attributeValueStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                        var attributeValueEnd = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                        var hasSeenInterpolation = false;
                        while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                        {
                            if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '"')
                            {
                                attributeValueEnd = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                                delimiterStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                                delimiterStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                                _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                                break;
                            }
                            else if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '@')
                            {
                                if (!hasSeenInterpolation)
                                {
                                    hasSeenInterpolation = true;
                                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                        attributeValueStartPosition,
                                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                                        (byte)GenericDecorationKind.Razor_AttributeValueInterpolationStart);
                                }
                                else
                                {
                                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                        attributeValueStartPosition,
                                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                                        (byte)GenericDecorationKind.Razor_AttributeValueInterpolationContinue);
                                }
                                
                                var interpolationStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                                var interpolationStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                                _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                                
                                if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '(')
                                {
                                    var matchParenthesis = 0;
                                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                                    {
                                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '(')
                                        {
                                            ++matchParenthesis;
                                        }
                                        else if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == ')')
                                        {
                                            --matchParenthesis;
                                            if (matchParenthesis == 0)
                                            {
                                                _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                                                break;
                                            }
                                        }
                                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                                    }
                                }
                                else
                                {
                                    SkipCSharpdentifier(tokenWalkerBuffer);
                                }
                                
                                tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                    interpolationStartPosition,
                                    tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                                    (byte)GenericDecorationKind.Razor_AttributeValueInjectedLanguageFragment);
                                
                                attributeValueStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                                attributeValueStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                                continue;
                            }
                            _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                        }
                        
                        if (hasSeenInterpolation)
                        {
                            tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                attributeValueStartPosition,
                                attributeValueEnd,
                                (byte)GenericDecorationKind.Razor_AttributeValueInterpolationContinue);
                        
                            tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                delimiterStartPosition,
                                delimiterStartPosition,
                                (byte)GenericDecorationKind.Razor_AttributeValueInterpolationEnd);
                        }
                        else
                        {
                            tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                attributeValueStartPosition,
                                attributeValueEnd,
                                (byte)GenericDecorationKind.Razor_AttributeValue);
                        }
                        
                        tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                            delimiterStartPosition,
                            tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                            (byte)GenericDecorationKind.Razor_AttributeDelimiter);
                        
                        context = RazorLexerContextKind.Expect_AttributeName;
                        break;
                    }
                    goto default;
                case '/':
                
                    if (tokenWalkerBuffer.StreamReaderWrap.PeekCharacter(1) == '>')
                    {
                        if (context == RazorLexerContextKind.Expect_AttributeName || context == RazorLexerContextKind.Expect_AttributeValue)
                        {
                            if (textSpanOfMostRecentTagOpen.DecorationByte != 0)
                            {
                                tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                                    textSpanOfMostRecentTagOpen.StartInclusiveIndex,
                                    textSpanOfMostRecentTagOpen.EndExclusiveIndex,
                                    (byte)GenericDecorationKind.Razor_TagNameSelf);
                                textSpanOfMostRecentTagOpen = default;
                            }
                            context = RazorLexerContextKind.Expect_TagOrText;
                        }
                    }
                
                    if (tokenWalkerBuffer.StreamReaderWrap.PeekCharacter(1) == '/')
                    {
                        goto default;
                    }
                    else if (tokenWalkerBuffer.StreamReaderWrap.PeekCharacter(1) == '*')
                    {
                        goto default;
                    }
                    else
                    {
                        goto default;
                    }
                    break;
                case '+':
                    if (tokenWalkerBuffer.StreamReaderWrap.PeekCharacter(1) == '+')
                    {
                        goto default;
                    }
                    else
                    {
                        goto default;
                    }
                    break;
                case '-':
                    if (tokenWalkerBuffer.StreamReaderWrap.PeekCharacter(1) == '-')
                    {
                        goto default;
                    }
                    else
                    {
                        goto default;
                    }
                    break;
                case '=':
                    if (context == RazorLexerContextKind.Expect_AttributeValue)
                    {
                        var attributeValueStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                        var attributeValueStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                        tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                            attributeValueStartPosition,
                            tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                            (byte)GenericDecorationKind.Razor_AttributeOperator);
                        break;
                    }
                
                    if (tokenWalkerBuffer.StreamReaderWrap.PeekCharacter(1) == '=')
                    {
                        goto default;
                    }
                    else if (tokenWalkerBuffer.StreamReaderWrap.PeekCharacter(1) == '>')
                    {
                        goto default;
                    }
                    else
                    {
                        goto default;
                    }
                    break;
                case '?':
                    if (tokenWalkerBuffer.StreamReaderWrap.PeekCharacter(1) == '?')
                    {
                        goto default;
                    }
                    else
                    {
                        goto default;
                    }
                    break;
                case '|':
                    if (tokenWalkerBuffer.StreamReaderWrap.PeekCharacter(1) == '|')
                    {
                        goto default;
                    }
                    else
                    {
                        goto default;
                    }
                case '&':
                    if (tokenWalkerBuffer.StreamReaderWrap.PeekCharacter(1) == '&')
                    {
                        goto default;
                    }
                    else
                    {
                        goto default;
                    }
                case '*':
                {
                    goto default;
                }
                case '!':
                {
                    if (tokenWalkerBuffer.StreamReaderWrap.PeekCharacter(1) == '=')
                    {
                        goto default;
                    }
                    else
                    {
                        goto default;
                    }
                }
                case ';':
                {
                    goto default;
                }
                case '(':
                {
                    goto default;
                }
                case ')':
                {
                    goto default;
                }
                case '{':
                {
                    /*if (interpolatedExpressionUnmatchedBraceCount != -1)
                        ++interpolatedExpressionUnmatchedBraceCount;*/
                
                    goto default;
                }
                case '}':
                {
                    /*if (interpolatedExpressionUnmatchedBraceCount != -1)
                    {
                        if (--interpolatedExpressionUnmatchedBraceCount <= 0)
                            goto forceExit;
                    }*/
                
                    goto default;
                }
                case '<':
                {
                    if (tokenWalkerBuffer.StreamReaderWrap.PeekCharacter(1) == '=')
                    {
                        goto default;
                    }
                    
                    var tagDecoration = (byte)GenericDecorationKind.Razor_TagNameOpen;
                    
                    if (context == RazorLexerContextKind.Expect_TagOrText)
                    {
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                        
                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '/')
                        {
                            tagDecoration = (byte)GenericDecorationKind.Razor_TagNameClose;
                            _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                        }
                        else if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '!')
                        {
                            _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                        }
                        
                        var tagNameStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                        var tagNameStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
                        while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                        {
                            if (!char.IsLetterOrDigit(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter) &&
                                tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter != '_' &&
                                tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter != '-' &&
                                tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter != ':' &&
                                tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter != '.')
                            {
                                break;
                            }
                            _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                        }
                        var textSpan = new TextEditorTextSpan(
                            tagNameStartPosition,
                            tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                            tagDecoration,
                            tagNameStartByte);
                        if (tagDecoration == (byte)GenericDecorationKind.Razor_TagNameOpen)
                        {
                            textSpanOfMostRecentTagOpen = textSpan;
                        }
                        tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                            textSpan.StartInclusiveIndex,
                            textSpan.EndExclusiveIndex,
                            textSpan.DecorationByte);

                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '>')
                        {
                            context = RazorLexerContextKind.Expect_TagOrText;
                        }
                        else
                        {
                            context = RazorLexerContextKind.Expect_AttributeName;
                        }
                        
                        break;
                    }
                    else
                    {
                        goto default;
                    }
                }
                case '>':
                {
                    context = RazorLexerContextKind.Expect_TagOrText;
                
                    if (tokenWalkerBuffer.StreamReaderWrap.PeekCharacter(1) == '=')
                    {
                        goto default;
                    }
                    else
                    {
                        goto default;
                    }
                }
                case '[':
                {
                    goto default;
                }
                case ']':
                {
                    goto default;
                }
                case '$':
                    if (tokenWalkerBuffer.StreamReaderWrap.NextCharacter == '"')
                    {
                        goto default;
                    }
                    else if (tokenWalkerBuffer.StreamReaderWrap.PeekCharacter(1) == '@' && tokenWalkerBuffer.StreamReaderWrap.PeekCharacter(2) == '"')
                    {
                        goto default;
                    }
                    else if (tokenWalkerBuffer.StreamReaderWrap.NextCharacter == '$')
                    {
                        /*var entryPositionIndex = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
                        var byteEntryIndex = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;

                        // The while loop starts counting from and including the first dollar sign.
                        var countDollarSign = 0;
                    
                        while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                        {
                            if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter != '$')
                                break;
                            
                            ++countDollarSign;
                            _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                        }*/
                        
                        goto default;
                        
                        /*if (tokenWalkerBuffer.StreamReaderWrap.NextCharacter == '"')
                            LexString(binder, ref lexerOutput, tokenWalkerBuffer.StreamReaderWrap, ref previousEscapeCharacterTextSpan, countDollarSign: countDollarSign, useVerbatim: false);*/
                    }
                    else
                    {
                        goto default;
                    }
                    break;
                case ':':
                {
                    goto default;
                }
                case '.':
                {
                    goto default;
                }
                case ',':
                {
                    goto default;
                }
                case '#':
                    goto default;
                default:
                    _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    break;
            }
        }

        forceExit:
        return new SyntaxToken(SyntaxKind.EndOfFileToken, default);
        throw new NotImplementedException();
        //return output;
    }
    
    private static void SkipHtmlIdentifier(TokenWalkerBuffer tokenWalkerBuffer)
    {
        while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
        {
            if (!char.IsLetterOrDigit(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter) &&
                tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter != '_' &&
                tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter != '-' &&
                tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter != ':')
            {
                break;
            }
            _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
        }
    }
    
    private static void SkipCSharpdentifier(TokenWalkerBuffer tokenWalkerBuffer)
    {
        while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
        {
            if (!char.IsLetterOrDigit(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter) &&
                tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter != '_')
            {
                break;
            }
            _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
        }
    }
    
    /// <summary>
    /// Some keywords are only available if the preceeding markup is an if statement / etc...
    /// </summary>
    public enum SyntaxContinuationKind
    {
        None,
        // @if, else if, else
        IfStatement,
        // @try, catch, finally
        TryStatement,
    }
    
    /// <summary>
    /// When this returns default, then the state of the lexer has entirely changed
    /// and the invoker should disregard any of their previous state and reset it.
    ///
    /// This method when finding a brace deliminated code blocked keyword will entirely lex to the close brace.
    /// </summary>
    public static SyntaxToken SkipCSharpdentifierOrKeyword(
        char[] keywordCheckBuffer,
        TokenWalkerBuffer tokenWalkerBuffer,
        SyntaxContinuationKind syntaxContinuationKind = SyntaxContinuationKind.None)
    {
        // To detect whether a word is an identifier or a keyword:
        // -------------------------------------------------------
        // A buffer of CSharpBinder.KeywordCheckBufferSize characters is used as the word is read ('stackalloc' is the longest keyword at 10 characters).
        // And every character in the word is casted as an int and summed.
        //
        // The sum of each char is used as a heuristic for whether the word might be a keyword.
        // The value isn't unique, but the collisions are minimal enough for this step to be useful.
        // A switch statement checks whether the word's char sum is equal to that of a known keyword.
        // 
        // If the char sum is equal to a keyword, then:
        // the word's length and the keyword's length are compared.
        //
        // If they both have the same length:
        // a for loop over the buffer is performed to determine
        // whether every character in the word is truly equal
        // to the keyword.
        //
        // The check is only performed for the length of the word, so the indices are always initialized in time.
        // 
    
        var wordStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
        var wordStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
        
        var lengthCharacter = 0;
        var characterIntSum = 0;
        
        int bufferIndex = 0;
    
        while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
        {
            if (!char.IsLetterOrDigit(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter) &&
                tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter != '_')
            {
                break;
            }
            
            characterIntSum += (int)tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter;
            ++lengthCharacter;
            if (bufferIndex < Clair.CompilerServices.CSharp.BinderCase.CSharpBinder.KeywordCheckBufferSize)
                keywordCheckBuffer[bufferIndex++] = tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter;
                
            _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
        }

        /*
         Directives
         ==========
         @attribute case 980: // attribute
         @page "/counter" case 413: // page
         @code case 411: // code
         @functions case 985: // functions
         @implements case 1086: // implements
         @inherits case 870: // inherits
         @model case 529: // model
         @inject case 637: // inject
         @layout case 670: // layout
         @namespace case 941: // namespace
         @preservewhitespace case 1945: // preservewhitespace
         @rendermode case 1061: // rendermode
         @using case 550: // using  // static Microsoft.AspNetCore.Components.Web.RenderMode
         @section case 757: // section
         @typeparam case 979: // typeparam
         */

        switch (characterIntSum)
        {
            case 1189: // addTagHelper
                if (lengthCharacter == 8 &&
                    keywordCheckBuffer[0] ==  'a' &&
                    keywordCheckBuffer[1] ==  'd' &&
                    keywordCheckBuffer[2] ==  'd' &&
                    keywordCheckBuffer[3] ==  'T' &&
                    keywordCheckBuffer[4] ==  'a' &&
                    keywordCheckBuffer[5] ==  'g' &&
                    keywordCheckBuffer[6] ==  'H' &&
                    keywordCheckBuffer[7] ==  'e' &&
                    keywordCheckBuffer[8] ==  'l' &&
                    keywordCheckBuffer[9] ==  'p' &&
                    keywordCheckBuffer[10] == 'e' &&
                    keywordCheckBuffer[11] == 'r')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    break;
                }
                
                goto default;
            case 980: // attribute
                if (lengthCharacter == 9 &&
                    keywordCheckBuffer[0] ==  'a' &&
                    keywordCheckBuffer[1] ==  't' &&
                    keywordCheckBuffer[2] ==  't' &&
                    keywordCheckBuffer[3] ==  'r' &&
                    keywordCheckBuffer[4] ==  'i' &&
                    keywordCheckBuffer[5] ==  'b' &&
                    keywordCheckBuffer[6] ==  'u' &&
                    keywordCheckBuffer[7] ==  't' &&
                    keywordCheckBuffer[8] ==  'e')
                {

                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    return new SyntaxToken(
                        SyntaxKind.RazorDirective,
                        new TextEditorTextSpan(
                            startInclusiveIndex: wordStartPosition,
                            endExclusiveIndex: tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                            decorationByte: (byte)GenericDecorationKind.Razor_InjectedLanguageFragment,
                            byteIndex: wordStartByte,
                            charIntSum: characterIntSum));
                }
                
                goto default;
            case 412: // case
                if (lengthCharacter == 4 &&
                    keywordCheckBuffer[0] ==  'c' &&
                    keywordCheckBuffer[1] ==  'a' &&
                    keywordCheckBuffer[2] ==  's' &&
                    keywordCheckBuffer[3] ==  'e')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    break;
                }
                
                goto default;
            case 534: // class
                if (lengthCharacter == 5 &&
                    keywordCheckBuffer[0] ==  'c' &&
                    keywordCheckBuffer[1] ==  'l' &&
                    keywordCheckBuffer[2] ==  'a' &&
                    keywordCheckBuffer[3] ==  's' &&
                    keywordCheckBuffer[4] ==  's')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    break;
                }
                
                goto default;
            case 411: // code
            case 985: // functions
                var isCode = false;
                if (lengthCharacter == 4 &&
                    keywordCheckBuffer[0] ==  'c' &&
                    keywordCheckBuffer[1] ==  'o' &&
                    keywordCheckBuffer[2] ==  'd' &&
                    keywordCheckBuffer[3] ==  'e')
                {
                    isCode = true;
                }
                
                var isFunctions = false;
                if (!isCode &&
                    lengthCharacter == 9 &&
                    keywordCheckBuffer[0] ==  'f' &&
                    keywordCheckBuffer[1] ==  'u' &&
                    keywordCheckBuffer[2] ==  'n' &&
                    keywordCheckBuffer[3] ==  'c' &&
                    keywordCheckBuffer[4] ==  't' &&
                    keywordCheckBuffer[5] ==  'i' &&
                    keywordCheckBuffer[6] ==  'o' &&
                    keywordCheckBuffer[7] ==  'n' &&
                    keywordCheckBuffer[8] ==  's')
                {
                    isFunctions = true;
                }
                
                if (!isCode && !isFunctions)
                    goto default;
            
                tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                    wordStartPosition,
                    tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                    (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    
                while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                {
                    if (!char.IsWhiteSpace(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter))
                    {
                        break;
                    }
                    
                    _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                }

                var resultToken = new SyntaxToken(
                    SyntaxKind.RazorDirective,
                    new TextEditorTextSpan(
                        startInclusiveIndex: wordStartPosition,
                        endExclusiveIndex: tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        decorationByte: (byte)GenericDecorationKind.Razor_InjectedLanguageFragment,
                        byteIndex: wordStartByte,
                        charIntSum: characterIntSum));

                if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '{')
                {
                    LexCSharpCodeBlock(tokenWalkerBuffer);
                    if (tokenWalkerBuffer.UseCSharpLexer && !tokenWalkerBuffer.IsInitialParse)
                        return resultToken;
                    return resultToken;
                }
                else
                {
                    return resultToken;
                }
            case 741: // default
                if (lengthCharacter == 7 &&
                    keywordCheckBuffer[0] ==  'd' &&
                    keywordCheckBuffer[1] ==  'e' &&
                    keywordCheckBuffer[2] ==  'f' &&
                    keywordCheckBuffer[3] ==  'a' &&
                    keywordCheckBuffer[4] ==  'u' &&
                    keywordCheckBuffer[5] ==  'l' &&
                    keywordCheckBuffer[6] ==  't')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    break;
                }
                
                goto default;
            case 211: // do
                if (lengthCharacter == 2 &&
                    keywordCheckBuffer[0] ==  'd' &&
                    keywordCheckBuffer[1] ==  'o')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    
                    // Move to start of do statement code block.
                    // Skip whitespace
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (!char.IsWhiteSpace(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter))
                            break;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    LexCSharpCodeBlock(tokenWalkerBuffer);
                    if (tokenWalkerBuffer.UseCSharpLexer && !tokenWalkerBuffer.IsInitialParse)
                        return new SyntaxToken(SyntaxKind.NotProvided, default); ;
                    
                    // Skip whitespace
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (!char.IsWhiteSpace(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter))
                            break;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    return SkipCSharpdentifierOrKeyword(
                        keywordCheckBuffer,
                        tokenWalkerBuffer);
                    
                    break;
                }
                
                goto default;
            case 327: // for
                if (lengthCharacter == 3 &&
                    keywordCheckBuffer[0] ==  'f' &&
                    keywordCheckBuffer[1] ==  'o' &&
                    keywordCheckBuffer[2] ==  'r')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    
                    // Move to start of for statement condition.
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '(')
                            break;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    // Move one beyond the end of for statement condition
                    var matchParenthesis = 0;
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '(')
                        {
                            ++matchParenthesis;
                        }
                        else if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == ')')
                        {
                            --matchParenthesis;
                            if (matchParenthesis == 0)
                            {
                                _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                                break;
                            }
                        }
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    // Skip whitespace
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (!char.IsWhiteSpace(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter))
                            break;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '{')
                    {
                        LexCSharpCodeBlock(tokenWalkerBuffer);
                        if (tokenWalkerBuffer.UseCSharpLexer && !tokenWalkerBuffer.IsInitialParse)
                            return new SyntaxToken(SyntaxKind.NotProvided, default); ;
                        return new SyntaxToken(SyntaxKind.NotProvided, default); ;
                    }
                    else
                    {
                        return new SyntaxToken(SyntaxKind.NotProvided, default);
                    }
                    
                    break;
                }
                
                goto default;
            case 728: // foreach
                if (lengthCharacter == 7 &&
                    keywordCheckBuffer[0] ==  'f' &&
                    keywordCheckBuffer[1] ==  'o' &&
                    keywordCheckBuffer[2] ==  'r' &&
                    keywordCheckBuffer[3] ==  'e' &&
                    keywordCheckBuffer[4] ==  'a' &&
                    keywordCheckBuffer[5] ==  'c' &&
                    keywordCheckBuffer[6] ==  'h')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    
                    // Move to start of foreach statement condition.
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '(')
                            break;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    // Move one beyond the end of foreach statement condition
                    var matchParenthesis = 0;
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '(')
                        {
                            ++matchParenthesis;
                        }
                        else if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == ')')
                        {
                            --matchParenthesis;
                            if (matchParenthesis == 0)
                            {
                                _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                                break;
                            }
                        }
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    // Skip whitespace
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (!char.IsWhiteSpace(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter))
                            break;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '{')
                    {
                        LexCSharpCodeBlock(tokenWalkerBuffer);
                        if (tokenWalkerBuffer.UseCSharpLexer && !tokenWalkerBuffer.IsInitialParse)
                            return new SyntaxToken(SyntaxKind.NotProvided, default);
                        return new SyntaxToken(SyntaxKind.NotProvided, default);
                    }
                    else
                    {
                        return new SyntaxToken(SyntaxKind.NotProvided, default);
                    }
                    
                    break;
                }
                
                goto default;
            case 670: // layout
                if (lengthCharacter == 6 &&
                    keywordCheckBuffer[0] ==  'l' &&
                    keywordCheckBuffer[1] ==  'a' &&
                    keywordCheckBuffer[2] ==  'y' &&
                    keywordCheckBuffer[3] ==  'o' &&
                    keywordCheckBuffer[4] ==  'u' &&
                    keywordCheckBuffer[5] ==  't')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    return new SyntaxToken(
                        SyntaxKind.RazorDirective,
                        new TextEditorTextSpan(
                            startInclusiveIndex: wordStartPosition,
                            endExclusiveIndex: tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                            decorationByte: (byte)GenericDecorationKind.Razor_InjectedLanguageFragment,
                            byteIndex: wordStartByte,
                            charIntSum: characterIntSum));
                }
                
                goto default;
            case 425: // !! DUPLICATES !!
                if (lengthCharacter != 4)
                    goto default;
                
                if (syntaxContinuationKind == SyntaxContinuationKind.IfStatement &&
                    keywordCheckBuffer[0] ==  'e' &&
                    keywordCheckBuffer[1] ==  'l' &&
                    keywordCheckBuffer[2] ==  's' &&
                    keywordCheckBuffer[3] ==  'e')
                {
                    // else
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    
                    // Move to start of else-if "if" text,
                    // or to start of 'else' codeblock
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == 'i')
                        {
                            return SkipCSharpdentifierOrKeyword(
                                keywordCheckBuffer,
                                tokenWalkerBuffer);
                        }
                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '{')
                            break;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    // Skip whitespace
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (!char.IsWhiteSpace(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter))
                            break;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '{')
                    {
                        LexCSharpCodeBlock(tokenWalkerBuffer);
                        if (tokenWalkerBuffer.UseCSharpLexer && !tokenWalkerBuffer.IsInitialParse)
                            return new SyntaxToken(SyntaxKind.NotProvided, default);
                        return new SyntaxToken(SyntaxKind.NotProvided, default);
                    }
                    else
                    {
                        return new SyntaxToken(SyntaxKind.NotProvided, default);
                    }
                    
                    break;
                }
                else if (keywordCheckBuffer[0] ==  'l' &&
                         keywordCheckBuffer[1] ==  'o' &&
                         keywordCheckBuffer[2] ==  'c' &&
                         keywordCheckBuffer[3] ==  'k')
                {
                    // lock
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    
                    // Move to start of lock statement condition.
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '(')
                            break;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    // Move one beyond the end of lock statement condition
                    var matchParenthesis = 0;
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '(')
                        {
                            ++matchParenthesis;
                        }
                        else if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == ')')
                        {
                            --matchParenthesis;
                            if (matchParenthesis == 0)
                            {
                                _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                                break;
                            }
                        }
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    // Skip whitespace
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (!char.IsWhiteSpace(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter))
                            break;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '{')
                    {
                        LexCSharpCodeBlock(tokenWalkerBuffer);
                        if (tokenWalkerBuffer.UseCSharpLexer && !tokenWalkerBuffer.IsInitialParse)
                            return new SyntaxToken(SyntaxKind.NotProvided, default);
                        
                        // Skip whitespace
                        while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                        {
                            if (!char.IsWhiteSpace(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter))
                                break;
                            _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                        }
                        
                        SkipCSharpdentifierOrKeyword(
                            keywordCheckBuffer,
                            tokenWalkerBuffer,
                            SyntaxContinuationKind.IfStatement);
                        
                        return new SyntaxToken(SyntaxKind.NotProvided, default);
                    }
                    else
                    {
                        return new SyntaxToken(SyntaxKind.NotProvided, default);
                    }
                    
                    break;
                }
                
                goto default;
            case 529: // model
                if (lengthCharacter == 5 &&
                    keywordCheckBuffer[0] ==  'm' &&
                    keywordCheckBuffer[1] ==  'o' &&
                    keywordCheckBuffer[2] ==  'd' &&
                    keywordCheckBuffer[3] ==  'e' &&
                    keywordCheckBuffer[4] ==  'l')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    return new SyntaxToken(
                        SyntaxKind.RazorDirective,
                        new TextEditorTextSpan(
                            startInclusiveIndex: wordStartPosition,
                            endExclusiveIndex: tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                            decorationByte: (byte)GenericDecorationKind.Razor_InjectedLanguageFragment,
                            byteIndex: wordStartByte,
                            charIntSum: characterIntSum));
                }
                
                goto default;
            case 413: // page
                if (lengthCharacter == 4 &&
                    keywordCheckBuffer[0] ==  'p' &&
                    keywordCheckBuffer[1] ==  'a' &&
                    keywordCheckBuffer[2] ==  'g' &&
                    keywordCheckBuffer[3] ==  'e')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    return new SyntaxToken(
                        SyntaxKind.RazorDirective,
                        new TextEditorTextSpan(
                            startInclusiveIndex: wordStartPosition,
                            endExclusiveIndex: tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                            decorationByte: (byte)GenericDecorationKind.Razor_InjectedLanguageFragment,
                            byteIndex: wordStartByte,
                            charIntSum: characterIntSum));
                }
                
                goto default;
            case 1945: // preservewhitespace
                if (lengthCharacter == 18 &&
                    keywordCheckBuffer[0] ==  'p' &&
                    keywordCheckBuffer[1] ==  'r' &&
                    keywordCheckBuffer[2] ==  'e' &&
                    keywordCheckBuffer[3] ==  's' &&
                    keywordCheckBuffer[4] ==  'e' &&
                    keywordCheckBuffer[5] ==  'r' &&
                    keywordCheckBuffer[6] ==  'v' &&
                    keywordCheckBuffer[7] ==  'e' &&
                    keywordCheckBuffer[8] ==  'w' &&
                    keywordCheckBuffer[9] ==  'h' &&
                    keywordCheckBuffer[10] ==  'i' &&
                    keywordCheckBuffer[11] ==  't' &&
                    keywordCheckBuffer[12] ==  'e' &&
                    keywordCheckBuffer[13] ==  's' &&
                    keywordCheckBuffer[14] ==  'p' &&
                    keywordCheckBuffer[15] ==  'a' &&
                    keywordCheckBuffer[16] ==  'c' &&
                    keywordCheckBuffer[17] ==  'e')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    return new SyntaxToken(
                        SyntaxKind.RazorDirective,
                        new TextEditorTextSpan(
                            startInclusiveIndex: wordStartPosition,
                            endExclusiveIndex: tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                            decorationByte: (byte)GenericDecorationKind.Razor_InjectedLanguageFragment,
                            byteIndex: wordStartByte,
                            charIntSum: characterIntSum));
                }
                
                goto default;
            case 207: // if
                if (lengthCharacter == 2 &&
                    keywordCheckBuffer[0] ==  'i' &&
                    keywordCheckBuffer[1] ==  'f')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    
                    // Move to start of if statement condition.
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '(')
                            break;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    // Move one beyond the end of if statement condition
                    var matchParenthesis = 0;
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '(')
                        {
                            ++matchParenthesis;
                        }
                        else if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == ')')
                        {
                            --matchParenthesis;
                            if (matchParenthesis == 0)
                            {
                                _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                                break;
                            }
                        }
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    // Skip whitespace
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (!char.IsWhiteSpace(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter))
                            break;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '{')
                    {
                        LexCSharpCodeBlock(tokenWalkerBuffer);
                        if (tokenWalkerBuffer.UseCSharpLexer && !tokenWalkerBuffer.IsInitialParse)
                            return new SyntaxToken(SyntaxKind.NotProvided, default);
                        
                        // Skip whitespace
                        while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                        {
                            if (!char.IsWhiteSpace(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter))
                                break;
                            _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                        }
                        
                        SkipCSharpdentifierOrKeyword(
                            keywordCheckBuffer,
                            tokenWalkerBuffer,
                            SyntaxContinuationKind.IfStatement);
                        
                        return new SyntaxToken(SyntaxKind.NotProvided, default);
                    }
                    else
                    {
                        return new SyntaxToken(SyntaxKind.NotProvided, default);
                    }
                    
                    break;
                }
                
                goto default;
            case 1086: // implements
                if (lengthCharacter == 10 &&
                    keywordCheckBuffer[0] ==  'i' &&
                    keywordCheckBuffer[1] ==  'm' &&
                    keywordCheckBuffer[2] ==  'p' &&
                    keywordCheckBuffer[3] ==  'l' &&
                    keywordCheckBuffer[4] ==  'e' &&
                    keywordCheckBuffer[5] ==  'm' &&
                    keywordCheckBuffer[6] ==  'e' &&
                    keywordCheckBuffer[7] ==  'n' &&
                    keywordCheckBuffer[8] ==  't' &&
                    keywordCheckBuffer[9] ==  's')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    return new SyntaxToken(
                        SyntaxKind.RazorDirective,
                        new TextEditorTextSpan(
                            startInclusiveIndex: wordStartPosition,
                            endExclusiveIndex: tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                            decorationByte: (byte)GenericDecorationKind.Razor_InjectedLanguageFragment,
                            byteIndex: wordStartByte,
                            charIntSum: characterIntSum));
                }
                
                goto default;
            case 870: // inherits
                if (lengthCharacter == 8 &&
                    keywordCheckBuffer[0] ==  'i' &&
                    keywordCheckBuffer[1] ==  'n' &&
                    keywordCheckBuffer[2] ==  'h' &&
                    keywordCheckBuffer[3] ==  'e' &&
                    keywordCheckBuffer[4] ==  'r' &&
                    keywordCheckBuffer[5] ==  'i' &&
                    keywordCheckBuffer[6] ==  't' &&
                    keywordCheckBuffer[7] ==  's')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    return new SyntaxToken(
                        SyntaxKind.RazorDirective,
                        new TextEditorTextSpan(
                            startInclusiveIndex: wordStartPosition,
                            endExclusiveIndex: tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                            decorationByte: (byte)GenericDecorationKind.Razor_InjectedLanguageFragment,
                            byteIndex: wordStartByte,
                            charIntSum: characterIntSum));
                }
                
                goto default;
            case 637: // inject
                if (lengthCharacter == 6 &&
                    keywordCheckBuffer[0] ==  'i' &&
                    keywordCheckBuffer[1] ==  'n' &&
                    keywordCheckBuffer[2] ==  'j' &&
                    keywordCheckBuffer[3] ==  'e' &&
                    keywordCheckBuffer[4] ==  'c' &&
                    keywordCheckBuffer[5] ==  't')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    return new SyntaxToken(
                        SyntaxKind.RazorDirective,
                        new TextEditorTextSpan(
                            startInclusiveIndex: wordStartPosition,
                            endExclusiveIndex: tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                            decorationByte: (byte)GenericDecorationKind.Razor_InjectedLanguageFragment,
                            byteIndex: wordStartByte,
                            charIntSum: characterIntSum));
                }
                
                goto default;
            case 941: // namespace
                if (lengthCharacter == 9 &&
                    keywordCheckBuffer[0] ==  'n' &&
                    keywordCheckBuffer[1] ==  'a' &&
                    keywordCheckBuffer[2] ==  'm' &&
                    keywordCheckBuffer[3] ==  'e' &&
                    keywordCheckBuffer[4] ==  's' &&
                    keywordCheckBuffer[5] ==  'p' &&
                    keywordCheckBuffer[6] ==  'a' &&
                    keywordCheckBuffer[7] ==  'c' &&
                    keywordCheckBuffer[8] ==  'e')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    return new SyntaxToken(
                        SyntaxKind.RazorDirective,
                        new TextEditorTextSpan(
                            startInclusiveIndex: wordStartPosition,
                            endExclusiveIndex: tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                            decorationByte: (byte)GenericDecorationKind.Razor_InjectedLanguageFragment,
                            byteIndex: wordStartByte,
                            charIntSum: characterIntSum));
                }
                
                goto default;
            case 1546: // !! DUPLICATES !!
                if (lengthCharacter != 15)
                    goto default;
                    
                if (keywordCheckBuffer[0] ==  'r' &&
                    keywordCheckBuffer[1] ==  'e' &&
                    keywordCheckBuffer[2] ==  'm' &&
                    keywordCheckBuffer[3] ==  'o' &&
                    keywordCheckBuffer[4] ==  'v' &&
                    keywordCheckBuffer[5] ==  'e' &&
                    keywordCheckBuffer[6] ==  't' &&
                    keywordCheckBuffer[7] ==  'a' &&
                    keywordCheckBuffer[8] ==  'g' &&
                    keywordCheckBuffer[9] ==  'h' &&
                    keywordCheckBuffer[10] ==  'e' &&
                    keywordCheckBuffer[11] ==  'l' &&
                    keywordCheckBuffer[12] ==  'p' &&
                    keywordCheckBuffer[13] ==  'e' &&
                    keywordCheckBuffer[14] ==  'r')
                {
                    // removeTagHelper
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    break;
                }
                else if (keywordCheckBuffer[0] ==  't' &&
                         keywordCheckBuffer[1] ==  'a' &&
                         keywordCheckBuffer[2] ==  'g' &&
                         keywordCheckBuffer[3] ==  'h' &&
                         keywordCheckBuffer[4] ==  'e' &&
                         keywordCheckBuffer[5] ==  'l' &&
                         keywordCheckBuffer[6] ==  'p' &&
                         keywordCheckBuffer[7] ==  'e' &&
                         keywordCheckBuffer[8] ==  'r' &&
                         keywordCheckBuffer[9] ==  'p' &&
                         keywordCheckBuffer[10] ==  'r' &&
                         keywordCheckBuffer[11] ==  'e' &&
                         keywordCheckBuffer[12] ==  'f' &&
                         keywordCheckBuffer[13] ==  'i' &&
                         keywordCheckBuffer[14] ==  'x')
                {
                    // tagHelperPrefix
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    break;
                }
                
                goto default;
            case 1061: // rendermode
                if (lengthCharacter == 10 &&
                    keywordCheckBuffer[0] ==  'r' &&
                    keywordCheckBuffer[1] ==  'e' &&
                    keywordCheckBuffer[2] ==  'n' &&
                    keywordCheckBuffer[3] ==  'd' &&
                    keywordCheckBuffer[4] ==  'e' &&
                    keywordCheckBuffer[5] ==  'r' &&
                    keywordCheckBuffer[6] ==  'm' &&
                    keywordCheckBuffer[7] ==  'o' &&
                    keywordCheckBuffer[8] ==  'd' &&
                    keywordCheckBuffer[9] ==  'e')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    return new SyntaxToken(
                        SyntaxKind.RazorDirective,
                        new TextEditorTextSpan(
                            startInclusiveIndex: wordStartPosition,
                            endExclusiveIndex: tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                            decorationByte: (byte)GenericDecorationKind.Razor_InjectedLanguageFragment,
                            byteIndex: wordStartByte,
                            charIntSum: characterIntSum));
                }
                
                goto default;
            case 757: // section
                if (lengthCharacter == 7 &&
                    keywordCheckBuffer[0] ==  's' &&
                    keywordCheckBuffer[1] ==  'e' &&
                    keywordCheckBuffer[2] ==  'c' &&
                    keywordCheckBuffer[3] ==  't' &&
                    keywordCheckBuffer[4] ==  'i' &&
                    keywordCheckBuffer[5] ==  'o' &&
                    keywordCheckBuffer[6] ==  'n')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    return new SyntaxToken(
                        SyntaxKind.RazorDirective,
                        new TextEditorTextSpan(
                            startInclusiveIndex: wordStartPosition,
                            endExclusiveIndex: tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                            decorationByte: (byte)GenericDecorationKind.Razor_InjectedLanguageFragment,
                            byteIndex: wordStartByte,
                            charIntSum: characterIntSum));
                }
                
                goto default;
            case 658: // switch
                if (lengthCharacter == 6 &&
                    keywordCheckBuffer[0] ==  's' &&
                    keywordCheckBuffer[1] ==  'w' &&
                    keywordCheckBuffer[2] ==  'i' &&
                    keywordCheckBuffer[3] ==  't' &&
                    keywordCheckBuffer[4] ==  'c' &&
                    keywordCheckBuffer[5] ==  'h')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                        
                    // Move to start of switch statement condition.
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '(')
                            break;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    // Move one beyond the end of switch statement condition
                    var matchParenthesis = 0;
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '(')
                        {
                            ++matchParenthesis;
                        }
                        else if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == ')')
                        {
                            --matchParenthesis;
                            if (matchParenthesis == 0)
                            {
                                _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                                break;
                            }
                        }
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    // Skip whitespace
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (!char.IsWhiteSpace(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter))
                            break;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '{')
                    {
                        LexCSharpCodeBlock(tokenWalkerBuffer);
                        if (tokenWalkerBuffer.UseCSharpLexer && !tokenWalkerBuffer.IsInitialParse)
                            return new SyntaxToken(SyntaxKind.NotProvided, default);
                        return new SyntaxToken(SyntaxKind.NotProvided, default);
                    }
                    else
                    {
                        return new SyntaxToken(SyntaxKind.NotProvided, default);
                    }
                    
                    break;
                }
                
                goto default;
            case 351: // try
                if (lengthCharacter == 3 &&
                    keywordCheckBuffer[0] ==  't' &&
                    keywordCheckBuffer[1] ==  'r' &&
                    keywordCheckBuffer[2] ==  'y')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    
                    // Skip whitespace
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (!char.IsWhiteSpace(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter))
                            break;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '{')
                    {
                        LexCSharpCodeBlock(tokenWalkerBuffer);
                        if (tokenWalkerBuffer.UseCSharpLexer)
                            return new SyntaxToken(SyntaxKind.NotProvided, default);
                        
                        // Skip whitespace
                        while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                        {
                            if (!char.IsWhiteSpace(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter))
                                break;
                            _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                        }
                        
                        SkipCSharpdentifierOrKeyword(
                            keywordCheckBuffer,
                            tokenWalkerBuffer,
                            SyntaxContinuationKind.TryStatement);
                        
                        return new SyntaxToken(SyntaxKind.NotProvided, default);
                    }
                    else
                    {
                        return new SyntaxToken(SyntaxKind.NotProvided, default);
                    }
                    
                    break;
                }
                
                goto default;
            case 515: // catch
                if (syntaxContinuationKind == SyntaxContinuationKind.TryStatement &&
                    lengthCharacter == 5 &&
                    keywordCheckBuffer[0] ==  'c' &&
                    keywordCheckBuffer[1] ==  'a' &&
                    keywordCheckBuffer[2] ==  't' &&
                    keywordCheckBuffer[3] ==  'c' &&
                    keywordCheckBuffer[4] ==  'h')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    
                    // Move to start of catch statement variable declaration.
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '(')
                            break;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    // Move one beyond the end of if statement condition
                    var matchParenthesis = 0;
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '(')
                        {
                            ++matchParenthesis;
                        }
                        else if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == ')')
                        {
                            --matchParenthesis;
                            if (matchParenthesis == 0)
                            {
                                _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                                break;
                            }
                        }
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    // Skip whitespace
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (!char.IsWhiteSpace(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter))
                            break;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '{')
                    {
                        LexCSharpCodeBlock(tokenWalkerBuffer);
                        if (tokenWalkerBuffer.UseCSharpLexer && !tokenWalkerBuffer.IsInitialParse)
                            return new SyntaxToken(SyntaxKind.NotProvided, default);
                        
                        // Skip whitespace
                        while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                        {
                            if (!char.IsWhiteSpace(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter))
                                break;
                            _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                        }
                        
                        SkipCSharpdentifierOrKeyword(
                            keywordCheckBuffer,
                            tokenWalkerBuffer,
                            SyntaxContinuationKind.TryStatement);
                        
                        return new SyntaxToken(SyntaxKind.NotProvided, default);
                    }
                    else
                    {
                        return new SyntaxToken(SyntaxKind.NotProvided, default);
                    }
                    
                    break;
                }
                
                goto default;
            case 751: // finally
                if (syntaxContinuationKind == SyntaxContinuationKind.TryStatement &&
                    lengthCharacter == 7 &&
                    keywordCheckBuffer[0] ==  'f' &&
                    keywordCheckBuffer[1] ==  'i' &&
                    keywordCheckBuffer[2] ==  'n' &&
                    keywordCheckBuffer[3] ==  'a' &&
                    keywordCheckBuffer[4] ==  'l' &&
                    keywordCheckBuffer[5] ==  'l' &&
                    keywordCheckBuffer[6] ==  'y')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    
                    // Skip whitespace
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (!char.IsWhiteSpace(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter))
                            break;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '{')
                    {
                        LexCSharpCodeBlock(tokenWalkerBuffer);
                        if (tokenWalkerBuffer.UseCSharpLexer && !tokenWalkerBuffer.IsInitialParse)
                            return new SyntaxToken(SyntaxKind.NotProvided, default);
                        return new SyntaxToken(SyntaxKind.NotProvided, default);
                    }
                    else
                    {
                        return new SyntaxToken(SyntaxKind.NotProvided, default);
                    }
                    
                    break;
                }
                
                goto default;
            case 979: // typeparam
                if (lengthCharacter == 9 &&
                    keywordCheckBuffer[0] ==  't' &&
                    keywordCheckBuffer[1] ==  'y' &&
                    keywordCheckBuffer[2] ==  'p' &&
                    keywordCheckBuffer[3] ==  'e' &&
                    keywordCheckBuffer[4] ==  'p' &&
                    keywordCheckBuffer[5] ==  'a' &&
                    keywordCheckBuffer[6] ==  'r' &&
                    keywordCheckBuffer[7] ==  'a' &&
                    keywordCheckBuffer[8] ==  'm')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    return new SyntaxToken(
                        SyntaxKind.RazorDirective,
                        new TextEditorTextSpan(
                            startInclusiveIndex: wordStartPosition,
                            endExclusiveIndex: tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                            decorationByte: (byte)GenericDecorationKind.Razor_InjectedLanguageFragment,
                            byteIndex: wordStartByte,
                            charIntSum: characterIntSum));
                }
                
                goto default;
            case 550: // using
                if (lengthCharacter == 5 &&
                    keywordCheckBuffer[0] ==  'u' &&
                    keywordCheckBuffer[1] ==  's' &&
                    keywordCheckBuffer[2] ==  'i' &&
                    keywordCheckBuffer[3] ==  'n' &&
                    keywordCheckBuffer[4] ==  'g')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                        
                    // Skip whitespace
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (!char.IsWhiteSpace(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter))
                            break;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter != '(')
                    {
                        return new SyntaxToken(
                            SyntaxKind.RazorDirective,
                            new TextEditorTextSpan(
                                startInclusiveIndex: wordStartPosition,
                                endExclusiveIndex: tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                                decorationByte: (byte)GenericDecorationKind.Razor_InjectedLanguageFragment,
                                byteIndex: wordStartByte,
                                charIntSum: characterIntSum));
                    }
                    
                    // Move one beyond the end of using statement condition
                    var matchParenthesis = 0;
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '(')
                        {
                            ++matchParenthesis;
                        }
                        else if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == ')')
                        {
                            --matchParenthesis;
                            if (matchParenthesis == 0)
                            {
                                _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                                break;
                            }
                        }
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    // Skip whitespace
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (!char.IsWhiteSpace(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter))
                            break;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '{')
                    {
                        LexCSharpCodeBlock(tokenWalkerBuffer);
                        if (tokenWalkerBuffer.UseCSharpLexer && !tokenWalkerBuffer.IsInitialParse)
                            return new SyntaxToken(SyntaxKind.NotProvided, default);
                        return new SyntaxToken(SyntaxKind.NotProvided, default);
                    }
                    else
                    {
                        return new SyntaxToken(SyntaxKind.NotProvided, default);
                    }
                    
                    break;
                }
                
                goto default;
            case 537: // while
                if (lengthCharacter == 5 &&
                    keywordCheckBuffer[0] ==  'w' &&
                    keywordCheckBuffer[1] ==  'h' &&
                    keywordCheckBuffer[2] ==  'i' &&
                    keywordCheckBuffer[3] ==  'l' &&
                    keywordCheckBuffer[4] ==  'e')
                {
                    tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                        wordStartPosition,
                        tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    
                    // Move to start of while statement condition.
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '(')
                            break;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    // Move one beyond the end of while statement condition
                    var matchParenthesis = 0;
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '(')
                        {
                            ++matchParenthesis;
                        }
                        else if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == ')')
                        {
                            --matchParenthesis;
                            if (matchParenthesis == 0)
                            {
                                _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                                break;
                            }
                        }
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    // Skip whitespace
                    while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
                    {
                        if (!char.IsWhiteSpace(tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter))
                            break;
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    }
                    
                    if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '{')
                    {
                        LexCSharpCodeBlock(tokenWalkerBuffer);
                        if (tokenWalkerBuffer.UseCSharpLexer && !tokenWalkerBuffer.IsInitialParse)
                            return new SyntaxToken(SyntaxKind.NotProvided, default);
                        return new SyntaxToken(SyntaxKind.NotProvided, default);
                    }
                    else if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == ';')
                    {
                        // This is convenient for the 'do-while' case.
                        // Albeit probably invalid when it is the 'while' case.
                        tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                            tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                            tokenWalkerBuffer.StreamReaderWrap.PositionIndex + 1,
                            (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                        return new SyntaxToken(SyntaxKind.NotProvided, default);
                    }
                    else
                    {
                        return new SyntaxToken(SyntaxKind.NotProvided, default);
                    }
                    
                    break;
                }
                
                goto default;
            default:
                break;
        }
        
        return new SyntaxToken(SyntaxKind.NotProvided, default);
    }
    
    /// <summary>
    /// Current character ought to be the open brace, and the syntax highlighting for the open brace will be made as part of this method.
    ///
    /// The C# simply gets syntax highlighted as one single color for now during the outlining process.
    /// This method is intended to handle all false positives for brace matching:
    /// - Single line comment
    /// - Multi line comment
    /// - String
    /// But, when it comes to 'string' only the simple case of one pair of double quotes deliminating the string is being explicitly handled.
    /// Anything else relating to 'string' might by coincidence lex correctly, or it might not.
    /// TODO: Handle raw string literals and check whether verbatim and interpolated strings lex correctly.
    ///
    /// This method returns 1 character after the close brace, or EOF.
    /// </summary>
    private static void LexCSharpCodeBlock(TokenWalkerBuffer tokenWalkerBuffer)
    {
        tokenWalkerBuffer.UseCSharpLexer = true;
        return;
        var openBraceStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
        var openBraceStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
        _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
        tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
            openBraceStartPosition,
            tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
            (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
        
        var cSharpStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
        var cSharpStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
        
        var braceMatch = 1;
        
        var isSingleLineComment = false;
        var isMultiLineComment = false;
        var isString = false;
        
        var previousCharWasForwardSlash = false;
        
        while (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
        {
            var localPreviousCharWasForwardSlash = previousCharWasForwardSlash;
            
            if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '/')
            {
                previousCharWasForwardSlash = true;
            }
            else
            {
                previousCharWasForwardSlash = false;
            }
        
            if (isMultiLineComment)
            {
                if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '*' && tokenWalkerBuffer.StreamReaderWrap.PeekCharacter(1) == '/')
                {
                    _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
                    isMultiLineComment = false;
                    continue;
                }
            }
            else if (isSingleLineComment)
            {
                if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '\r' ||
                    tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '\n')
                {
                    isSingleLineComment = false;
                }
            }
            else if (isString)
            {
                if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '"')
                {
                    isString = false;
                }
            }
            else if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '"')
            {
                isString = true;
            }
            else if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '/')
            {
                if (localPreviousCharWasForwardSlash)
                {
                    isSingleLineComment = true;
                }
            }
            else if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '*')
            {
                if (localPreviousCharWasForwardSlash)
                {
                    isMultiLineComment = true;
                }
            }
            else if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '}')
            {
                if (--braceMatch == 0)
                    break;
            }
            else if (tokenWalkerBuffer.StreamReaderWrap.CurrentCharacter == '{')
            {
                ++braceMatch;
            }
        
            _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
        }

        tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
            cSharpStartPosition,
            tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
            (byte)GenericDecorationKind.Razor_CSharpMarker);
        
        // The while loop has 2 break cases, thus !IsEof means "*@" was the break cause.
        if (!tokenWalkerBuffer.StreamReaderWrap.IsEof)
        {
            var closeBraceStartPosition = tokenWalkerBuffer.StreamReaderWrap.PositionIndex;
            var closeBraceStartByte = tokenWalkerBuffer.StreamReaderWrap.ByteIndex;
            
            _ = tokenWalkerBuffer.StreamReaderWrap.ReadCharacter();
            tokenWalkerBuffer.TextEditorModel?.__SetDecorationByteRange(
                closeBraceStartPosition,
                tokenWalkerBuffer.StreamReaderWrap.PositionIndex,
                (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
        }
    }
}
