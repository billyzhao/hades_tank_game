using System;
using System.Collections.Generic;
using System.Linq;

namespace Game1;

/// <summary>根据本局已选内容识别当前核心下最成型的路线；并列时保持目录顺序。</summary>
public sealed class BuildRouteAnalyzer
{
    private readonly BuildRouteCatalog _routes;

    public BuildRouteAnalyzer(BuildRouteCatalog routes)
    {
        _routes = routes ?? throw new ArgumentNullException(nameof(routes));
    }

    public BuildRouteAnalysis Analyze(
        CoreId coreId,
        IEnumerable<string> selectedProtocolIds,
        IEnumerable<string> selectedAuxiliaryIds,
        ContentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(selectedProtocolIds);
        ArgumentNullException.ThrowIfNull(selectedAuxiliaryIds);
        ArgumentNullException.ThrowIfNull(catalog);

        Dictionary<string, int> scores = _routes.GetRoutes(coreId)
            .ToDictionary(route => route.Tag, _ => 0, StringComparer.Ordinal);
        foreach (string protocolId in selectedProtocolIds)
        {
            foreach (string tag in catalog.GetProtocol(protocolId).Tags)
            {
                if (scores.ContainsKey(tag)) scores[tag]++;
            }
        }
        foreach (string auxiliaryId in selectedAuxiliaryIds)
        {
            foreach (string tag in catalog.GetAuxiliary(auxiliaryId).BuildTags)
            {
                if (scores.ContainsKey(tag)) scores[tag]++;
            }
        }

        BuildRouteDefinition best = _routes.GetRoutes(coreId)
            .OrderByDescending(route => scores[route.Tag])
            .First();
        int score = scores[best.Tag];
        return new BuildRouteAnalysis(score > 0 ? best : null, score);
    }
}

public sealed record BuildRouteAnalysis(BuildRouteDefinition Route, int Score)
{
    public bool IsFormed => Route is not null && Score > 0;
}
