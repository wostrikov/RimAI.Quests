using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Ustas.RimAI.Communication.Client;
using Ustas.RimAI.Communication.Data;
using Ustas.RimAI.Communication.Util;
using Ustas.RimAI.Quests.Services.Streaming;
using Verse;

namespace Ustas.RimAI.Quests.Services
{
    /// <summary>
    /// Service for generating AI-powered quest descriptions.
    /// This service integrates with RimTalk's AI functionality to create
    /// dynamic, context-aware quest narratives.
    /// </summary>
    public static class QuestDescriptionGenerator
    {
        private static readonly HashSet<int> _processingQuests = new HashSet<int>();

        public static int ProcessingCount => _processingQuests.Count;

        public static void ClearCache()
        {
            _processingQuests.Clear();
        }

        /// <summary>
        /// Generates an AI-powered description for a quest asynchronously
        /// </summary>
        public static async void GenerateQuestDescriptionAsync(Quest quest)
        {
            try
            {
                if (quest == null)
                    return;

                int questId = quest.id;

                // Skip if already processing
                if (_processingQuests.Contains(questId))
                    return;

                _processingQuests.Add(questId);

                if (Prefs.DevMode)
                {
                    Log.Message(
                        $"[RimAI.Quests] Generating AI description for quest: {quest.name}"
                    );
                }

                // Build the prompt
                string prompt = BuildQuestPrompt(quest);
                string instruction = BuildSystemInstruction();

                if (Prefs.DevMode)
                {
                    var config = Ustas.RimAI.Communication.Settings.Get().GetActiveConfig();
                    var model = config?.SelectedModel ?? "Unknown";
                    Log.Message($"[RimAI.Quests] Using model: {model}");
                    Log.Message($"[RimAI.Quests] Instruction:\n{instruction}");
                    Log.Message($"[RimAI.Quests] Prompt:\n{prompt}");
                }

                // Store original description
                var originalDescription = quest.description.ToString();

                // Call RimTalk's AI service with streaming
                var result = await CallRimTalkAI(instruction, prompt, quest);

                if (Prefs.DevMode && result != null)
                {
                    Log.Message($"[RimAI.Quests] AI Response (processed):\n{result}");
                }

                // Streaming already updated the description in real-time
                // The result is just for logging/verification
                if (result != null)
                {
                    if (Prefs.DevMode)
                        Log.Message($"[RimAI.Quests] Successfully enhanced quest: {quest.name}");
                }
                else
                {
                    // Restore original description on failure
                    quest.description = new TaggedString(originalDescription);

                    if (Prefs.DevMode)
                    {
                        Log.Warning(
                            $"[RimAI.Quests] Failed to generate enhancement for quest: {quest.name}"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI.Quests] Error generating quest description: {ex}");
            }
            finally
            {
                _processingQuests.Remove(quest.id);
            }
        }

        /// <summary>
        /// Builds the system instruction for the AI based on RimTalk's configuration
        /// </summary>
        private static string BuildSystemInstruction()
        {
            var settings = Ustas.RimAI.Communication.Settings.Get();

            // Get base instruction from RimTalk (respects user customization)
            var baseInstruction = string.IsNullOrWhiteSpace(settings.CustomInstruction)
                ? Ustas.RimAI.Communication.Data.Constant.DefaultInstruction
                : settings.CustomInstruction;

            // Quest-specific instruction (use custom or default)
            string questInstruction = string.IsNullOrWhiteSpace(
                RimTalkQuestsMod.Settings.customQuestInstruction
            )
                ? Ustas.RimAI.Quests.Constant.GetDefaultQuestInstruction()
                : RimTalkQuestsMod.Settings.customQuestInstruction;

            return baseInstruction + "\n\n" + questInstruction;
        }

        /// <summary>
        /// Builds the prompt for AI quest description generation
        /// </summary>
        private static string BuildQuestPrompt(Quest quest)
        {
            var sb = new StringBuilder();

            // Quest basic info
            sb.AppendLine($"Quest Title: {quest.name}");
            sb.AppendLine($"Quest Description: {quest.description}");
            sb.AppendLine();

            // Quest metadata
            if (quest.root != null)
            {
                sb.AppendLine($"Type: {quest.root.defName}");
            }

            if (quest.challengeRating > 0)
            {
                sb.AppendLine($"Challenge: {quest.challengeRating}");
            }

            // Quest rewards
            AppendQuestRewards(sb, quest);

            // Scene information (reusing Ustas.RimAI.Communication's mechanism)
            AppendSceneContext(sb);

            // Faction history context
            AppendFactionContext(sb, quest);

            return sb.ToString();
        }

        /// <summary>
        /// Appends quest rewards information
        /// </summary>
        private static void AppendQuestRewards(StringBuilder sb, Quest quest)
        {
            var choiceParts = quest.PartsListForReading.OfType<QuestPart_Choice>().ToList();

            if (choiceParts.Any())
            {
                sb.AppendLine();
                sb.AppendLine("--- Quest Rewards ---");

                foreach (var choicePart in choiceParts)
                {
                    if (choicePart.choices != null && choicePart.choices.Count > 0)
                    {
                        sb.AppendLine($"Choose one of {choicePart.choices.Count} options:");

                        for (int i = 0; i < choicePart.choices.Count; i++)
                        {
                            var choice = choicePart.choices[i];
                            sb.AppendLine($"  Option {i + 1}:");

                            if (choice.rewards != null && choice.rewards.Any())
                            {
                                foreach (var reward in choice.rewards)
                                {
                                    try
                                    {
                                        var rewardDesc = reward.GetDescription(default);
                                        if (!string.IsNullOrEmpty(rewardDesc))
                                        {
                                            sb.Append($"    - {rewardDesc}");

                                            // Add item description if it's a thing reward
                                            if (
                                                reward is Reward_Items itemReward
                                                && itemReward.items != null
                                            )
                                            {
                                                foreach (var thing in itemReward.items)
                                                {
                                                    if (thing?.def != null)
                                                    {
                                                        var itemDesc = thing.def.description;
                                                        if (!string.IsNullOrEmpty(itemDesc))
                                                        {
                                                            sb.Append($" ({itemDesc})");
                                                            break; // Only show description for first item type
                                                        }
                                                    }
                                                }
                                            }

                                            sb.AppendLine();
                                        }
                                    }
                                    // RimAI.catch-boundary: ALLOWED_TOP_LEVEL_BOUNDARY — broken reward defs must not abort quest prompt
                                    catch (Exception ex)
                                    {
                                        Log.WarningOnce("[RimAI.Quests] Reward description failed: " + ex, reward.GetHashCode());
                                        sb.AppendLine(
                                            $"    - {reward.GetType().Name} (description unavailable)"
                                        );
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Appends scene context (time, weather, location, wealth) like RimTalk
        /// </summary>
        private static void AppendSceneContext(StringBuilder sb)
        {
            var gameData = CommonUtil.GetInGameData();
            var currentMap = Find.CurrentMap;

            if (currentMap == null)
                return;

            sb.AppendLine();
            sb.AppendLine("--- Current Scene ---");

            // Time and date
            sb.AppendLine($"Time: {gameData.Hour12HString}");
            sb.AppendLine($"Date: {gameData.DateString}");
            sb.AppendLine($"Season: {gameData.SeasonString}");
            sb.AppendLine($"Weather: {gameData.WeatherString}");

            // Colony info
            var colonists = currentMap.mapPawns.FreeColonistsSpawnedCount;
            sb.AppendLine($"Colony: {colonists} colonists");

            var wealth = currentMap.wealthWatcher.WealthTotal;
            sb.AppendLine($"Wealth: {Describer.Wealth(wealth)}");

            // Location
            if (currentMap.Parent != null)
            {
                sb.AppendLine($"Location: {currentMap.Parent.Label}");
            }
        }

        /// <summary>
        /// Appends faction history and relationship context
        /// </summary>
        private static void AppendFactionContext(StringBuilder sb, Quest quest)
        {
            // Get involved factions
            var factions = quest.InvolvedFactions?.ToList();
            if (factions == null || factions.Count == 0)
                return;

            var mainFaction = factions.FirstOrDefault();
            if (mainFaction == null)
                return;

            sb.AppendLine();
            sb.AppendLine("--- Faction Context ---");
            sb.AppendLine($"From: {mainFaction.Name}");

            // Faction relationship
            var playerFaction = Faction.OfPlayer;
            if (playerFaction != null)
            {
                var relation = mainFaction.RelationKindWith(playerFaction);
                var goodwill = mainFaction.GoodwillWith(playerFaction);
                sb.AppendLine($"Relationship: {relation} (goodwill: {goodwill})");
            }

            // Historical quests from this faction
            AppendFactionQuestHistory(sb, mainFaction);
        }

        /// <summary>
        /// Appends recent quest history with this faction
        /// </summary>
        private static void AppendFactionQuestHistory(StringBuilder sb, Faction faction)
        {
            var questManager = Find.QuestManager;
            if (questManager == null)
                return;

            // Get recent completed/failed quests from this faction
            var recentQuests = questManager.QuestsListForReading
                .Where(
                    q =>
                        q.Historical
                        && q.InvolvedFactions.Contains(faction)
                        && q.TicksSinceCleanup < GenDate.TicksPerQuadrum
                ) // Last quadrum
                .OrderByDescending(q => q.cleanupTick)
                .Take(3)
                .ToList();

            if (recentQuests.Any())
            {
                sb.AppendLine($"Recent history with {faction.Name}:");
                foreach (var q in recentQuests)
                {
                    var outcome = q.State switch
                    {
                        QuestState.EndedSuccess => "succeeded",
                        QuestState.EndedFailed => "failed",
                        _ => "ended"
                    };
                    sb.AppendLine($"  - {q.name} ({outcome})");
                }
            }
        }

        /// <summary>
        /// Calls RimTalk's AI service to generate quest enhancement using streaming
        /// </summary>
        private static async Task<string> CallRimTalkAI(
            string instruction,
            string prompt,
            Quest quest
        )
        {
            // Get AI client from RimTalk
            var client = await AIClientFactory.GetAIClientAsync();
            if (client == null)
            {
                Log.Warning(
                    "[RimAI.Quests] Failed to get AI client - check RimTalk configuration"
                );
                return null;
            }

            // Build message list
            var messages = new List<(Role, string)> { (Role.User, prompt) };

            var postProcessor = new ThinkReasoningPostProcessor();
            var originalDescription = quest.description.ToString();
            bool cleanDuringStreaming = RimTalkQuestsMod.Settings.cleanThinkTagsDuringStreaming;

            var streamingClient = StreamingClientFactory.Create(client);

            if (RimTalkQuestsMod.Settings.verboseDebugLogging && Prefs.DevMode)
            {
                Log.Message("[RimAI.Quests] Starting plain text streaming API call...");
                Log.Message(
                    $"[RimAI.Quests] Post-process mode: {(cleanDuringStreaming ? "real-time" : "final-only")}"
                );
            }

            int chunkCount = 0;
            var payload = await streamingClient.StreamFromSettingsAsync(
                instruction,
                messages,
                chunk =>
                {
                    chunkCount++;

                    if (RimTalkQuestsMod.Settings.verboseDebugLogging && Prefs.DevMode)
                    {
                        Log.Message(
                            $"[RimAI.Quests] Chunk #{chunkCount} received: [{chunk?.Length ?? 0} chars] '{chunk}'"
                        );
                    }

                    if (!string.IsNullOrEmpty(chunk))
                    {
                        postProcessor.AppendChunk(chunk);

                        var displayContent = cleanDuringStreaming
                            ? postProcessor.GetProcessedText()
                            : postProcessor.GetRawText();

                        // Update quest description in real-time
                        var enhancedDescription =
                            originalDescription + "\n\n───────────\n\n" + displayContent;
                        quest.description = new TaggedString(enhancedDescription);

                        if (RimTalkQuestsMod.Settings.verboseDebugLogging && Prefs.DevMode)
                        {
                            Log.Message(
                                $"[RimAI.Quests] Updated quest.description (display chars: {displayContent.Length}, raw chars: {postProcessor.GetRawText().Length})"
                            );
                        }
                    }
                }
            );

            if (RimTalkQuestsMod.Settings.verboseDebugLogging && Prefs.DevMode)
            {
                Log.Message(
                    $"[RimAI.Quests] Streaming completed. Total chunks: {chunkCount}, Final raw length: {postProcessor.GetRawText().Length}"
                );
            }

            var finalRawText = payload?.Response;
            if (string.IsNullOrEmpty(finalRawText))
            {
                finalRawText = postProcessor.GetRawText();
            }

            if (string.IsNullOrEmpty(finalRawText))
            {
                return null;
            }

            if (Prefs.DevMode)
            {
                Log.Message($"[RimAI.Quests] AI Response (raw):\n{finalRawText}");
            }

            var finalProcessedText = postProcessor.ProcessFinal(finalRawText);
            var finalDescription = originalDescription + "\n\n───────────\n\n" + finalProcessedText;
            quest.description = new TaggedString(finalDescription);

            return finalProcessedText;
        }

        /// <summary>
        /// Simple JSON value extraction
        /// </summary>
        private static string ExtractJsonValue(string json, string key)
        {
            try
            {
                string searchKey = $"\"{key}\"";
                int keyIndex = json.IndexOf(searchKey);
                if (keyIndex == -1)
                    return null;

                int colonIndex = json.IndexOf(':', keyIndex);
                if (colonIndex == -1)
                    return null;

                int startQuote = json.IndexOf('"', colonIndex);
                if (startQuote == -1)
                    return null;

                int endQuote = startQuote + 1;
                while (endQuote < json.Length)
                {
                    if (json[endQuote] == '"' && json[endQuote - 1] != '\\')
                        break;
                    endQuote++;
                }

                if (endQuote >= json.Length)
                    return null;

                return json.Substring(startQuote + 1, endQuote - startQuote - 1)
                    .Replace("\\n", "\n")
                    .Replace("\\\"", "\"");
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if RimTalk's AI service is available and configured
        /// </summary>
        public static bool IsAIServiceAvailable()
        {
            if (!ModsConfig.IsActive("ustas.rimai.communication"))
                return false;

            // Check if RimTalk has an active configuration
            var settings = Ustas.RimAI.Communication.Settings.Get();
            var activeConfig = settings?.GetActiveConfig();

            return activeConfig != null;
        }
    }
}
