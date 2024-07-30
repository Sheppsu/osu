// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to correctly aim at every object in the map with a uniform CircleSize and normalized distances.
    /// </summary>
    public class Aim : OsuStrainSkill
    {
        protected override double ConsistencyMean => withSliders ? 0.3351283539 : 0.31709432;
        protected override double ConsistencyStdev => withSliders ? 0.07727181531 : 0.06992028354;

        public Aim(Mod[] mods, double clockRate, bool withSliders)
            : base(mods, clockRate)
        {
            this.withSliders = withSliders;
        }

        private readonly bool withSliders;

        private double currentStrain;

        private double skillMultiplier => 23.55;
        private double strainDecayBase => 0.15;

        private double strainDecay(double ms) => Math.Pow(strainDecayBase, ms / 1000);

        protected override double CalculateInitialStrain(double time, DifficultyHitObject current) => currentStrain * strainDecay(time - current.Previous(0).StartTime);

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            currentStrain *= strainDecay(current.DeltaTime);
            double difficultyValue = AimEvaluator.EvaluateDifficultyOf(current, withSliders) * skillMultiplier;
            currentStrain += difficultyValue;

            ProcessConsistency(difficultyValue);

            return currentStrain;
        }
    }
}
