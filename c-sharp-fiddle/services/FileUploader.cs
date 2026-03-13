using System.Security.Cryptography;
using System.Text.Json;
using c_sharp_fiddle.Models;

namespace c_sharp_fiddle
{
    internal record ChunkMetadata(string Id, string Hash, long Start, long End);

    internal record FileMetadata(string FileName, List<ChunkMetadata> Chunks);

    internal class FileUploader(ILogger logger)
    {
        private readonly string inputDirectory = "D:\\Projects\\c-sharp-fiddle\\c-sharp-fiddle\\input";
        private readonly string outputDirectory = "D:\\Projects\\c-sharp-fiddle\\c-sharp-fiddle\\output";
        private const int ChunkSize = 1024 * 1024; // 1 MB
        private const int BatchSize = 10;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public async Task<bool> UploadFile(string fileName)
        {
            var inputPath = Path.Combine(inputDirectory, fileName);
            if (!File.Exists(inputPath))
            {
                logger.Log($"File not found: {inputPath}");
                return false;
            }

            Directory.CreateDirectory(outputDirectory);

            using var fileHandle = File.OpenHandle(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.Asynchronous);
            var fileSize = RandomAccess.GetLength(fileHandle);

            var ranges = Enumerable.Range(0, (int)Math.Ceiling((double)fileSize / ChunkSize))
                .Select(i => (index: i, start: (long)i * ChunkSize, end: Math.Min((long)(i + 1) * ChunkSize, fileSize)))
                .ToList();

            var chunkMetadatas = new ChunkMetadata[ranges.Count];

            foreach (var batch in ranges.Chunk(BatchSize))
            {
                await Task.WhenAll(batch.Select(async item =>
                {
                    var length = (int)(item.end - item.start);
                    var buffer = new byte[length];
                    await RandomAccess.ReadAsync(fileHandle, buffer, item.start);

                    var chunkId = Guid.NewGuid().ToString();
                    var hash = Convert.ToHexString(SHA256.HashData(buffer));
                    var chunkPath = Path.Combine(outputDirectory, $"{fileName}.chunk.{chunkId}");

                    await File.WriteAllBytesAsync(chunkPath, buffer);
                    chunkMetadatas[item.index] = new ChunkMetadata(chunkId, hash, item.start, item.end);
                    logger.Log($"Written chunk {chunkId} [{item.start}-{item.end}] ({length} bytes), hash: {hash}");
                }));
            }

            var metadata = new FileMetadata(fileName, [.. chunkMetadatas]);
            var metaPath = Path.Combine(outputDirectory, $"{fileName}.meta.json");
            await using var metaStream = File.Create(metaPath);
            await JsonSerializer.SerializeAsync(metaStream, metadata, JsonOptions);

            logger.Log($"File split into {chunkMetadatas.Length} chunk(s). Metadata written to {metaPath}");
            return true;
        }

        public async Task<IReadOnlyList<ChunkMetadata>> ReassembleFile(string fileName)
        {
            var metaPath = Path.Combine(outputDirectory, $"{fileName}.meta.json");
            if (!File.Exists(metaPath))
            {
                logger.Log($"Metadata file not found: {metaPath}");
                return [];
            }

            await using var metaStream = File.OpenRead(metaPath);
            var metadata = await JsonSerializer.DeserializeAsync<FileMetadata>(metaStream);
            if (metadata is null)
            {
                logger.Log("Failed to deserialize metadata.");
                return [];
            }

            var outputPath = Path.Combine(outputDirectory, fileName);
            using var outHandle = File.OpenHandle(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, FileOptions.Asynchronous);

            var failedChunks = new System.Collections.Concurrent.ConcurrentBag<ChunkMetadata>();

            foreach (var batch in metadata.Chunks.Chunk(BatchSize))
            {
                await Task.WhenAll(batch.Select(async chunk =>
                {
                    var chunkPath = Path.Combine(outputDirectory, $"{fileName}.chunk.{chunk.Id}");
                    if (!File.Exists(chunkPath))
                    {
                        logger.Log($"Chunk {chunk.Id} missing");
                        failedChunks.Add(chunk);
                        return;
                    }

                    var chunkData = await File.ReadAllBytesAsync(chunkPath);
                    var actualHash = Convert.ToHexString(SHA256.HashData(chunkData));
                    if (actualHash != chunk.Hash)
                    {
                        logger.Log($"Hash mismatch for chunk {chunk.Id}: expected {chunk.Hash}, got {actualHash}");
                        failedChunks.Add(chunk);
                        return;
                    }

                    await RandomAccess.WriteAsync(outHandle, chunkData, chunk.Start);
                    logger.Log($"Assembled chunk {chunk.Id} at [{chunk.Start}-{chunk.End}] (verified)");
                }));
            }

            if (failedChunks.IsEmpty)
                logger.Log($"Reassembled {metadata.Chunks.Count} chunk(s) -> {outputPath}");
            else
                logger.Log($"{failedChunks.Count} chunk(s) failed — call RepairChunks to recover");

            return [.. failedChunks];
        }

