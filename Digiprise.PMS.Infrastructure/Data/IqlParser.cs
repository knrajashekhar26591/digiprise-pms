using System;
using System.Linq;
using System.Text.RegularExpressions;
using Digiprise.PMS.Domain.Entities;
using Digiprise.PMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Digiprise.PMS.Infrastructure.Data;

/// <summary>
/// A lightweight parser to convert string-based Internal Query Language (IQL) 
/// into optimized Entity Framework Core queries.
/// </summary>
public static class IqlParser
{
    // Matches field, operator, and value. Example: project = DIG
    private static readonly Regex ClauseRegex = new Regex(
        @"(?<field>\w+)\s+(?<op>=|!=|IN|~)\s+(?<val>currentUser\(\)|\([^)]+\)|""[^""]+""|'[^']+'|\S+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IQueryable<Issue> ApplyIql(this IQueryable<Issue> query, string? iql, int currentUserId)
    {
        if (string.IsNullOrWhiteSpace(iql))
            return query;

        // V1 Linear Parser: Split clauses by AND
        var clauses = iql.Split(new[] { " AND ", " and " }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var clause in clauses)
        {
            var match = ClauseRegex.Match(clause.Trim());
            if (!match.Success) continue;

            var field = match.Groups["field"].Value.ToLowerInvariant();
            var op = match.Groups["op"].Value.ToUpperInvariant();
            var rawVal = match.Groups["val"].Value;

            var val = CleanValue(rawVal);
            query = ApplyClause(query, field, op, val, rawVal, currentUserId);
        }

        return query;
    }

    private static string CleanValue(string val)
    {
        if (val.StartsWith("(") && val.EndsWith(")")) val = val.Substring(1, val.Length - 2);
        if (val.StartsWith("\"") && val.EndsWith("\"")) val = val.Substring(1, val.Length - 2);
        if (val.StartsWith("'") && val.EndsWith("'")) val = val.Substring(1, val.Length - 2);
        return val.Trim();
    }

    private static IQueryable<Issue> ApplyClause(IQueryable<Issue> query, string field, string op, string val, string rawVal, int currentUserId)
    {
        // Handle dynamic currentUser() binding
        if (rawVal.Equals("currentUser()", StringComparison.OrdinalIgnoreCase))
        {
            val = currentUserId.ToString();
        }

        var values = val.Split(new[] { ',', '"', '\'' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(v => v.Trim()).ToList();

        switch (field)
        {
            case "project":
                if (op == "=") query = query.Where(i => i.Project!.Key == val || i.IssueKey.StartsWith(val + "-"));
                else if (op == "IN") query = query.Where(i => values.Contains(i.Project!.Key));
                break;
            case "status":
                if (int.TryParse(val, out var statusId))
                {
                    if (op == "=") query = query.Where(i => i.StatusId == statusId);
                    else if (op == "!=") query = query.Where(i => i.StatusId != statusId);
                }
                else if (op == "IN")
                {
                    var statusIds = values.Select(v => int.TryParse(v, out var id) ? id : 0).Where(id => id > 0).ToList();
                    query = query.Where(i => statusIds.Contains(i.StatusId));
                }
                break;
            case "assignee":
                if (int.TryParse(val, out var assigneeId))
                {
                    if (op == "=") query = query.Where(i => i.AssigneeId == assigneeId);
                    else if (op == "!=") query = query.Where(i => i.AssigneeId != assigneeId);
                }
                break;
            case "reporter":
                if (int.TryParse(val, out var reporterId))
                {
                    if (op == "=") query = query.Where(i => i.ReporterId == reporterId);
                }
                break;
            case "issuetype":
                if (Enum.TryParse<IssueType>(val, true, out var type))
                {
                    if (op == "=") query = query.Where(i => i.IssueType == type);
                    else if (op == "!=") query = query.Where(i => i.IssueType != type);
                }
                break;
            case "priority":
                if (Enum.TryParse<Priority>(val, true, out var prio))
                {
                    if (op == "=") query = query.Where(i => i.Priority == prio);
                    else if (op == "!=") query = query.Where(i => i.Priority != prio);
                }
                else if (op == "IN")
                {
                    var prios = values.Select(v => Enum.TryParse<Priority>(v, true, out var p) ? p : (Priority?)null)
                                      .Where(p => p.HasValue).Select(p => p!.Value).ToList();
                    query = query.Where(i => prios.Contains(i.Priority));
                }
                break;
            case "sprint":
                if (int.TryParse(val, out var sprintId))
                {
                    if (op == "=") query = query.Where(i => i.SprintId == sprintId);
                }
                break;
            case "summary":
            case "text":
                if (op == "~") query = query.Where(i => EF.Functions.ILike(i.Summary, $"%{val}%"));
                break;
        }

        return query;
    }
}
