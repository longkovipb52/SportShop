using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using System.Diagnostics;

namespace SportShop.Services
{
    public class ModelTrainingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ModelTrainingBackgroundService> _logger;
        private readonly IConfiguration _configuration;
        
        private readonly int _minNewInteractions;
        private readonly TimeSpan _trainingInterval;
        private readonly string _mlProjectPath;
        private readonly string _pythonExecutable;
        private readonly bool _activateVenv;
        
        public ModelTrainingBackgroundService(
            IServiceProvider serviceProvider, 
            ILogger<ModelTrainingBackgroundService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
            
            // Đọc config từ appsettings.json
            _minNewInteractions = _configuration.GetValue<int>("MLTraining:MinNewInteractions", 10);
            _trainingInterval = TimeSpan.FromMinutes(_configuration.GetValue<int>("MLTraining:IntervalMinutes", 60));
            _mlProjectPath = _configuration.GetValue<string>("MLTraining:ProjectPath", "C:\\Users\\PC\\source\\repos\\SportShop_ML")!;
            _pythonExecutable = _configuration.GetValue<string>("MLTraining:PythonExecutable", "python")!;
            _activateVenv = _configuration.GetValue<bool>("MLTraining:ActivateVenv", true);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🤖 Model Training Background Service started");
            _logger.LogInformation($"📁 ML Project Path: {_mlProjectPath}");
            _logger.LogInformation($"🐍 Python Executable: {_pythonExecutable}");
            _logger.LogInformation($"⚙️ Check interval: {_trainingInterval.TotalMinutes} minutes");
            _logger.LogInformation($"📊 Min interactions threshold: {_minNewInteractions}");
            
            // Delay 1 phút trước khi bắt đầu check đầu tiên
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndTriggerTraining();
                    await Task.Delay(_trainingInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown, không log error
                    _logger.LogInformation("🛑 Model Training Background Service stopped gracefully");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 Error in Model Training Background Service");
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
            }
        }

        private async Task CheckAndTriggerTraining()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Đếm số interaction mới từ lần train cuối
            var lastTrainingTime = await GetLastTrainingTimeAsync();
            var newInteractionsCount = await context.InteractionEvent
                .Where(e => e.CreatedAt > lastTrainingTime)
                .CountAsync();
                
            _logger.LogInformation($"📈 Found {newInteractionsCount} new interactions since {lastTrainingTime:yyyy-MM-dd HH:mm:ss}");
            
