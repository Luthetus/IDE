using Clair.Extensions.CompilerServices.Syntax;

namespace Clair.Tests.csproj;

public class LexerTests
{
    // Essentially the idea is:
    //
    // Currently the Lexer stores a List of SyntaxToken.
    // The Lexer runs in its entirety, and adds every token to that List.
    //
    // There are two branches that you can encounter problems from here.
    //
    // Either you aren't caching the list of SyntaxToken, then clearing it each time you start a Lex
    //     - In this scenario you're going to have extremely expensive allocations due to the amortized storage of a List
    //     - It starts at capacity of 0, then increases in capacity with respect to your needs.
    //     - each increase in capacity however is an allocation, and if you need space
    //           to store thousands upon thousands of items contiguously, then this is quite an "expensive" allocation.
    // OR
    // You are caching it.
    //     - This means you now have a List of syntax token that you only use while a Lex is being performed.
    //     - This List has a contiguous block of memory for thousands upon thousands of SyntaxToken.
    //     - This is just sitting in memory all the time.
    //     - After a GC collection the generational heap in question is defragged. Thus you have to copy and move the memory around.
    //     - Now you're defragging with this massive List sitting in the Large Object Heap (LOH).
    //     - It depends what generation you collected but in the case where you collect the Large Object Heap it is going to slow things down.
    //     - Add to that the fact that the large object heap (LOH) is only collected during a gen 2 collection, and I speculate
    //           that gen 2 collections would therefore be slowed down quite a bit by this massive List of SyntaxToken.
    //
    // So, you can instead "yield return" each token to the Parser inside a loop.
    
    //
    
    // Similarly, a previous optimization I have done was to no longer read file text into memory. Instead I stream the text as needed.
    // I mention this because the optimization of "yield return"ing to the parser each token is "similar" in concept to streaming
    // you don't want to "read all the tokens into memory" at once.
    //
    // I have to go do yard work but... StreamReaderWrap is essentially the exact code needed to do this change.
    // The filesystem is streamed via a StreamReader, the StreamReader is an enumerable pattern,
    // my StreamReaderWrap abstracts the enumerable pattern into a foreach variable pattern.
    //
    // I need an "abstraction" on the Lexer that will stream to me each token as if I were iterating over a foreach loop.
    //
    // 
    
    // Okay, like I said I have to go do yard work.
    // I'm waiting on a background check from QuickChek to start 12 hours a week doing freezer things.
    //
    // I will make this idea. And I am hurt. 
    
    // Concerns: State maintenance
    // - yield return should do all of this for me if I end up using it.
    //     - If I use yield return, then I'm going to have to modify a lot of code because I'm using various keywords
    //       that I believe are not allowed in a yield context. ( I think 'ref' is one )
    //
    // - String interpolation
    //   - is there any edge case scenarios involved with this?
    //
    // ````public class Foo { }
    //
    // public
    // class
    // Foo
    // {
    // }
    //
    // Original code:
    // ```csharp
    // 
    //
    // var syntaxTokenList = CSharpLexer.Lex(stream);
    //
    // Console.WriteLine(syntaxTokenList);
    // # [
    // #    public
    // #    class
    // #    Foo
    // #    {
    // #    }
    // # ];
    //
    // var compilationUnit = new CSharpCompilationUnit();
    // CSharpParser.Parse(syntaxTokenList, ref compilationUnit);
    // ```
    //
    // New code:
    // ```csharp
    //
    // var compilationUnit = new CSharpCompilationUnit();
    // var state = new CompilationUnitState();
    //
    // # ...
    //
    //
    // # I don't want to allocate a parser everytime I parse a C# file, thus I'm making this static.
    // # A ref to the struct "state" will be similar to having an object instance.
    // # I want the state to be a struct because this method is possibly invoked with extreme frequency.
    // # For example, the solution wide parse will in under 4 seconds invoke the method 'COUNT_CSHARP_FILES * 2'
    // # and maybe ClairIde.sln has ~1,000 C# files.
    // # All this needs to be then cleaned up by the garbage collector.
    // # You might think, "well that isn't a big deal, surely the GC will clear it no issue".
    // # But you can't be certain about that. The memory that you think will easily be GC'd might
    // # sit around in the respective generational heap and go through promotions rather than being cleared as you expected.
    // # So I always like to just avoid the allocation in the first place if it is an option because you never "really know"
    // # what the GC will do. (you might have some reference that is alive, but you didn't realize it... etc...)
    // 
    //
    // public static class Parser
    // {
    //     public static void Parse(ref CompilationUnitState state)
    //     {
    //         // If I'm going to have the Lexer and Parser share the same state,
    //         // I can stay organized while I plan out the solution by just
    //         // prefixing 'Lexer' to any state relating to the Lexer.
    //         //
    //         // If you go on to make the API public, and used in production code.
    //         // Then you've just cemented the 'Lexer_' prefix.
    //         // The issue with that is 'Lexer_' is not necessarily the easiest to read code.
    //         // But you can continue focusing in the short term on the actual algorithms, rather than names.
    //         //
    //         // I mean, all IDE's have the ability to rename refactor don't they?
    //         // So when you settled on the final name you just refactor it (and wait to make it public and in production code).
    //         // What is a "rename refactor"... I totally can do that I just need the moment to be perfect for me to demonstrate the feature...
    //         //
    //         // A lot of this state for the 'Lexer_' prefixed properties would come from the "original code".
    //         // Any "lexer related state" in the "original code" can likely be moved to this shared state between the Lexer and Parser
    //         // in the new code.
    //         //
    //         // I mention this because I'm leaning towards NOT using "yield return" here. So I'd have to manage the state myself.
    //         // I think I maintain the opinion that I'll start by Lexing in "blocks".
    //         // This means I'll still have a List<SyntaxToken>, but this List won't be a capacity that fits thousands of thousands of SyntaxToken,
    //         // it preferably would be an extremely small capacity.
    //         // Maybe a capacity of '4'.
    //         // 
    //         // I'll call this a 'cache' not a 'block'.
    //         // I'm a bit torn between the two words.
    //         // What I plan doesn't feel quite like a "cache" it feels slightly different
    //         // but I'll call it a "cache" from now on.
    // 
    //         
    //         while (!state.Lexer_IsEof)
    //         {
    //             
    //         }
    //     }
    // }
    //
    // # 'CSharpCompilationUnit' is output
    // # 'CSharpParserState' changes to be 'CompilationUnitState'
    //
    // # The separation between 'CSharpCompilationUnit' and 'CompilationUnitState' relates to
    // # "computational state" that can "immediately" have any memory associated with it cleared upon the function returning.
    // # versus "resulting state" that needs to be stored long term.
    // ```
    //
    
    // I'm recording a small part of my screen
    // not nearly enough space for this...
    
    public class Lexer
    {
        public SyntaxToken CurrentToken { get; set; }
    }
    
    public class Parser
    {
        public void Parse()
        {
            var lexer = new Lexer();
            
            // "foreach variable" pattern
            // OR
            // enumerable pattern
            //
            
            // Enumerable pattern is likely prefered but
            // I'm most comfortable with "foreach variable" pattern
            // and I have a lot of things to do before I bother with considering changing to enumerable pattern.
            
            while (true)
            {
                lexer.Lex();
            }
        }
    }

    [Fact]
    public void FastTurnoverRunSomething()
    {
        var parser = new Parser();
        
    }
}
