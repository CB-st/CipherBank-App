/** Artificial delay so skeletons / loading UX can be exercised in mock mode. */
export function mockLatency(msMin = 400, msMax = 900): Promise<void> {
  const ms = msMin + Math.floor(Math.random() * (msMax - msMin + 1));
  return new Promise((resolve) => setTimeout(resolve, ms));
}
