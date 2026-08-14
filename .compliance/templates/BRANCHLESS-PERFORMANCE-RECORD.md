# Branchless performance decision record

## Identity

- Change/PR:
- Owner:
- Date:
- Hot method and source path:
- Workload/user outcome affected:

## Profiling evidence

- Profiler/trace:
- Share of end-to-end cost:
- Current throughput/latency:
- Production input sizes:
- Observed branch distribution/correlation:

## Behavioral contract

- Scalar reference:
- Inputs/outputs:
- Overflow/rounding mode:
- `NaN`, infinity, signed-zero behavior:
- Exception and side-effect order:
- Aliasing/tail behavior:
- Constant-time requirement, if any:

## Candidates

| Candidate | Technique | Expected advantage | Semantic/portability risk |
|---|---|---|---|
| Reference | readable branch | baseline | |
| Candidate A | | | |
| Candidate B | | | |

## Benchmark environment

- SDK/runtime:
- OS:
- CPU/architecture:
- Instruction sets:
- Tiered compilation/dynamic PGO:
- ReadyToRun/AOT:
- Benchmark command/commit:

## Results

| Dataset/distribution | Size | Reference | Candidate | Ratio | Allocations | Branch misses | Notes |
|---|---:|---:|---:|---:|---:|---:|---|
| Predictable/skewed | | | | | | | |
| 50/50 random | | | | | | | |
| Sorted/bursty | | | | | | | |
| Production sample | | | | | | | |

## Generated code

- Disassembly artifact:
- Branches/conditional moves/masked operations observed:
- Register/instruction-count concerns:
- Differences across x64/Arm64 or other supported targets:

## Verification

- Differential/property tests:
- Boundary and vector-tail tests:
- End-to-end measurement:
- SonarQube/security review:

## Decision

- Keep/reject/dispatch:
- Required fallback:
- Minimum retained benefit:
- Rebenchmark trigger (runtime, CPU, algorithm, or workload change):
- Removal path:
