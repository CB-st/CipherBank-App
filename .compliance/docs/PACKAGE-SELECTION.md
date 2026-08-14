# Package selection record

Complete this record before adding a dependency to the migrated repository.

- Capability required:
- Why the Base Class Library or current framework is insufficient:
- Candidate packages:
- Selected package and version:
- Maintainer/project health:
- License and commercial-use implications:
- Supported target frameworks:
- Trimming/Native AOT behavior:
- Transitive dependency impact:
- Security history and update process:
- Serialization/public-contract impact:
- Abstraction boundary that contains the package:
- Test strategy:
- Removal/replacement strategy:

## Common selections

| Need | Default direction |
|---|---|
| Hosting, DI, options, logging | `Microsoft.Extensions.*` |
| JSON | `System.Text.Json` |
| HTTP resilience | `Microsoft.Extensions.Http.Resilience` |
| Full ORM | EF Core with the deployed provider |
| Explicit SQL | Dapper or ADO.NET |
| Structured logs | Serilog behind `ILogger<T>` |
| Traces and metrics | OpenTelemetry |
| Validation | FluentValidation when DataAnnotations are insufficient |
| DI scanning/decorators | Scrutor |
| Unit tests | xUnit v3, NUnit, or MSTest—choose one |
| Test doubles | NSubstitute or another single consistent framework |
| Real integration dependencies | Testcontainers |
| Benchmarks | BenchmarkDotNet |
| Numerical work | MathNet.Numerics |
| Physical units | UnitsNet |
| Scientific plots | ScottPlot or another consciously selected renderer |
| MVVM observable properties/commands | CommunityToolkit.Mvvm |
| Vectorized numeric primitives | System.Numerics.Tensors (`TensorPrimitives`) before hand-written hardware intrinsics |
| Pooled buffers | `System.Buffers` (`ArrayPool<T>`, `MemoryPool<T>`) — BCL, no package needed |
| Native/unmanaged memory | `System.Runtime.InteropServices.NativeMemory` — BCL, no package needed |
| Portable GPU compute | ILGPU (CUDA/OpenCL/CPU backends, cross-platform, built-in CPU fallback accelerator) |
| Windows DirectX 12 compute shaders in C# | ComputeSharp |

Do not install this whole table. Each package must answer a current requirement.

For branchless work, keep BenchmarkDotNet in a dedicated benchmark project and use the base class library (`Math`, `System.Numerics`, `System.Runtime.Intrinsics`, and `CryptographicOperations`) for production implementation unless profiling proves another dependency is required. Complete `templates/BRANCHLESS-PERFORMANCE-RECORD.md` before retaining the optimization.
