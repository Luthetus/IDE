using Microsoft.AspNetCore.Components;
using Clair.Common.RazorLib.FileSystems.Models;

namespace Clair.Common.RazorLib.FileSystems.Displays;

public partial class FileTemplatesDisplay : ComponentBase
{
    [Parameter, EditorRequired]
    public string FileName { get; set; } = null!;

    private List<FileTemplatesFormWrapper> _fileTemplatesFormWrappersList = new();
    private List<FileTemplatesFormWrapper> _relatedMatchWrappersList = new();
    private FileTemplatesFormWrapper? _exactMatchWrapper;
    public FileTemplate? ExactMatchFileTemplate => _exactMatchWrapper?.FileTemplate;

    private readonly List<FileTemplate> _emptyFileTemplateList = new List<FileTemplate>();

    public List<FileTemplate>? RelatedMatchFileTemplates => _relatedMatchWrappersList
        .Where(x => x.IsChecked)
        .Select(x => x.FileTemplate)
        .ToList();

    protected override void OnInitialized()
    {
        _fileTemplatesFormWrappersList =
            // The order of the entries in <see cref="_fileTemplatesList"/> is important
            // as the .FirstOrDefault(x => ...true...) is used.
            new List<FileTemplate>()
            {
                CommonFacts.RazorCodebehind,
                CommonFacts.RazorMarkup,
                CommonFacts.CSharpClass
            }
            .Select(x => new FileTemplatesFormWrapper(x, true))
            .ToList();
    }

    private class FileTemplatesFormWrapper
    {
        public FileTemplatesFormWrapper(FileTemplate fileTemplate, bool isChecked)
        {
            FileTemplate = fileTemplate;
            IsChecked = isChecked;
        }

        public FileTemplate FileTemplate { get; }
        public bool IsChecked { get; set; }
    }

    private void GetRelatedFileTemplates()
    {
        if (_exactMatchWrapper is null)
        {
            _relatedMatchWrappersList = new();
            return;
        }

        var relatedMatches = CommonFacts.GetRelatedFileTemplates(_exactMatchWrapper.FileTemplate.FileExtensionNoPeriod);
        relatedMatches ??= _emptyFileTemplateList;

        _relatedMatchWrappersList = relatedMatches
            .Select(rel => _fileTemplatesFormWrappersList.First(wrap => rel.Id == wrap.FileTemplate.Id))
            .ToList();
    }
}
