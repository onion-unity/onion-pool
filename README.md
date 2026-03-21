# 🧅 Onion.Pool

Smart, Thread-Safe, and Asynchronous Data Pooling for Unity

## Getting started

### Installation

Install via Unity Package Manager (UPM).

`https://github.com/onion-unity/onion-pool.git`

### Data Class Definition

```csharp
using Onion.Pool;

public class MyData : IPoolable  {
    public int value;

    public void Clear() {
        value = 0; // Reset data for reuse
    }
}
```

### Get/Release Data

```csharp

var data = DataPool<MyData>.Get();
DataPool<MyData>.Release(data);

```

### Trim Settings
Gradually reduces pool size based on peak usage to prevent memory bloating and CPU spikes during downtime.

```csharp
DataPool<MyData>.trimAuto = true;      // Enable/Disable auto-cleanup
DataPool<MyData>.trimInterval = 5.0f;  // Check every 5 seconds
DataPool<MyData>.trimRatio = 0.2f;     // Decay by 20% of peak capacity

DataPool<MyData>.capacity = 32;        // Ensure capacity
```
