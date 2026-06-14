namespace IdentityService.Auth.Infrastructure 

{
    public class SnowflakeIdEngine
    {
        private readonly long _workerId;
        private readonly long _datacenterId;
        private long _sequence = 0L;

        private const long Twepoch = 1767129600000L; // 自定义时间起点戳 (2026-01-01 00:00:00 UTC)
        private const long WorkerIdBits = 5L;
        private const long DatacenterIdBits = 5L;
        private const long MaxWorkerId = -1L ^ (-1L << (int)WorkerIdBits);
        private const long MaxDatacenterId = -1L ^ (-1L << (int)DatacenterIdBits);
        private const long SequenceBits = 12L;

        private readonly object _lock = new();
        private long _lastTimestamp = -1L;

        public SnowflakeIdEngine(long workerId = 1, long datacenterId = 1)
        {
            if (workerId > MaxWorkerId || workerId < 0) throw new ArgumentException($"worker Id can't be greater than {MaxWorkerId} or less than 0");
            if (datacenterId > MaxDatacenterId || datacenterId < 0) throw new ArgumentException($"datacenter Id can't be greater than {MaxDatacenterId} or less than 0");
            _workerId = workerId;
            _datacenterId = datacenterId;
        }

        public long NextId()
        {
            lock (_lock)
            {
                var timestamp = TimeGen();
                if (timestamp < _lastTimestamp) throw new Exception($"Clock moved backwards. Refusing to generate id for {_lastTimestamp - timestamp} milliseconds");

                if (_lastTimestamp == timestamp)
                {
                    _sequence = (_sequence + 1) & (-1L ^ (-1L << (int)SequenceBits));
                    if (_sequence == 0) timestamp = TilNextMillis(_lastTimestamp);
                }
                else
                {
                    _sequence = 0L;
                }

                _lastTimestamp = timestamp;
                return ((timestamp - Twepoch) << (int)(WorkerIdBits + DatacenterIdBits + SequenceBits)) |
                       (_datacenterId << (int)(WorkerIdBits + SequenceBits)) |
                       (_workerId << (int)SequenceBits) | _sequence;
            }
        }

        private long TilNextMillis(long lastTimestamp)
        {
            var timestamp = TimeGen();
            while (timestamp <= lastTimestamp) timestamp = TimeGen();
            return timestamp;
        }

        private long TimeGen() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
