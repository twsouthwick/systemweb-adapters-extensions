// MIT License.

using System.Web;
using Microsoft.AspNetCore.Http;

namespace Swick.SystemWebAdapters.Extensions.HttpHandlers;

internal interface IHttpHandlerEndpointFactory
{
    Endpoint Create(IHttpHandler handler);
}
