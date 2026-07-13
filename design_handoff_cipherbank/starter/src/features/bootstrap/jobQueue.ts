export type JobPriority = 0 | 1 | 2 | 3; // P0..P3 — lower runs first

export type SyncJob = {
  id: string;
  priority: JobPriority;
  /** Optional single-flight key (usually ticker symbol). */
  symbol?: string;
  run: () => Promise<void>;
};

const GLOBAL_CONCURRENCY = 2;

type Queued = SyncJob & { enqueuedAt: number };

/**
 * Priority job queue: at most one in-flight job per symbol; global cap 2.
 * P0/P1 never starved by P3 — higher priority always dequeued first.
 */
class JobQueueImpl {
  private queue: Queued[] = [];
  private inflight = new Set<string>();
  private inflightSymbols = new Set<string>();
  private running = 0;
  private pausedP3 = false;

  setPauseP3(pause: boolean) {
    this.pausedP3 = pause;
    if (!pause) this.pump();
  }

  enqueue(job: SyncJob): void {
    if (job.symbol) {
      const sym = job.symbol.toUpperCase();
      const already =
        this.inflightSymbols.has(sym) ||
        this.queue.some((q) => q.symbol?.toUpperCase() === sym);
      if (already) return;
      job = { ...job, symbol: sym };
    }
    if (this.queue.some((q) => q.id === job.id) || this.inflight.has(job.id)) return;
    this.queue.push({ ...job, enqueuedAt: Date.now() });
    this.queue.sort((a, b) => a.priority - b.priority || a.enqueuedAt - b.enqueuedAt);
    this.pump();
  }

  private pump() {
    while (this.running < GLOBAL_CONCURRENCY) {
      const nextIdx = this.queue.findIndex((j) => {
        if (this.pausedP3 && j.priority === 3) return false;
        if (j.symbol && this.inflightSymbols.has(j.symbol)) return false;
        return true;
      });
      if (nextIdx < 0) break;
      const [job] = this.queue.splice(nextIdx, 1);
      this.running += 1;
      this.inflight.add(job.id);
      if (job.symbol) this.inflightSymbols.add(job.symbol);

      Promise.resolve()
        .then(() => job.run())
        .catch(() => {
          /* jobs own their errors */
        })
        .finally(() => {
          this.running -= 1;
          this.inflight.delete(job.id);
          if (job.symbol) this.inflightSymbols.delete(job.symbol);
          this.pump();
        });
    }
  }

  /** Test / debug helpers */
  pendingCount() {
    return this.queue.length;
  }
  inflightCount() {
    return this.running;
  }
}

export const jobQueue = new JobQueueImpl();
