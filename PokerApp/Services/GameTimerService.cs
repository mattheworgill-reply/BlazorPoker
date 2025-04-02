using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PokerApp.Data;
using PokerApp.Hubs;

public class GameTimerService : BackgroundService
{
    private readonly IHubContext<PokerHub> _hubContext;
    private readonly ILogger<GameTimerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Dictionary<string, GameTimerState> _activeTimers = new();
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    public GameTimerService(
        IHubContext<PokerHub> hubContext,
        ILogger<GameTimerService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _hubContext = hubContext;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public class GameTimerState
    {
        public string TableId { get; set; }
        public int RemainingSeconds { get; set; }
        public bool IsPaused { get; set; }
        public DateTime LastUpdateTime { get; set; }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run a timer tick every second
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessAllTimers();
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessAllTimers()
    {
        await _lock.WaitAsync();
        try
        {
            var timersToUpdate = new List<GameTimerState>();

            // Find timers that need updates
            foreach (var timer in _activeTimers.Values)
            {
                if (!timer.IsPaused && timer.RemainingSeconds > 0)
                {
                    timer.RemainingSeconds--;
                    timersToUpdate.Add(timer);
                }
            }

            // Release lock before making external calls
            _lock.Release();

            // Send updates for each timer
            foreach (var timer in timersToUpdate)
            {
                await UpdateTableTimer(timer.TableId, timer.IsPaused, timer.RemainingSeconds);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing timers");
            if (_lock.CurrentCount == 0)
                _lock.Release();
        }
    }

    private async Task UpdateTableTimer(string tableId, bool isPaused, int remainingSeconds)
    {
        try
        {
            _logger.LogDebug("[GameTimer] UpdateTableTimer");

            using (var scope = _scopeFactory.CreateScope())
            {
                // Get the DbContextFactory from the scope
                var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

                // Use the factory to create a DbContext
                await using var context = await contextFactory.CreateDbContextAsync();

                var table = await context.PokerTables
                    .FirstOrDefaultAsync(t => t.Id == tableId);

                if (table != null)
                {
                    table.TimeRemaining = remainingSeconds;
                    table.IsGamePaused = isPaused;
                    await context.SaveChangesAsync();

                    // Broadcast to clients
                    await _hubContext.Clients.Group($"table_{tableId}")
                        .SendAsync("TimerUpdate", remainingSeconds, isPaused);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating timer for table {tableId}");
        }
    }

    public async Task StartTimer(string tableId, int durationSeconds)
    {
        _logger.LogDebug("[GameTimer] StartTimer with seconds: " + durationSeconds);
        await _lock.WaitAsync();
        try
        {
            _activeTimers[tableId] = new GameTimerState
            {
                TableId = tableId,
                RemainingSeconds = durationSeconds,
                IsPaused = false,
                LastUpdateTime = DateTime.UtcNow
            };
        }
        finally
        {
            _lock.Release();
            _logger.LogDebug("Lock Release in StartTimer");
        }

        // Send initial update
        await UpdateTableTimer(tableId, false, durationSeconds);
    }

    public async Task PauseTimer(string tableId)
    {
        await _lock.WaitAsync();
        try
        {
            if (_activeTimers.TryGetValue(tableId, out var timer))
            {
                timer.IsPaused = true;
                _logger.LogInformation($"Timer paused for table {tableId} with {timer.RemainingSeconds} seconds remaining");
            }
        }
        finally
        {
            _lock.Release();
            _logger.LogDebug("Lock Release in PauseTimer");
        }

        // Send pause update
        await UpdateTableTimer(tableId, true, GetRemainingTime(tableId));
    }

    public async Task ResumeTimer(string tableId)
    {
        await _lock.WaitAsync();
        try
        {
            if (_activeTimers.TryGetValue(tableId, out var timer))
            {
                timer.IsPaused = false;
                timer.LastUpdateTime = DateTime.UtcNow;
                _logger.LogInformation($"Timer resumed for table {tableId} with {timer.RemainingSeconds} seconds remaining");
            }
        }
        finally
        {
            _lock.Release();
            _logger.LogDebug("Lock Release in ResumeTimer");
        }

        // Send resume update
        await UpdateTableTimer(tableId, false, GetRemainingTime(tableId));
    }

    public async Task ResetTimer(string tableId, int durationSeconds)
    {
        _logger.LogDebug("[GameTimer] Reset Timer... " + durationSeconds);
        bool timerReset = false;
        await _lock.WaitAsync();
        try
        {
            if (_activeTimers.TryGetValue(tableId, out var timer))
            {
                timer.RemainingSeconds = durationSeconds;
                timer.LastUpdateTime = DateTime.UtcNow;
                timerReset = true;
                _logger.LogInformation($"Timer reset for table {tableId} to {durationSeconds} seconds");
            }
            else
            {
                _logger.LogError("No timer found for " + tableId);
            }
        }
        finally
        {
            _lock.Release();
            _logger.LogDebug("Lock Release in ResetTimer");
        }


        if (!timerReset)
        {
            await StartTimer(tableId, durationSeconds);
            return;
        }

        // Send reset update
        var isPaused = IsTimerPaused(tableId);
        await UpdateTableTimer(tableId, isPaused, durationSeconds);
    }

    public async Task StopTimer(string tableId)
    {
        await _lock.WaitAsync();
        try
        {
            if (_activeTimers.ContainsKey(tableId))
            {
                _activeTimers.Remove(tableId);
                _logger.LogInformation($"Timer removed for table {tableId}");
            }
        }
        finally
        {
            _lock.Release();
            _logger.LogDebug("Lock Release in StopTimer");
        }
    }

    // Helper methods

    public int GetRemainingTime(string tableId)
    {
        if (_activeTimers.TryGetValue(tableId, out var timer))
            return timer.RemainingSeconds;
        return 0;
    }

    public bool IsTimerPaused(string tableId)
    {
        if (_activeTimers.TryGetValue(tableId, out var timer))
            return timer.IsPaused;
        return true;
    }
}