import fs from 'node:fs';
import path from 'node:path';
import { dataStorePath } from './playwright.config.js';

/**
 * Starts every run from an empty database. The store is a single JSON document, so
 * deleting the file is the whole reset — JsonFileDataStore recreates it on first write.
 *
 * This runs before Playwright starts the web server, which matters: the store is loaded
 * once into a singleton at startup, so deleting the file afterwards would change nothing.
 */
export default function globalSetup() {
  fs.mkdirSync(path.dirname(dataStorePath), { recursive: true });
  fs.rmSync(dataStorePath, { force: true });
  fs.rmSync(`${dataStorePath}.tmp`, { force: true });
}
