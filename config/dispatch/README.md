# Dispatch Configuration

`sync-scheduler.json` controls the process-wide market synchronization scheduler.
Concurrency is intentionally small for mobile I/O and battery use; priority P1 is
always dequeued before P2, with FIFO ordering inside a priority.
