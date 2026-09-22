#!/usr/bin/env node

const sourcePath = process.argv.at(-1);
process.stderr.write(`${sourcePath}:3:8: error: simulated compiler failure\n`);
process.exitCode = 1;
