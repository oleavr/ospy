const CONDITION_PROCESS_NAME = 1;
const CONDITION_FUNCTION_NAME = 2;
const CONDITION_RETURN_ADDRESS = 4;
const CONDITION_LOCAL_ADDRESS = 8;
const CONDITION_LOCAL_PORT = 16;
const CONDITION_REMOTE_ADDRESS = 32;
const CONDITION_REMOTE_PORT = 64;

export interface SoftwallRule {
  conditions: number;
  processName: string;
  functionName: string;
  returnAddress: number;
  localAddress: number;
  localPort: number;
  remoteAddress: number;
  remotePort: number;
  retval: number;
  lastError: number;
}

export interface SoftwallQuery {
  functionName: string;
  returnAddress: number;
  local: Endpoint | null;
  remote: Endpoint | null;
}

export interface Endpoint {
  address: number;
  port: number;
}

export interface SoftwallVerdict {
  retval: number;
  lastError: number;
}

let rules: SoftwallRule[] = [];
const processName = Process.mainModule.name.toLowerCase();

export function configureSoftwall(newRules: SoftwallRule[]): void {
  rules = newRules;
}

export function softwallDecision(query: SoftwallQuery): SoftwallVerdict | null {
  for (const rule of rules) {
    if (ruleMatches(rule, query))
      return { retval: rule.retval, lastError: rule.lastError };
  }
  return null;
}

function ruleMatches(rule: SoftwallRule, query: SoftwallQuery): boolean {
  if ((rule.conditions & CONDITION_PROCESS_NAME) !== 0 && rule.processName.toLowerCase() !== processName)
    return false;

  if ((rule.conditions & CONDITION_FUNCTION_NAME) !== 0 && rule.functionName !== query.functionName)
    return false;

  if ((rule.conditions & CONDITION_RETURN_ADDRESS) !== 0 && rule.returnAddress !== query.returnAddress)
    return false;

  if ((rule.conditions & CONDITION_LOCAL_ADDRESS) !== 0 && (query.local === null || rule.localAddress !== query.local.address))
    return false;

  if ((rule.conditions & CONDITION_LOCAL_PORT) !== 0 && (query.local === null || rule.localPort !== query.local.port))
    return false;

  if ((rule.conditions & CONDITION_REMOTE_ADDRESS) !== 0 && (query.remote === null || rule.remoteAddress !== query.remote.address))
    return false;

  if ((rule.conditions & CONDITION_REMOTE_PORT) !== 0 && (query.remote === null || rule.remotePort !== query.remote.port))
    return false;

  return true;
}
