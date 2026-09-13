using Atlas.Ui.Api.Generated;
using Atlas.Ui.Services;
using Microsoft.JSInterop;
using Moq;

namespace Atlas.Ui.Tests.Services;

internal static class AutosaveTestSupport
{
    public static AppCacheService CreateCache(IAtlasApiClient api)
    {
        Mock<IJSRuntime> js = new();
        return new AppCacheService(api, new LocalSettings(js.Object), new SelectionState());
    }
}
