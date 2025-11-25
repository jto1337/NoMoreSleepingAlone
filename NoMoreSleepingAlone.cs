using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace NoMoreSleepingAlone {
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            Log.Message("[No More Sleeping Alone] Loaded successfully!");

        }
    }

    public class PawnTicker : GameComponent
    {
        private int tickCounter = 0;
        private const int TicksPerUpdate = 3600; // approx. every IRL minute at 1x speed.

        // cache results to minimize api queries
        public List<Pawn> PawnsSleepingAlone_Cache = new List<Pawn>();
        public String AlertExplanation_Cache = "";

        public PawnTicker(Game game) : base()
        {
            // constructor required for the Ticker variable below.
        }

        public override void GameComponentTick()
        {
            tickCounter++;
            
            // only ping api every X ticks to limit API calls and performance impact
            if (tickCounter >= TicksPerUpdate)
            {
                tickCounter = 0;
                FindPawnsSleepingAlone();

            }
        }

        private void FindPawnsSleepingAlone()
        {
            List<Pawn> PlayerPawns = Find.CurrentMap.mapPawns.AllHumanlike.Where(n => n.Faction == Faction.OfPlayer).ToList();
            List<Pawn> PawnsSleepingAlone = new List<Pawn>();
            List<Thought> AllThoughts = new List<Thought>();

            // The name of the mood to search for.
            String NameOfMoodToLookFor = "Sleeping alone";

            foreach (var pawn in PlayerPawns)
            {
                AllThoughts.Clear();

                // Get all mood thoughts
                pawn.needs.mood.thoughts.GetAllMoodThoughts(AllThoughts);

                if (AllThoughts != null) // avoids errors
                {

                    // Find the Thought object that corresponds to the 'sleeping alone' thought
                    Thought TargetThought = AllThoughts.FirstOrDefault(n => String.Equals(n.LabelCap, NameOfMoodToLookFor, StringComparison.CurrentCultureIgnoreCase));

                    if (TargetThought != null) // avoids errors
                    {
                        if (TargetThought.MoodOffset() < 0) // make sure it's a negative mood and some other mod doesn't remove the impact.
                        {
                            // Log.Message($"Found thought {TargetThought.LabelCap} with mood offset {TargetThought.MoodOffset()}");
                            PawnsSleepingAlone.Add(pawn);
                        }
                    }

                }

            }

            if (PawnsSleepingAlone.Count == 0)
            {
                AlertExplanation_Cache = "No one is upset about sleeping alone.";
            }
            else
            {
                // use StringBuilder to format output text. it should look something like:
                    // The following pawns are upset about sleeping alone. Consider making double beds.
                    //
                    // Pawn1
                    // Pawn2
                    // Pawn3
                    // Pawn4

                var StringBuilder = new System.Text.StringBuilder();
                StringBuilder.AppendLine("The following pawns are upset about sleeping alone. Consider making double beds.");
                StringBuilder.AppendLine();

                foreach (var pawn in PawnsSleepingAlone)
                {
                    StringBuilder.AppendLine(pawn.LabelShort);
                }

                AlertExplanation_Cache = StringBuilder.ToString();
            }

            PawnsSleepingAlone_Cache.Clear();
            PawnsSleepingAlone_Cache.AddRange(PawnsSleepingAlone);
        }
    }

    public class AlertSleepingAlone : Alert // the alert that appears on the right side of the screen
    {
        public AlertSleepingAlone()
        {
            defaultLabel = "Colonists sleeping alone"; // name of the alert
            defaultPriority = AlertPriority.Medium; 
        }

        public override AlertReport GetReport() // body of the alert
        {

            var ticker = Current.Game.GetComponent<PawnTicker>(); // used to access the GameTicker class above

            // return the list of pawns if any exist, otherwise return false
            return ticker.PawnsSleepingAlone_Cache.Any() ? AlertReport.CulpritsAre(ticker.PawnsSleepingAlone_Cache) : false;

        }

        public override TaggedString GetExplanation() // hover-over tooltip text
        {
            var ticker = Current.Game.GetComponent<PawnTicker>(); // used to access the GameTicker class above
            return ticker.AlertExplanation_Cache; // return the hover-over tool tip text

        }


    }
    
}
