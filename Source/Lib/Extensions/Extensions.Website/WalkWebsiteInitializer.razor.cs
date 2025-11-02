using System.Text;
using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib.BackgroundTasks.Models;
using Clair.Common.RazorLib.Keys.Models;
using Clair.Common.RazorLib.FileSystems.Models;
using Clair.TextEditor.RazorLib;
using Clair.TextEditor.RazorLib.Lexers.Models;
using Clair.TextEditor.RazorLib.TextEditors.Models;
using Clair.Extensions.DotNet;
using Clair.Extensions.DotNet.DotNetSolutions.Models;
using Clair.Website.RazorLib.Websites.ProjectTemplates.Models;
using Clair.Ide.Wasm.Facts;

namespace Clair.Website.RazorLib;

public partial class ClairWebsiteInitializer : ComponentBase
{
    [Inject]
    private DotNetService DotNetService { get; set; } = null!;

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            DotNetService.TextEditorService.CommonService.Continuous_Enqueue(new BackgroundTask(
                Key<IBackgroundTaskGroup>.Empty,
                Do_ClairWebsiteInitializerOnAfterRenderAsync));
        }
        
        return Task.CompletedTask;
    }
    
    public async ValueTask Do_ClairWebsiteInitializerOnAfterRenderAsync()
    {
        await ParseSolutionAsync().ConfigureAwait(false);

        // This code block is hacky. I want the Solution Explorer to from the get-go be fully expanded, so the user can see 'Program.cs'
        {
            DotNetService.TextEditorService.CommonService.TreeView_MoveRight(
                DotNetSolutionState.TreeViewSolutionExplorerStateKey,
                false,
                false);

            DotNetService.TextEditorService.CommonService.TreeView_MoveRight(
                DotNetSolutionState.TreeViewSolutionExplorerStateKey,
                false,
                false);

            DotNetService.TextEditorService.CommonService.TreeView_MoveRight(
                DotNetSolutionState.TreeViewSolutionExplorerStateKey,
            false,
                false);
        }
    }

    private async Task ParseSolutionAsync()
    {
        var tokenBuilder = new StringBuilder();
        var formattedBuilder = new StringBuilder();
    
        // Create a Blazor Wasm app
        var parentDirectoryOfProject = "/BlazorCrudApp/ConsoleApp/";

        if (parentDirectoryOfProject is null)
            throw new NotImplementedException();

        var ancestorDirectory = parentDirectoryOfProject;
        
        // Ensure Parent Directories Exist
        {
            InMemoryFileSystemProvider inMemoryFileSystemProvider = (InMemoryFileSystemProvider)DotNetService.CommonService.FileSystemProvider;

            await ((InMemoryFileSystemProvider.InMemoryDirectoryHandler)inMemoryFileSystemProvider.Directory).UnsafeCreateDirectoryAsync(
                "/BlazorCrudApp/");
            await ((InMemoryFileSystemProvider.InMemoryDirectoryHandler)inMemoryFileSystemProvider.Directory).UnsafeCreateDirectoryAsync(
                "/BlazorCrudApp/ConsoleApp/");
        }
        
        TextEditorModel programCsModel;
        // ProgramCs
        {
            var absolutePathString = "/BlazorCrudApp/ConsoleApp/Program.cs";
            
            Website_WriteAllText(
                absolutePathString,
                InitialSolutionFacts.PERSON_CS_CONTENTS,
                tokenBuilder,
                formattedBuilder);

            programCsModel = new TextEditorModel(
                new ResourceUri(absolutePathString),
                DateTime.UtcNow,
                "cs",
                InitialSolutionFacts.PERSON_CS_CONTENTS,
                DotNetService.TextEditorService.GetDecorationMapper("cs"),
                DotNetService.TextEditorService.GetCompilerService("cs"),
                DotNetService.TextEditorService);
        }
        
        TextEditorModel csprojModel;
        // Csproj
        {
            
            Website_WriteAllText(
                InitialSolutionFacts.BLAZOR_CRUD_APP_WASM_CSPROJ_ABSOLUTE_FILE_PATH,
                ConsoleAppFacts.CsprojContents,
                tokenBuilder,
                formattedBuilder);
            
            csprojModel = new TextEditorModel(
                new ResourceUri(InitialSolutionFacts.BLAZOR_CRUD_APP_WASM_CSPROJ_ABSOLUTE_FILE_PATH),
                DateTime.UtcNow,
                "csproj",
                ConsoleAppFacts.CsprojContents,
                DotNetService.TextEditorService.GetDecorationMapper("csproj"),
                DotNetService.TextEditorService.GetCompilerService("csproj"),
                DotNetService.TextEditorService);
        }
        
        var solutionAbsolutePath = new AbsolutePath(
            InitialSolutionFacts.SLN_ABSOLUTE_FILE_PATH,
            false,
            DotNetService.TextEditorService.CommonService.FileSystemProvider,
            tokenBuilder,
            formattedBuilder,
            AbsolutePathNameKind.NameWithExtension);
        
        TextEditorModel exampleSolutionModel;
        // ExampleSolution.sln
        {
            Website_WriteAllText(
                InitialSolutionFacts.SLN_ABSOLUTE_FILE_PATH,
                InitialSolutionFacts.SLN_CONTENTS,
                tokenBuilder,
                formattedBuilder);
            
            var absolutePath = solutionAbsolutePath;

            exampleSolutionModel = new TextEditorModel(
                new ResourceUri(InitialSolutionFacts.SLN_ABSOLUTE_FILE_PATH),
                DateTime.UtcNow,
                "sln",
                InitialSolutionFacts.SLN_CONTENTS,
                DotNetService.TextEditorService.GetDecorationMapper("sln"),
                DotNetService.TextEditorService.GetCompilerService("sln"),
                DotNetService.TextEditorService);            
        }

        // This line is also in ClairExtensionsDotNetInitializer,
        // but its duplicated here because the website
        // won't open the first file correctly without this.
        DotNetService.TextEditorService.UpsertHeader("cs", typeof(Clair.Extensions.CompilerServices.Displays.TextEditorCompilerServiceHeaderDisplay));

        DotNetService.Enqueue(new DotNetWorkArgs
        {
            WorkKind = DotNetWorkKind.SetDotNetSolution,
            DotNetSolutionAbsolutePath = solutionAbsolutePath,
        });
        
        DotNetService.TextEditorService.WorkerArbitrary.PostUnique(editContext =>
        {
            // programCsModel
            {
                DotNetService.TextEditorService.Model_RegisterCustom(editContext, programCsModel);
                
                var modelModifier = editContext.GetModelModifier(programCsModel.PersistentState.ResourceUri);
    
                if (modelModifier is null)
                    return ValueTask.CompletedTask;
    
                modelModifier.PerformRegisterPresentationModelAction(TextEditorFacts.FindOverlayPresentation_EmptyPresentationModel);
    
                programCsModel.PersistentState.CompilerService.RegisterResource(
                    programCsModel.PersistentState.ResourceUri,
                    shouldTriggerResourceWasModified: true);
            }
            
            /*// csprojModel
            {
                DotNetService.TextEditorService.Model_RegisterCustom(editContext, csprojModel);
                
                var modelModifier = editContext.GetModelModifier(csprojModel.PersistentState.ResourceUri);
    
                if (modelModifier is null)
                    return ValueTask.CompletedTask;
    
                DotNetService.TextEditorService.Model_AddPresentationModel(
                    editContext,
                    modelModifier,
                    TextEditorFacts.FindOverlayPresentation_EmptyPresentationModel);
    
                csprojModel.PersistentState.CompilerService.RegisterResource(
                    csprojModel.PersistentState.ResourceUri,
                    shouldTriggerResourceWasModified: true);
            }
            
            // exampleSolutionModel
            {
                DotNetService.TextEditorService.Model_RegisterCustom(editContext, exampleSolutionModel);
                
                var modelModifier = editContext.GetModelModifier(exampleSolutionModel.PersistentState.ResourceUri);
    
                if (modelModifier is null)
                    return ValueTask.CompletedTask;
    
                DotNetService.TextEditorService.Model_AddPresentationModel(
                    editContext,
                    modelModifier,
                    TextEditorFacts.FindOverlayPresentation_EmptyPresentationModel);
    
                exampleSolutionModel.PersistentState.CompilerService.RegisterResource(
                    exampleSolutionModel.PersistentState.ResourceUri,
                    shouldTriggerResourceWasModified: true);
            }*/
            
            return ValueTask.CompletedTask;
        });
        
        DotNetService.TextEditorService.WorkerArbitrary.PostUnique(async editContext =>
        {
            // Display a file from the get-go so the user is less confused on what the website is.
            var absolutePath = new AbsolutePath(
                "/BlazorCrudApp/ConsoleApp/Program.cs",
                false,
                DotNetService.TextEditorService.CommonService.FileSystemProvider,
                tokenBuilder,
                formattedBuilder,
                AbsolutePathNameKind.NameWithExtension);
        
            await DotNetService.TextEditorService.OpenInEditorAsync(
                editContext,
                absolutePath.Value,
                false,
                null,
                new Category("main"),
                DotNetService.TextEditorService.NewViewModelKey());
        });
    }
    
    public void Website_WriteAllText(
        string absolutePathString,
        string contents,
        StringBuilder stringBuilder,
        StringBuilder formattedBuilder)
    {
        InMemoryFileSystemProvider inMemoryFileSystemProvider = (InMemoryFileSystemProvider)DotNetService.CommonService.FileSystemProvider;

        var absolutePath = new AbsolutePath(
            absolutePathString,
            false,
            DotNetService.CommonService.FileSystemProvider,
            tokenBuilder: stringBuilder,
            formattedBuilder,
            AbsolutePathNameKind.NameWithExtension);

        var outFile = new InMemoryFile(
            contents,
            absolutePath,
            DateTime.UtcNow,
            false);

        inMemoryFileSystemProvider.__Files.Add(outFile);

        DotNetService.CommonService.FileSystemProvider.DeletionPermittedRegister(
            new SimplePath(absolutePathString, isDirectory: false),
            tokenBuilder: stringBuilder,
            formattedBuilder);
    }
}
