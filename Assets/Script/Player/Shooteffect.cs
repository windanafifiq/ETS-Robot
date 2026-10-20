using UnityEngine;

namespace RobotArena
{
    // Efek tembakan yang dibuat lewat kode (tanpa aset tambahan):
    //   MuzzleFlash = kilatan di moncong saat menembak
    //   Impact      = percikan saat peluru mengenai sesuatu
    //   AddTrail    = jejak cahaya mengikuti peluru
    public static class ShootEffect
    {
        public static readonly Color PlayerColor = new Color(1f, 0.9f, 0.35f);
        public static readonly Color EnemyColor = new Color(1f, 0.35f, 0.25f);

        public static Color ColorFor(Team team)
        {
            return team == Team.Player ? PlayerColor : EnemyColor;
        }

        public static void MuzzleFlash(Transform firePoint, Color color)
        {
            // Semburan percikan ke depan.
            SpawnBurst(firePoint.position, firePoint.rotation, color, 8, 3f, 7f, 0.15f, 0.15f, 0.35f, 25f);

            // Kilatan cahaya sangat singkat.
            GameObject lightObj = new GameObject("MuzzleLight");
            lightObj.transform.position = firePoint.position;

            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = 5f;
            l.intensity = 3f;

            Object.Destroy(lightObj, 0.05f);
        }

        // Suara 2D: volume tidak bergantung jarak kamera (kamera game ini tinggi di atas arena).
        public static void PlaySound2D(AudioClip clip, float volume)
        {
            if (clip == null) return;

            GameObject go = new GameObject("Sfx_" + clip.name);
            AudioSource source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.spatialBlend = 0f;   // 0 = 2D
            source.playOnAwake = false;
            source.Play();

            Object.Destroy(go, clip.length + 0.2f);
        }

        public static void Impact(Vector3 position, Color color)
        {
            SpawnBurst(position, Quaternion.identity, color, 12, 2f, 5f, 0.3f, 0.1f, 0.2f, 0f);
        }

        public static void AddTrail(GameObject target, Color color)
        {
            TrailRenderer trail = target.AddComponent<TrailRenderer>();
            trail.time = 0.15f;
            trail.startWidth = 0.15f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;
            trail.material = PickupEffect.GetMaterial();
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;

            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = g;
        }

        private static void SpawnBurst(Vector3 position, Quaternion rotation, Color color, int count,
            float speedMin, float speedMax, float lifetime, float sizeMin, float sizeMax, float coneAngle)
        {
            GameObject go = new GameObject("ShotEffect");
            go.transform.SetPositionAndRotation(position, rotation);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = lifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });

            ParticleSystem.ShapeModule shape = ps.shape;
            if (coneAngle > 0f)
            {
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = coneAngle;
                shape.radius = 0.02f;
            }
            else
            {
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.1f;
            }

            PickupEffect.ApplyFade(ps);

            ps.GetComponent<ParticleSystemRenderer>().material = PickupEffect.GetMaterial();
            ps.Play();

            Object.Destroy(go, 2f);
        }
    }
}