        public async Task<IReadOnlyList<ChunkMetadata>> RegenerateChunks(string fileName, IReadOnlyList<ChunkMetadata> chunksToRepair)
        {
            var inputPath = Path.Combine(inputDirectory, fileName);
            if (!File.Exists(inputPath))
            {
                logger.Log($"Source file not found: {inputPath}");
                return [];
            }

            using var srcHandle = File.OpenHandle(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.Asynchronous);
            var regenerated = new System.Collections.Concurrent.ConcurrentBag<ChunkMetadata>();
            var metaChanged = false;

            foreach (var batch in chunksToRepair.Chunk(BatchSize))
            {
                await Task.WhenAll(batch.Select(async chunk =>
                {
                    var length = (int)(chunk.End - chunk.Start);
                    var buffer = new byte[length];
                    await RandomAccess.ReadAsync(srcHandle, buffer, chunk.Start);

                    var newHash = Convert.ToHexString(SHA256.HashData(buffer));
                    var updated = newHash != chunk.Hash
                        ? chunk with { Hash = newHash }
                        : chunk;

                    if (updated.Hash != chunk.Hash)
                    {
                        logger.Log($"Hash changed for chunk {chunk.Id} [{chunk.Start}-{chunk.End}]: {chunk.Hash} -> {newHash}");
                        metaChanged = true;
                    }

                    var chunkPath = Path.Combine(outputDirectory, $"{fileName}.chunk.{updated.Id}");
                    await File.WriteAllBytesAsync(chunkPath, buffer);
                    regenerated.Add(updated);
                    logger.Log($"Regenerated chunk {updated.Id} [{updated.Start}-{updated.End}]");
                }));
            }

            if (metaChanged)
            {
                var metaPath = Path.Combine(outputDirectory, $"{fileName}.meta.json");
                FileMetadata? metadata = null;
                await using (var metaStream = File.OpenRead(metaPath))
                {
                    metadata = await JsonSerializer.DeserializeAsync<FileMetadata>(metaStream);
                    metaStream.Close();
                }

                if (metadata is not null)
                {
                    var updatedById = regenerated.ToDictionary(c => c.Start);
                    var updatedChunks = metadata.Chunks
                        .Select(c => updatedById.TryGetValue(c.Start, out var updated) ? updated : c)
                        .ToList();

                    await using var writeStream = File.Create(metaPath);
                    await JsonSerializer.SerializeAsync(writeStream, metadata with { Chunks = updatedChunks }, JsonOptions);
                    logger.Log("Metadata updated with new chunk hashes");
                }
            }

            return [.. regenerated];
        }

        public async Task<bool> PatchFile(string fileName, IReadOnlyList<ChunkMetadata> repairedChunks)
        {
            var outputPath = Path.Combine(outputDirectory, fileName);
            if (!File.Exists(outputPath))
            {
                logger.Log($"Output file not found: {outputPath}");
                return false;
            }

            using var outHandle = File.OpenHandle(outputPath, FileMode.Open, FileAccess.Write, FileShare.None, FileOptions.Asynchronous);

            foreach (var batch in repairedChunks.Chunk(BatchSize))
            {
                await Task.WhenAll(batch.Select(async chunk =>
                {
                    var chunkPath = Path.Combine(outputDirectory, $"{fileName}.chunk.{chunk.Id}");
                    var chunkData = await File.ReadAllBytesAsync(chunkPath);
                    await RandomAccess.WriteAsync(outHandle, chunkData, chunk.Start);
                    logger.Log($"Patched [{chunk.Start}-{chunk.End}] in output file");
                }));
            }

            logger.Log($"Patched {repairedChunks.Count} chunk(s) into {outputPath}");
            return true;
        }
    }
}
