using System;
using System.IO;
using System.Threading.Tasks;
using DataStudioDataMgr.Models;

namespace DataStudioDataMgr.Services.GiveX
{
    /// <summary>
    /// Loads AWG GiveX aggregated email stats CSV files from the AWG network path, persists to MongoDB, and archives processed files.
    /// </summary>
    public class AwgEmailStatsService
    {
        public const string AggregatedFileNamePrefix = "Aggregated_Email_Stats_";

        private readonly GiveXFileService _fileService;
        private readonly GiveXCsvService _csvService;
        private readonly GiveXMongoService _mongoService;

        public AwgEmailStatsService()
        {
            _fileService = new GiveXFileService();
            _csvService = new GiveXCsvService();
            _mongoService = new GiveXMongoService();
        }

        public AwgEmailStatsService(GiveXFileService fileService, GiveXCsvService csvService, GiveXMongoService mongoService)
        {
            _fileService = fileService;
            _csvService = csvService;
            _mongoService = mongoService;
        }

        public async Task ProcessAwgEmailStatsAsync()
        {
            Console.WriteLine("=== AWG EMAIL STATS (GIVEX CURATED) — START ===");

            var paths = _fileService.EnumerateAwgCsvFilesByPrefix(AggregatedFileNamePrefix);
            if (paths.Count == 0)
            {
                Console.WriteLine("No matching CSV files to process.");
                Console.WriteLine("=== AWG EMAIL STATS — END ===");
                return;
            }

            bool connectionOk = await _mongoService.TestConnectionAsync(_mongoService.ResolvedAwgGiveXCuratedDatabaseName);
            if (!connectionOk)
            {
                Console.WriteLine("MongoDB connection failed; skipping AWG email stats load.");
                Console.WriteLine("=== AWG EMAIL STATS — END ===");
                return;
            }

            await _mongoService.EnsureAwgGiveXCuratedEmailsCollectionExistsAsync();

            int totalInserted = 0;
            foreach (var filePath in paths)
            {
                try
                {
                    var rows = await _csvService.ProcessAwgAggregatedEmailStatsFileAsync(filePath);
                    if (rows.Count > 0)
                        totalInserted += await _mongoService.StoreAwgEmailStatsAsync(rows);

                    var archived = await _fileService.ArchiveFileAsync(filePath, AwgEmailStats.AwgClientToken, "email");
                    if (!archived)
                        Console.WriteLine($"Warning: could not archive {Path.GetFileName(filePath)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {Path.GetFileName(filePath)}: {ex.Message}");
                }
            }

            await _mongoService.CreateAwgGiveXCuratedEmailIndexesAsync();
            var collectionTotal = await _mongoService.GetAwgGiveXCuratedEmailsCountAsync();
            Console.WriteLine($"AWG email stats run complete. Rows inserted this run: {totalInserted}. Total documents in {_mongoService.ResolvedAwgGiveXCuratedDatabaseName}.{_mongoService.ResolvedAwgGiveXCuratedCollectionName}: {collectionTotal}");
            Console.WriteLine("=== AWG EMAIL STATS — END ===");
        }
    }
}
