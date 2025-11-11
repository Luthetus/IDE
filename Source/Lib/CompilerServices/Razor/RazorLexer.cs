using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.TextEditor.RazorLib.Decorations.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;

namespace Clair.CompilerServices.Razor;

public static class RazorLexer
{
    public enum RazorLexerContextKind
    {
        Expect_TagOrText,
        // Expect_TagName, // There is no expect tag name, you can't have whitespace here
        Expect_AttributeName,
        Expect_AttributeValue,
    }

    public static RazorLexerOutput Lex(char[] keywordCheckBuffer, StreamReaderWrap streamReaderWrap, TextEditorModel modelModifier)
    {
        var context = RazorLexerContextKind.Expect_TagOrText;
        var output = new RazorLexerOutput(modelModifier);
        
        // This gets updated throughout the loop
        var startPosition = streamReaderWrap.PositionIndex;
        var startByte = streamReaderWrap.ByteIndex;
        
        TextEditorTextSpan textSpanOfMostRecentTagOpen = default;
        
        bool cSharpPermitted = false;
        
        while (!streamReaderWrap.IsEof)
        {
            switch (streamReaderWrap.CurrentCharacter)
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
                        if (streamReaderWrap.CurrentCharacter == '@')
                        {
                            var atCharStartPosition = streamReaderWrap.PositionIndex;
                            var atCharStartByte = streamReaderWrap.ByteIndex;
                            _ = streamReaderWrap.ReadCharacter();
                            // Attribute skips HTML identifier because ':' example: 'onclick:stopPropagation="true"'
                            SkipHtmlIdentifier(streamReaderWrap);
                            output.ModelModifier.__SetDecorationByteRange(
                                atCharStartPosition,
                                streamReaderWrap.PositionIndex,
                                (byte)GenericDecorationKind.Razor_AttributeNameInjectedLanguageFragment);
                        }
                        else
                        {
                            var attributeNameStartPosition = streamReaderWrap.PositionIndex;
                            var attributeNameStartByte = streamReaderWrap.ByteIndex;
                            var wasInjectedLanguageFragment = false;
                            while (!streamReaderWrap.IsEof)
                            {
                                if (!char.IsLetterOrDigit(streamReaderWrap.CurrentCharacter))
                                {
                                    if (streamReaderWrap.CurrentCharacter != '_' &&
                                        streamReaderWrap.CurrentCharacter != '-' &&
                                        streamReaderWrap.CurrentCharacter != ':')
                                    {
                                        break;
                                    }
                                }
                                _ = streamReaderWrap.ReadCharacter();
                            }

                            output.ModelModifier.__SetDecorationByteRange(
                                attributeNameStartPosition,
                                streamReaderWrap.PositionIndex,
                                (byte)GenericDecorationKind.Razor_AttributeName);
                        }
                        
                        context = RazorLexerContextKind.Expect_AttributeValue;
                        break;
                    }
                    else if (context == RazorLexerContextKind.Expect_AttributeValue)
                    {
                        if (streamReaderWrap.CurrentCharacter == '@')
                        {
                            var atCharStartPosition = streamReaderWrap.PositionIndex;
                            var atCharStartByte = streamReaderWrap.ByteIndex;
                            _ = streamReaderWrap.ReadCharacter();
                            SkipCSharpdentifier(streamReaderWrap);
                            output.ModelModifier.__SetDecorationByteRange(
                                atCharStartPosition,
                                streamReaderWrap.PositionIndex,
                                (byte)GenericDecorationKind.Razor_AttributeValueInjectedLanguageFragment);
                        }
                        else
                        {
                            var attributeValueStartPosition = streamReaderWrap.PositionIndex;
                            var attributeValueStartByte = streamReaderWrap.ByteIndex;
                            while (!streamReaderWrap.IsEof)
                            {
                                if (!char.IsLetterOrDigit(streamReaderWrap.CurrentCharacter))
                                {
                                    if (streamReaderWrap.CurrentCharacter == '@' &&
                                        streamReaderWrap.CurrentCharacter != '_' &&
                                        streamReaderWrap.CurrentCharacter != '-' &&
                                        streamReaderWrap.CurrentCharacter != ':')
                                    {
                                        break;
                                    }
                                }
                                _ = streamReaderWrap.ReadCharacter();
                            }
                            output.ModelModifier.__SetDecorationByteRange(
                                attributeValueStartPosition,
                                streamReaderWrap.PositionIndex,
                                (byte)GenericDecorationKind.Razor_AttributeValue);
                        }
                        
                        context = RazorLexerContextKind.Expect_AttributeName;
                        break;
                    }
                    else if (context == RazorLexerContextKind.Expect_TagOrText)
                    {
                        var textStartPosition = streamReaderWrap.PositionIndex;
                        var textStartByte = streamReaderWrap.ByteIndex;
                        while (!streamReaderWrap.IsEof)
                        {
                            if (streamReaderWrap.CurrentCharacter == '<')
                            {
                                break;
                            }
                            else if (streamReaderWrap.CurrentCharacter == '@')
                            {
                                output.ModelModifier.__SetDecorationByteRange(
                                    textStartPosition,
                                    streamReaderWrap.PositionIndex,
                                    (byte)GenericDecorationKind.Razor_Text);
                                var atCharStartPosition = streamReaderWrap.PositionIndex;
                                var atCharStartByte = streamReaderWrap.ByteIndex;
                                _ = streamReaderWrap.ReadCharacter();
                            
                                if (streamReaderWrap.CurrentCharacter == '*')
                                {
                                    _ = streamReaderWrap.ReadCharacter();
                                    output.ModelModifier.__SetDecorationByteRange(
                                        atCharStartPosition,
                                        streamReaderWrap.PositionIndex,
                                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                                        
                                    var commentStartPosition = streamReaderWrap.PositionIndex;
                                    var commentStartByte = streamReaderWrap.ByteIndex;
                                    
                                    while (!streamReaderWrap.IsEof)
                                    {
                                        if (streamReaderWrap.CurrentCharacter == '*' && streamReaderWrap.PeekCharacter(1) == '@')
                                            break;
                                    
                                        _ = streamReaderWrap.ReadCharacter();
                                    }
                                    
                                    output.ModelModifier.__SetDecorationByteRange(
                                        commentStartPosition,
                                        streamReaderWrap.PositionIndex,
                                        (byte)GenericDecorationKind.Razor_Comment);
                                    
                                    // The while loop has 2 break cases, thus !IsEof means "*@" was the break cause.
                                    if (!streamReaderWrap.IsEof)
                                    {
                                        var starStartPosition = streamReaderWrap.PositionIndex;
                                        var starStartByte = streamReaderWrap.ByteIndex;
                                        
                                        _ = streamReaderWrap.ReadCharacter();
                                        _ = streamReaderWrap.ReadCharacter();
                                        output.ModelModifier.__SetDecorationByteRange(
                                            starStartPosition,
                                            streamReaderWrap.PositionIndex,
                                            (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                                    }
                                }
                                else if (streamReaderWrap.CurrentCharacter == '{')
                                {
                                    output.ModelModifier.__SetDecorationByteRange(
                                        atCharStartPosition,
                                        streamReaderWrap.PositionIndex,
                                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                                        
                                    LexCSharpCodeBlock(streamReaderWrap, output);
                                }
                                else
                                {
                                    output.ModelModifier.__SetDecorationByteRange(
                                        atCharStartPosition,
                                        streamReaderWrap.PositionIndex,
                                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                                
                                    var wordStartPosition = streamReaderWrap.PositionIndex;
                                    var wordStartByte = streamReaderWrap.ByteIndex;
                                    
                                    var everythingWasHandledForMe = SkipCSharpdentifierOrKeyword(keywordCheckBuffer, streamReaderWrap, output, elsePermitted: false);
                                    if (!everythingWasHandledForMe)
                                    {
                                        output.ModelModifier.__SetDecorationByteRange(
                                            wordStartPosition,
                                            streamReaderWrap.PositionIndex,
                                            (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                                    }
                                }
                                
                                textStartPosition = streamReaderWrap.PositionIndex;
                                textStartByte = streamReaderWrap.ByteIndex;
                                continue;
                            }
                            _ = streamReaderWrap.ReadCharacter();
                        }
                        output.ModelModifier.__SetDecorationByteRange(
                            textStartPosition,
                            streamReaderWrap.PositionIndex,
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
                        var attributeValueStartPosition = streamReaderWrap.PositionIndex;
                        var attributeValueStartByte = streamReaderWrap.ByteIndex;
                        while (!streamReaderWrap.IsEof)
                        {
                            if (!char.IsLetterOrDigit(streamReaderWrap.CurrentCharacter) &&
                                streamReaderWrap.CurrentCharacter != '_' &&
                                streamReaderWrap.CurrentCharacter != '-' &&
                                streamReaderWrap.CurrentCharacter != ':')
                            {
                                break;
                            }
                            _ = streamReaderWrap.ReadCharacter();
                        }
                        output.ModelModifier.__SetDecorationByteRange(
                            attributeValueStartPosition,
                            streamReaderWrap.PositionIndex,
                            (byte)GenericDecorationKind.Razor_AttributeValue);
                        context = RazorLexerContextKind.Expect_AttributeName;
                        break;
                    }
                    
                    goto default;
                case '\'':
                    if (context == RazorLexerContextKind.Expect_AttributeValue)
                    {
                        var delimiterStartPosition = streamReaderWrap.PositionIndex;
                        var delimiterStartByte = streamReaderWrap.ByteIndex;
                        _ = streamReaderWrap.ReadCharacter();
                        output.ModelModifier.__SetDecorationByteRange(
                            delimiterStartPosition,
                            streamReaderWrap.PositionIndex,
                            (byte)GenericDecorationKind.Razor_AttributeDelimiter);
                            
                        var attributeValueStartPosition = streamReaderWrap.PositionIndex;
                        var attributeValueStartByte = streamReaderWrap.ByteIndex;
                        var attributeValueEnd = streamReaderWrap.PositionIndex;
                        var hasSeenInterpolation = false;
                        while (!streamReaderWrap.IsEof)
                        {
                            if (streamReaderWrap.CurrentCharacter == '\'')
                            {
                                attributeValueEnd = streamReaderWrap.PositionIndex;
                                delimiterStartPosition = streamReaderWrap.PositionIndex;
                                delimiterStartByte = streamReaderWrap.ByteIndex;
                                _ = streamReaderWrap.ReadCharacter();
                                break;
                            }
                            else if (streamReaderWrap.CurrentCharacter == '@')
                            {
                                if (!hasSeenInterpolation)
                                {
                                    hasSeenInterpolation = true;
                                    output.ModelModifier.__SetDecorationByteRange(
                                        attributeValueStartPosition,
                                        streamReaderWrap.PositionIndex,
                                        (byte)GenericDecorationKind.Razor_AttributeValueInterpolationStart);
                                }
                                else
                                {
                                    output.ModelModifier.__SetDecorationByteRange(
                                        attributeValueStartPosition,
                                        streamReaderWrap.PositionIndex,
                                        (byte)GenericDecorationKind.Razor_AttributeValueInterpolationContinue);
                                }
                                
                                var interpolationStartPosition = streamReaderWrap.PositionIndex;
                                var interpolationStartByte = streamReaderWrap.ByteIndex;
                                _ = streamReaderWrap.ReadCharacter();
                                SkipCSharpdentifier(streamReaderWrap);
                                output.ModelModifier.__SetDecorationByteRange(
                                    interpolationStartPosition,
                                    streamReaderWrap.PositionIndex,
                                    (byte)GenericDecorationKind.Razor_AttributeValueInjectedLanguageFragment);
                                
                                attributeValueStartPosition = streamReaderWrap.PositionIndex;
                                attributeValueStartByte = streamReaderWrap.ByteIndex;
                                continue;
                            }
                            _ = streamReaderWrap.ReadCharacter();
                        }
                        
                        if (hasSeenInterpolation)
                        {
                            output.ModelModifier.__SetDecorationByteRange(
                                attributeValueStartPosition,
                                attributeValueEnd,
                                (byte)GenericDecorationKind.Razor_AttributeValueInterpolationContinue);
                        
                            output.ModelModifier.__SetDecorationByteRange(
                                delimiterStartPosition,
                                delimiterStartPosition,
                                (byte)GenericDecorationKind.Razor_AttributeValueInterpolationEnd);
                        }
                        else
                        {
                            output.ModelModifier.__SetDecorationByteRange(
                                attributeValueStartPosition,
                                attributeValueEnd,
                                (byte)GenericDecorationKind.Razor_AttributeValue);
                        }
                        
                        output.ModelModifier.__SetDecorationByteRange(
                            delimiterStartPosition,
                            streamReaderWrap.PositionIndex,
                            (byte)GenericDecorationKind.Razor_AttributeDelimiter);
                            
                        context = RazorLexerContextKind.Expect_AttributeName;
                        break;
                    }
                    goto default;
                case '"':
                    if (context == RazorLexerContextKind.Expect_AttributeValue)
                    {
                        var delimiterStartPosition = streamReaderWrap.PositionIndex;
                        var delimiterStartByte = streamReaderWrap.ByteIndex;
                        _ = streamReaderWrap.ReadCharacter();
                        output.ModelModifier.__SetDecorationByteRange(
                            delimiterStartPosition,
                            streamReaderWrap.PositionIndex,
                            (byte)GenericDecorationKind.Razor_AttributeDelimiter);
                        
                        var attributeValueStartPosition = streamReaderWrap.PositionIndex;
                        var attributeValueStartByte = streamReaderWrap.ByteIndex;
                        var attributeValueEnd = streamReaderWrap.PositionIndex;
                        var hasSeenInterpolation = false;
                        while (!streamReaderWrap.IsEof)
                        {
                            if (streamReaderWrap.CurrentCharacter == '"')
                            {
                                attributeValueEnd = streamReaderWrap.PositionIndex;
                                delimiterStartPosition = streamReaderWrap.PositionIndex;
                                delimiterStartByte = streamReaderWrap.ByteIndex;
                                _ = streamReaderWrap.ReadCharacter();
                                break;
                            }
                            else if (streamReaderWrap.CurrentCharacter == '@')
                            {
                                if (!hasSeenInterpolation)
                                {
                                    hasSeenInterpolation = true;
                                    output.ModelModifier.__SetDecorationByteRange(
                                        attributeValueStartPosition,
                                        streamReaderWrap.PositionIndex,
                                        (byte)GenericDecorationKind.Razor_AttributeValueInterpolationStart);
                                }
                                else
                                {
                                    output.ModelModifier.__SetDecorationByteRange(
                                        attributeValueStartPosition,
                                        streamReaderWrap.PositionIndex,
                                        (byte)GenericDecorationKind.Razor_AttributeValueInterpolationContinue);
                                }
                                
                                var interpolationStartPosition = streamReaderWrap.PositionIndex;
                                var interpolationStartByte = streamReaderWrap.ByteIndex;
                                _ = streamReaderWrap.ReadCharacter();
                                SkipCSharpdentifier(streamReaderWrap);
                                output.ModelModifier.__SetDecorationByteRange(
                                    interpolationStartPosition,
                                    streamReaderWrap.PositionIndex,
                                    (byte)GenericDecorationKind.Razor_AttributeValueInjectedLanguageFragment);
                                
                                attributeValueStartPosition = streamReaderWrap.PositionIndex;
                                attributeValueStartByte = streamReaderWrap.ByteIndex;
                                continue;
                            }
                            _ = streamReaderWrap.ReadCharacter();
                        }
                        
                        if (hasSeenInterpolation)
                        {
                            output.ModelModifier.__SetDecorationByteRange(
                                attributeValueStartPosition,
                                attributeValueEnd,
                                (byte)GenericDecorationKind.Razor_AttributeValueInterpolationContinue);
                        
                            output.ModelModifier.__SetDecorationByteRange(
                                delimiterStartPosition,
                                delimiterStartPosition,
                                (byte)GenericDecorationKind.Razor_AttributeValueInterpolationEnd);
                        }
                        else
                        {
                            output.ModelModifier.__SetDecorationByteRange(
                                attributeValueStartPosition,
                                attributeValueEnd,
                                (byte)GenericDecorationKind.Razor_AttributeValue);
                        }
                        
                        output.ModelModifier.__SetDecorationByteRange(
                            delimiterStartPosition,
                            streamReaderWrap.PositionIndex,
                            (byte)GenericDecorationKind.Razor_AttributeDelimiter);
                        
                        context = RazorLexerContextKind.Expect_AttributeName;
                        break;
                    }
                    goto default;
                case '/':
                
                    if (streamReaderWrap.PeekCharacter(1) == '>')
                    {
                        if (context == RazorLexerContextKind.Expect_AttributeName || context == RazorLexerContextKind.Expect_AttributeValue)
                        {
                            if (textSpanOfMostRecentTagOpen.DecorationByte != 0)
                            {
                                output.ModelModifier?.__SetDecorationByteRange(
                                    textSpanOfMostRecentTagOpen.StartInclusiveIndex,
                                    textSpanOfMostRecentTagOpen.EndExclusiveIndex,
                                    (byte)GenericDecorationKind.Razor_TagNameSelf);
                                textSpanOfMostRecentTagOpen = default;
                            }
                            context = RazorLexerContextKind.Expect_TagOrText;
                        }
                    }
                
                    if (streamReaderWrap.PeekCharacter(1) == '/')
                    {
                        goto default;
                    }
                    else if (streamReaderWrap.PeekCharacter(1) == '*')
                    {
                        goto default;
                    }
                    else
                    {
                        goto default;
                    }
                    break;
                case '+':
                    if (streamReaderWrap.PeekCharacter(1) == '+')
                    {
                        goto default;
                    }
                    else
                    {
                        goto default;
                    }
                    break;
                case '-':
                    if (streamReaderWrap.PeekCharacter(1) == '-')
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
                        var attributeValueStartPosition = streamReaderWrap.PositionIndex;
                        var attributeValueStartByte = streamReaderWrap.ByteIndex;
                        _ = streamReaderWrap.ReadCharacter();
                        output.ModelModifier.__SetDecorationByteRange(
                            attributeValueStartPosition,
                            streamReaderWrap.PositionIndex,
                            (byte)GenericDecorationKind.Razor_AttributeOperator);
                        break;
                    }
                
                    if (streamReaderWrap.PeekCharacter(1) == '=')
                    {
                        goto default;
                    }
                    else if (streamReaderWrap.PeekCharacter(1) == '>')
                    {
                        goto default;
                    }
                    else
                    {
                        goto default;
                    }
                    break;
                case '?':
                    if (streamReaderWrap.PeekCharacter(1) == '?')
                    {
                        goto default;
                    }
                    else
                    {
                        goto default;
                    }
                    break;
                case '|':
                    if (streamReaderWrap.PeekCharacter(1) == '|')
                    {
                        goto default;
                    }
                    else
                    {
                        goto default;
                    }
                case '&':
                    if (streamReaderWrap.PeekCharacter(1) == '&')
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
                    if (streamReaderWrap.PeekCharacter(1) == '=')
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
                    if (streamReaderWrap.PeekCharacter(1) == '=')
                    {
                        goto default;
                    }
                    
                    var tagDecoration = (byte)GenericDecorationKind.Razor_TagNameOpen;
                    
                    if (context == RazorLexerContextKind.Expect_TagOrText)
                    {
                        _ = streamReaderWrap.ReadCharacter();
                        
                        if (streamReaderWrap.CurrentCharacter == '/')
                        {
                            tagDecoration = (byte)GenericDecorationKind.Razor_TagNameClose;
                            _ = streamReaderWrap.ReadCharacter();
                        }
                        else if (streamReaderWrap.CurrentCharacter == '!')
                        {
                            _ = streamReaderWrap.ReadCharacter();
                        }
                        
                        var tagNameStartPosition = streamReaderWrap.PositionIndex;
                        var tagNameStartByte = streamReaderWrap.ByteIndex;
                        while (!streamReaderWrap.IsEof)
                        {
                            if (!char.IsLetterOrDigit(streamReaderWrap.CurrentCharacter) &&
                                streamReaderWrap.CurrentCharacter != '_' &&
                                streamReaderWrap.CurrentCharacter != '-' &&
                                streamReaderWrap.CurrentCharacter != ':')
                            {
                                break;
                            }
                            _ = streamReaderWrap.ReadCharacter();
                        }
                        var textSpan = new TextEditorTextSpan(
                            tagNameStartPosition,
                            streamReaderWrap.PositionIndex,
                            tagDecoration,
                            tagNameStartByte);
                        if (tagDecoration == (byte)GenericDecorationKind.Razor_TagNameOpen)
                        {
                            textSpanOfMostRecentTagOpen = textSpan;
                        }
                        output.ModelModifier.__SetDecorationByteRange(
                            textSpan.StartInclusiveIndex,
                            textSpan.EndExclusiveIndex,
                            textSpan.DecorationByte);

                        if (streamReaderWrap.CurrentCharacter == '>')
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
                
                    if (streamReaderWrap.PeekCharacter(1) == '=')
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
                    if (streamReaderWrap.NextCharacter == '"')
                    {
                        goto default;
                    }
                    else if (streamReaderWrap.PeekCharacter(1) == '@' && streamReaderWrap.PeekCharacter(2) == '"')
                    {
                        goto default;
                    }
                    else if (streamReaderWrap.NextCharacter == '$')
                    {
                        /*var entryPositionIndex = streamReaderWrap.PositionIndex;
                        var byteEntryIndex = streamReaderWrap.ByteIndex;

                        // The while loop starts counting from and including the first dollar sign.
                        var countDollarSign = 0;
                    
                        while (!streamReaderWrap.IsEof)
                        {
                            if (streamReaderWrap.CurrentCharacter != '$')
                                break;
                            
                            ++countDollarSign;
                            _ = streamReaderWrap.ReadCharacter();
                        }*/
                        
                        goto default;
                        
                        /*if (streamReaderWrap.NextCharacter == '"')
                            LexString(binder, ref lexerOutput, streamReaderWrap, ref previousEscapeCharacterTextSpan, countDollarSign: countDollarSign, useVerbatim: false);*/
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
                    _ = streamReaderWrap.ReadCharacter();
                    break;
            }
        }

        forceExit:
        return output;
    }
    
    private static void SkipHtmlIdentifier(StreamReaderWrap streamReaderWrap)
    {
        while (!streamReaderWrap.IsEof)
        {
            if (!char.IsLetterOrDigit(streamReaderWrap.CurrentCharacter) &&
                streamReaderWrap.CurrentCharacter != '_' &&
                streamReaderWrap.CurrentCharacter != '-' &&
                streamReaderWrap.CurrentCharacter != ':')
            {
                break;
            }
            _ = streamReaderWrap.ReadCharacter();
        }
    }
    
    private static void SkipCSharpdentifier(StreamReaderWrap streamReaderWrap)
    {
        while (!streamReaderWrap.IsEof)
        {
            if (!char.IsLetterOrDigit(streamReaderWrap.CurrentCharacter) &&
                streamReaderWrap.CurrentCharacter != '_')
            {
                break;
            }
            _ = streamReaderWrap.ReadCharacter();
        }
    }
    
    /// <summary>
    /// When this returns true, then the state of the lexer has entirely changed
    /// and the invoker should disregard any of their previous state and reset it.
    ///
    /// This method when finding a brace deliminated code blocked keyword will entirely lex to the close brace.
    /// </summary>
    private static bool SkipCSharpdentifierOrKeyword(
        char[] keywordCheckBuffer,
        StreamReaderWrap streamReaderWrap,
        RazorLexerOutput output,
        bool elsePermitted)
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
    
        var wordStartPosition = streamReaderWrap.PositionIndex;
        var wordStartByte = streamReaderWrap.ByteIndex;
        
        var lengthCharacter = 0;
        var characterIntSum = 0;
        
        int bufferIndex = 0;
    
        while (!streamReaderWrap.IsEof)
        {
            if (!char.IsLetterOrDigit(streamReaderWrap.CurrentCharacter) &&
                streamReaderWrap.CurrentCharacter != '_')
            {
                break;
            }
            
            characterIntSum += (int)streamReaderWrap.CurrentCharacter;
            ++lengthCharacter;
            if (bufferIndex < Clair.CompilerServices.CSharp.BinderCase.CSharpBinder.KeywordCheckBufferSize)
                keywordCheckBuffer[bufferIndex++] = streamReaderWrap.CurrentCharacter;
                
            _ = streamReaderWrap.ReadCharacter();
        }
        
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
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
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
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    break;
                }
                
                goto default;
            case 412: // case
                if (lengthCharacter == 4 &&
                    keywordCheckBuffer[0] ==  'c' &&
                    keywordCheckBuffer[1] ==  'a' &&
                    keywordCheckBuffer[2] ==  's' &&
                    keywordCheckBuffer[3] ==  'e')
                {
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
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
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
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
            
                output.ModelModifier.__SetDecorationByteRange(
                    wordStartPosition,
                    streamReaderWrap.PositionIndex,
                    (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    
                while (!streamReaderWrap.IsEof)
                {
                    if (!char.IsWhiteSpace(streamReaderWrap.CurrentCharacter))
                    {
                        break;
                    }
                    
                    _ = streamReaderWrap.ReadCharacter();
                }
                
                if (streamReaderWrap.CurrentCharacter == '{')
                {
                    LexCSharpCodeBlock(streamReaderWrap, output);
                    return true;
                }
                else
                {
                    return true;
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
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    break;
                }
                
                goto default;
            case 211: // do
                if (lengthCharacter == 2 &&
                    keywordCheckBuffer[0] ==  'd' &&
                    keywordCheckBuffer[1] ==  'o')
                {
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    
                    // Move to start of do statement code block.
                    // Skip whitespace
                    while (!streamReaderWrap.IsEof)
                    {
                        if (!char.IsWhiteSpace(streamReaderWrap.CurrentCharacter))
                            break;
                        _ = streamReaderWrap.ReadCharacter();
                    }
                    LexCSharpCodeBlock(streamReaderWrap, output);
                    
                    // Skip whitespace
                    while (!streamReaderWrap.IsEof)
                    {
                        if (!char.IsWhiteSpace(streamReaderWrap.CurrentCharacter))
                            break;
                        _ = streamReaderWrap.ReadCharacter();
                    }
                    
                    return SkipCSharpdentifierOrKeyword(
                        keywordCheckBuffer,
                        streamReaderWrap,
                        output,
                        elsePermitted: false);
                    
                    break;
                }
                
                goto default;
            case 327: // for
                if (lengthCharacter == 3 &&
                    keywordCheckBuffer[0] ==  'f' &&
                    keywordCheckBuffer[1] ==  'o' &&
                    keywordCheckBuffer[2] ==  'r')
                {
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    
                    // Move to start of for statement condition.
                    while (!streamReaderWrap.IsEof)
                    {
                        if (streamReaderWrap.CurrentCharacter == '(')
                            break;
                        _ = streamReaderWrap.ReadCharacter();
                    }
                    
                    // Move one beyond the end of for statement condition
                    var matchParenthesis = 0;
                    while (!streamReaderWrap.IsEof)
                    {
                        if (streamReaderWrap.CurrentCharacter == '(')
                        {
                            ++matchParenthesis;
                        }
                        else if (streamReaderWrap.CurrentCharacter == ')')
                        {
                            --matchParenthesis;
                            if (matchParenthesis == 0)
                            {
                                _ = streamReaderWrap.ReadCharacter();
                                break;
                            }
                        }
                        _ = streamReaderWrap.ReadCharacter();
                    }
                    
                    // Skip whitespace
                    while (!streamReaderWrap.IsEof)
                    {
                        if (!char.IsWhiteSpace(streamReaderWrap.CurrentCharacter))
                            break;
                        _ = streamReaderWrap.ReadCharacter();
                    }
                    
                    if (streamReaderWrap.CurrentCharacter == '{')
                    {
                        LexCSharpCodeBlock(streamReaderWrap, output);
                        return true;
                    }
                    else
                    {
                        return true;
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
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    
                    // Move to start of foreach statement condition.
                    while (!streamReaderWrap.IsEof)
                    {
                        if (streamReaderWrap.CurrentCharacter == '(')
                            break;
                        _ = streamReaderWrap.ReadCharacter();
                    }
                    
                    // Move one beyond the end of foreach statement condition
                    var matchParenthesis = 0;
                    while (!streamReaderWrap.IsEof)
                    {
                        if (streamReaderWrap.CurrentCharacter == '(')
                        {
                            ++matchParenthesis;
                        }
                        else if (streamReaderWrap.CurrentCharacter == ')')
                        {
                            --matchParenthesis;
                            if (matchParenthesis == 0)
                            {
                                _ = streamReaderWrap.ReadCharacter();
                                break;
                            }
                        }
                        _ = streamReaderWrap.ReadCharacter();
                    }
                    
                    // Skip whitespace
                    while (!streamReaderWrap.IsEof)
                    {
                        if (!char.IsWhiteSpace(streamReaderWrap.CurrentCharacter))
                            break;
                        _ = streamReaderWrap.ReadCharacter();
                    }
                    
                    if (streamReaderWrap.CurrentCharacter == '{')
                    {
                        LexCSharpCodeBlock(streamReaderWrap, output);
                        return true;
                    }
                    else
                    {
                        return true;
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
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    break;
                }
                
                goto default;
            case 425: // !! DUPLICATES !!
                if (lengthCharacter != 4)
                    goto default;
                
                if (elsePermitted &&
                    keywordCheckBuffer[0] ==  'e' &&
                    keywordCheckBuffer[1] ==  'l' &&
                    keywordCheckBuffer[2] ==  's' &&
                    keywordCheckBuffer[3] ==  'e')
                {
                    // else
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    
                    // Move to start of else-if "if" text,
                    // or to start of 'else' codeblock
                    while (!streamReaderWrap.IsEof)
                    {
                        if (streamReaderWrap.CurrentCharacter == 'i')
                        {
                            return SkipCSharpdentifierOrKeyword(
                                keywordCheckBuffer,
                                streamReaderWrap,
                                output,
                                elsePermitted: false);
                        }
                        if (streamReaderWrap.CurrentCharacter == '{')
                            break;
                        _ = streamReaderWrap.ReadCharacter();
                    }
                    
                    // Skip whitespace
                    while (!streamReaderWrap.IsEof)
                    {
                        if (!char.IsWhiteSpace(streamReaderWrap.CurrentCharacter))
                            break;
                        _ = streamReaderWrap.ReadCharacter();
                    }
                    
                    if (streamReaderWrap.CurrentCharacter == '{')
                    {
                        LexCSharpCodeBlock(streamReaderWrap, output);
                        return true;
                    }
                    else
                    {
                        return true;
                    }
                    
                    break;
                }
                else if (keywordCheckBuffer[0] ==  'l' &&
                         keywordCheckBuffer[1] ==  'o' &&
                         keywordCheckBuffer[2] ==  'c' &&
                         keywordCheckBuffer[3] ==  'k')
                {
                    // lock
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
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
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    break;
                }
                
                goto default;
            case 413: // page
                if (lengthCharacter == 4 &&
                    keywordCheckBuffer[0] ==  'p' &&
                    keywordCheckBuffer[1] ==  'a' &&
                    keywordCheckBuffer[2] ==  'g' &&
                    keywordCheckBuffer[3] ==  'e')
                {
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    break;
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
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    break;
                }
                
                goto default;
            case 207: // if
                if (lengthCharacter == 2 &&
                    keywordCheckBuffer[0] ==  'i' &&
                    keywordCheckBuffer[1] ==  'f')
                {
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    
                    // Move to start of if statement condition.
                    while (!streamReaderWrap.IsEof)
                    {
                        if (streamReaderWrap.CurrentCharacter == '(')
                            break;
                        _ = streamReaderWrap.ReadCharacter();
                    }
                    
                    // Move one beyond the end of if statement condition
                    var matchParenthesis = 0;
                    while (!streamReaderWrap.IsEof)
                    {
                        if (streamReaderWrap.CurrentCharacter == '(')
                        {
                            ++matchParenthesis;
                        }
                        else if (streamReaderWrap.CurrentCharacter == ')')
                        {
                            --matchParenthesis;
                            if (matchParenthesis == 0)
                            {
                                _ = streamReaderWrap.ReadCharacter();
                                break;
                            }
                        }
                        _ = streamReaderWrap.ReadCharacter();
                    }
                    
                    // Skip whitespace
                    while (!streamReaderWrap.IsEof)
                    {
                        if (!char.IsWhiteSpace(streamReaderWrap.CurrentCharacter))
                            break;
                        _ = streamReaderWrap.ReadCharacter();
                    }
                    
                    if (streamReaderWrap.CurrentCharacter == '{')
                    {
                        LexCSharpCodeBlock(streamReaderWrap, output);
                        
                        // Skip whitespace
                        while (!streamReaderWrap.IsEof)
                        {
                            if (!char.IsWhiteSpace(streamReaderWrap.CurrentCharacter))
                                break;
                            _ = streamReaderWrap.ReadCharacter();
                        }
                        
                        SkipCSharpdentifierOrKeyword(
                            keywordCheckBuffer,
                            streamReaderWrap,
                            output,
                            elsePermitted: true);
                        
                        return true;
                    }
                    else
                    {
                        return true;
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
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    break;
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
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    break;
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
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    break;
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
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    break;
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
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
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
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
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
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    break;
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
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    break;
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
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                        
                    // Move to start of switch statement condition.
                    while (!streamReaderWrap.IsEof)
                    {
                        if (streamReaderWrap.CurrentCharacter == '(')
                            break;
                        _ = streamReaderWrap.ReadCharacter();
                    }
                    
                    // Move one beyond the end of switch statement condition
                    var matchParenthesis = 0;
                    while (!streamReaderWrap.IsEof)
                    {
                        if (streamReaderWrap.CurrentCharacter == '(')
                        {
                            ++matchParenthesis;
                        }
                        else if (streamReaderWrap.CurrentCharacter == ')')
                        {
                            --matchParenthesis;
                            if (matchParenthesis == 0)
                            {
                                _ = streamReaderWrap.ReadCharacter();
                                break;
                            }
                        }
                        _ = streamReaderWrap.ReadCharacter();
                    }
                    
                    // Skip whitespace
                    while (!streamReaderWrap.IsEof)
                    {
                        if (!char.IsWhiteSpace(streamReaderWrap.CurrentCharacter))
                            break;
                        _ = streamReaderWrap.ReadCharacter();
                    }
                    
                    if (streamReaderWrap.CurrentCharacter == '{')
                    {
                        LexCSharpCodeBlock(streamReaderWrap, output);
                        return true;
                    }
                    else
                    {
                        return true;
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
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
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
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    break;
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
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
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
                    output.ModelModifier.__SetDecorationByteRange(
                        wordStartPosition,
                        streamReaderWrap.PositionIndex,
                        (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                    
                    // Move to start of while statement condition.
                    while (!streamReaderWrap.IsEof)
                    {
                        if (streamReaderWrap.CurrentCharacter == '(')
                            break;
                        _ = streamReaderWrap.ReadCharacter();
                    }
                    
                    // Move one beyond the end of while statement condition
                    var matchParenthesis = 0;
                    while (!streamReaderWrap.IsEof)
                    {
                        if (streamReaderWrap.CurrentCharacter == '(')
                        {
                            ++matchParenthesis;
                        }
                        else if (streamReaderWrap.CurrentCharacter == ')')
                        {
                            --matchParenthesis;
                            if (matchParenthesis == 0)
                            {
                                _ = streamReaderWrap.ReadCharacter();
                                break;
                            }
                        }
                        _ = streamReaderWrap.ReadCharacter();
                    }
                    
                    // Skip whitespace
                    while (!streamReaderWrap.IsEof)
                    {
                        if (!char.IsWhiteSpace(streamReaderWrap.CurrentCharacter))
                            break;
                        _ = streamReaderWrap.ReadCharacter();
                    }
                    
                    if (streamReaderWrap.CurrentCharacter == '{')
                    {
                        LexCSharpCodeBlock(streamReaderWrap, output);
                        return true;
                    }
                    else if (streamReaderWrap.CurrentCharacter == ';')
                    {
                        // This is convenient for the 'do-while' case.
                        // Albeit probably invalid when it is the 'while' case.
                        output.ModelModifier.__SetDecorationByteRange(
                            streamReaderWrap.PositionIndex,
                            streamReaderWrap.PositionIndex + 1,
                            (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
                        _ = streamReaderWrap.ReadCharacter();
                        return true;
                    }
                    else
                    {
                        return true;
                    }
                    
                    break;
                }
                
                goto default;
            default:
                break;
        }
        
        return false;
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
    private static void LexCSharpCodeBlock(StreamReaderWrap streamReaderWrap, RazorLexerOutput output)
    {
        var openBraceStartPosition = streamReaderWrap.PositionIndex;
        var openBraceStartByte = streamReaderWrap.ByteIndex;
        _ = streamReaderWrap.ReadCharacter();
        output.ModelModifier.__SetDecorationByteRange(
            openBraceStartPosition,
            streamReaderWrap.PositionIndex,
            (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
        
        var cSharpStartPosition = streamReaderWrap.PositionIndex;
        var cSharpStartByte = streamReaderWrap.ByteIndex;
        
        var braceMatch = 1;
        
        var isSingleLineComment = false;
        var isMultiLineComment = false;
        var isString = false;
        
        var previousCharWasForwardSlash = false;
        
        while (!streamReaderWrap.IsEof)
        {
            var localPreviousCharWasForwardSlash = previousCharWasForwardSlash;
            
            if (streamReaderWrap.CurrentCharacter == '/')
            {
                previousCharWasForwardSlash = true;
            }
            else
            {
                previousCharWasForwardSlash = false;
            }
        
            if (isMultiLineComment)
            {
                if (streamReaderWrap.CurrentCharacter == '*' && streamReaderWrap.PeekCharacter(1) == '/')
                {
                    _ = streamReaderWrap.ReadCharacter();
                    _ = streamReaderWrap.ReadCharacter();
                    isMultiLineComment = false;
                    continue;
                }
            }
            else if (isSingleLineComment)
            {
                if (streamReaderWrap.CurrentCharacter == '\r' ||
                    streamReaderWrap.CurrentCharacter == '\n')
                {
                    isSingleLineComment = false;
                }
            }
            else if (isString)
            {
                if (streamReaderWrap.CurrentCharacter == '"')
                {
                    isString = false;
                }
            }
            else if (streamReaderWrap.CurrentCharacter == '"')
            {
                isString = true;
            }
            else if (streamReaderWrap.CurrentCharacter == '/')
            {
                if (localPreviousCharWasForwardSlash)
                {
                    isSingleLineComment = true;
                }
            }
            else if (streamReaderWrap.CurrentCharacter == '*')
            {
                if (localPreviousCharWasForwardSlash)
                {
                    isMultiLineComment = true;
                }
            }
            else if (streamReaderWrap.CurrentCharacter == '}')
            {
                if (--braceMatch == 0)
                    break;
            }
            else if (streamReaderWrap.CurrentCharacter == '{')
            {
                ++braceMatch;
            }
        
            _ = streamReaderWrap.ReadCharacter();
        }

        output.ModelModifier.__SetDecorationByteRange(
            cSharpStartPosition,
            streamReaderWrap.PositionIndex,
            (byte)GenericDecorationKind.Razor_CSharpMarker);
        
        // The while loop has 2 break cases, thus !IsEof means "*@" was the break cause.
        if (!streamReaderWrap.IsEof)
        {
            var closeBraceStartPosition = streamReaderWrap.PositionIndex;
            var closeBraceStartByte = streamReaderWrap.ByteIndex;
            
            _ = streamReaderWrap.ReadCharacter();
            output.ModelModifier.__SetDecorationByteRange(
                closeBraceStartPosition,
                streamReaderWrap.PositionIndex,
                (byte)GenericDecorationKind.Razor_InjectedLanguageFragment);
        }
    }
    
    /*
        =======================
        =======================
        1189 // addTagHelper
        980 // attribute
        412 // case
        534 // class
        411 // code
        741 // default
        211 // do
        327 // for
        728 // foreach
        985 // functions
        670 // layout
        425 // lock
        529 // model
        413 // page
        1945 // preservewhitespace
        207 // if
        1086 // implements
        870 // inherits
        637 // inject
        941 // namespace
        !DUPLICATE!:1546/tagHelperPrefix
            1546 // removeTagHelper
            1546 // tagHelperPrefix
        1061 // rendermode
        757 // section
        658 // switch
        351 // try
        979 // typeparam
        550 // using
        537 // while
        =======================
        =======================
    
        var keywordList = new List<string>
        {
            "addTagHelper",
            "attribute",
            "case",
            "class",
            "code",
            "default",
            "do",
            "for",
            "foreach",
            "functions",
            "layout",
            "lock",
            "model",
            "page",
            "preservewhitespace",
            "if",
            "implements",
            "inherits",
            "inject",
            "namespace",
            "removeTagHelper",
            "rendermode",
            "section",
            "switch",
            "tagHelperPrefix",
            "try",
            "typeparam",
            "using",
            "while",
        };
        
        Console.WriteLine("=======================");
        Console.WriteLine("=======================");
        
        var hashSet = new HashSet<int>();
        
        foreach (var keyword in keywordList)
        {
            var sum = 0;
            foreach (var character in keyword)
            {
                sum += (int)character;
            }
            
            if (!hashSet.Add(sum))
            {
                Console.WriteLine($"!DUPLICATE!:{sum}/{keyword}");
            }
            Console.WriteLine($"{sum} // {keyword}");
        }
        
        Console.WriteLine("=======================");
        Console.WriteLine("=======================");
        */
    
    /*
        
        
            
        
            a97 + d100                = 197
            a97 + t116                = 213
            c99 + a97                 = 196
            c99 + l108                = 207
            c99 + o111                = 210
            d100 + e101               = 201
            d100 + o111^              = 211
            f102 + o111 + r114^       = 327
            f102 + o111 + r114 + e101 = 428
            f102 + u117               = 219
            l108 + a97                = 205
            l108 + o111               = 219
            m109                      = 109
            p112 + a97                = 209
            p112 + r114               = 226
            i105 + f102^              = 207
            i105 + m109               = 214
            i105 + n110 + h104        = 319
            i105 + n110 + j106        = 321
            n110                      = 110
            r114 + e101 + m109        = 324
            r114 + e101 + n110        = 325
            s115 + e101               = 216
            s115 + w119               = 234
            t116 + a97                = 213
            t116 + r114               = 230
            t116 + y121               = 237
            u117                      = 117
            w119                      = 119
        
        
        
        ================================================
        ================================================
        
        
        
        
        
            ad
            at
            ca
            cl
            co
            de
            do^
            for^
            fore
            fu
            la
            lo
            m
            pa
            pr
            if^
            im
            inh
            inj
            n
            rem
            ren
            se
            sw
            ta
            tr
            ty
            u
            w
        */
        
        /*
            addTagHelper
            attribute
            case
            class
            code { }
            default
            do
            for
            foreach
            functions { }
            layout
            lock
            model
            page
            preservewhitespace
            if
            implements
            inherits
            inject
            namespace
            removeTagHelper
            rendermode
            section
            switch
            tagHelperPrefix
            try
            typeparam
            using
            while
        */
    
    /*
    https://learn.microsoft.com/en-us/aspnet/core/mvc/views/razor?view=aspnetcore-9.0
    =================================================================================
    
    Conditionals: @if, else if, else, and @switch
    
    Looping: @for, @foreach, @while, and @do while
    
    Compound: @using
    
    ...: @try, catch, finally
    
    ...: @lock
    
    Directives
    ----------
    @attribute [Authorize]                    
    @page "/counter"
    @code { } // More than one @code block is permissible.
    @functions { }  // More than one @functions block is permissible.
    // In Razor components, use @code over @functions to add C# members.
    @implements IDisposable
    @inherits TypeNameOfClassToInheritFrom
    @inherits CustomRazorPage<TModel>
    @model and @inherits can be used in the same view. @inherits can be in a _ViewImports.cshtml file that the view imports:
    @inherits CustomRazorPage<TModel>
    @inject
    @layout
    @model // CSHTML
    @namespace
    @page
    @preservewhitespace
    @rendermode
    @section
    @typeparam
    @using
    
    Directive attributes
    --------------------
    @attributes
    @bind
    @bind:culture
    @formname
    @on{EVENT}
    @on{EVENT}:preventDefault
    @on{EVENT}:stopPropagation
    @key
    @ref
    
    Templated Razor delegates
    -------------------------
    @<tag>...</tag>
    @{ Func<dynamic, object> petTemplate = @<p>You have a pet named <strong>@item.Name</strong>.</p>; }
    
    Tag Helpers // MVC
    ------------------
    @addTagHelper
    @removeTagHelper
    @tagHelperPrefix
    
    Razor reserved keywords
    -----------------------
    page
    namespace
    functions
    inherits
    model
    section
    helper (Not currently supported by ASP.NET Core)
    // Razor keywords are escaped with @(Razor Keyword) (for example, @(functions)).

    C# Razor keywords
    -----------------
    case
    do
    default
    for
    foreach
    if
    else
    lock
    switch
    try
    catch
    finally
    using
    while
    C# Razor keywords must be double-escaped with @(@C# Razor Keyword) (for example, @(@case)).
        The first @ escapes the Razor parser.
        The second @ escapes the C# parser.
    
    Reserved keywords not used by Razor
    -----------------------------------
    class
    
    Templating methods
    ------------------
    
    Flat A
    
    https://learn.microsoft.com/en-us/aspnet/core/mvc/views/razor?view=aspnetcore-9.0
    =================================================================================
    
    @if, else if, else
    @switch
    @for,
    @foreach,
    @while,
    @do while
    @using
    @try, catch, finally
    @lock
    @attribute [Authorize]                    
    @page "/counter"
    @code { } // More than one @code block is permissible.
    @functions { }  // More than one @functions block is permissible.
    // In Razor components, use @code over @functions to add C# members.
    @implements IDisposable
    @inherits TypeNameOfClassToInheritFrom
    @inherits CustomRazorPage<TModel>
    @model and @inherits can be used in the same view. @inherits can be in a _ViewImports.cshtml file that the view imports:
    @inherits CustomRazorPage<TModel>
    @inject
    @layout
    @model // CSHTML
    @namespace
    @page
    @preservewhitespace
    @rendermode
    @section
    @typeparam
    @using
    @<tag>...</tag>
    @{ Func<dynamic, object> petTemplate = @<p>You have a pet named <strong>@item.Name</strong>.</p>; }
    @addTagHelper
    @removeTagHelper
    @tagHelperPrefix
    page
    namespace
    functions
    inherits
    model
    section
    helper (Not currently supported by ASP.NET Core)
    case
    do
    default
    for
    foreach
    if
    else
    lock
    switch
    try
    catch
    finally
    using
    while
    class
    
    Directive attributes
    --------------------
    @attributes
    @bind
    @bind:culture
    @formname
    @on{EVENT}
    @on{EVENT}:preventDefault
    @on{EVENT}:stopPropagation
    @key
    @ref
    
    Flat B
    
    https://learn.microsoft.com/en-us/aspnet/core/mvc/views/razor?view=aspnetcore-9.0
    =================================================================================
    
    if
    switch
    for
    foreach
    while
    do
    using
    try
    lock
    attribute
    page
    code { }
    functions { }
    implements
    inherits
    inject
    layout
    namespace
    page
    preservewhitespace
    rendermode
    section
    typeparam
    using
    addTagHelper
    removeTagHelper
    tagHelperPrefix
    page
    namespace
    functions
    inherits
    model
    section
    case
    do
    default
    for
    foreach
    if
    else
    lock
    switch
    try
    catch
    finally
    using
    while
    class
    
    Flat C
    
    https://learn.microsoft.com/en-us/aspnet/core/mvc/views/razor?view=aspnetcore-9.0
    =================================================================================
    
    addTagHelper
    attribute
    case
    class
    code { }
    default
    do
    for
    foreach
    functions { }
    layout
    lock
    model
    page
    preservewhitespace
    if
    implements
    inherits
    inject
    namespace
    removeTagHelper
    rendermode
    section
    switch
    tagHelperPrefix
    try
    typeparam
    using
    while
    
    Flat D
    
    https://learn.microsoft.com/en-us/aspnet/core/mvc/views/razor?view=aspnetcore-9.0
    =================================================================================
    
    a
     ddTagHelper
     ttribute
     
    c
     ase
     lass
     ode { }
     
    d
     efault
     o
     
    f
     or
     oreach
     unctions { }
     
    l
     ayout
     ock
     
    m
     odel
     
    p
     age
     reservewhitespace
     
    i
     f
     mplements
     nherits
     nject
     
    n
     amespace
     
    r
     emoveTagHelper
     endermode
     
    s
     ection
     witch
     
    t
     agHelperPrefix
     ry
     ypeparam
     
    u
     sing
     
    w
     hile
    
    Flat E
    
    https://learn.microsoft.com/en-us/aspnet/core/mvc/views/razor?view=aspnetcore-9.0
    =================================================================================
    
    ad
      dTagHelper
    at
      tribute
     
    ca
      se
    cl
      ass
    co
      de { }
     
    de
      fault
    do
      // 'do' was the word itself
     
    fo
      r
      reach
    fu
      nctions { }
     
    la
      yout
    lo
      ck
     
    m
     odel
     
    pa
      ge
    pr
      eservewhitespace
     
    if
      // 'if' was the word itself
    im
      plements
    in
      herits
      ject
     
    n
     amespace
     
    re
      moveTagHelper
      ndermode
     
    se
      ction
    sw
      itch
     
    ta
      gHelperPrefix
    tr
      y
    ty
      peparam
     
    u
     sing
     
    w
     hile
    
    Flat F
    
    https://learn.microsoft.com/en-us/aspnet/core/mvc/views/razor?view=aspnetcore-9.0
    =================================================================================
    
    ad
      dTagHelper
    at
      tribute
     
    ca
      se
    cl
      ass
    co
      de { }
     
    de
      fault
    do
      // 'do' was the word itself
     
    for
       // 'for' was a word itself
       each
    fu
      nctions { }
     
    la
      yout
    lo
      ck
     
    m
     odel
     
    pa
      ge
    pr
      eservewhitespace
     
    if
      // 'if' was the word itself
    im
      plements
    inh
       erits
    inj
       ect
     
    n
     amespace
     
    rem
       oveTagHelper
    ren
       dermode
     
    se
      ction
    sw
      itch
     
    ta
      gHelperPrefix
    tr
      y
    ty
      peparam
     
    u
     sing
     
    w
     hile
    
    Flat G
            
    https://learn.microsoft.com/en-us/aspnet/core/mvc/views/razor?view=aspnetcore-9.0
    =================================================================================
    
    ad
    at
     
    ca
    cl
    co
     
    de
    do // 'do' was a word itself
      
     
    for // 'for' was a word itself
       each
    fu
     
    la
    lo
     
    m
     
    pa
    pr
     
    if // 'if' was a word itself
    im
    inh
    inj
     
    n
     
    rem
    ren
     
    se
    sw
     
    ta
    tr
    ty
     
    u
     
    w
    
    Flat H
            
    https://learn.microsoft.com/en-us/aspnet/core/mvc/views/razor?view=aspnetcore-9.0
    =================================================================================
    
    ad
    at
    ca
    cl
    co
    de
    do^
    for^
    fore
    fu
    la
    lo
    m
    pa
    pr
    if^
    im
    inh
    inj
    n
    rem
    ren
    se
    sw
    ta
    tr
    ty
    u
    w
    */
}






/*switch (binder.CSharpCompilerService.SafeGetText(lexerOutput.ResourceUri.Value, textSpan) ?? string.Empty)
{
    // NonContextualKeywords-NonControl
    // ================================
    
    // case "abstract":
        
    // case "as":
        
    // case "base":
        
    // case "bool":
        
    // case "byte":
        
    // case "catch":
        
    // case "char":
        
    // case "checked":
        
    // case "class":
        
    // case "const":
        
    // case "decimal":
        
    // case "default":
        
    // case "delegate":
        
    // case "double":
        
    // case "enum":
        
    // case "event":
        
    // case "explicit":
        
    // case "extern":
        
    // case "false":
        
    // case "finally":
        
    // case "fixed":
        
    // case "float":
        
    // case "implicit":
        
    // case "in":
        
    // case "int":
        
    // case "interface":
        
    // case "internal":
        
    // case "is":
        
    // case "lock":
        
    // case "long":
        
    // case "namespace":
        
    // case "new":
        
    // case "null":
        
    // case "object":
        
    // case "operator":
        
    // case "out":
        
    // case "override":
        
    // case "params":
        
    // case "private":
        
    // case "protected":
        
    // case "public":
        
    // case "readonly":
        
    // case "ref":
        
    // case "sbyte":
        
    // case "sealed":
        
    // case "short":
        
    // case "sizeof":
        
    // case "stackalloc":
        
    // case "static":
        
    // case "string":
        
    // case "struct":
        
    // case "this":
        
    // case "true":
        
    // case "try":
        
    // case "typeof":
        
    // case "uint":
        
    // case "ulong":
        
    // case "unchecked":
        
    // case "unsafe":
        
    // case "ushort":
        
    // case "using":
        
    // case "virtual":
        
    // case "void":
        
    // case "volatile":
        
    // NonContextualKeywords-IsControl
    // ===============================
    // case "break":
        
    // case "case":
        
    // case "continue":
        
    // case "do":
        
    // case "else":
        
    // case "for":
        
    // case "foreach":
        
    // case "goto":
        
    // case "if":
        
    // case "return":
        
    // case "switch":
        
    // case "throw":
        
    // case "while":
        
    // ContextualKeywords-NotControl
    // =============================
    // case "add":
        
    // case "and":
        
    // case "alias":
        
    // case "ascending":
        
    // case "args":
        
    // case "async":
        
    // case "await":
        
    // case "by":
        
    // case "descending":
        
    // case "dynamic":
        
    // case "equals":
        
    // case "file":
        
    // case "from":
        
    // case "get":
        
    // case "global":
        
    // case "group":
        
    // case "init":
        
    // case "into":
        
    // case "join":
        
    // case "let":
        
    // case "managed":
        
    // case "nameof":
        
    // case "nint":
        
    // case "not":
        
    // case "notnull":
        
    // case "nuint":
        
    // case "on":
        
    // case "or":
        
    // case "orderby":
        
    // case "partial":
        
    // case "record":
        
    // case "remove":
        
    // case "required":
        
    // case "scoped":
        
    // case "select":
        
    // case "set":
        
    // case "unmanaged":
        
    // case "value":
        
    // case "var":
        
    // case "when":
        
    // case "where":
        
    // case "with":
        
    // ContextualKeywords-IsControl
    // ============================
    // case "yield":
        
    default:
        lexerOutput.SyntaxTokenList.Add(new SyntaxToken(SyntaxKind.IdentifierToken, textSpan));
        return;
}*/








/*
var keywordList = new List<string>
        {
            // NonContextualKeywords-NonControl
            // ================================
            "abstract",
            "as",
            "base",
            "bool",
            "byte",
            "catch",
            "char",
            "checked",
            "class",
            "const",
            "decimal",
            "default",
            "delegate",
            "double",
            "enum",
            "event",
            "explicit",
            "extern",
            "false",
            "finally",
            "fixed",
            "float",
            "implicit",
            "in",
            "int",
            "interface",
            "internal",
            "is",
            "lock",
            "long",
            "namespace",
            "new",
            "null",
            "object",
            "operator",
            "out",
            "override",
            "params",
            "private",
            "protected",
            "public",
            "readonly",
            "ref",
            "sbyte",
            "sealed",
            "short",
            "sizeof",
            "stackalloc",
            "static",
            "string",
            "struct",
            "this",
            "true",
            "try",
            "typeof",
            "uint",
            "ulong",
            "unchecked",
            "unsafe",
            "ushort",
            "using",
            "virtual",
            "void",
            "volatile",
            // NonContextualKeywords-IsControl
            // ===============================
            "break",
            "case",
            "continue",
            "do",
            "else",
            "for",
            "foreach",
            "goto",
            "if",
            "return",
            "switch",
            "throw",
            "while",
            // ContextualKeywords-NotControl
            // =============================
            "add",
            "and",
            "alias",
            "ascending",
            "args",
            "async",
            "await",
            "by",
            "descending",
            "dynamic",
            "equals",
            "file",
            "from",
            "get",
            "global",
            "group",
            "init",
            "into",
            "join",
            "let",
            "managed",
            "nameof",
            "nint",
            "not",
            "notnull",
            "nuint",
            "on",
            "or",
            "orderby",
            "partial",
            "record",
            "remove",
            "required",
            "scoped",
            "select",
            "set",
            "unmanaged",
            "value",
            "var",
            "when",
            "where",
            "with",
            // ContextualKeywords-IsControl
            // ============================
            "yield",
        };
        
        Console.WriteLine("=======================");
        Console.WriteLine("=======================");
        
        // var hashSet = new HashSet<int>();
        
        foreach (var keyword in keywordList)
        {
            var sum = 0;
            foreach (var character in keyword)
            {
                sum += (int)character;
            }
            
            // if (!hashSet.Add(sum))
            // {
            //    Console.WriteLine($"!DUPLICATE!:{sum}/{keyword}");
            //}
            Console.WriteLine($"case {sum}: // {keyword}");
        }
        
        Console.WriteLine("=======================");
        Console.WriteLine("=======================");
*/

/*var keywordList = new List<string>
        {
            // NonContextualKeywords-NonControl
            // ================================
            "abstract",
            "as",
            "base",
            "bool",
            "byte",
            "catch",
            "char",
            "checked",
            "class",
            "const",
            "decimal",
            "default",
            "delegate",
            "double",
            "enum",
            "event",
            "explicit",
            "extern",
            "false",
            "finally",
            "fixed",
            "float",
            "implicit",
            "in",
            "int",
            "interface",
            "internal",
            "is",
            "lock",
            "long",
            "namespace",
            "new",
            "null",
            "object",
            "operator",
            "out",
            "override",
            "params",
            "private",
            "protected",
            "public",
            "readonly",
            "ref",
            "sbyte",
            "sealed",
            "short",
            "sizeof",
            "stackalloc",
            "static",
            "string",
            "struct",
            "this",
            "true",
            "try",
            "typeof",
            "uint",
            "ulong",
            "unchecked",
            "unsafe",
            "ushort",
            "using",
            "virtual",
            "void",
            "volatile",
            // NonContextualKeywords-IsControl
            // ===============================
            "break",
            "case",
            "continue",
            "do",
            "else",
            "for",
            "foreach",
            "goto",
            "if",
            "return",
            "switch",
            "throw",
            "while",
            // ContextualKeywords-NotControl
            // =============================
            "add",
            "and",
            "alias",
            "ascending",
            "args",
            "async",
            "await",
            "by",
            "descending",
            "dynamic",
            "equals",
            "file",
            "from",
            "get",
            "global",
            "group",
            "init",
            "into",
            "join",
            "let",
            "managed",
            "nameof",
            "nint",
            "not",
            "notnull",
            "nuint",
            "on",
            "or",
            "orderby",
            "partial",
            "record",
            "remove",
            "required",
            "scoped",
            "select",
            "set",
            "unmanaged",
            "value",
            "var",
            "when",
            "where",
            "with",
            // ContextualKeywords-IsControl
            // ============================
            "yield",
        };
        
        Console.WriteLine("=======================");
        Console.WriteLine("=======================");
        
        // var hashSet = new HashSet<int>();
        
        var longestKeyword = string.Empty;
        
        foreach (var keyword in keywordList)
        {
            if (keyword.Length > longestKeyword.Length)
                longestKeyword = keyword;
        
            var sum = 0;
            foreach (var character in keyword)
            {
                sum += (int)character;
            }
            
            // if (!hashSet.Add(sum))
            // {
            //    Console.WriteLine($"!DUPLICATE!:{sum}/{keyword}");
            //}
            Console.WriteLine($"case {sum}: // {keyword}");
        }
        
        Console.WriteLine($"{longestKeyword} {longestKeyword.Length}");
        
        Console.WriteLine("=======================");
        Console.WriteLine("=======================");*/
        
        











