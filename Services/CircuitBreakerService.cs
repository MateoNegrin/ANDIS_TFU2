namespace TFU_Architecture.Services;

// Patrón: Circuit Breaker
public class CircuitBreakerService
{
    private readonly object _lock = new object();
    private string _state = "CLOSED"; // CLOSED, OPEN, HALF_OPEN
    private int _failureCount = 0;
    private DateTime? _openedAt;
    private int _failureThreshold;
    private int _openSeconds;

    public CircuitBreakerService(int failureThreshold = 3, int openSeconds = 10)
    {
        _failureThreshold = failureThreshold;
        _openSeconds = openSeconds;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        lock (_lock)
        {
            CheckStateTransition();

            if (_state == "OPEN")
            {
                throw new InvalidOperationException("Servicio temporalmente no disponible");
            }
        }

        try
        {
            var result = await operation();
            lock (_lock)
            {
                OnSuccess();
            }
            return result;
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                OnFailure();
            }
            throw;
        }
    }

    private void CheckStateTransition()
    {
        if (_state == "OPEN" && _openedAt.HasValue)
        {
            var elapsed = DateTime.UtcNow - _openedAt.Value;
            if (elapsed.TotalSeconds >= _openSeconds)
            {
                _state = "HALF_OPEN";
                _failureCount = 0;
                Console.WriteLine($"[Circuit Breaker] → HALF_OPEN");
            }
        }
    }

    private void OnSuccess()
    {
        if (_state == "HALF_OPEN")
        {
            _state = "CLOSED";
            _failureCount = 0;
            Console.WriteLine($"[Circuit Breaker] → CLOSED");
        }
        else if (_state == "CLOSED")
        {
            _failureCount = 0;
        }
    }

    private void OnFailure()
    {
        _failureCount++;
        Console.WriteLine($"[Circuit Breaker] Failure {_failureCount}/{_failureThreshold}");

        if (_state == "HALF_OPEN")
        {
            _state = "OPEN";
            _openedAt = DateTime.UtcNow;
            Console.WriteLine($"[Circuit Breaker] → OPEN");
        }
        else if (_state == "CLOSED" && _failureCount >= _failureThreshold)
        {
            _state = "OPEN";
            _openedAt = DateTime.UtcNow;
            Console.WriteLine($"[Circuit Breaker] → OPEN");
        }
    }

    public (string State, int Failures) GetStatus()
    {
        lock (_lock)
        {
            CheckStateTransition();
            return (_state, _failureCount);
        }
    }
}
