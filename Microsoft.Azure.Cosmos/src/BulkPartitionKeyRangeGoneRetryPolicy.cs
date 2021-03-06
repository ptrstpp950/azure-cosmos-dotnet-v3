﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Routing;
    using Microsoft.Azure.Documents;

    /// <summary>
    /// Used only in the context of Bulk Stream operations.
    /// </summary>
    /// <see cref="BatchAsyncBatcher"/>
    /// <see cref="ItemBatchOperationContext"/>
    internal sealed class BulkPartitionKeyRangeGoneRetryPolicy : IDocumentClientRetryPolicy
    {
        private readonly IDocumentClientRetryPolicy nextRetryPolicy;
        private readonly ContainerInternal container;

        public BulkPartitionKeyRangeGoneRetryPolicy(
            ContainerInternal container,
            IDocumentClientRetryPolicy nextRetryPolicy)
        {
            this.container = container ?? throw new ArgumentNullException(nameof(container));
            this.nextRetryPolicy = nextRetryPolicy;
        }

        public async Task<ShouldRetryResult> ShouldRetryAsync(
            Exception exception,
            CancellationToken cancellationToken)
        {
            if (exception is CosmosException clientException)
            {
                ShouldRetryResult shouldRetryResult = await this.ShouldRetryInternalAsync(
                    clientException.StatusCode,
                    (SubStatusCodes)clientException.SubStatusCode,
                    cancellationToken);

                if (shouldRetryResult != null)
                {
                    return shouldRetryResult;
                }

                if (this.nextRetryPolicy == null)
                {
                    return ShouldRetryResult.NoRetry();
                }
            }

            return await this.nextRetryPolicy.ShouldRetryAsync(exception, cancellationToken);
        }

        public async Task<ShouldRetryResult> ShouldRetryAsync(
            ResponseMessage cosmosResponseMessage,
            CancellationToken cancellationToken)
        {
            ShouldRetryResult shouldRetryResult = await this.ShouldRetryInternalAsync(
                cosmosResponseMessage?.StatusCode,
                cosmosResponseMessage?.Headers.SubStatusCode,
                cancellationToken);
            if (shouldRetryResult != null)
            {
                return shouldRetryResult;
            }

            if (this.nextRetryPolicy == null)
            {
                return ShouldRetryResult.NoRetry();
            }

            return await this.nextRetryPolicy.ShouldRetryAsync(cosmosResponseMessage, cancellationToken);
        }

        public void OnBeforeSendRequest(DocumentServiceRequest request)
        {
            this.nextRetryPolicy.OnBeforeSendRequest(request);
        }

        private async Task<ShouldRetryResult> ShouldRetryInternalAsync(
            HttpStatusCode? statusCode,
            SubStatusCodes? subStatusCode,
            CancellationToken cancellationToken)
        {
            if (statusCode == HttpStatusCode.Gone)
            {
                if (subStatusCode == SubStatusCodes.PartitionKeyRangeGone
                    || subStatusCode == SubStatusCodes.CompletingSplit
                    || subStatusCode == SubStatusCodes.CompletingPartitionMigration)
                {
                    PartitionKeyRangeCache partitionKeyRangeCache = await this.container.ClientContext.DocumentClient.GetPartitionKeyRangeCacheAsync();
                    string containerRid = await this.container.GetCachedRIDAsync(forceRefresh: false, cancellationToken: cancellationToken);
                    await partitionKeyRangeCache.TryGetOverlappingRangesAsync(containerRid, FeedRangeEpk.FullRange.Range, forceRefresh: true);
                    return ShouldRetryResult.RetryAfter(TimeSpan.Zero);
                }

                if (subStatusCode == SubStatusCodes.NameCacheIsStale)
                {
                    return ShouldRetryResult.RetryAfter(TimeSpan.Zero);
                }
            }

            return null;
        }
    }
}
