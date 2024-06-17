// MIT License.

using Microsoft.AspNetCore.Http.Features;

namespace Swick.SystemWebAdapters.Extensions.HttpHandlers;

internal static class FeatureExtensions
{
#if !NET7_0_OR_GREATER
    public static T GetRequiredFeature<T>(this IFeatureCollection features)
        => features.Get<T>() ?? throw new InvalidOperationException($"Could not find feature {typeof(T).FullName}");
#endif
}
