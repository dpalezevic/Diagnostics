// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Diagnostics
{
    public class StatusCodePagesMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly StatusCodePagesOptions _options;

        public StatusCodePagesMiddleware(RequestDelegate next, StatusCodePagesOptions options)
        {
            _next = next;
            _options = options;
            if (_options.HandleAsync == null)
            {
                throw new ArgumentException("Missing options.HandleAsync implementation.");
            }
        }

        public async Task Invoke(HttpContext context)
        {
            var statusCodeFeature = new StatusCodePagesFeature();
            context.SetFeature<IStatusCodePagesFeature>(statusCodeFeature);

            await _next(context);

            if (!statusCodeFeature.Enabled)
            {
                // Check if the feature is still available because other middleware (such as a web API written in MVC) could
                // have disabled the feature to prevent HTML status code responses from showing up to an API client.
                return;
            }

            // Do nothing if a response body has already been provided.
            if (context.Response.HasStarted
                || context.Response.StatusCode < 400
                || context.Response.StatusCode >= 600
                || context.Response.ContentLength.HasValue
                || !string.IsNullOrEmpty(context.Response.ContentType))
            {
                return;
            }

            var statusCodeContext = new StatusCodeContext(context, _options, _next);
            await _options.HandleAsync(statusCodeContext);
        }
    }
}