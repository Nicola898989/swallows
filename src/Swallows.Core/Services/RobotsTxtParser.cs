using System.Text.RegularExpressions;

namespace Swallows.Core.Services;

public class RobotsTxtParser
{
    private readonly HttpClient _http;
    
    // UserAgent -> (DisallowPaths, AllowPaths)
    private Dictionary<string, (List<string> Disallows, List<string> Allows)> _rules = new();
    
    // Store content just in case or for debug
    public string RawContent { get; private set; } = "";

    public RobotsTxtParser(HttpClient http)
    {
        _http = http;
    }

    public async Task ParseRobotsTxtAsync(string baseUrl, string userAgent)
    {
        // We ignore the userAgent argument here for fetching, 
        // but maybe the test implies we only parse relevant rules for this UA?
        // But IsPathAllowed takes UA again. 
        // Let's fetch and parse everything.
        
        try 
        {
            var robotsUrl = baseUrl.TrimEnd('/') + "/robots.txt";
            var response = await _http.GetAsync(robotsUrl);
            if (response.IsSuccessStatusCode)
            {
                RawContent = await response.Content.ReadAsStringAsync();
                Parse(RawContent);
            }
        }
        catch 
        {
            // Ignore errors, assume allowed
        }
    }

    private void Parse(string content)
    {
        _rules.Clear();
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        var currentUserAgents = new List<string>();
        
        foreach (var line in lines)
        {
            var cleanLine = line.Split('#')[0].Trim();
            if (string.IsNullOrEmpty(cleanLine)) continue;
            
            var parts = cleanLine.Split(new[] { ':' }, 2);
            if (parts.Length != 2) continue;
            
            var field = parts[0].Trim().ToLowerInvariant();
            var value = parts[1].Trim();
            
            if (field == "user-agent")
            {
                // If we encounter a new User-agent block, are previous UAs finished?
                // Standard says: successive U-A lines start a record.
                // But once a non-U-A line appears, the record logic starts.
                // So: if we have rules, we clear current user agents?
                // Actually, simplistic parsing:
                // Check if the previous line was NOT a user-agent? 
                // Let's assume standard grouping: User-agent lines, then rule lines.
                // If we see User-agent again after seeing rules, it's a new block.
                
                // Keep it simple: if we are handling rules and see "User-agent", we reset current agents.
                // But we need to know if we were processing rules.
                
                // Let's track "processingRules" state?
                // If we are adding rules to current agents, and hit User-agent, new block.
                
                // For this simple implementation:
                // Just add to `currentUserAgents`. If we encounter rules, add to all `currentUserAgents`.
                // Implicitly, if we see User-agent and we HAVE processed rules recently, we clear `currentUserAgents`.
                
                 // Hacky state machine:
                 // 1. Accumulate UAs.
                 // 2. Accumulate Rules for UAs.
                 // 3. If User-agent seen and we have accumulated rules, clear UAs.
                 
                 // Wait, this is state dependent.
                 // Easier: 
                 if (currentUserAgents.Count > 0 && _rules.ContainsKey(currentUserAgents[0]) && (_rules[currentUserAgents[0]].Disallows.Count > 0 || _rules[currentUserAgents[0]].Allows.Count > 0))
                 {
                     // New block strictly? 
                     // Usually yes.
                     // But multiple UA lines can be contiguous:
                     // User-agent: A
                     // User-agent: B
                     // Disallow: /
                     
                     // If last line was NOT User-agent, then clear.
                     // I need to track previous line type?
                 }
                 
                 // Let's just blindly add. But if we see User-agent, do we clear previous list if it was used?
                 // Let's clear if the *previous* command was a rule.
             }
        }
        
        // Let's try a better loop
        currentUserAgents.Clear();
        bool lastWasRule = false;
        
        foreach (var line in lines)
        {
             var cleanLine = line.Split('#')[0].Trim();
            if (string.IsNullOrEmpty(cleanLine)) continue;
            
            var parts = cleanLine.Split(new[] { ':' }, 2);
            if (parts.Length != 2) continue;
            
            var field = parts[0].Trim().ToLowerInvariant();
            var value = parts[1].Trim();
            
            if (field == "user-agent")
            {
                if (lastWasRule)
                {
                    currentUserAgents.Clear();
                }
                currentUserAgents.Add(value);
                
                // Ensure dict exists
                if (!_rules.ContainsKey(value))
                {
                    _rules[value] = (new List<string>(), new List<string>());
                }
                
                lastWasRule = false;
            }
            else if (field == "disallow")
            {
                foreach(var ua in currentUserAgents)
                {
                    _rules[ua].Disallows.Add(value);
                }
                lastWasRule = true;
            }
            else if (field == "allow")
            {
                foreach(var ua in currentUserAgents)
                {
                    _rules[ua].Allows.Add(value);
                }
                lastWasRule = true;
            }
        }
    }

    public bool IsPathAllowed(string path, string userAgent)
    {
        // Logic:
        // 1. Check specific UA rules.
        // 2. Check wildcard "*" rules.
        // 3. Return true if allowed.
        
        var uasToCheck = new[] { userAgent, "*" };
        
        foreach(var ua in uasToCheck)
        {
            if (_rules.TryGetValue(ua, out var rules))
            {
                 // Check allow first? Or longest match?
                 // Standard: Longest match wins.
                 
                 var matches = new List<(bool Allowed, int Length)>();
                 
                 foreach(var dis in rules.Disallows)
                 {
                     if (path.StartsWith(dis, StringComparison.OrdinalIgnoreCase))
                     {
                         matches.Add((false, dis.Length));
                     }
                 }
                 
                 foreach(var al in rules.Allows)
                 {
                     if (path.StartsWith(al, StringComparison.OrdinalIgnoreCase))
                     {
                         matches.Add((true, al.Length));
                     }
                 }
                 
                 if (matches.Any())
                 {
                     // Return result of longest match
                     var best = matches.OrderByDescending(m => m.Length).First();
                     
                     // If we found a match for specific UA, we stop?
                     // Verify behavior. Standard says specific UA record overrides wildcard record completely.
                     // If we are checking "Googlebot" and we found "Googlebot" block, we use it and IGNORE *.
                     // My loop checks both.
                     // Fix: If `ua` (first iteration) is found in _rules, we use it and return result.
                     // unless `ua` was `*`.
                     
                     if (ua != "*" || uasToCheck.Length == 1)
                     {
                         return best.Allowed;
                     }
                     // If we are here, we are in check 1 (Specific UA).
                     // We found rules for it. We MUST apply them and return.
                     return best.Allowed;
                 }
                 else
                 {
                     // UA entry exists but no matching paths? 
                     // It means "Allowed".
                     // If we found "User-agent: Googlebot" but no Disallow lines matched the path?
                     // Then it's allowed.
                     // And we should NOT check "*".
                     if (ua != "*") return true; 
                 }
            }
        }
        
        // Default allow
        return true;
    }
}
