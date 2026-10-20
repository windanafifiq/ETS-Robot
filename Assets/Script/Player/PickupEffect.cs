using UnityEngine;

namespace RobotArena
{
    // Efek partikel sederhana yang dibuat lewat kode (tanpa aset tambahan).
    //   Burst  = ledakan partikel sesaat (saat pickup diambil)
    //   Attach = aura partikel yang mengikuti objek selama durasi tertentu
    public static class PickupEffect
    {
        private static Material particleMaterial;

        public static Material GetMaterial()
        {
            if (particleMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                particleMaterial = new Material(shader);
            }
            return particleMaterial;
        }

        public static void Burst(Vector3 position, Color color)
        {
            GameObject go = new GameObject("PickupBurst");
            go.transform.position = position;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.6f;
            main.loop = false;
            main.startLifetime = 0.8f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startColor = color;
            main.gravityModifier = -0.15f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)30) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            ApplyFade(ps);

            ps.GetComponent<ParticleSystemRenderer>().material = GetMaterial();
            ps.Play();

            Object.Destroy(go, 3f);
        }

        public static void Attach(Transform parent, Color color, float duration)
        {
            GameObject go = new GameObject("PickupAura");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.2f, 0f);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = 0.6f;
            main.startSpeed = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            main.startColor = color;
            main.gravityModifier = -0.3f;
            main.simulationSpace = ParticleSystemSimulationSpace.World; // meninggalkan jejak saat bergerak

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 30f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.6f;
            shape.rotation = new Vector3(-90f, 0f, 0f); // cincin mendatar di sekitar kaki

            ApplyFade(ps);

            ps.GetComponent<ParticleSystemRenderer>().material = GetMaterial();
            ps.Play();

            Object.Destroy(go, duration);
        }

        public static void ApplyFade(ParticleSystem ps)
        {
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;

            Gradient fade = new Gradient();
            fade.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(fade);

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        }
    }
}