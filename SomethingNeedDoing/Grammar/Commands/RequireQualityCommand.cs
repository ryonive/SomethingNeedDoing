﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.Grammar.Modifiers;
using SomethingNeedDoing.Managers;

namespace SomethingNeedDoing.Grammar.Commands;

/// <summary>
/// The /requirequality command.
/// </summary>
internal class RequireQualityCommand : MacroCommand
{
    private static readonly Regex Regex = new(@"^/requirequality\s+(?<quality>\d+)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly uint requiredQuality;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequireQualityCommand"/> class.
    /// </summary>
    /// <param name="text">Original text.</param>
    /// <param name="quality">Quality value.</param>
    /// <param name="wait">Wait value.</param>
    private RequireQualityCommand(string text, uint quality, WaitModifier wait)
        : base(text, wait)
    {
        this.requiredQuality = quality;
    }

    /// <summary>
    /// Parse the text as a command.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <returns>A parsed command.</returns>
    public static RequireQualityCommand Parse(string text)
    {
        _ = WaitModifier.TryParse(ref text, out var waitModifier);

        var match = Regex.Match(text);
        if (!match.Success)
            throw new MacroSyntaxError(text);

        var qualityValue = match.Groups["quality"].Value;
        var quality = uint.Parse(qualityValue, CultureInfo.InvariantCulture);

        return new RequireQualityCommand(text, quality, waitModifier);
    }

    /// <inheritdoc/>
    public async override Task Execute(CancellationToken token)
    {
        PluginLog.Debug($"Executing: {this.Text}");

        if (!this.IsQualityGood())
            throw new MacroPause("Required quality was not found", UiColor.Red);

        await this.PerformWait(token);
    }

    private unsafe bool IsQualityGood()
    {
        var addon = Service.GameGui.GetAddonByName("RecipeNote", 1);
        if (addon == IntPtr.Zero)
        {
            PluginLog.Error("AddonRecipeNote was null");
            return false;
        }

        var addonPtr = (AddonRecipeNote*)addon;
        if (addonPtr->Quality == null)
        {
            PluginLog.Error("Quality node was null");
            return false;
        }

        var qualityStr = addonPtr->Quality->NodeText.ToString();
        if (!int.TryParse(qualityStr, out var quality))
        {
            PluginLog.Error($"Could not parse quality node: '{qualityStr}'");
            return false;
        }

        return quality >= this.requiredQuality;
    }
}
