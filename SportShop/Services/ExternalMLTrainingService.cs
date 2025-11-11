using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using System.Diagnostics;
using System.Text.Json;

namespace SportShop.Services
{
    public class ExternalMLTrainingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ExternalMLTrainingService> _logger;

        public ExternalMLTrainingService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<ExternalMLTrainingService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Trigger training manually
        /// </summary>
        public async Task<TrainingResult> TriggerTrainingAsync()
        {
            try
            {
                var mlProjectPath = _configuration["MLTraining:ProjectPath"];
                var executablePath = _configuration["MLTraining:ExecutablePath"];
                var mainScript = _configuration["MLTraining:MainScript"];

                if (string.IsNullOrEmpty(mlProjectPath) || !Directory.Exists(mlProjectPath))
                {
                    return new TrainingResult
                    {
                        Success = false,
                        Message = "ML project path not found or invalid"
                    };
                }

                // Export dữ liệu mới nhất cho ML project
                await ExportLatestDataAsync();

                // SỬA: Sử dụng MainScript từ config
                var scriptPath = !string.IsNullOrEmpty(mainScript)
                    ? Path.Combine(mlProjectPath, mainScript)
                    : throw new InvalidOperationException("MainScript not configured in appsettings.json");

                // Kiểm tra script có tồn tại không
                if (!File.Exists(scriptPath))
                {
                    return new TrainingResult
                    {
                        Success = false,
                        Message = $"Training script not found: {scriptPath}"
                    };
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = executablePath ?? "python", // Sử dụng ExecutablePath từ config
                    Arguments = $"\"{scriptPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = mlProjectPath
                };

                // Thêm environment variables
                startInfo.Environment["SPORTSHOP_DB_CONNECTION"] = _configuration.GetConnectionString("DefaultConnection");
                startInfo.Environment["PYTHONPATH"] = mlProjectPath;
                startInfo.Environment["PYTHONIOENCODING"] = "utf-8";

                var startTime = DateTime.Now;
                using var process = Process.Start(startInfo);

                if (process == null)
                {
                    return new TrainingResult
                    {
                        Success = false,
                        Message = "Failed to start training process"
                    };
                }

                // Timeout after 15 minutes
                var timeout = TimeSpan.FromMinutes(15);
                using var cts = new CancellationTokenSource(timeout);

                try
                {
                    await process.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Timeout occurred
                }

                var duration = DateTime.Now - startTime;

                if (!process.HasExited)
                {
                    process.Kill();
                    return new TrainingResult
                    {
                        Success = false,
                        Message = $"Training timed out after {timeout.TotalMinutes} minutes",
                        Duration = duration
                    };
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                if (process.ExitCode == 0)
                {
                    _logger.LogInformation($"Training completed successfully in {duration.TotalSeconds:F1} seconds");
                    return new TrainingResult
                    {
                        Success = true,
                        Message = $"Training completed successfully in {duration.TotalSeconds:F1} seconds",
                        Output = output,
                        Duration = duration
                    };
                }
                else
                {
                    _logger.LogError($"Training failed with exit code {process.ExitCode}: {error}");
                    return new TrainingResult
                    {
                        Success = false,
                        Message = $"Training failed with exit code {process.ExitCode}",
                        Output = output,
                        Error = error,
                        Duration = duration
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering training");
                return new TrainingResult
                {
                    Success = false,
                    Message = $"Exception occurred: {ex.Message}",
                    Error = ex.ToString()
                };
            }
        }

        /// <summary>
        /// Export latest interaction data for ML training
        /// </summary>
        private async Task ExportLatestDataAsync()
        {
            try
            {
                var mlProjectPath = _configuration["MLTraining:ProjectPath"];
                if (string.IsNullOrEmpty(mlProjectPath))
                {
                    throw new InvalidOperationException("ML project path not configured");
                }

                var dataPath = Path.Combine(mlProjectPath, "data");
                Directory.CreateDirectory(dataPath);

                // Export interactions - SỬA LỖI NULL CHECK
                var interactions = await _context.InteractionEvent
                    .Include(e => e.Product)
                    .Include(e => e.User)
                    .Select(e => new
                    {
                        user_id = e.UserID,
                        product_id = e.ProductID,
                        event_type = e.EventType ?? "unknown",
                        rating = e.Rating,
                        timestamp = e.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                        // SỬA LỖI: Sử dụng null-conditional và null-coalescing
                        product_category = e.Product != null ? (int?)e.Product.CategoryID : null,
                        product_brand = e.Product != null ? (int?)e.Product.BrandID : null,
                        product_price = e.Product != null ? (decimal?)e.Product.Price : null
                    })
                    .OrderBy(e => e.timestamp)
                    .ToListAsync();

                var interactionsJson = JsonSerializer.Serialize(interactions, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                await File.WriteAllTextAsync(
                    Path.Combine(dataPath, "interactions.json"),
                    interactionsJson
                );

                // Export products - SỬA LỖI NULL CHECK
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Select(p => new
                    {
                        product_id = p.ProductID,
                        name = p.Name ?? "Unknown Product", // Sửa: Name thay vì ProductName
                        category_id = p.CategoryID,
                        brand_id = p.BrandID,
                        price = p.Price,
                        // SỬA LỖI: Kiểm tra null trước khi truy cập property
                        category_name = p.Category != null ? p.Category.Name : "Unknown Category", // Sửa: Name thay vì CategoryName
                        brand_name = p.Brand != null ? p.Brand.Name : "Unknown Brand" // Sửa: Name thay vì BrandName
                    })
                    .ToListAsync();

                var productsJson = JsonSerializer.Serialize(products, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                await File.WriteAllTextAsync(
                    Path.Combine(dataPath, "products.json"),
                    productsJson
                );

                // Export users cho ML training
                var users = await _context.Users
                    .Select(u => new
                    {
                        user_id = u.UserID,
                        username = u.Username ?? "guest",
                        email = u.Email ?? "",
                        created_at = u.CreatedAt != null ? u.CreatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : ""
                    })
                    .ToListAsync();

                var usersJson = JsonSerializer.Serialize(users, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                await File.WriteAllTextAsync(
                    Path.Combine(dataPath, "users.json"),
                    usersJson
                );

                _logger.LogInformation($"Exported {interactions.Count} interactions, {products.Count} products, and {users.Count} users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data for ML training");
                throw;
            }
        }

        /// <summary>
        /// Get training statistics
        /// </summary>
        public async Task<TrainingStats> GetStatsAsync()
        {
            var totalInteractions = await _context.InteractionEvent.CountAsync();
            var uniqueUsers = await _context.InteractionEvent
                .Where(e => e.UserID > 0) // Sửa: UserID không phải nullable
                .Select(e => e.UserID)
                .Distinct()
                .CountAsync();

            var uniqueProducts = await _context.InteractionEvent
                .Where(e => e.ProductID.HasValue)
                .Select(e => e.ProductID)
                .Distinct()
                .CountAsync();

            var lastWeekInteractions = await _context.InteractionEvent
                .Where(e => e.CreatedAt >= DateTime.Now.AddDays(-7))
                .CountAsync();

            var newInteractionsSinceLastTraining = 0;
            var lastTraining = await GetLastTrainingTimeAsync();

            if (lastTraining.HasValue)
            {
                newInteractionsSinceLastTraining = await _context.InteractionEvent
                    .Where(e => e.CreatedAt > lastTraining.Value)
                    .CountAsync();
            }

            return new TrainingStats
            {
                TotalInteractions = totalInteractions,
                UniqueUsers = uniqueUsers,
                UniqueProducts = uniqueProducts,
                LastWeekInteractions = lastWeekInteractions,
                NewInteractionsSinceLastTraining = newInteractionsSinceLastTraining,
                LastTrainingTime = lastTraining,
                DataQuality = totalInteractions > 1000 ? "Good" : "Needs more data"
            };
        }

        /// <summary>
        /// Update last training time
        /// </summary>
        public async Task UpdateLastTrainingTimeAsync()
        {
            try
            {
                var timestampFile = Path.Combine(Path.GetTempPath(), "sportshop_last_training.txt");
                await File.WriteAllTextAsync(timestampFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                _logger.LogInformation("Updated last training timestamp");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last training time");
            }
        }

        private async Task<DateTime?> GetLastTrainingTimeAsync()
        {
            try
            {
                var timestampFile = Path.Combine(Path.GetTempPath(), "sportshop_last_training.txt");

                if (File.Exists(timestampFile))
                {
                    var timestamp = await File.ReadAllTextAsync(timestampFile);
                    if (DateTime.TryParse(timestamp, out var lastTime))
                        return lastTime;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading last training time");
            }

            return null;
        }
    }

    public class TrainingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Output { get; set; } = "";
        public string Error { get; set; } = "";
        public TimeSpan Duration { get; set; }
    }

    public class TrainingStats
    {
        public int TotalInteractions { get; set; }
        public int UniqueUsers { get; set; }
        public int UniqueProducts { get; set; }
        public int LastWeekInteractions { get; set; }
        public int NewInteractionsSinceLastTraining { get; set; }
        public DateTime? LastTrainingTime { get; set; }
        public string DataQuality { get; set; } = "";
    }
}