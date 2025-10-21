namespace Clair.Common.RazorLib;

public partial class CommonService
{
    public async ValueTask Storage_SetValue(string key, object? value)
    {
        await JsRuntimeCommonApi.LocalStorageSetItem(
                key,
                value)
            .ConfigureAwait(false);
    }
}
