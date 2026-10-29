using NUnit.Framework;

namespace BaseGame.Playground.Stats.Tests
{
    public class StatsSimulatorTests
    {
        [Test]
        public void SimulateStepsOrSkip_SickFor45Minutes_BothSpend105Hunger()
        {
            PlaygroundEntity stepped = CreateSickEntity();
            PlaygroundEntity skipped = CreateSickEntity();

            for (int i = 0; i < 6; i++)
            {
                stepped.Simulator.Simulate(10);
            }

            skipped.Simulator.Simulate(60);

            Assert.That(HungerSpent(stepped), Is.EqualTo(105f).Within(1e-3f));
            Assert.That(HungerSpent(skipped), Is.EqualTo(105f).Within(1e-3f));
        }

        [Test]
        public void SimulateSkip_StarvesMidway_SickStartsAtThreshold()
        {
            var entity = PlaygroundEntity.Create(PlaygroundCatalog.CreateStatSettings(), PlaygroundCatalog.CreateEffects());
            entity.Stats.GetResource(PlaygroundCatalog.Hunger).Set(40f);

            entity.Simulator.Simulate(80);

            // Hunger ran out at 40; Sick (45) then ran 40 of its minutes.
            Assert.That(entity.Effects.TryGet(PlaygroundCatalog.Sick, out EffectInstance sick), Is.True);
            Assert.That(sick.Remaining, Is.EqualTo(5d).Within(1e-6));
        }

        private static PlaygroundEntity CreateSickEntity()
        {
            var entity = PlaygroundEntity.Create(PlaygroundCatalog.CreateStatSettings(), PlaygroundCatalog.CreateEffects());
            entity.Stats.GetStat(PlaygroundCatalog.MaxHunger).SetBase(500f);
            Resource hunger = entity.Stats.GetResource(PlaygroundCatalog.Hunger);
            hunger.Set(hunger.Max);
            entity.Effects.Apply(entity.ResolveEffect(PlaygroundCatalog.Sick));
            return entity;
        }

        private static float HungerSpent(PlaygroundEntity entity) =>
            500f - entity.Stats.GetResource(PlaygroundCatalog.Hunger).Current;
    }
}