            // Chỉ train khi có đủ dữ liệu mới
            if (newInteractionsCount >= _minNewInteractions)
            {
                _logger.LogInformation($"🚀 Triggering model training with {newInteractionsCount} new interactions");
                
                var success = await TriggerExternalTraining();
                if (success)
                {
                    await UpdateLastTrainingTimeAsync();
                    _logger.LogInformation("✅ Model training completed successfully");
                }
                else
                {
                    _logger.LogError("❌ Model training failed");
                }
            }
            else
            {
                _logger.LogInformation($"⏳ Not enough new data. Need {_minNewInteractions - newInteractionsCount} more interactions");
            }
        }

        private async Task<bool> TriggerExternalTraining()
        {
            try
            {
                // 1. Export data trước khi train
                using var scope = _serviceProvider.CreateScope();
                var externalTrainingService = scope.ServiceProvider.GetRequiredService<ExternalMLTrainingService>();
                
                _logger.LogInformation("📤 Exporting latest data for ML training...");
                
                // Export data
                await ExportDataForTraining(externalTrainingService);
                
                // 2. Trigger Python training với venv
                var success = await RunPythonTrainingScript();
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error triggering external training");
                return false;
            }
        }

        private async Task ExportDataForTraining(ExternalMLTrainingService trainingService)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                var dataPath = Path.Combine(_mlProjectPath, "data");
                Directory.CreateDirectory(dataPath);

                // Export interactions
                var interactions = await context.InteractionEvent
                    .Include(e => e.Product)
                    .Include(e => e.User)
                    .OrderBy(e => e.CreatedAt)
                    .Select(e => new
                    {
                        user_id = e.UserID,
                        product_id = e.ProductID,
                        event_type = e.EventType ?? "unknown",
                        rating = e.Rating,
                        created_at = e.CreatedAt
                    })
                    .ToListAsync();

                // Format timestamp on client side
                var formattedInteractions = interactions.Select(e => new
                {
                    user_id = e.user_id,
                    product_id = e.product_id,
                    event_type = e.event_type,
                    rating = e.rating,
                    timestamp = e.created_at.ToString("yyyy-MM-dd HH:mm:ss")
                }).ToList();

                var interactionsJson = System.Text.Json.JsonSerializer.Serialize(formattedInteractions, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(Path.Combine(dataPath, "interactions.json"), interactionsJson);
                _logger.LogInformation($"📊 Exported {interactions.Count} interactions to data/interactions.json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data for training");
                throw;
            }
        }

        private async Task<bool> RunPythonTrainingScript()
        {
            try
            {
                var mainScript = _configuration["MLTraining:MainScript"];
                var scriptPath = Path.Combine(_mlProjectPath, mainScript!);

                if (!File.Exists(scriptPath))
                {
                    _logger.LogError($"❌ Training script not found: {scriptPath}");
                    return false;
                }

                ProcessStartInfo startInfo;

                if (_activateVenv)
                {
                    // Sử dụng Python từ venv
                    startInfo = new ProcessStartInfo
                    {
                        FileName = _pythonExecutable, // Đường dẫn đầy đủ đến python.exe trong venv
                        Arguments = $"\"{scriptPath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = _mlProjectPath
                    };

                    // Thêm venv Scripts vào PATH
                    var venvScripts = Path.Combine(_mlProjectPath, ".venv", "Scripts");
                    var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                    startInfo.Environment["PATH"] = $"{venvScripts};{currentPath}";
                    
                    // Thiết lập VIRTUAL_ENV
                    startInfo.Environment["VIRTUAL_ENV"] = Path.Combine(_mlProjectPath, ".venv");
                }
                else
                {
                    // Sử dụng batch script để activate venv
                    var batchScript = CreateVenvActivationBatch();
                    
                    startInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C \"{batchScript}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = _mlProjectPath
                    };
                }

                // Thêm environment variables
                startInfo.Environment["SPORTSHOP_DB_CONNECTION"] = _configuration.GetConnectionString("DefaultConnection");
                startInfo.Environment["PYTHONIOENCODING"] = "utf-8";

                _logger.LogInformation($"🚀 Executing: {startInfo.FileName} {startInfo.Arguments}");
                _logger.LogInformation($"📁 Working directory: {_mlProjectPath}");

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    // Timeout after 15 minutes
                    var timeout = TimeSpan.FromMinutes(15);
                    using var cts = new CancellationTokenSource(timeout);
                    
                    try
                    {
                        await process.WaitForExitAsync(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        process.Kill();
                        _logger.LogError($"⏰ Training script timed out after {timeout.TotalMinutes} minutes");
                        return false;
                    }
                    
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    
                    if (process.ExitCode == 0)
                    {
                        _logger.LogInformation($"✅ Training output: {output}");
                        return true;
                    }
                    else
                    {
                        _logger.LogError($"❌ Training failed (Exit code {process.ExitCode})");
                        _logger.LogError($"🔍 Error details: {error}");
                        _logger.LogError($"📤 Output: {output}");
                        return false;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error running Python training script");
                return false;
            }
        }

        private string CreateVenvActivationBatch()
        {
            var batchPath = Path.Combine(Path.GetTempPath(), "activate_and_train.bat");
            var mainScript = _configuration["MLTraining:MainScript"];
            var scriptPath = Path.Combine(_mlProjectPath, mainScript!);

            var batchContent = $@"@echo off
cd /d ""{_mlProjectPath}""
call .venv\Scripts\activate.bat
python ""{scriptPath}""
";

            File.WriteAllText(batchPath, batchContent);
            return batchPath;
        }

        private async Task<DateTime> GetLastTrainingTimeAsync()
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
            
            return DateTime.Now.AddDays(-7); // Default 7 ngày trước
        }

        private async Task UpdateLastTrainingTimeAsync()
        {
            try
            {
                var timestampFile = Path.Combine(Path.GetTempPath(), "sportshop_last_training.txt");
                await File.WriteAllTextAsync(timestampFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                _logger.LogInformation("📝 Updated last training timestamp");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last training time");
            }
        }
    }
}