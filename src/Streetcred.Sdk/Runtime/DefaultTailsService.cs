﻿using System.Collections.Concurrent;
using System.Threading.Tasks;
using Hyperledger.Indy.BlobStorageApi;
using Streetcred.Sdk.Contracts;
using Streetcred.Sdk.Utils;
using Hyperledger.Indy.PoolApi;
using System;
using System.Linq;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Streetcred.Sdk.Runtime
{
    /// <inheritdoc />
    public class DefaultTailsService : ITailsService
    {
        protected static readonly ConcurrentDictionary<string, BlobStorageReader> BlobReaders =
            new ConcurrentDictionary<string, BlobStorageReader>();

        protected readonly ILedgerService LedgerService;
        protected readonly HttpClient HttpClient;

        public DefaultTailsService(ILedgerService ledgerService)
        {
            LedgerService = ledgerService;
            HttpClient = new HttpClient();
        }

        /// <inheritdoc />
        public async Task<BlobStorageReader> OpenTailsAsync(string filename)
        {
            var baseDir = EnvironmentUtils.GetTailsPath();

            var tailsWriterConfig = new
            {
                base_dir = baseDir,
                uri_pattern = string.Empty,
                file = filename
            };

            if (BlobReaders.TryGetValue(filename, out var blobReader))
            {
                return blobReader;
            }

            blobReader = await BlobStorage.OpenReaderAsync("default", tailsWriterConfig.ToJson());
            BlobReaders.TryAdd(filename, blobReader);
            return blobReader;
        }

        /// <inheritdoc />
        public async Task<BlobStorageWriter> CreateTailsAsync()
        {
            var tailsWriterConfig = new
            {
                base_dir = EnvironmentUtils.GetTailsPath(),
                uri_pattern = string.Empty
            };

            var blobWriter = await BlobStorage.OpenWriterAsync("default", tailsWriterConfig.ToJson());
            return blobWriter;
        }

        /// <inheritdoc />
        public async Task<string> EnsureTailsExistsAsync(Pool pool, string revocationRegistryId)
        {
            var revocationRegistry =
                await LedgerService.LookupRevocationRegistryDefinitionAsync(pool, null, revocationRegistryId);
            var tailsUri = JObject.Parse(revocationRegistry.ObjectJson)["value"]["tailsLocation"].ToObject<string>();

            var tailsfile = Path.Combine(EnvironmentUtils.GetTailsPath(), new Uri(tailsUri).Segments.Last());

            if (!File.Exists(tailsfile))
            {
                File.WriteAllBytes(
                    path: tailsfile,
                    bytes: await HttpClient.GetByteArrayAsync(tailsUri));
            }

            return Path.GetFileName(tailsfile);
        }
    }
}