'use strict';

function promisePool(taskFns, limit) {
  return new Promise((resolve, reject) => {
    // Validate limit: must be a positive integer
    if (!Number.isInteger(limit) || limit <= 0) {
      reject(new TypeError('limit must be a positive integer'));
      return;
    }

    const n = taskFns.length;
    if (n === 0) {
      resolve([]);
      return;
    }

    const results = new Array(n);
    let activeCount = 0; // number of tasks currently running
    let nextIndex = 0;   // index of the next task to start
    let hasError = false;

    function startTask(i) {
      if (hasError) return;
      activeCount++;
      const taskFn = taskFns[i];
      let taskPromise;
      try {
        const ret = taskFn();
        // If the function returns a promise, use it; otherwise wrap in Promise.resolve
        if (ret && typeof ret.then === 'function') {
          taskPromise = ret;
        } else {
          taskPromise = Promise.resolve(ret);
        }
      } catch (err) {
        // Synchronous throw – reject the whole pool immediately
        hasError = true;
        reject(err);
        return;
      }

      taskPromise
        .then(value => {
          results[i] = value;
          activeCount--;
          if (!hasError) {
            // Start next tasks while we have capacity and tasks left
            while (activeCount < limit && nextIndex < n) {
              startTask(nextIndex++);
            }
            // If all tasks have been started and completed, resolve the pool
            if (nextIndex >= n && activeCount === 0) {
              resolve(results);
            }
          }
        })
        .catch(err => {
          hasError = true;
          reject(err);
          // No new tasks are started after an error
        });
    }

    // Launch up to `limit` initial tasks
    while (activeCount < limit && nextIndex < n) {
      startTask(nextIndex++);
    }
  });
}

async function retry(fn, options = {}) {
  const { retries = 3, delayMs = 0, factor = 2, shouldRetry = (err) => true } = options;
  let attempt = 1;
  let lastError = null;

  while (true) {
    try {
      const result = await fn(attempt);
      return result;
    } catch (err) {
      lastError = err;
      // Stop retrying if we've exhausted the allowed attempts or shouldRetry is false
      if (attempt > retries || !shouldRetry(err)) {
        throw err;
      }
      // Wait the calculated delay before the next attempt
      const delay = delayMs * Math.pow(factor, attempt - 1);
      await new Promise(resolve => setTimeout(resolve, delay));
      attempt++;
    }
  }
}

module.exports = { promisePool, retry };
